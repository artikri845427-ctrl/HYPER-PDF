// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Services.Pdfium;

/// <summary>Describes why a document could not be opened.</summary>
public enum PdfOpenStatus
{
    /// <summary>The document was opened successfully.</summary>
    Success = 0,

    /// <summary>The file does not exist or is not readable.</summary>
    FileNotFound,

    /// <summary>The file is not a valid PDF (or is damaged beyond repair).</summary>
    InvalidFormat,

    /// <summary>The document is encrypted and no (or no valid) password was supplied.</summary>
    PasswordRequired,

    /// <summary>PDFium reported an unexpected error.</summary>
    Unknown
}

/// <summary>Raised when a PDF document cannot be opened or parsed.</summary>
public sealed class PdfOpenException : Exception
{
    /// <summary>Creates a new instance.</summary>
    public PdfOpenException(PdfOpenStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    /// <summary>Creates a new instance with an inner exception.</summary>
    public PdfOpenException(PdfOpenStatus status, string message, Exception innerException)
        : base(message, innerException)
    {
        Status = status;
    }

    /// <summary>Machine readable reason of the failure.</summary>
    public PdfOpenStatus Status { get; }
}
