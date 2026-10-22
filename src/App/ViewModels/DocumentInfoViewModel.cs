// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;

namespace HyperPDF.ViewModels;

/// <summary>View model of the document properties dialog.</summary>
public sealed class DocumentInfoViewModel : ViewModelBase
{
    /// <summary>Creates the view model for a document.</summary>
    public DocumentInfoViewModel(DocumentInfo info)
    {
        Info = info;

        var rows = new List<DocumentInfoRow>
        {
            new("File name", info.FileName),
            new("Location", info.FilePath),
            new("Size", FormatSize(info.FileSize)),
            new("Last modified", info.LastModified?.ToString("u") ?? "unknown"),
            new("Pages", info.PageCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("PDF version", info.PdfVersion),
            new("Title", info.Title),
            new("Author", info.Author),
            new("Subject", info.Subject),
            new("Keywords", info.Keywords),
            new("Creator", info.Creator),
            new("Producer", info.Producer),
            new("Created", info.CreationDate),
            new("Modified", info.ModDate),
            new("File identifier", info.FileId),
            new("Encrypted", info.IsEncrypted
                ? $"yes (revision {info.SecurityHandlerRevision})"
                : "no")
        };

        Rows = rows;
    }

    /// <summary>The raw document information.</summary>
    public DocumentInfo Info { get; }

    /// <summary>The rows shown in the dialog.</summary>
    public IReadOnlyList<DocumentInfoRow> Rows { get; }

    /// <summary>True when the document is encrypted.</summary>
    public bool IsEncrypted => Info.IsEncrypted;

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return bytes + " bytes";
        }

        if (bytes < 1024 * 1024)
        {
            return (bytes / 1024d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
        }

        return (bytes / (1024d * 1024d)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MB";
    }
}

/// <summary>A single labelled value of the document properties dialog.</summary>
public sealed record DocumentInfoRow(string Label, string Value)
{
    /// <summary>True when the row carries a usable value.</summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
}
