// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Runtime.InteropServices;

namespace HyperPDF.Services.Pdfium;

/// <summary>
/// Raw P/Invoke bindings for the subset of the PDFium C API used by HyperPDF.
/// </summary>
/// <remarks>
/// <para>
/// The bindings target the PDFium build shipped in the <c>bblanchon.PDFium.Win32</c>
/// NuGet package, version 158.0.8111 (identical to the <c>chromium/8111</c> release of
/// <c>bblanchon/pdfium-binaries</c>). Every declaration below mirrors the public headers
/// of that build, which are shipped inside the very same NuGet package.
/// </para>
/// <para>
/// PDFium declares its exported functions with <c>__stdcall</c> on Windows
/// (<c>FPDF_CALLCONV</c>), therefore every import uses
/// <see cref="CallingConvention.StdCall"/> and <see cref="ExactSpelling"/> so that the CLR
/// never probes for an <c>A</c>/<c>W</c> suffixed variant.
/// </para>
/// </remarks>
internal static class PdfiumNative
{
    /// <summary>Name of the native library (pdfium.dll) as it is deployed next to the executable.</summary>
    internal const string LibraryName = "pdfium";

    // ---------------------------------------------------------------------
    // Constants
    // ---------------------------------------------------------------------

    /// <summary>Render annotations and form fields (FPDF_ANNOT).</summary>
    internal const int FPDF_ANNOT = 0x01;

    /// <summary>Bitmap format: 32 bit BGR with alpha (FPDFBitmap_BGRA).</summary>
    internal const int FPDFBitmap_BGRA = 4;

    /// <summary>FPDF_ERR_SUCCESS.</summary>
    internal const uint FPDF_ERR_SUCCESS = 0;

    /// <summary>FPDF_ERR_PASSWORD - the document requires a password.</summary>
    internal const uint FPDF_ERR_PASSWORD = 4;

    /// <summary>FPDF_ERR_FORMAT - the file is not a valid PDF.</summary>
    internal const uint FPDF_ERR_FORMAT = 3;

    /// <summary>FPDF_ERR_FILE - the file could not be opened.</summary>
    internal const uint FPDF_ERR_FILE = 2;

    // ---------------------------------------------------------------------
    // Structures
    // ---------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    internal struct FS_SIZEF
    {
        public float width;
        public float height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FS_RECTF
    {
        public float left;
        public float top;
        public float right;
        public float bottom;
    }

    /// <summary>Identifier flavour requested from <c>FPDF_GetFileIdentifier</c>.</summary>
    internal enum FPDF_FILEIDTYPE
    {
        FILEIDTYPE_PERMANENT = 0,
        FILEIDTYPE_CHANGING = 1
    }

    // ---------------------------------------------------------------------
    // Library lifetime
    // ---------------------------------------------------------------------

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDF_InitLibrary();

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDF_DestroyLibrary();

    // ---------------------------------------------------------------------
    // Document life cycle
    // ---------------------------------------------------------------------

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern IntPtr FPDF_LoadDocument([MarshalAs(UnmanagedType.LPUTF8Str)] string file_path, [MarshalAs(UnmanagedType.LPStr)] string password);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern int FPDF_GetPageCount(IntPtr document);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern float FPDF_GetPageWidthF(IntPtr page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern float FPDF_GetPageHeightF(IntPtr page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDF_GetPageSizeByIndexF(IntPtr document, int page_index, out FS_SIZEF size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDF_GetPageBoundingBox(IntPtr page, out FS_RECTF rect);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDF_GetFileVersion(IntPtr document, out int file_version);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern int FPDF_GetSecurityHandlerRevision(IntPtr document);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern uint FPDF_GetLastError();

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern uint FPDF_GetFileIdentifier(IntPtr document, FPDF_FILEIDTYPE id_type, byte[] buffer, uint buflen);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern uint FPDF_GetPageLabel(IntPtr document, int page_index, ushort[] buffer, uint buflen);

    // ---------------------------------------------------------------------
    // Bitmap / rendering
    // ---------------------------------------------------------------------

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern IntPtr FPDFBitmap_CreateEx(int width, int height, int format, IntPtr first_scan, int stride);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags);

    /// <summary>
    /// Renders a page directly into a GDI device context. Used by the print pipeline.
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDF_RenderPage(IntPtr hdc, IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags);

    // ---------------------------------------------------------------------
    // Text extraction
    // ---------------------------------------------------------------------

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern IntPtr FPDFText_LoadPage(IntPtr page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern void FPDFText_ClosePage(IntPtr text_page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern int FPDFText_CountChars(IntPtr text_page);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern int FPDFText_GetText(IntPtr text_page, int start_index, int count, ushort[] result);

    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    internal static extern bool FPDFText_GetCharBox(IntPtr text_page, int index, out double left, out double right, out double bottom, out double top);
}
