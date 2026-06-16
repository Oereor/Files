# Files — Project Overview

> **Open-source file manager for Windows, built with WinUI 3 / .NET 10**
>
> Generated: 2026-06-16 | Repository: https://github.com/files-community/Files

---

## 1. What Is This?

**Files** is a modern, open-source, WinUI 3-based file manager for Windows — a full-featured alternative to the built-in Windows File Explorer. It supports tabs, dual-pane layouts, tagging, archive management, cloud-drive integration, FTP, Git, theming, and more. The project is maintained by the **Files Community** and licensed under the **MIT License** (since 2018). It is distributed via the Microsoft Store, sideload installers, and classic MSIX packages.

---

## 2. Technology Stack

| Layer | Technology |
|---|---|
| **UI Framework** | WinUI 3 (Windows App SDK 2.2.0) |
| **Runtime** | .NET 10 (`net10.0-windows10.0.26100.0`) |
| **Language** | C# (with C++/WinRT launchers and dialogs) |
| **Min Windows** | Windows 10, build 19041+ |
| **Architecture** | MVVM (via CommunityToolkit.Mvvm) |
| **Packaging** | MSIX (classic installer + store packages) |
| **Interop** | CsWin32, CsWinRT, Vanara (Windows Shell), Win32 P/Invoke |
| **Storage** | OwlCore.Storage abstraction, direct Win32, Shell APIs, FTP |
| **SQLite** | Microsoft.Data.Sqlite + SQLitePCLRaw (for layout prefs, file tags) |
| **CI/CD** | GitHub Actions (ci.yml, cd-sideload-*, cd-store-*) |
| **Localization** | Crowdin (`.resw` resource files per locale) |
| **Logging** | Sentry (crash/error telemetry) |

### Key NuGet Dependencies

- **CommunityToolkit** suite: Mvvm, WinUI controls (ColorPicker, SettingsControls, Sizers, Converters, Behaviors, Triggers, MarkdownTextBlock, Shimmer)
- **Vanara.Windows.Shell** & **Vanara.Windows.Extensions** — managed Windows Shell wrappers
- **Microsoft.Windows.CsWin32** v0.3.264 — source-generated P/Invoke
- **Microsoft.Windows.CsWinRT** v2.2.0 — WinRT interop
- **OwlCore.Storage** v0.14 — abstract storage layer
- **FluentFTP** v53 — FTP client
- **LibGit2Sharp** v0.30 — Git integration
- **SevenZipSharp** + **SharpZipLib** — archive compression/decompression
- **DiscUtils.Udf** — UDF disk image support
- **TagLibSharp** v2.3 — media file metadata
- **Win2D** (Microsoft.Graphics.Win2D) v1.3.2 — image rendering
- **Sentry** v6 — error monitoring

---

## 3. Solution Architecture

The solution (`Files.slnx`) uses the new .NET SDK-style solution format and is organized into **src/core**, **src/platforms**, and **tests**, targeting `arm64`, `x64`, and `x86`.

### 3.1 Core Layer (`/src/core/`)

Projects with **no UI dependency** — pure storage contracts, shared helpers, and source generators.

| Project | Purpose |
|---|---|
| `Files.Core.Storage` | Low-level storage abstractions: contracts (`IStorageService`, `IDeviceWatcher`, `ITrashWatcher`), enums (`StorableKind`), storable base classes, direct-copy/move interfaces. Does NOT depend on the WinUI app. |
| `Files.Shared` | Shared models, helpers, extensions, and utilities used across multiple projects. Includes: `PathHelpers`, `ChecksumHelpers`, `FileExtensionHelpers`, `AsyncManualResetEvent`, custom attributes (`GeneratedRichCommandAttribute`, `RegistrySerializableAttribute`). |
| `Files.Core.SourceGenerator` | Roslyn source generators and analyzers: custom code-generators, code-fix providers, and parser utilities used at compile time by the solution. |

### 3.2 Platform Layer (`/src/platforms/`)

All UI-aware and platform-specific projects.

#### The Main App: `Files.App`

The WinUI 3 desktop application. Follows **MVVM** architecture:

- **Actions/** — ~160+ command actions implementing `IAction` / `IToggleAction`. Organized by category:
  - **Content** — Archive operations (compress/decompress via 7-Zip/ZIP), image rotation, font/certificate/driver installation, "set as background/wallpaper/lockscreen", "run as admin/another user", selection, sharing, tags, preview popups.
  - **Display** — Grouping, sorting, layout switching, files-first/folders-first ordering.
  - **FileSystem** — Copy, cut, paste, delete (soft/permanent), rename, create file/folder/shortcut/ADS, format drives, empty/restore recycle bin, flatten folder, open file location.
  - **Git** — Clone, fetch, pull, push, sync, init.
  - **Global** — Theme switching (light/dark/default), compact overlay, full screen, undo/redo, search, edit path.
  - **Navigation** — Tab management (new, close, duplicate, reopen closed, next/previous), pane management (split horizontal/vertical, close/focus other pane), navigation (back, forward, up, home), open in new tab/pane/window.
  - **Open** — Settings, terminal (user/admin), classic properties, Storage Sense, IDE integration, command palette, log files.
  - **Show** — Toggle sidebar, toolbar, preview pane, details pane, info pane, shelf pane, dual pane, hidden items, file extensions, dot files, filter header.
  - **Sidebar** — Pin/unpin/copy from sidebar.
  - **Shelf** — Copy, cut, delete items from the shelf.
  - **Start** — Pin to / unpin from Start Menu.

- **ViewModels/** — Top-level view models:
  - `MainPageViewModel` — Primary shell with multi-tab, dual-pane state.
  - `ShellViewModel` — Core shell logic (path navigation, item enumeration).
  - `HomeViewModel` — Home page with widgets.
  - `ReleaseNotesViewModel` — Release notes display.

- **Views/** — WinUI pages:
  - `MainPage.xaml` — Main application shell.
  - `ShellPanesPage.xaml` — Dual-pane layout host.
  - `HomePage.xaml` — Home/widget page.
  - `SplashScreenPage.xaml` — App launch splash.
  - `ReleaseNotesPage.xaml` — Release notes dialog/page.

- **Services/** — Injected services (DI via `Microsoft.Extensions.DependencyInjection`):
  - **App/** — Localization, theming, threading, dialog service, resource management, app updates (store/sideload/none).
  - **Settings/** — Per-category settings services: Appearance, Application, Layout, General, Folders, File Tags, Dev Tools, Info Pane, Actions, User Settings.
  - **Storage/** — Archive operations, cache, icon cache, devices, network, security, trash bin.
  - **Windows/** — Shell integration, jump lists, quick access, recent items, start menu, wallpaper, compatibility, COM dialog wrappers, INI file handling.
  - **DateTimeFormatter/** — Abstract + concrete datetime formatters (system, user, application, universal).
  - **PreviewPopupProviders/** — Integration with QuickLook, SeerPro, and PowerToys Peek for file preview popups.
  - **SizeProvider/** — Cached/on-demand drive size calculations.

- **Helpers/** — Static utility classes:
  - **Application/** — App lifecycle, language, toast notifications.
  - **Dialog/** — Dynamic dialog factory and display helper.
  - **Layout/** — Adaptive layout calculation, `LayoutPreferencesManager` (SQLite-backed per-folder layout preferences).
  - **Navigation/** — Multitasking/tabs helpers, navigation state tracking, interaction tracker.
  - **UI/** — Backdrop (Mica/Acrylic), theme resources, drag zones, filesystem UI helpers.
  - **Win32/** — P/Invoke definitions organized by concern: Process, Shell, Storage, WindowManagement. Also included: manually defined constant/enum/method/struct P/Invokes.
  - **WMI/** — Management event watcher for device/drive change notifications.
  - **MenuFlyout/** — Context menu generation from model to UI elements.
  - Other: `BitmapHelper`, `ColorHelpers`, `CredentialsHelpers`, `LocationHelpers`, `MediaFileHelper`, `NaturalStringComparer`, `PathNormalization`, `RegexHelpers`, `RegistryHelpers`, `ShareItemHelpers`, `TypesConverter`.

- **Utils/** — Non-service state/logic:
  - **Cloud/** — Cloud drive detection (Google Drive, Dropbox, OneDrive, Box, Synology Drive, OX Drive, Generic). Monitors sync status.
  - **CommandLine/** — Parses command-line arguments for app launch (e.g., open a specific folder).
  - **FileTags/** — Tagging system backed by SQLite (assign, remove, query file tags).
  - **Git/** — Git integration via LibGit2Sharp (`GitHelpers`, `IVersionControl`, `LibGit2`).
  - **Global/** — `QuickAccessManager`, `WSLDistroManager`, `WindowsStorageDeviceWatcher`.
  - **Library/** — Windows Libraries management.
  - **Logger/** — Sentry-based error/crash logging.
  - **Serialization/** — JSON settings serialization with caching (`BaseObservableJsonSettings`, `CachingJsonSettingsDatabase`).
  - **Shell/** — Context menu hosting, file association helpers, launch/COM helpers, shell file operations (IFileOperation), shell libraries, preview handler hosting, new menu items.
  - **Signatures/** — Digital signature verification utilities.
  - **StatusCenter/** — In-app operation status/progress tracking (copy/move/delete progress models).
  - **Storage/** — The deepest subsystem:
    - **Collection/** — `BulkConcurrentObservableCollection`, `ConcurrentCollection`, `GroupedCollection`, sorting and grouping helpers.
    - **Enumerators/** — `Win32StorageEnumerator`, `UniversalStorageEnumerator` — enumerate filesystem items.
    - **Helpers/** — `DeviceManager`, `DriveHelpers`, `StorageHelpers`, `FtpHelpers`, `MtpHelpers`, `FileThumbnailHelper`, `FilePropertiesHelpers`, `SyncRootHelpers`, `StorageSenseHelper`, `FontFileHelper`, `FileSystemResult`, `FileSystemTasks`.
    - **History/** — Undo/redo via `StorageHistory` and `StorageHistoryOperations`.
    - **Operations/** — `FilesystemOperations` (async file ops), `ShellFilesystemOperations` (COM-based file ops via IFileOperation), `FileOperationsHelpers`, `FileSizeCalculator`.
    - **StorageBaseItems/** — Abstract base classes for storage files/folders/properties (`BaseStorageFile`, `BaseStorageFolder`, `BaseBasicProperties`, `BaseStorageItemExtraProperties`).
    - **StorageItems/** — Concrete storage representations: `NativeStorageFile`, `SystemStorageFile`, `SystemStorageFolder`, `ShellStorageFile`, `ShellStorageFolder`, `FtpStorageFile`, `FtpStorageFolder`, `ZipStorageFile`, `ZipStorageFolder`, `VirtualStorageFile`, `VirtualStorageFolder`, `BaseQueryResults`.
  - **Taskbar/** — System tray icon and overlay window.
  - **Widgets/** — Home page widget rendering helpers.

- **UserControls/** — Reusable UI components:
  - **FilePreviews/** — Preview renderers for: text, code, HTML, CSS, Markdown, images, rich text, PDF, media (audio/video), folders, shell-registered preview handlers.
  - **Pane/** — `InfoPane` (file metadata/details) and `ShelfPane` (element staging area).
  - **Selection/** — Rectangle selection strategies (extend, ignore, invert selection), `RectangleSelection` behavior.
  - **TabBar/** — Custom tab bar control (`TabBar`, `TabBarItem`, `ITabBar`, `ITabBarItem`).
  - **Widgets/** — Home page widgets: Drives, File Tags, Network Locations, Quick Access, Recent Files.
  - **Menus/** — Custom menu flyout items with themed icons, file-tags context menu.
  - **KeyboardShortcut/** — Keyboard shortcut visualization control.
  - **StatusCenter/** — Operation progress display with speed graph.
  - `NavigationToolbar`, `StatusBar`, `Toolbar`, `DataGridHeader`, `FileIcon`, `FolderEmptyIndicator`, `ComboBoxEx`.

- **Dialogs/** — Modal dialogs: AddItem, BulkRename, CloneRepo, CreateArchive, CreateShortcut, Credential, DecompressArchive, DynamicDialog, ElevateConfirm, FileTooLarge, FilesystemOperation, GitHubLogin, ReorderSidebarItems, AddBranch.

- **Converters/** — XAML value converters.
- **Extensions/** — C# extension methods for the app layer.
- **Data/** — Data models, enums, and constants.
- **Assets/** — Icons, images, app assets.
- **Strings/** — Localized `.resw` resource files (managed via Crowdin).
- **Styles/** — XAML theme/style resources.

#### Other Platform Projects

| Project | Language | Purpose |
|---|---|---|
| `Files.App.Controls` | C# | Reusable WinUI 3 custom controls: **AdaptiveGridView**, **BladeView** (nested navigation panels), **BreadcrumbBar**, **GridSplitter**, **Omnibar** (command palette/Omnibar text box), **Sidebar**, **Toolbar** (with Button, ToggleButton, RadioButton, SplitButton, FlyoutButton, Separator variants), **ThemedIcon** (multi-layer theme-aware icons), **StorageBar** / **StorageRing** (disk usage visualization). |
| `Files.App.CsWin32` | C# | Source-generated Win32 interop — add native APIs to `NativeMethods.txt` and CsWin32 generates the P/Invoke wrappers. |
| `Files.App.BackgroundTasks` | C# | Background tasks (currently: app-update task). |
| `Files.App.Server` | C# | App-service component (COM server) for handling file operations from external processes. |
| `Files.App.Storage` | C# | App-level storage abstractions: FTP, Windows-native, and legacy storage implementations. |
| `Files.App.Launcher` | C++ | Native C++ launcher entry point (`.vcxproj`). |
| `Files.App.OpenDialog` | C++ | File-open dialog app — invoked by the shell when Files is set as the default file picker. Has a Win32 variant. |
| `Files.App.SaveDialog` | C++ | File-save dialog app — same pattern as OpenDialog. Has a Win32 variant. |
| `Files.App (Package)` | — | MSIX packaging project assets (appxmanifest, package configuration). |

### 3.3 Tests (`/tests/`)

| Project | Type | Purpose |
|---|---|---|
| `Files.App.UITests` | UI automation | WinAppDriver/Appium-based UI tests. Includes test views and data assets. |
| `Files.InteractionTests` | Interaction | CI automation interaction tests. |
| `Files.App.UnitTests` | Unit | Placeholder/stale in current checkout. |

---

## 4. Key Architectural Patterns

### 4.1 MVVM with Dependency Injection

The app uses `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Hosting` for DI. ViewModels bind to Views via `CommunityToolkit.Mvvm` (source-generated `[ObservableProperty]`, `[RelayCommand]`). Services are registered in the DI container and injected into ViewModels.

### 4.2 Action System

All user-facing commands (160+) implement `IAction` or `IToggleAction`. Actions are organized into a deep namespace hierarchy and are registered/loaded via reflection. This enables:
- Centralized keyboard shortcut binding
- Consistent command palette access
- Reusable command logic across toolbar, context menu, and Omnibar

### 4.3 Multi-Pane, Multi-Tab Shell

The shell supports:
- **Tabs** — Multiple tabs per pane, drag-and-drop reordering
- **Dual pane** — Horizontal or vertical split
- **Independent navigation** — Each pane has its own navigation stack (back/forward/up/home)

### 4.4 Storage Abstraction

A layered storage architecture:
1. **`Files.Core.Storage`** — Contracts only (no app dependency): `IStorageService`, `IWatcher`, `IDeviceWatcher`, `ITrashWatcher`
2. **`Files.App.Storage`** — App-level implementations: FTP, Windows-native, Legacy
3. **`Files.App/Utils/Storage/`** — Rich storage items with properties, thumbnails, cloud status, enumerated collections
4. **`OwlCore.Storage`** — Third-party abstract storage library

Filesystem operations are dual-path:
- **Async .NET APIs** (`FilesystemOperations`) — For most operations
- **COM `IFileOperation`** (`ShellFilesystemOperations`) — For shell-integrated operations with progress dialogs

### 4.5 Custom Controls Library

`Files.App.Controls` is a substantial reusable control library with:
- **Sidebar** — Pre-built sidebar with pinning, drag-drop, and item model abstraction
- **Toolbar** — Full toolbar system with overflow, toggle buttons, split buttons, radio buttons
- **BreadcrumbBar** — Address bar with breadcrumb navigation
- **Omnibar** — Command palette with text member path binding
- **BladeView** — Nested navigation panel (like Azure Portal / Settings app)
- **ThemedIcon** — Multi-layer, theme-aware icon system
- **StorageBar / StorageRing** — Disk usage donut/bar charts

### 4.6 Win32 & COM Interop

- **CsWin32** — Primary path for new P/Invoke (add APIs to `NativeMethods.txt`)
- **Vanara** — Higher-level managed wrappers for Windows Shell APIs
- **CsWinRT** — WinRT interop
- **Manual P/Invoke** — Legacy manual `Win32PInvoke.*` files in Helpers, to be migrated to CsWin32 (see `docs/interop-unmarshaled-conversion.md`)
- **C++/WinRT launchers** — `Files.App.Launcher`, `Files.App.OpenDialog`, `Files.App.SaveDialog` use C++ for early-process / COM-out-of-process entry points

### 4.7 Settings & Persistence

- Settings are stored as JSON files, loaded via `BaseObservableJsonSettings` with a caching layer (`CachingJsonSettingsDatabase`)
- Per-folder layout preferences (view mode, sort order, column widths) are stored in **SQLite** via `LayoutPreferencesDatabaseManager`
- File tags are stored in **SQLite** via `FileTagsDatabase`

### 4.8 Context Menu Integration

The app hosts Windows Shell context menus (`ContextMenu` class) via COM `IContextMenu` — showing the native right-click menu inside the Files app. Custom context menu items can also be defined.

---

## 5. Build & CI/CD

### Local Build

```powershell
msbuild -restore src\Files.App\Files.App.csproj /p:Configuration=Debug /p:Platform=x64
```

The main `Files.App.csproj` has build dependencies on the C++ launcher and dialog projects.

### CI/CD Pipelines (GitHub Actions)

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | PRs, pushes to main | Build + test (x64, arm64). Source of truth for build/packaging commands. |
| `cd-sideload-preview.yml` | Scheduled / manual | Build and deploy preview sideload MSIX. |
| `cd-sideload-stable.yml` | Manual | Build and deploy stable sideload MSIX. |
| `cd-store-preview.yml` | Scheduled / manual | Build and submit preview to Microsoft Store. |
| `cd-store-stable.yml` | Manual | Build and submit stable to Microsoft Store. |
| `format-xaml.yml` | PRs | XAML formatting check/lint. |

### Packaging Scripts

CI uses PowerShell scripts for:
- `Configure-AppxManifest.ps1` — Sets package identity per build flavor
- `Create-MsixBundle.ps1` — Creates signed MSIX bundles
- `Generate-SelfCertPfx.ps1` — Generates self-signed certs for sideload
- `Convert-TrxToMarkdown.ps1` — Converts test results to markdown

---

## 6. Localization

Managed via **Crowdin** (`crowdin.yml`). Source strings are in `src/Files.App/Strings/en-US/*.resw`. Translations are pulled per-locale into sibling folders (e.g., `de-DE/`, `fr-FR/`).

---

## 7. Community & Contribution

- **License:** MIT (Copyright 2018–present, Files Community)
- **Issue templates:** Bug report and feature request (YAML forms)
- **Pull request template:** Standardized PR template
- **Code of Conduct:** Contributor Covenant
- **Funding:** Community-sponsored (`.github/FUNDING.yml`)
- **Privacy:** Detailed privacy notice (`.github/PRIVACY.md`)

---

## 8. File Count & Scale Summary

| Area | Approx. Files |
|---|---|
| Actions (C#) | ~160 |
| App Services (C#) | ~50 |
| App Helpers (C#) | ~45 |
| App Utils (C#) | ~90 |
| UserControls (XAML + C#) | ~55 |
| Dialogs (XAML + C#) | ~14 |
| Controls Library (C#) | ~80 |
| Core.Storage + Shared | ~40 |
| Tests | ~15 |
| CI/CD + Scripts | ~15 |

**Total:** Approximately 500+ source files across the solution, making this a substantial, production-grade Windows desktop application.

---

## 9. Key Facts at a Glance

- **App name:** Files
- **Type:** Windows desktop file manager (WinUI 3)
- **First commit:** 2018
- **License:** MIT
- **Platforms:** arm64, x64, x86
- **Runtime:** .NET 10 (Windows App SDK 2.2)
- **Min OS:** Windows 10 19041 / Windows 11
- **Distribution:** Microsoft Store, sideload MSIX, classic installer
- **Tests:** UI automation (Appium + WinAppDriver), interaction tests
- **CI:** GitHub Actions
- **Localization:** Crowdin (community-translated)
