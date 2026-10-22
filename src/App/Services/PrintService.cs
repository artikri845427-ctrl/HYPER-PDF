// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using HyperPDF.Services.Pdfium;
using PrintDialog = System.Windows.Forms.PrintDialog;

namespace HyperPDF.Services;

/// <summary>Prints PDF documents.</summary>
public interface IPrintService
{
    /// <summary>
    /// Shows the print dialog and prints the document.
    /// </summary>
    /// <param name="document">The document to print.</param>
    /// <param name="documentName">Name used for the print job.</param>
    /// <param name="error">A human readable error message when printing failed.</param>
    /// <returns><c>true</c> when the job was sent to the printer, <c>false</c> when it was cancelled.</returns>
    bool Print(PdfDocument document, string documentName, out string? error);
}

/// <summary>
/// Prints PDF pages with the Windows print system. The pages are rasterised with PDFium and
/// drawn onto the printer device context, which produces correct output on every printer
/// and with the "Microsoft Print to PDF" driver as well.
/// </summary>
public sealed class PrintService : IPrintService
{
    private const int MaxPrintPixels = 4000;

    /// <inheritdoc />
    public bool Print(PdfDocument document, string documentName, out string? error)
    {
        ArgumentNullException.ThrowIfNull(document);
        error = null;

        try
        {
            using var printDialog = new PrintDialog
            {
                AllowSomePages = true,
                AllowSelection = false,
                AllowPrintToFile = true
            };

            if (printDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            IReadOnlyList<int> pages = ResolvePages(printDialog.PrinterSettings, document.PageCount);
            if (pages.Count == 0)
            {
                error = "The selected print range does not contain any page.";
                return false;
            }

            using var printDocument = new PrintDocument
            {
                PrinterSettings = printDialog.PrinterSettings,
                DocumentName = documentName,
                OriginAtMargins = false
            };

            int counter = 0;
            printDocument.PrintPage += (_, args) =>
            {
                if (counter >= pages.Count)
                {
                    args.Cancel = true;
                    return;
                }

                int pageNumber = pages[counter++];
                RenderPage(document, pageNumber - 1, args);
            };

            printDocument.Print();
            return true;
        }
        catch (Exception ex) when (ex is InvalidPrinterException or System.ComponentModel.Win32Exception)
        {
            error = "The selected printer is not available. Check the printer and try again.";
            return false;
        }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or InvalidOperationException)
        {
            error = "The document could not be printed: " + ex.Message;
            return false;
        }
    }

    private static IReadOnlyList<int> ResolvePages(PrinterSettings settings, int pageCount)
    {
        var pages = new List<int>();

        if (settings.PrintRange == PrintRange.SomePages)
        {
            int start = Math.Max(1, settings.FromPage);
            int end = Math.Min(pageCount, settings.ToPage);
            for (int page = start; page <= end; page++)
            {
                pages.Add(page);
            }
        }
        else
        {
            for (int page = 1; page <= pageCount; page++)
            {
                pages.Add(page);
            }
        }

        return pages;
    }

    private static void RenderPage(PdfDocument document, int pageIndex, PrintPageEventArgs args)
    {
        Graphics? graphics = args.Graphics;
        if (graphics is null)
        {
            args.Cancel = true;
            return;
        }

        var (pageWidth, pageHeight) = document.GetPageSize(pageIndex);

        float dpi = graphics.DpiX;
        if (dpi <= 0 || float.IsNaN(dpi))
        {
            dpi = 96f;
        }

        int pixelWidth = (int)Math.Min(MaxPrintPixels, Math.Ceiling(pageWidth * dpi / 72f));
        int pixelHeight = (int)Math.Min(MaxPrintPixels, Math.Ceiling(pageHeight * dpi / 72f));
        pixelWidth = Math.Max(1, pixelWidth);
        pixelHeight = Math.Max(1, pixelHeight);

        RenderedPage rendered = PdfiumRenderer.Render(document, pageIndex, pixelWidth, pixelHeight);

        using Bitmap bitmap = ToDrawingBitmap(rendered);
        var destination = FitInside(args.MarginBounds, pageWidth, pageHeight);
        graphics.DrawImage(bitmap, destination);
    }

    private static Rectangle FitInside(Rectangle bounds, float contentWidth, float contentHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return bounds;
        }

        double scale = Math.Min(bounds.Width / contentWidth, bounds.Height / contentHeight);
        int width = (int)Math.Max(1, Math.Floor(contentWidth * scale));
        int height = (int)Math.Max(1, Math.Floor(contentHeight * scale));

        int x = bounds.X + ((bounds.Width - width) / 2);
        int y = bounds.Y + ((bounds.Height - height) / 2);
        return new Rectangle(x, y, width, height);
    }

    private static Bitmap ToDrawingBitmap(RenderedPage page)
    {
        var bitmap = new Bitmap(page.Width, page.Height, PixelFormat.Format32bppRgb);
        var rect = new Rectangle(0, 0, page.Width, page.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
        try
        {
            int stride = page.Width * 4;
            for (int y = 0; y < page.Height; y++)
            {
                IntPtr row = IntPtr.Add(data.Scan0, y * data.Stride);
                Marshal.Copy(page.Pixels, y * stride, row, Math.Min(stride, data.Stride));
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }
}
