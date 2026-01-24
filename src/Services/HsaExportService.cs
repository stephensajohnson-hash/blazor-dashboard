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
        
        try 
        {
            // We skip the ledger page entirely to avoid Font dependencies on Linux.
            foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
            {
                try 
                {
                    if (r.ContentType != null && r.ContentType.ToLower().Contains("pdf"))
                    {
                        using var ms = new MemoryStream(r.FileData!);
                        var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                        for (int i = 0; i < attachment.PageCount; i++) 
                        {
                            outputDocument.AddPage(attachment.Pages[i]);
                        }
                    }
                    else if (r.ContentType != null && r.ContentType.ToLower().Contains("image"))
                    {
                        var imgPage = outputDocument.AddPage();
                        using var ms = new MemoryStream(r.FileData!);
                        using var img = XImage.FromStream(ms);
                        var imgGfx = XGraphics.FromPdfPage(imgPage);
                        
                        double width = imgPage.Width.Point;
                        double height = (width / img.PixelWidth) * img.PixelHeight;
                        
                        // Limit height to page boundaries
                        if (height > imgPage.Height.Point) height = imgPage.Height.Point;
                        
                        imgGfx.DrawImage(img, 0, 0, width, height);
                    }
                }
                catch (Exception fileEx) 
                { 
                    Console.WriteLine($"STITCH_ERROR for {r.Provider}: {fileEx.Message}");
                }
            }

            // PDF requires at least one page to be valid
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