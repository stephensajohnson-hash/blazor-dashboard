using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        Console.WriteLine("PDF_CHECKPOINT: 1 - Starting Service");
        using var outputDocument = new PdfDocument();
        
        try 
        {
            // 1. GENERATE THE LEDGER PAGE
            Console.WriteLine("PDF_CHECKPOINT: 2 - Creating Ledger Page");
            var page = outputDocument.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            
            // Standard fonts do not require a FontResolver - this bypasses the Linux font issue
            var fontTitle = new XFont("Helvetica", 18, XFontStyleEx.Bold);
            var fontHeader = new XFont("Helvetica", 10, XFontStyleEx.Bold);
            var fontRegular = new XFont("Helvetica", 10, XFontStyleEx.Regular);

            double yPos = 40;
            gfx.DrawString("HSA Reimbursement Submission", fontTitle, XBrushes.Blue, new XPoint(40, yPos));
            yPos += 40;

            Console.WriteLine("PDF_CHECKPOINT: 3 - Drawing Headers");
            gfx.DrawString("Date", fontHeader, XBrushes.Black, new XPoint(40, yPos));
            gfx.DrawString("Provider", fontHeader, XBrushes.Black, new XPoint(120, yPos));
            gfx.DrawString("Amount", fontHeader, XBrushes.Black, new XPoint(500, yPos));
            yPos += 20;

            foreach (var r in receipts)
            {
                Console.WriteLine($"PDF_CHECKPOINT: 4 - Drawing Row for {r.Id}");
                string dateStr = r.ServiceDate.ToString("yyyy-MM-dd");
                string provStr = r.Provider ?? "Unknown";
                string amtStr = $"${r.Amount:N2}";

                gfx.DrawString(dateStr, fontRegular, XBrushes.Black, new XPoint(40, yPos));
                gfx.DrawString(provStr, fontRegular, XBrushes.Black, new XPoint(120, yPos));
                gfx.DrawString(amtStr, fontRegular, XBrushes.Black, new XPoint(500, yPos));
                yPos += 20;
            }

            // 2. APPEND ATTACHMENTS
            Console.WriteLine("PDF_CHECKPOINT: 5 - Starting Attachments");
            foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
            {
                Console.WriteLine($"PDF_CHECKPOINT: 6 - Processing File for {r.Provider}");
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
                        imgGfx.DrawImage(img, 0, 0, imgPage.Width.Point, 400); // Fixed height for testing
                    }
                }
                catch (Exception fileEx) 
                { 
                    Console.WriteLine($"PDF_CHECKPOINT: FILE_ERROR on {r.Id} - {fileEx.Message}");
                }
            }

            Console.WriteLine("PDF_CHECKPOINT: 7 - Saving to Stream");
            using var finalMs = new MemoryStream();
            outputDocument.Save(finalMs);
            return finalMs.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF_CRASH: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw; 
        }
    }
}