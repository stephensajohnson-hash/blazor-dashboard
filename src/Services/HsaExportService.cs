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
        
        // 1. GENERATE THE LEDGER PAGE MANUALLY (Fast, no Browser needed)
        var page = outputDocument.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        
        var fontTitle = new XFont("Verdana", 18, XFontStyleEx.Bold);
        var fontHeader = new XFont("Verdana", 10, XFontStyleEx.Bold);
        var fontRegular = new XFont("Verdana", 10, XFontStyleEx.Regular);

        double yPos = 40;
        gfx.DrawString("HSA Reimbursement Submission", fontTitle, XBrushes.Blue, new XPoint(40, yPos));
        yPos += 25;
        gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", fontRegular, XBrushes.Gray, new XPoint(40, yPos));
        yPos += 40;

        // Draw Table Headers
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
            gfx.DrawString(r.Provider?.Substring(0, Math.Min(r.Provider.Length, 25)) ?? "---", fontRegular, XBrushes.Black, new XPoint(120, yPos));
            gfx.DrawString(r.Type ?? "---", fontRegular, XBrushes.Black, new XPoint(300, yPos));
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
                if (r.ContentType != null && r.ContentType.Contains("pdf"))
                {
                    using var ms = new MemoryStream(r.FileData!);
                    var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < attachment.PageCount; i++) outputDocument.AddPage(attachment.Pages[i]);
                }
                else if (r.ContentType != null && r.ContentType.Contains("image"))
                {
                    var imgPage = outputDocument.AddPage();
                    using var ms = new MemoryStream(r.FileData!);
                    using var img = XImage.FromStream(ms);
                    var imgGfx = XGraphics.FromPdfPage(imgPage);
                    double width = imgPage.Width.Point;
                    double height = (width / img.PixelWidth) * img.PixelHeight;
                    imgGfx.DrawImage(img, 0, 0, width, height);
                }
            }
            catch { /* File skipped if corrupt */ }
        }

        using var finalMs = new MemoryStream();
        outputDocument.Save(finalMs);
        return finalMs.ToArray();
    }
}