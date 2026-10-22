// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using HyperPDF.Models;
using HyperPDF.Services;
using HyperPDF.ViewModels;

namespace HyperPDF.Views;

/// <summary>The main window of HyperPDF.</summary>
public partial class MainWindow : Window
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 0x00000002;

    private readonly AppServices _services;
    private bool _isRestoringPlacement;

    /// <summary>Creates the main window.</summary>
    public MainWindow(AppServices services)
    {
        _services = services;
        InitializeComponent();

        ViewModel = new MainViewModel(
            services.Settings,
            services.RecentFiles,
            services.Dialogs,
            services.FileDialogs,
            services.Shell,
            services.Theme,
            services.Documents);

        DataContext = ViewModel;

        ViewModel.FindRequested += OnFindRequested;
        ViewModel.ThemeChanged += OnThemeChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        SourceInitialized += OnSourceInitialized;
        StateChanged += OnWindowStateChanged;
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;

        UpdateMaximizeGlyph();
        UpdateMaximisedLayout();
    }

    /// <summary>The view model of the main window.</summary>
    public MainViewModel ViewModel { get; }

    /// <summary>Opens a document given as command line argument or shell association.</summary>
    public async Task OpenPathAsync(string path)
    {
        await ViewModel.OpenFileAsync(path).ConfigureAwait(true);
    }

    private void OnFindRequested(object? sender, EventArgs e) => DocumentView.ShowFindBar();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedDocument))
        {
            DocumentView.ApplyFitZoom();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        // The colours are resolved through dynamic resources, only the window chrome needs a refresh.
        InvalidateVisual();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        CheckForStartupUpdateAsync();
    }

    private async void CheckForStartupUpdateAsync()
    {
        try
        {
            UpdateSettings updates = _services.Settings.Settings.Updates;
            if (!updates.AutoCheckOnStartup)
            {
                return;
            }

            // At most one automatic check every 24 hours.
            if (updates.LastCheckUtc is DateTime last && DateTime.UtcNow - last < TimeSpan.FromHours(24))
            {
                return;
            }

            Services.UpdateManifest? manifest = await _services.Updates
                .CheckForUpdatesAsync()
                .ConfigureAwait(true);

            updates.LastCheckUtc = DateTime.UtcNow;
            await _services.Settings.SaveAsync().ConfigureAwait(true);

            if (manifest is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(updates.SkippedVersion) &&
                string.Equals(updates.SkippedVersion, manifest.Version, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UpdateDialog.ShowAvailable(manifest, _services);
        }
        catch (Exception)
        {
            // An update check must never disturb the reading experience.
        }
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed && WindowState == WindowState.Normal)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // The window was already being moved by the system.
            }
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.PageDown)
        {
            ViewModel.NextPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.PageUp)
        {
            ViewModel.PreviousPageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && ViewModel.SelectedDocument is not null)
        {
            DocumentView.HideFindBar();
            e.Handled = true;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(WindowMessageHook);
        }

        RestorePlacement();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (_isRestoringPlacement)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            ViewModel.IsFullScreen = true;
        }
        else if (ViewModel.IsFullScreen && WindowState == WindowState.Normal)
        {
            ViewModel.IsFullScreen = false;
        }

        UpdateMaximizeGlyph();
        UpdateMaximisedLayout();
    }

    private void UpdateMaximisedLayout()
    {
        // A maximised WPF window extends 8px (the resize border) beyond the monitor. Padding
        // the layout with the same amount keeps the title bar and the status bar fully visible
        // on every edge, exactly like a normal operating system window.
        RootLayout.Margin = WindowState == WindowState.Maximized
            ? new Thickness(6)
            : new Thickness(0);
    }

    private void UpdateMaximizeGlyph()
    {
        MaximizeGlyph.Data = WindowState == WindowState.Maximized
            ? (Geometry)(Application.Current.TryFindResource("IconWindowRestore") ?? MaximizeGlyph.Data)
            : (Geometry)(Application.Current.TryFindResource("IconWindowMaximize") ?? MaximizeGlyph.Data);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        foreach (DocumentViewModel document in ViewModel.Documents)
        {
            document.SaveSessionState();
        }

        SavePlacement();
    }

    private void RestorePlacement()
    {
        WindowPlacementState placement = _services.Settings.Settings.Window;

        if (placement.Width > 0 && placement.Height > 0)
        {
            Width = placement.Width;
            Height = placement.Height;
        }

        if (placement.HasValidPosition)
        {
            Left = placement.Left!.Value;
            Top = placement.Top!.Value;
        }

        _isRestoringPlacement = true;
        try
        {
            WindowState = placement.IsMaximized ? WindowState.Maximized : WindowState.Normal;
        }
        finally
        {
            _isRestoringPlacement = false;
        }
    }

    private void SavePlacement()
    {
        WindowPlacementState placement = _services.Settings.Settings.Window;

        if (WindowState == WindowState.Normal)
        {
            placement.Left = Left;
            placement.Top = Top;
            placement.Width = ActualWidth > 0 ? ActualWidth : Width;
            placement.Height = ActualHeight > 0 ? ActualHeight : Height;
            placement.IsMaximized = false;
        }
        else
        {
            placement.IsMaximized = WindowState == WindowState.Maximized;
            placement.IsFullScreen = ViewModel.IsFullScreen;
        }
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmGetMinMaxInfo)
        {
            AdjustMaximisedBounds(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void AdjustMaximisedBounds(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };

        if (GetMonitorInfo(monitor, ref info))
        {
            var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            minMax.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
            minMax.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
            minMax.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
            minMax.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
            Marshal.StructureToPtr(minMax, lParam, true);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
