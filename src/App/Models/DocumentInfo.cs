// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>Information shown in the "Document properties" dialog.</summary>
public sealed class DocumentInfo
{
    /// <summary>Full path of the document.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File name of the document.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Size of the file in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Last write time of the file.</summary>
    public DateTime? LastModified { get; set; }

    /// <summary>Number of pages.</summary>
    public int PageCount { get; set; }

    /// <summary>PDF version, for example 1.7.</summary>
    public string PdfVersion { get; set; } = "unknown";

    /// <summary>Document title taken from the PDF info dictionary.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Document author taken from the PDF info dictionary.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Document subject taken from the PDF info dictionary.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Document keywords taken from the PDF info dictionary.</summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>Application that created the document.</summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>Application that produced the document.</summary>
    public string Producer { get; set; } = string.Empty;

    /// <summary>Creation date of the document.</summary>
    public string CreationDate { get; set; } = string.Empty;

    /// <summary>Modification date of the document.</summary>
    public string ModDate { get; set; } = string.Empty;

    /// <summary>Permanent file identifier of the document.</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>True when the document is encrypted.</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>Security handler revision (2, 3, 4 or 6) of an encrypted document.</summary>
    public int SecurityHandlerRevision { get; set; }
}
