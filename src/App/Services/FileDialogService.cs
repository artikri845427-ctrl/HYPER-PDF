// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using Microsoft.Win32;

namespace HyperPDF.Services;

/// <summary>Shows the native file pickers.</summary>
public interface IFileDialogService
{
    /// <summary>Shows an open dialog and returns the selected file, or <c>null</c>.</summary>
    string? OpenPdf(string? initialDirectory = null);

    /// <summary>Shows an open dialog that allows multiple selection.</summary>
    IReadOnlyList<string> OpenPdfs(string? initialDirectory = null);
}

/// <summary>Default <see cref="IFileDialogService"/> implementation built on the WPF common dialogs.</summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? OpenPdf(string? initialDirectory = null)
    {
        IReadOnlyList<string> files = OpenPdfs(initialDirectory, allowMultiple: false);
        return files.Count > 0 ? files[0] : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> OpenPdfs(string? initialDirectory = null) => OpenPdfs(initialDirectory, allowMultiple: true);

    private static IReadOnlyList<string> OpenPdfs(string? initialDirectory, bool allowMultiple)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open PDF document",
            Filter = "PDF documents (*.pdf)|*.pdf|All files (*.*)|*.*",
            DefaultExt = ".pdf",
            CheckFileExists = true,
            Multiselect = allowMultiple,
            AddExtension = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FileNames : Array.Empty<string>();
    }
}
