// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;

namespace HyperPDF.Services;

/// <summary>Maintains the list of recently opened documents.</summary>
public interface IRecentFilesService
{
    /// <summary>The recent documents, newest first.</summary>
    IReadOnlyList<RecentFileEntry> Items { get; }

    /// <summary>Adds (or refreshes) a document in the list.</summary>
    void Add(string filePath);

    /// <summary>Removes a single document from the list.</summary>
    void Remove(string filePath);

    /// <summary>Removes every document from the list.</summary>
    void Clear();

    /// <summary>Raised whenever the list changed.</summary>
    event EventHandler? Changed;
}

/// <summary>Stores the recent documents inside the application settings.</summary>
public sealed class RecentFilesService : IRecentFilesService
{
    private readonly ISettingsService _settingsService;

    /// <summary>Creates the service.</summary>
    public RecentFilesService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <inheritdoc />
    public IReadOnlyList<RecentFileEntry> Items => _settingsService.Settings.RecentFiles;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void Add(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return;
        }

        var list = _settingsService.Settings.RecentFiles;
        list.RemoveAll(e => string.Equals(e.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

        long size = 0;
        try
        {
            var info = new FileInfo(fullPath);
            if (info.Exists)
            {
                size = info.Length;
            }
        }
        catch (IOException)
        {
            size = 0;
        }

        list.Insert(0, new RecentFileEntry(fullPath, DateTime.UtcNow, size));

        while (list.Count > AppSettings.MaxRecentFiles)
        {
            list.RemoveAt(list.Count - 1);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Remove(string filePath)
    {
        int removed = _settingsService.Settings.RecentFiles.RemoveAll(
            e => string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (removed > 0)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_settingsService.Settings.RecentFiles.Count == 0)
        {
            return;
        }

        _settingsService.Settings.RecentFiles.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
