// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Services;
using HyperPDF.Services.Pdfium;

namespace HyperPDF;

/// <summary>Container that wires the HyperPDF services together.</summary>
public sealed class AppServices : IDisposable
{
    private bool _disposed;

    private AppServices(
        ISettingsService settings,
        IRecentFilesService recentFiles,
        IDialogService dialogs,
        IFileDialogService fileDialogs,
        IShellService shell,
        IThemeService theme,
        IPrintService print,
        IDocumentService documents,
        IUpdateService updates)
    {
        Settings = settings;
        RecentFiles = recentFiles;
        Dialogs = dialogs;
        FileDialogs = fileDialogs;
        Shell = shell;
        Theme = theme;
        Print = print;
        Documents = documents;
        Updates = updates;
    }

    /// <summary>The container created by the running application, if any.</summary>
    public static AppServices? Current { get; internal set; }

    /// <summary>User preferences.</summary>
    public ISettingsService Settings { get; }

    /// <summary>Recent documents.</summary>
    public IRecentFilesService RecentFiles { get; }

    /// <summary>Modal dialogs.</summary>
    public IDialogService Dialogs { get; }

    /// <summary>File pickers.</summary>
    public IFileDialogService FileDialogs { get; }

    /// <summary>Windows shell integration.</summary>
    public IShellService Shell { get; }

    /// <summary>Light and dark theme.</summary>
    public IThemeService Theme { get; }

    /// <summary>Printing.</summary>
    public IPrintService Print { get; }

    /// <summary>Opening of PDF documents.</summary>
    public IDocumentService Documents { get; }

    /// <summary>Checking, downloading and installing of HyperPDF updates.</summary>
    public IUpdateService Updates { get; }

    /// <summary>Creates the default service container.</summary>
    public static AppServices CreateDefault()
    {
        ISettingsService settings = new SettingsService();
        IRecentFilesService recentFiles = new RecentFilesService(settings);
        IDialogService dialogs = new DialogService();
        IFileDialogService fileDialogs = new FileDialogService();
        IShellService shell = new ShellService();
        IThemeService theme = new ThemeService();
        IPrintService print = new PrintService();
        IDocumentService documents = new DocumentService(settings, print, dialogs);
        IUpdateService updates = new UpdateService();

        var services = new AppServices(settings, recentFiles, dialogs, fileDialogs, shell, theme, print, documents, updates);
        Current = services;
        return services;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        PdfiumLibrary.Instance.Dispose();
    }
}
