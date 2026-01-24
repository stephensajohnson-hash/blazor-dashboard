using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        using var outputDocument = new PdfDocument();
        // Enable memory optimization
        outputDocument.Options.ColorMode = PdfColorMode.Rgb;
        outputDocument.Options.CompressContentStreams = true;

        try 
        {
            foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
            {
                try 
                {
                    if (r.ContentType != null && r.ContentType.ToLower().Contains("pdf"))
                    {
                        using var ms = new MemoryStream(r.FileData!);
                        using var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                        foreach (var page in attachment.Pages)
                        {
                            outputDocument.AddPage(page);
                        }
                    }
                    else if (r.ContentType != null && r.ContentType.ToLower().Contains("image"))
                    {
                        using var ms = new MemoryStream(r.FileData!);
                        // Using XImage.FromStream and disposing it quickly
                        using var img = XImage.FromStream(ms);
                        var imgPage = outputDocument.AddPage();
                        using var imgGfx = XGraphics.FromPdfPage(imgPage);
                        
                        double width = imgPage.Width.Point;
                        double height = (width / img.PixelWidth) * img.PixelHeight;
                        
                        if (height > imgPage.Height.Point) height = imgPage.Height.Point;
                        
                        imgGfx.DrawImage(img, 0, 0, width, height);
                    }
                }
                catch (Exception fileEx) 
                { 
                    Console.WriteLine($"STITCH_ERROR: {fileEx.Message}");
                }
            }

            if (outputDocument.PageCount == 0) outputDocument.AddPage();

            using var finalMs = new MemoryStream();
            outputDocument.Save(finalMs);
            return finalMs.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL_STITCH_CRASH: {ex.Message}");
            throw; 
        }
    }
}