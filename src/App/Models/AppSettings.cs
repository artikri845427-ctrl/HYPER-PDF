// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>All user configurable preferences of HyperPDF.</summary>
public sealed class AppSettings
{
    /// <summary>Maximum number of entries kept in the recent documents list.</summary>
    public const int MaxRecentFiles = 12;

    /// <summary>Colour theme.</summary>
    public AppTheme Theme { get; set; } = AppTheme.Light;

    /// <summary>Page layout mode.</summary>
    public ViewMode ViewMode { get; set; } = ViewMode.Continuous;

    /// <summary>Zoom strategy.</summary>
    public ZoomMode ZoomMode { get; set; } = ZoomMode.FitWidth;

    /// <summary>Zoom percentage used when <see cref="ZoomMode"/> is <see cref="ZoomMode.Custom"/>.</summary>
    public double ZoomPercent { get; set; } = 100;

    /// <summary>True when the thumbnail sidebar is visible.</summary>
    public bool ShowThumbnails { get; set; } = true;

    /// <summary>True when the status bar is visible.</summary>
    public bool ShowStatusBar { get; set; } = true;

    /// <summary>True when the last reading position of a document is restored.</summary>
    public bool RestoreLastPosition { get; set; } = true;

    /// <summary>True when the welcome screen is shown instead of an empty workspace.</summary>
    public bool ShowWelcomeScreen { get; set; } = true;

    /// <summary>Recently opened documents, newest first.</summary>
    public List<RecentFileEntry> RecentFiles { get; set; } = new();

    /// <summary>Per document session state, keyed by the full document path.</summary>
    public Dictionary<string, DocumentSessionState> DocumentStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Main window placement.</summary>
    public WindowPlacementState Window { get; set; } = new();

    /// <summary>Automatic update preferences.</summary>
    public UpdateSettings Updates { get; set; } = new();

    /// <summary>Creates a deep copy of the settings.</summary>
    public AppSettings Clone()
    {
        var clone = new AppSettings
        {
            Theme = Theme,
            ViewMode = ViewMode,
            ZoomMode = ZoomMode,
            ZoomPercent = ZoomPercent,
            ShowThumbnails = ShowThumbnails,
            ShowStatusBar = ShowStatusBar,
            RestoreLastPosition = RestoreLastPosition,
            ShowWelcomeScreen = ShowWelcomeScreen,
            Window = new WindowPlacementState
            {
                Left = Window.Left,
                Top = Window.Top,
                Width = Window.Width,
                Height = Window.Height,
                IsMaximized = Window.IsMaximized,
                IsFullScreen = Window.IsFullScreen
            },
            Updates = new UpdateSettings
            {
                AutoCheckOnStartup = Updates.AutoCheckOnStartup,
                SkippedVersion = Updates.SkippedVersion,
                LastCheckUtc = Updates.LastCheckUtc
            },
            RecentFiles = RecentFiles.Select(e => e.Clone()).ToList(),
            DocumentStates = new Dictionary<string, DocumentSessionState>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var pair in DocumentStates)
        {
            clone.DocumentStates[pair.Key] = new DocumentSessionState
            {
                LastPageIndex = pair.Value.LastPageIndex,
                ZoomPercent = pair.Value.ZoomPercent,
                ViewMode = pair.Value.ViewMode
            };
        }

        return clone;
    }
}

/// <summary>Automatic update preferences of HyperPDF.</summary>
public sealed class UpdateSettings
{
    /// <summary>True when HyperPDF checks for a new release at every start.</summary>
    public bool AutoCheckOnStartup { get; set; } = true;

    /// <summary>Version the user asked to skip, or null.</summary>
    public string? SkippedVersion { get; set; }

    /// <summary>UTC timestamp of the last background update check, or null.</summary>
    public DateTime? LastCheckUtc { get; set; }
}
