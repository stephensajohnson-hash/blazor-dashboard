using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts, string? txKey = null, string? description = null)
    {
        using var outputDocument = new PdfDocument();
        outputDocument.Options.CompressContentStreams = true;

        try 
        {
            var page = outputDocument.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            
            var fontTitle = new XFont("Roboto", 18, XFontStyleEx.Bold);
            var fontLabel = new XFont("Roboto", 10, XFontStyleEx.Bold);
            var fontHeader = new XFont("Roboto", 10, XFontStyleEx.Bold);
            var fontTable = new XFont("Roboto", 9, XFontStyleEx.Regular);
            var fontTotal = new XFont("Roboto", 11, XFontStyleEx.Bold);

            double yPos = 50;
            double bottomMargin = 740;

            // --- HEADER SECTION ---
            gfx.DrawString("HSA Reimbursement Ledger", fontTitle, XBrushes.Black, new XPoint(40, yPos));
            yPos += 25;

            if (!string.IsNullOrEmpty(txKey))
            {
                gfx.DrawString("Transaction Key:", fontLabel, XBrushes.Black, new XPoint(40, yPos));
                gfx.DrawString(txKey, fontTable, XBrushes.Black, new XPoint(130, yPos));
                yPos += 15;
            }

            if (!string.IsNullOrEmpty(description))
            {
                gfx.DrawString("Description:", fontLabel, XBrushes.Black, new XPoint(40, yPos));
                gfx.DrawString(description, fontTable, XBrushes.Black, new XPoint(130, yPos));
                yPos += 15;
            }

            gfx.DrawString($"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}", fontTable, XBrushes.Gray, new XPoint(40, yPos));
            yPos += 30;

            var colDate = (X: 40.0, W: 65.0);
            var colPatient = (X: 110.0, W: 90.0);
            var colProvider = (X: 205.0, W: 155.0);
            var colCategory = (X: 365.0, W: 130.0);
            var colAmount = (X: 500.0, W: 50.0);

            DrawTableHeaders(gfx, fontHeader, colDate.X, colPatient.X, colProvider.X, colCategory.X, colAmount.X, ref yPos);

            foreach (var r in receipts)
            {
                double hPatient = MeasureHeight(gfx, r.Patient ?? "---", fontTable, colPatient.W);
                double hProvider = MeasureHeight(gfx, r.Provider ?? "---", fontTable, colProvider.W);
                double hCategory = MeasureHeight(gfx, r.Type ?? "Medical", fontTable, colCategory.W);
                double rowHeight = Math.Max(18, Math.Max(hPatient, Math.Max(hProvider, hCategory)));

                if (yPos + rowHeight > bottomMargin)
                {
                    gfx.Dispose();
                    page = outputDocument.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = 50;
                    DrawTableHeaders(gfx, fontHeader, colDate.X, colPatient.X, colProvider.X, colCategory.X, colAmount.X, ref yPos);
                }

                DrawWrappedText(gfx, r.ServiceDate.ToString("MM/dd/yyyy"), fontTable, colDate.X, yPos, colDate.W);
                DrawWrappedText(gfx, r.Patient ?? "---", fontTable, colPatient.X, yPos, colPatient.W);
                DrawWrappedText(gfx, r.Provider ?? "---", fontTable, colProvider.X, yPos, colProvider.W);
                DrawWrappedText(gfx, r.Type ?? "Medical", fontTable, colCategory.X, yPos, colCategory.W);
                gfx.DrawString($"${r.Amount:N2}", fontTable, XBrushes.Black, new XPoint(colAmount.X, yPos + 9));
                
                yPos += rowHeight + 6;
            }

            // --- GRAND TOTAL ---
            if (yPos + 50 > bottomMargin)
            {
                gfx.Dispose();
                page = outputDocument.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPos = 50;
            }

            yPos += 10;
            gfx.DrawLine(XPens.Black, 40, yPos, 550, yPos);
            yPos += 20;
            gfx.DrawString("Total Reimbursement Amount:", fontTotal, XBrushes.Black, new XPoint(310, yPos));
            gfx.DrawString($"${receipts.Sum(x => x.Amount):N2}", fontTotal, XBrushes.DarkGreen, new XPoint(500, yPos));
            
            gfx.Dispose();

            foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
            {
                try 
                {
                    if (r.ContentType?.ToLower().Contains("pdf") == true)
                    {
                        using var ms = new MemoryStream(r.FileData!);
                        using var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                        foreach (var aPage in attachment.Pages) outputDocument.AddPage(aPage);
                    }
                    else if (r.ContentType?.ToLower().Contains("image") == true)
                    {
                        using var ms = new MemoryStream(r.FileData!);
                        using var img = XImage.FromStream(ms);
                        var imgPage = outputDocument.AddPage();
                        using var imgGfx = XGraphics.FromPdfPage(imgPage);
                        double width = imgPage.Width.Point;
                        double height = (width / img.PixelWidth) * img.PixelHeight;
                        if (height > imgPage.Height.Point) height = imgPage.Height.Point;
                        imgGfx.DrawImage(img, 0, 0, width, height);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"STITCH_ERR: {ex.Message}"); }
            }

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

    private void DrawTableHeaders(XGraphics gfx, XFont font, double xD, double xP, double xPr, double xC, double xA, ref double y)
    {
        gfx.DrawString("Date", font, XBrushes.Black, new XPoint(xD, y));
        gfx.DrawString("Patient", font, XBrushes.Black, new XPoint(xP, y));
        gfx.DrawString("Provider", font, XBrushes.Black, new XPoint(xPr, y));
        gfx.DrawString("Category", font, XBrushes.Black, new XPoint(xC, y));
        gfx.DrawString("Amount", font, XBrushes.Black, new XPoint(xA, y));
        y += 5;
        gfx.DrawLine(XPens.Black, 40, y, 550, y);
        y += 15;
    }

    private void DrawWrappedText(XGraphics gfx, string text, XFont font, double x, double y, double width)
    {
        XRect rect = new XRect(x, y, width, 1000);
        XTextFormatter tf = new XTextFormatter(gfx);
        tf.DrawString(text, font, XBrushes.Black, rect, XStringFormats.TopLeft);
    }

    private double MeasureHeight(XGraphics gfx, string text, XFont font, double width)
    {
        var words = text.Split(' ');
        int lines = 1;
        double currentLineWidth = 0;
        foreach (var word in words)
        {
            double wordWidth = gfx.MeasureString(word + " ", font).Width;
            if (currentLineWidth + wordWidth > width) { lines++; currentLineWidth = wordWidth; }
            else { currentLineWidth += wordWidth; }
        }
        return lines * font.Height;
    }
}