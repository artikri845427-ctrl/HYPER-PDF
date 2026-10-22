// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Services.Pdfium;

/// <summary>
/// Owns the global PDFium library lifetime. PDFium must be initialised exactly once per
/// process before any other call and destroyed when the process shuts down.
/// </summary>
public sealed class PdfiumLibrary : IDisposable
{
    private static readonly object Gate = new();
    private static int _referenceCount;
    private static bool _initialized;

    private bool _disposed;

    /// <summary>The process wide instance used by HyperPDF.</summary>
    public static PdfiumLibrary Instance { get; } = new();

    /// <summary>True when the native library has been initialised successfully.</summary>
    public bool IsInitialized
    {
        get
        {
            lock (Gate)
            {
                return _initialized;
            }
        }
    }

    /// <summary>Version of the bundled PDFium native library.</summary>
    public string EngineVersion => BrandInfo.PdfiumVersion;

    private PdfiumLibrary()
    {
    }

    /// <summary>Initialises PDFium if required. Safe to call from multiple threads.</summary>
    public void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            PdfiumNative.FPDF_InitLibrary();
            _initialized = true;
            _referenceCount++;
        }
    }

    /// <summary>Releases the PDFium library when the last user disposes.</summary>
    public void Release()
    {
        lock (Gate)
        {
            if (!_initialized)
            {
                return;
            }

            _referenceCount--;
            if (_referenceCount <= 0)
            {
                _referenceCount = 0;
                PdfiumNative.FPDF_DestroyLibrary();
                _initialized = false;
            }
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
        Release();
    }
}
