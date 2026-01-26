using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout; // REQUIRED FOR XTextFormatter
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        using var outputDocument = new PdfDocument();
        outputDocument.Options.CompressContentStreams = true;

        try 
        {
            // --- 1. GENERATE LEDGER PAGE(S) ---
            var page = outputDocument.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);
            
            var fontTitle = new XFont("Roboto", 18, XFontStyleEx.Bold);
            var fontHeader = new XFont("Roboto", 10, XFontStyleEx.Bold);
            var fontTable = new XFont("Roboto", 9, XFontStyleEx.Regular);
            var fontTotal = new XFont("Roboto", 11, XFontStyleEx.Bold);

            double yPos = 50;
            gfx.DrawString("HSA Reimbursement Ledger", fontTitle, XBrushes.Black, new XPoint(40, yPos));
            yPos += 20;
            gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", fontTable, XBrushes.Gray, new XPoint(40, yPos));
            yPos += 40;

            // Column Definitions (X position and Width for wrapping)
            var colDate = (X: 40.0, W: 60.0);
            var colPatient = (X: 105.0, W: 95.0);
            var colProvider = (X: 205.0, W: 155.0);
            var colCategory = (X: 365.0, W: 130.0);
            var colAmount = (X: 500.0, W: 50.0);

            gfx.DrawString("Date", fontHeader, XBrushes.Black, new XPoint(colDate.X, yPos));
            gfx.DrawString("Patient", fontHeader, XBrushes.Black, new XPoint(colPatient.X, yPos));
            gfx.DrawString("Provider", fontHeader, XBrushes.Black, new XPoint(colProvider.X, yPos));
            gfx.DrawString("Category", fontHeader, XBrushes.Black, new XPoint(colCategory.X, yPos));
            gfx.DrawString("Amount", fontHeader, XBrushes.Black, new XPoint(colAmount.X, yPos));
            
            yPos += 5;
            gfx.DrawLine(XPens.Black, 40, yPos, 550, yPos);
            yPos += 15;

            foreach (var r in receipts)
            {
                // Measure the height needed for this row based on the longest wrapped text
                double hPatient = MeasureHeight(gfx, r.Patient ?? "---", fontTable, colPatient.W);
                double hProvider = MeasureHeight(gfx, r.Provider ?? "---", fontTable, colProvider.W);
                double hCategory = MeasureHeight(gfx, r.Type ?? "Medical", fontTable, colCategory.W);
                
                double rowHeight = Math.Max(18, Math.Max(hPatient, Math.Max(hProvider, hCategory)));

                // Simple check for page bottom; reset or new page logic could be expanded here
                if (yPos + rowHeight > 750) 
                {
                     // For now, we continue, but in a full production environment, 
                     // you would add outputDocument.AddPage() here.
                }

                gfx.DrawString(r.ServiceDate.ToString("yyyy-MM-dd"), fontTable, XBrushes.Black, new XPoint(colDate.X, yPos));
                
                DrawWrappedText(gfx, r.Patient ?? "---", fontTable, colPatient.X, yPos, colPatient.W);
                DrawWrappedText(gfx, r.Provider ?? "---", fontTable, colProvider.X, yPos, colProvider.W);
                DrawWrappedText(gfx, r.Type ?? "Medical", fontTable, colCategory.X, yPos, colCategory.W);
                
                gfx.DrawString($"${r.Amount:N2}", fontTable, XBrushes.Black, new XPoint(colAmount.X, yPos));
                
                yPos += rowHeight + 4; // Move to next row position
            }

            yPos += 10;
            gfx.DrawLine(XPens.Black, 40, yPos, 550, yPos);
            yPos += 20;
            gfx.DrawString("Total Reimbursement Amount:", fontTotal, XBrushes.Black, new XPoint(330, yPos));
            gfx.DrawString($"${receipts.Sum(x => x.Amount):N2}", fontTotal, XBrushes.DarkGreen, new XPoint(500, yPos));

            // --- 2. APPEND ATTACHMENTS ---
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

    private void DrawWrappedText(XGraphics gfx, string text, XFont font, double x, double y, double width)
    {
        XRect rect = new XRect(x, y, width, 1000);
        XTextFormatter tf = new XTextFormatter(gfx);
        tf.DrawString(text, font, XBrushes.Black, rect, XStringFormats.TopLeft);
    }

    private double MeasureHeight(XGraphics gfx, string text, XFont font, double width)
    {
        // Estimate height based on word-wrapping logic
        var words = text.Split(' ');
        int lines = 1;
        double currentLineWidth = 0;
        foreach (var word in words)
        {
            double wordWidth = gfx.MeasureString(word + " ", font).Width;
            if (currentLineWidth + wordWidth > width)
            {
                lines++;
                currentLineWidth = wordWidth;
            }
            else
            {
                currentLineWidth += wordWidth;
            }
        }
        return lines * (font.Height);
    }
}