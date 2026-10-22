// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>An entry of the "recent documents" list.</summary>
public sealed class RecentFileEntry
{
    /// <summary>Creates an empty entry (required for JSON deserialisation).</summary>
    public RecentFileEntry()
    {
    }

    /// <summary>Creates a new entry.</summary>
    public RecentFileEntry(string filePath, DateTime openedUtc, long sizeInBytes)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        OpenedUtc = openedUtc;
        SizeInBytes = sizeInBytes;
    }

    /// <summary>Full path of the document.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File name of the document.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Point in time the document was opened for the last time (UTC).</summary>
    public DateTime OpenedUtc { get; set; }

    /// <summary>Size of the file in bytes.</summary>
    public long SizeInBytes { get; set; }

    /// <summary>Creates a copy of this entry.</summary>
    public RecentFileEntry Clone() => new(FilePath, OpenedUtc, SizeInBytes)
    {
        FileName = FileName
    };
}
