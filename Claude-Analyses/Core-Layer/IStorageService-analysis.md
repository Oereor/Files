# `IStorageService` — Interface Dissection

> **Namespace:** `Files.Core.Storage` | **Project:** `Files.Core.Storage`
> **File:** `src/Files.Core.Storage/IStorageService.cs`
> **Dependency chain:** Core contract → OwlCore.Storage (NuGet) → App-layer implementations

---

## 1. Interface Definition (Verbatim)

```csharp
namespace Files.Core.Storage
{
    public interface IStorageService
    {
        Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default);
        Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default);
    }
}
```

At face value this is a **two-method factory pattern** — given a string key (`id`), produce an `IFile` or `IFolder`. But its architectural weight is far larger than two methods suggest: it is the **root of the application's entire filesystem abstraction tree**.

---

## 2. Role in the Architecture

`IStorageService` sits at the **seam between the app and the filesystem**. It is the single point of entry through which every file and folder reference is obtained. The app does not `new FileInfo(...)` or call `StorageFile.GetFileFromPathAsync(...)` directly — it asks an `IStorageService` for a storable by its `id` (typically a path string).

```
                     ┌────────────────────────────┐
                     │      Files.App (WinUI)      │
                     │  ViewModels / Services /    │
                     │  Actions / Widgets          │
                     └─────────────┬──────────────┘
                                   │  consumes
                                   ▼
                     ┌────────────────────────────┐
                     │      IStorageService        │  ◄── Core contract
                     │  (Files.Core.Storage)       │
                     └─────────────┬──────────────┘
                                   │  implemented by
              ┌────────────────────┼──────────────────────┐
              │                    │                       │
              ▼                    ▼                       ▼
   ┌──────────────────┐  ┌─────────────────┐  ┌──────────────────────┐
   │ NativeStorage    │  │ FtpStorage      │  │ (WindowsStorable —   │
   │ LegacyService    │  │ Service         │  │  modern path, WIP)   │
   │ [Obsolete]       │  │                 │  │                      │
   └──────────────────┘  └─────────────────┘  └──────────────────────┘
```

---

## 3. The Two Methods — Deeper Semantics

### 3.1 `Task<IFile> GetFileAsync(string id, CancellationToken)`

| Aspect | Detail |
|---|---|
| **Input** | `id` — a string that uniquely identifies a file. For local filesystems this is an absolute path. For FTP, it's a URI-like connection string + path. For virtual storage, it could be any opaque key. |
| **Output** | `IFile` — from **OwlCore.Storage** (external NuGet package). Not defined in this project. Provides `OpenStreamAsync(FileAccess, CancellationToken)` for reading/writing file contents. |
| **Error semantics** | **Throws on failure** (file not found, access denied, etc.). The doc explicitly says "otherwise throws an exception." Callers are expected to either handle the exception or use the `TryGet*` extension methods. |
| **Cancellation** | Token passed through to the underlying implementation; checked at the implementation's discretion. |

### 3.2 `Task<IFolder> GetFolderAsync(string id, CancellationToken)`

| Aspect | Detail |
|---|---|
| **Input** | `id` — analogous to the file path, identifying a directory/container. |
| **Output** | `IFolder` — from **OwlCore.Storage**. Represents a container that can enumerate children (`GetItemsAsync(StorableKind, CancellationToken)`), retrieve named children (`GetFirstByNameAsync(string, CancellationToken)`), and (if castable to `IModifiableFolder`) create/delete children. |
| **Error semantics** | Same "throw on failure" contract as `GetFileAsync`. |

### Key Design Observation: `id` is abstract

The `id` parameter is intentionally untyped (`string`). It is **not** constrained to a path, URI, or any specific format. This is what allows `IStorageService` to represent local NTFS paths, FTP URIs, MTP device identifiers, virtual/fuse filesystems, or future adapters — all through the same interface. The interpretation of `id` is entirely the implementation's responsibility.

---

## 4. Interfaces Returned — The OwlCore.Storage Hierarchy

`IFile` and `IFolder` come from **OwlCore.Storage** (v0.14.0.999), an external abstract storage library. Here's how they fit in the hierarchy:

```
OwlCore.Storage namespace:
  IStorable                    ← base: has Id, Name
    ├── IStorableChild         ← has GetParentAsync()
    │     ├── IFile            ← has OpenStreamAsync(FileAccess, ...)
    │     └── IFolder          ← has GetItemsAsync(StorableKind, ...),
    │                              GetFirstByNameAsync(name, ...)
    ├── IModifiableFolder      ← has CreateFileAsync(), CreateFolderAsync(),
    │                              DeleteAsync()
    │     ├── IDirectCopy      ← has CreateCopyOfAsync()  (Files.Core.Storage)
    │     └── IDirectMove      ← has MoveFromAsync()      (Files.Core.Storage)
    └── IFolderExtended        (from Files.Core.Storage extensions)
```

**Critical insight:** The `IFile` and `IFolder` that `IStorageService` returns are **not** Files' own types — they are **third-party abstractions** from OwlCore.Storage. The Files project then:
1. Wraps them with its own `IWindowsStorable` / `WindowsStorable` / `WindowsFile` / `WindowsFolder` classes.
2. Extends them with `IDirectCopy` / `IDirectMove` interfaces in `Files.Core.Storage.Storables`.
3. Adds Try-pattern convenience methods via `StorageExtensions` (file, folder, and service extension classes).

---

## 5. The Extension Layer — "Try" Pattern Wrappers

`StorageExtensions.Service.cs` adds three `Try*` methods that convert the throw-on-failure semantics into **null-on-failure**:

```csharp
// Core convenience method — tries folder first, falls back to file:
public static async Task<IStorable?> TryGetStorableAsync(
    this IStorageService storageService, string id, CancellationToken ct)
{
    return (IStorable?)await storageService.TryGetFolderAsync(id, ct)
                    ?? await storageService.TryGetFileAsync(id, ct);
}

// Individual try-wraps:
public static async Task<IFolder?> TryGetFolderAsync(...)
public static async Task<IFile?>   TryGetFileAsync(...)
```

These are what the app layer almost always calls (not the raw `GetFileAsync`/`GetFolderAsync`). They swallow all exceptions and return `null` — a pragmatic choice for UI code that just wants to know "did this work?" without caring whether the failure was "file not found" vs "permission denied."

The companion `StorageExtensions.File.cs` and `StorageExtensions.Folder.cs` add similar Try-pattern methods on `IFile` and `IFolder` directly (e.g., `TryGetFileByNameAsync`, `TryCreateFileAsync`, `TryOpenStreamAsync`, `CopyContentsToAsync`).

---

## 6. Inheritance — `IFtpStorageService`

```csharp
namespace Files.Core.Storage
{
    public interface IFtpStorageService : IStorageService
    {
        // Empty body — inherits both methods unchanged
    }
}
```

This is a **marker interface** with no additional members. It exists solely for DI disambiguation — the DI container can distinguish "give me the FTP service" from "give me the local storage service" even though they share the exact same method signatures. It follows the **Interface Segregation Principle** in spirit: FTP consumers depend on `IFtpStorageService`, local consumers depend on `IStorageService`, and neither knows which concrete implementation it gets.

---

## 7. Implementations

### 7.1 `NativeStorageLegacyService` (Current Default)

**Location:** `src/Files.App.Storage/Legacy/NativeStorageLegacy/NativeStorageService.cs`
**Registration:** `services.AddSingleton<IStorageService, NativeStorageLegacyService>()` in `AppLifecycleHelper.cs` (line 249)
**Status:** Marked `[Obsolete("Use the new WindowsStorable")]`

```csharp
public sealed class NativeStorageLegacyService : IStorageService
{
    public async Task<IFile> GetFileAsync(string id, CancellationToken ct)
    {
        return new SystemFile(id);   // OwlCore.Storage.System.IO.SystemFile
    }

    public async Task<IFolder> GetFolderAsync(string id, CancellationToken ct)
    {
        return new SystemFolder(id); // OwlCore.Storage.System.IO.SystemFolder
    }
}
```

This is a **thin wrapper** around OwlCore.Storage's own `SystemFile`/`SystemFolder`, which in turn wrap `System.IO.FileInfo`/`DirectoryInfo`. It's the simplest possible implementation — each call constructs a new wrapper, no caching, no validation beyond what `SystemFile`/`SystemFolder` do internally.

### 7.2 `FtpStorageService`

**Location:** `src/Files.App.Storage/Ftp/FtpStorageService.cs`
**Registration:** `services.AddSingleton<IFtpStorageService, FtpStorageService>()`

Translates the abstract `id` into FTP connection details (via `FtpHelpers`), connects via **FluentFTP**, validates the remote object exists and is the correct type, then returns `FtpStorageFile` or `FtpStorageFolder` wrapping the FTP path. Throws `DirectoryNotFoundException` / `FileNotFoundException` on mismatch.

### 7.3 `WindowsStorable` / `WindowsFile` / `WindowsFolder` (Modern Path, In Progress)

**Location:** `src/Files.App.Storage/Windows/`

This is the **intended replacement** for `NativeStorageLegacyService`. Instead of wrapping `System.IO`, it uses **native IShellItem pointers** (via CsWin32):

```csharp
public unsafe abstract class WindowsStorable : IWindowsStorable
{
    public IShellItem* ThisPtr { get; set; }   // Raw COM pointer
    public IContextMenu* ContextMenu { get; set; }
    public string Id => this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
    public string Name => this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);
}
```

`WindowsFolder` also holds a `ShellNewMenu` COM pointer (for the "New →" submenu). `WindowsFile.OpenStreamAsync` currently throws `NotImplementedException` — confirming this is a work-in-progress migration path. The goal is to replace the `System.IO`-based approach with a native Shell API-based approach that can expose shell properties (thumbnails, context menus, display names) directly from `IShellItem`, avoiding the impedance mismatch between `System.IO` and the Windows Shell namespace.

### 7.4 Implementation Comparison

| Feature | NativeStorageLegacy | FtpStorageService | WindowsStorable (WIP) |
|---|---|---|---|
| Backing API | System.IO (FileInfo/DirectoryInfo) | FluentFTP | IShellItem (COM) |
| `id` format | Absolute path | FTP URI string | Parsing name / known-folder GUID |
| Shell context menu | ✗ | ✗ | ✓ (via IContextMenu*) |
| Shell display names | ✗ | ✗ | ✓ (via SIGDN) |
| Thumbnails | ✗ | ✗ | Planned |
| Status | **Default (obsolete)** | Active | **Under development** |

---

## 8. Consumption in the App

`IStorageService` is injected into many app-layer consumers. Key consumers include:

| Consumer | How It's Used |
|---|---|
| **`BaseWidgetViewModel`** | Injected as `StorageService` property — all home page widgets (Quick Access, Recent Files, Drives, File Tags, Network Locations) use it to resolve paths to storables for display. |
| **`FileTagsService`** | Calls `StorageService.TryGetStorableAsync(item.FilePath)` to materialize tagged file paths into displayable `IStorable` objects. Called in `GetItemsForTagAsync`, `GetAllFileTagsAsync`, `GetAllItemsGroupedByTagAsync`. |
| **`ShelfPane`** | Uses it to resolve shelf item paths to storables. |
| **`PinToStartAction` / `UnpinFromStartAction`** | Uses it to resolve paths to storables before pinning/unpinning to the Windows Start Menu. |
| **`AppLifecycleHelper`** | Registers it in the DI container on app startup. |

---

## 9. Design Assessment

### Strengths

1. **Clean abstraction boundary** — The app layer never touches `System.IO.FileInfo` or `StorageFile` directly. Everything goes through `IStorageService` → `IFile`/`IFolder`, making it possible to support non-local filesystems (FTP, MTP, archives, cloud) through the same interface.

2. **Single Responsibility, Well-Delegated** — `IStorageService` only does one thing: *resolve an identifier to a storable object*. All subsequent operations (read, write, enumerate, copy, move, delete) belong to the `IFile`/`IFolder`/`IModifiableFolder`/`IDirectCopy`/`IDirectMove` interfaces. This is correct separation.

3. **DI-friendly** — The interface is simple enough to mock in tests, and the marker-interface pattern (`IFtpStorageService : IStorageService`) enables transparent multi-backend support via the DI container.

4. **Try-pattern via extensions** — Separating the throwing methods (interface) from the null-returning Try methods (extension methods) keeps the interface minimal while providing both semantics. This follows the .NET convention of `int.Parse` vs `int.TryParse`.

5. **Migration path is visible** — The `[Obsolete]` attribute on `NativeStorageLegacyService` with a message pointing to the replacement is the right way to signal architectural intent.

### Weaknesses / Open Questions

1. **The return types are third-party** — `IFile` and `IFolder` come from `OwlCore.Storage`, not from `Files.Core.Storage`. This means the core contract depends on an external library's type hierarchy. If OwlCore.Storage changes or is abandoned, the entire storage abstraction layer would need rework.

2. **`id` is untyped** — While this enables multiple backends, it also means there is no compile-time distinction between a local path, an FTP URI, and a virtual ID. This pushes validation to runtime and makes it impossible to know (from the type alone) what kind of storage a given `IStorageService` represents.

3. **`NativeStorageLegacyService` is the default but deprecated** — The code ships with the obsolete implementation as the singleton default while the modern replacement (`WindowsStorable`) is incomplete (`OpenStreamAsync` throws `NotImplementedException`). This suggests the migration is mid-flight.

4. **No async advantage in legacy implementation** — `NativeStorageLegacyService.GetFileAsync` just does `await Task.CompletedTask; return new SystemFile(id);` — the async is entirely cosmetic. The `SystemFile` constructor does synchronous `FileInfo` construction. True async would require `Windows.Storage.StorageFile` APIs or at least `FileStream` with `isAsync: true`.

5. **No method to enumerate top-level storables** — There's no `GetRootsAsync()` or `GetDevicesAsync()` on `IStorageService`. Drive/device enumeration is handled by entirely separate services (`IDeviceWatcher`, `StorageDevicesService`), so `IStorageService` is only useful *after* you already know the path/id you want. This splits the filesystem concern across multiple interfaces that aren't visibly related.

6. **No path-combining or navigation helpers** — The interface provides no way to derive a child path from a parent. `IFolder.GetFirstByNameAsync()` fills this role on the returned *storable*, but there's no service-level helper for path manipulation — that responsibility falls to `PathHelpers` in `Files.Shared`.

---

## 10. The Full Type Map

```
Files.Core.Storage (this project)
├── IStorageService.cs              ◄── The root contract
│     ├── GetFileAsync(string) → IFile          (OwlCore.Storage)
│     └── GetFolderAsync(string) → IFolder      (OwlCore.Storage)
│
├── IFtpStorageService.cs           ◄── Marker: IStorageService
│
├── Contracts/
│     ├── IWatcher.cs               ◄── Folder change watcher (Start/Stop)
│     ├── ITrashWatcher.cs          ◄── IWatcher + trash events
│     └── IDeviceWatcher.cs         ◄── Device add/remove events
│
├── Enums/
│     └── StorableKind.cs           ◄── Files | Folders | All (Flags)
│
├── EventArguments/
│     └── DeviceEventArgs.cs        ◄── DeviceName + DeviceId
│
├── Extensions/
│     ├── StorageExtensions.Service.cs  ◄── TryGetStorableAsync, TryGetFileAsync, TryGetFolderAsync
│     ├── StorageExtensions.File.cs     ◄── OpenStreamAsync (with FileShare), TryOpenStreamAsync, CopyContentsToAsync
│     └── StorageExtensions.Folder.cs   ◄── TryGetFileByNameAsync, TryGetFolderByNameAsync, TryCreateFileAsync, TryCreateFolderAsync
│
└── Storables/DirectStorage/
      ├── IDirectCopy.cs            ◄── IModifiableFolder + CreateCopyOfAsync
      └── IDirectMove.cs            ◄── IModifiableFolder + MoveFromAsync

App-layer implementations (Files.App.Storage):
├── Legacy/NativeStorageLegacy/NativeStorageService.cs  [Obsolete]
├── Ftp/FtpStorageService.cs                             (active)
└── Windows/WindowsStorable.cs + WindowsFile/Folder      (in-progress replacement)

OwlCore.Storage (external NuGet):
├── IStorable                    (Id, Name)
├── IStorableChild               (GetParentAsync)
├── IFile                        (OpenStreamAsync)
├── IFolder                      (GetItemsAsync, GetFirstByNameAsync)
├── IModifiableFolder            (CreateFileAsync, CreateFolderAsync, DeleteAsync)
└── System.IO wrappers:          SystemFile, SystemFolder
```

---

## 11. Summary

`IStorageService` is a **strategically minimal** interface. Its two methods encode a single design decision: *"the app obtains filesystem objects by asking a service for them by string key, not by constructing them directly."* This indirection is what enables FTP support, the in-progress `IShellItem` migration, future virtual filesystems, and testability — all without changing a single line of consuming ViewModel code.

The cost of this abstraction is that the returned `IFile`/`IFolder` types are owned by a third-party library (OwlCore.Storage), the `id` parameter is semantically unconstrained, and the current default implementation is both synchronous-under-the-hood and marked obsolete — indicating an architecture in active transition.
