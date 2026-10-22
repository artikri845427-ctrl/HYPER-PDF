// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyperPDF.Services.Pdfium;

/// <summary>
/// A rendered PDF page held in a managed BGRA buffer.
/// </summary>
public sealed class RenderedPage
{
    /// <summary>Creates a new rendered page.</summary>
    public RenderedPage(int width, int height, byte[] pixels, float pageWidthPoints, float pageHeightPoints)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
        PageWidthPoints = pageWidthPoints;
        PageHeightPoints = pageHeightPoints;
    }

    /// <summary>Width of the bitmap in pixels.</summary>
    public int Width { get; }

    /// <summary>Height of the bitmap in pixels.</summary>
    public int Height { get; }

    /// <summary>Raw BGRA pixels, top-down, stride = <see cref="Width"/> * 4.</summary>
    public byte[] Pixels { get; }

    /// <summary>Width of the source page in PDF points.</summary>
    public float PageWidthPoints { get; }

    /// <summary>Height of the source page in PDF points.</summary>
    public float PageHeightPoints { get; }

    /// <summary>Converts the buffer into a frozen <see cref="BitmapSource"/> ready for WPF.</summary>
    public BitmapSource ToBitmapSource()
    {
        var source = BitmapSource.Create(
            Width,
            Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            Pixels,
            Width * 4);

        source.Freeze();
        return source;
    }
}

/// <summary>Helpers that transform a <see cref="RenderedPage"/> before it is displayed.</summary>
public static class RenderedPageExtensions
{
    /// <summary>
    /// Draws a translucent highlight rectangle over the given device space rectangle.
    /// </summary>
    /// <param name="page">The rendered page that is modified in place.</param>
    /// <param name="left">Left edge in pixels.</param>
    /// <param name="top">Top edge in pixels.</param>
    /// <param name="right">Right edge in pixels.</param>
    /// <param name="bottom">Bottom edge in pixels.</param>
    /// <param name="r">Red component of the highlight colour.</param>
    /// <param name="g">Green component of the highlight colour.</param>
    /// <param name="b">Blue component of the highlight colour.</param>
    /// <param name="alpha">Opacity between 0 and 1.</param>
    public static void BlendHighlight(
        this RenderedPage page,
        int left,
        int top,
        int right,
        int bottom,
        byte r,
        byte g,
        byte b,
        double alpha)
    {
        int x0 = Math.Max(0, Math.Min(left, right));
        int x1 = Math.Min(page.Width, Math.Max(left, right));
        int y0 = Math.Max(0, Math.Min(top, bottom));
        int y1 = Math.Min(page.Height, Math.Max(top, bottom));

        if (x1 <= x0 || y1 <= y0)
        {
            return;
        }

        double a = Math.Clamp(alpha, 0, 1);
        int stride = page.Width * 4;
        byte[] pixels = page.Pixels;

        for (int y = y0; y < y1; y++)
        {
            int rowStart = y * stride;
            for (int x = x0; x < x1; x++)
            {
                int index = rowStart + (x * 4);
                byte dstB = pixels[index];
                byte dstG = pixels[index + 1];
                byte dstR = pixels[index + 2];

                pixels[index] = (byte)(dstB + ((b - dstB) * a));
                pixels[index + 1] = (byte)(dstG + ((g - dstG) * a));
                pixels[index + 2] = (byte)(dstR + ((r - dstR) * a));
            }
        }
    }
}

/// <summary>Renders PDF pages into managed bitmaps using PDFium.</summary>
public static class PdfiumRenderer
{
    /// <summary>
    /// Renders a single page.
    /// </summary>
    /// <param name="document">The document to render from.</param>
    /// <param name="pageIndex">Zero based page index.</param>
    /// <param name="pixelWidth">Target width in pixels.</param>
    /// <param name="pixelHeight">Target height in pixels.</param>
    public static RenderedPage Render(PdfDocument document, int pageIndex, int pixelWidth, int pixelHeight)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "The render size must be positive.");
        }

        lock (document.SyncRoot)
        {
            IntPtr page = PdfiumNative.FPDF_LoadPage(document.Handle, pageIndex);
            if (page == IntPtr.Zero)
            {
                throw new PdfOpenException(PdfOpenStatus.InvalidFormat, $"Page {pageIndex + 1} could not be rendered.");
            }

            IntPtr bitmap = IntPtr.Zero;
            try
            {
                float pageWidth = PdfiumNative.FPDF_GetPageWidthF(page);
                float pageHeight = PdfiumNative.FPDF_GetPageHeightF(page);

                // alpha = 1 -> BGRA bitmap, which keeps the page opaque white instead of transparent.
                bitmap = PdfiumNative.FPDFBitmap_Create(pixelWidth, pixelHeight, 1);
                if (bitmap == IntPtr.Zero)
                {
                    throw new PdfOpenException(PdfOpenStatus.Unknown, "The PDFium bitmap could not be created.");
                }

                PdfiumNative.FPDFBitmap_FillRect(bitmap, 0, 0, pixelWidth, pixelHeight, 0xFFFFFFFF);

                PdfiumNative.FPDF_RenderPageBitmap(
                    bitmap,
                    page,
                    0,
                    0,
                    pixelWidth,
                    pixelHeight,
                    0,
                    PdfiumNative.FPDF_ANNOT);

                IntPtr buffer = PdfiumNative.FPDFBitmap_GetBuffer(bitmap);
                int stride = PdfiumNative.FPDFBitmap_GetStride(bitmap);
                if (buffer == IntPtr.Zero || stride < pixelWidth * 4)
                {
                    throw new PdfOpenException(PdfOpenStatus.Unknown, "The PDFium bitmap buffer is invalid.");
                }

                var pixels = new byte[stride * pixelHeight];
                Marshal.Copy(buffer, pixels, 0, pixels.Length);

                if (stride != pixelWidth * 4)
                {
                    pixels = CompactRows(pixels, stride, pixelWidth, pixelHeight);
                }

                return new RenderedPage(pixelWidth, pixelHeight, pixels, pageWidth, pageHeight);
            }
            finally
            {
                if (bitmap != IntPtr.Zero)
                {
                    PdfiumNative.FPDFBitmap_Destroy(bitmap);
                }

                PdfiumNative.FPDF_ClosePage(page);
            }
        }
    }

    private static byte[] CompactRows(byte[] source, int stride, int width, int height)
    {
        var result = new byte[width * 4 * height];
        int targetStride = width * 4;
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(source, y * stride, result, y * targetStride, targetStride);
        }

        return result;
    }
}
