// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Globalization;
using System.Text;

namespace HyperPDF.Services.Pdfium;

/// <summary>
/// A PDF document backed by a native PDFium <c>FPDF_DOCUMENT</c> handle.
/// </summary>
/// <remarks>
/// PDFium documents are not thread safe. Every native call issued through this class is
/// serialised by <see cref="SyncRoot"/>; higher layers (rendering, text search, printing)
/// must hold the same lock for the duration of a native operation.
/// </remarks>
public sealed class PdfDocument : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, (float Width, float Height)> _pageSizes = new();
    private IntPtr _handle;
    private bool _disposed;

    private PdfDocument(IntPtr handle, string filePath, int pageCount, int pdfVersion, bool isEncrypted)
    {
        _handle = handle;
        FilePath = filePath;
        PageCount = pageCount;
        PdfVersion = pdfVersion;
        IsEncrypted = isEncrypted;
    }

    /// <summary>Full path of the document on disk.</summary>
    public string FilePath { get; }

    /// <summary>File name (without directory) of the document.</summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>Number of pages in the document.</summary>
    public int PageCount { get; }

    /// <summary>PDF version of the document, for example 1.7 or 2.0.</summary>
    public int PdfVersion { get; }

    /// <summary>True when the document is protected by an encryption handler.</summary>
    public bool IsEncrypted { get; }

    /// <summary>Object that must be held while issuing native calls against this document.</summary>
    public object SyncRoot => _syncRoot;

    /// <summary>Raw native document handle. Internal use only.</summary>
    internal IntPtr Handle => _handle;

    /// <summary>True when the native handle is still valid.</summary>
    public bool IsOpen => !_disposed && _handle != IntPtr.Zero;

    /// <summary>
    /// Opens a document from disk.
    /// </summary>
    /// <param name="path">Absolute path of the PDF file.</param>
    /// <param name="password">Optional password for encrypted documents.</param>
    /// <exception cref="PdfOpenException">The document could not be opened.</exception>
    public static PdfDocument Open(string path, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new PdfOpenException(PdfOpenStatus.FileNotFound, "No file path was supplied.");
        }

        if (!File.Exists(path))
        {
            throw new PdfOpenException(PdfOpenStatus.FileNotFound, $"The file '{path}' does not exist.");
        }

        PdfiumLibrary.Instance.EnsureInitialized();

        IntPtr handle;
        try
        {
            handle = PdfiumNative.FPDF_LoadDocument(path, password ?? string.Empty);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            throw new PdfOpenException(
                PdfOpenStatus.Unknown,
                "The PDFium engine (pdfium.dll) could not be loaded. Please reinstall HyperPDF.",
                ex);
        }

        if (handle == IntPtr.Zero)
        {
            uint lastError = PdfiumNative.FPDF_GetLastError();
            throw new PdfOpenException(MapError(lastError, password), BuildErrorMessage(lastError, password, path));
        }

        int pageCount = PdfiumNative.FPDF_GetPageCount(handle);
        if (pageCount <= 0)
        {
            PdfiumNative.FPDF_CloseDocument(handle);
            throw new PdfOpenException(
                PdfOpenStatus.InvalidFormat,
                $"'{Path.GetFileName(path)}' does not contain any pages.");
        }

        PdfiumNative.FPDF_GetFileVersion(handle, out int version);
        int revision = PdfiumNative.FPDF_GetSecurityHandlerRevision(handle);
        bool encrypted = revision > 0;

        return new PdfDocument(handle, path, pageCount, version, encrypted);
    }

    private static PdfOpenStatus MapError(uint lastError, string? password)
    {
        return lastError switch
        {
            PdfiumNative.FPDF_ERR_PASSWORD => PdfOpenStatus.PasswordRequired,
            PdfiumNative.FPDF_ERR_FORMAT => PdfOpenStatus.InvalidFormat,
            PdfiumNative.FPDF_ERR_FILE => PdfOpenStatus.FileNotFound,
            _ => PdfOpenStatus.Unknown
        };
    }

    private static string BuildErrorMessage(uint lastError, string? password, string path)
    {
        string name = Path.GetFileName(path);
        return lastError switch
        {
            PdfiumNative.FPDF_ERR_PASSWORD when string.IsNullOrEmpty(password) =>
                $"'{name}' is password protected. Enter the document password to open it.",
            PdfiumNative.FPDF_ERR_PASSWORD =>
                $"The password for '{name}' is incorrect.",
            PdfiumNative.FPDF_ERR_FORMAT =>
                $"'{name}' is not a valid PDF file or the file is damaged.",
            PdfiumNative.FPDF_ERR_FILE =>
                $"'{name}' could not be read. The file may be in use or access was denied.",
            _ =>
                $"'{name}' could not be opened (PDFium error {lastError})."
        };
    }

    /// <summary>Returns the size of a page in PDF points (1/72 inch).</summary>
    public (float Width, float Height) GetPageSize(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        lock (_syncRoot)
        {
            if (_pageSizes.TryGetValue(pageIndex, out var cached))
            {
                return cached;
            }

            var size = default(PdfiumNative.FS_SIZEF);
            if (!PdfiumNative.FPDF_GetPageSizeByIndexF(_handle, pageIndex, out size) || size.width <= 0 || size.height <= 0)
            {
                // Fall back to loading the page explicitly.
                IntPtr page = PdfiumNative.FPDF_LoadPage(_handle, pageIndex);
                if (page == IntPtr.Zero)
                {
                    throw new PdfOpenException(PdfOpenStatus.InvalidFormat, $"Page {pageIndex + 1} could not be loaded.");
                }

                try
                {
                    size = new PdfiumNative.FS_SIZEF
                    {
                        width = PdfiumNative.FPDF_GetPageWidthF(page),
                        height = PdfiumNative.FPDF_GetPageHeightF(page)
                    };
                }
                finally
                {
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }

            var result = (size.width, size.height);
            _pageSizes[pageIndex] = result;
            return result;
        }
    }

    /// <summary>Returns the label of a page (for example "iv" or "A-12") when the document defines one.</summary>
    public string GetPageLabel(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= PageCount)
        {
            return string.Empty;
        }

        lock (_syncRoot)
        {
            var buffer = new ushort[64];
            uint written = PdfiumNative.FPDF_GetPageLabel(_handle, pageIndex, buffer, (uint)(buffer.Length * sizeof(ushort)));
            if (written == 0)
            {
                return string.Empty;
            }

            int length = (int)Math.Min(written, buffer.Length) - 1;
            if (length <= 0)
            {
                return string.Empty;
            }

            var characters = new char[length];
            for (int i = 0; i < length; i++)
            {
                characters[i] = (char)buffer[i];
            }

            return new string(characters);
        }
    }

    /// <summary>Returns the permanent file identifier of the document as a hexadecimal string.</summary>
    public string GetFileIdentifier()
    {
        lock (_syncRoot)
        {
            var buffer = new byte[64];
            uint written = PdfiumNative.FPDF_GetFileIdentifier(
                _handle,
                PdfiumNative.FPDF_FILEIDTYPE.FILEIDTYPE_PERMANENT,
                buffer,
                (uint)buffer.Length);

            if (written == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder((int)written * 2);
            for (int i = 0; i < written && i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("X2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }

    /// <summary>Returns the encryption handler revision, or 0 when the document is not encrypted.</summary>
    public int GetSecurityHandlerRevision()
    {
        lock (_syncRoot)
        {
            return PdfiumNative.FPDF_GetSecurityHandlerRevision(_handle);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_syncRoot)
        {
            if (_handle != IntPtr.Zero)
            {
                PdfiumNative.FPDF_CloseDocument(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
