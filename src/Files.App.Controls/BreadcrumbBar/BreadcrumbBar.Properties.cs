﻿// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class BreadcrumbBar : Control
	{
		[GeneratedDependencyProperty]
		public partial FrameworkElement? RootItem { get; set; }

		[GeneratedDependencyProperty]
		public partial object? ItemsSource { get; set; }

		[GeneratedDependencyProperty]
		public partial object? ItemTemplate { get; set; }

		[GeneratedDependencyProperty]
		public partial string? EllipsisButtonToolTip { get; set; }

		[GeneratedDependencyProperty]
		public partial string? RootItemToolTip { get; set; }

		[GeneratedDependencyProperty]
		public partial string? RootItemChevronToolTip { get; set; }
	}
}
