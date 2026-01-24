using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        Console.WriteLine("PDF_CHECKPOINT: 1 - Starting Stitch-Only Service");
        using var outputDocument = new PdfDocument();
        
        try 
        {
            // We are skipping the Ledger page entirely to avoid Font dependencies.
            // We are going straight to the attachments.
            
            int fileCount = 0;
            foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
            {
                fileCount++;
                Console.WriteLine($"PDF_CHECKPOINT: 2 - Stitching file {fileCount} for {r.Provider}");
                
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
                        
                        // Scale to fit page width
                        double width = imgPage.Width.Point;
                        double height = (width / img.PixelWidth) * img.PixelHeight;
                        
                        // Limit height to avoid overflow
                        if (height > imgPage.Height.Point) height = imgPage.Height.Point;
                        
                        imgGfx.DrawImage(img, 0, 0, width, height);
                    }
                }
                catch (Exception fileEx) 
                { 
                    Console.WriteLine($"PDF_CHECKPOINT: ERROR on file {fileCount} - {fileEx.Message}");
                }
            }

            if (outputDocument.PageCount == 0)
            {
                // If no files were selected, we have to add one blank page 
                // because a PDF with 0 pages is invalid.
                outputDocument.AddPage();
            }

            Console.WriteLine("PDF_CHECKPOINT: 3 - Saving final document");
            using var finalMs = new MemoryStream();
            outputDocument.Save(finalMs);
            return finalMs.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF_CRASH: {ex.Message}");
            throw; 
        }
    }
}