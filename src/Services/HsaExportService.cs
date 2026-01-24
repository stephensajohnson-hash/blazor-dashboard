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
        
        // 1. GENERATE THE LEDGER PAGE
        var page = outputDocument.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        
        // Use 'Arial' or 'Helvetica' - Linux environments usually map these to DejaVuSans
        var fontTitle = new XFont("Arial", 18, XFontStyleEx.Bold);
        var fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
        var fontRegular = new XFont("Arial", 10, XFontStyleEx.Regular);

        double yPos = 40;
        gfx.DrawString("HSA Reimbursement Submission", fontTitle, XBrushes.Blue, new XPoint(40, yPos));
        yPos += 25;
        gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", fontRegular, XBrushes.Gray, new XPoint(40, yPos));
        yPos += 40;

        // Table Headers
        gfx.DrawString("Date", fontHeader, XBrushes.Black, new XPoint(40, yPos));
        gfx.DrawString("Provider", fontHeader, XBrushes.Black, new XPoint(120, yPos));
        gfx.DrawString("Category", fontHeader, XBrushes.Black, new XPoint(300, yPos));
        gfx.DrawString("Amount", fontHeader, XBrushes.Black, new XPoint(500, yPos));
        yPos += 5;
        gfx.DrawLine(XPens.Black, 40, yPos, 550, yPos);
        yPos += 15;

        foreach (var r in receipts)
        {
            gfx.DrawString(r.ServiceDate.ToString("yyyy-MM-dd"), fontRegular, XBrushes.Black, new XPoint(40, yPos));
            
            // Null-safe provider text
            string provText = r.Provider ?? "---";
            if (provText.Length > 25) provText = provText.Substring(0, 22) + "...";
            gfx.DrawString(provText, fontRegular, XBrushes.Black, new XPoint(120, yPos));
            
            gfx.DrawString(r.Type ?? "Other", fontRegular, XBrushes.Black, new XPoint(300, yPos));
            gfx.DrawString($"${r.Amount:N2}", fontRegular, XBrushes.Black, new XPoint(500, yPos));
            
            yPos += 20;
            if (yPos > 750) { page = outputDocument.AddPage(); gfx = XGraphics.FromPdfPage(page); yPos = 40; }
        }

        yPos += 10;
        gfx.DrawLine(XPens.Black, 40, yPos, 550, yPos);
        yPos += 20;
        gfx.DrawString("Total Submission:", fontHeader, XBrushes.Black, new XPoint(380, yPos));
        gfx.DrawString($"${receipts.Sum(x => x.Amount):N2}", fontHeader, XBrushes.DarkGreen, new XPoint(500, yPos));

        // 2. APPEND ATTACHMENTS
        foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
        {
            try 
            {
                // PDF HANDLING
                if (r.ContentType != null && r.ContentType.ToLower().Contains("pdf"))
                {
                    using var ms = new MemoryStream(r.FileData!);
                    var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < attachment.PageCount; i++) 
                    {
                        outputDocument.AddPage(attachment.Pages[i]);
                    }
                }
                // IMAGE HANDLING
                else if (r.ContentType != null && r.ContentType.ToLower().Contains("image"))
                {
                    var imgPage = outputDocument.AddPage();
                    using var ms = new MemoryStream(r.FileData!);
                    using var img = XImage.FromStream(ms);
                    var imgGfx = XGraphics.FromPdfPage(imgPage);
                    
                    double width = imgPage.Width.Point;
                    double height = (width / img.PixelWidth) * img.PixelHeight;
                    
                    // Cap height to page height
                    if (height > imgPage.Height.Point) height = imgPage.Height.Point - 40;
                    
                    imgGfx.DrawImage(img, 0, 0, width, height);
                }
            }
            catch (Exception ex) 
            { 
                // Don't kill the whole export for one bad image
                Console.WriteLine($"FILE_APPEND_ERROR: {ex.Message}");
            }
        }

        using var finalMs = new MemoryStream();
        outputDocument.Save(finalMs);
        return finalMs.ToArray();
    }
}