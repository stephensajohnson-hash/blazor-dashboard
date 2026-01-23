using PuppeteerSharp;
using PuppeteerSharp.Media; // Required for PaperFormat
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.Text;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public async Task<byte[]> CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        // 1. GENERATE THE LEDGER PAGE VIA PUPPETEER
        var html = BuildLedgerHtml(receipts);
        
        // Fix for CS1674: BrowserFetcher in newer versions isn't used with 'using' 
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        
        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        
        // Use the explicit Media namespace for PaperFormat
        var ledgerPdfBytes = await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4 });
        await browser.CloseAsync();

        // 2. MERGE EVERYTHING VIA PDFSHARP
        using var outputDocument = new PdfDocument();
        
        // Add the Ledger
        using (var ledgerStream = new MemoryStream(ledgerPdfBytes))
        {
            var ledgerDoc = PdfReader.Open(ledgerStream, PdfDocumentOpenMode.Import);
            CopyPages(ledgerDoc, outputDocument);
        }

        // Add Attachments
        foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
        {
            try 
            {
                if (r.ContentType != null && r.ContentType.Contains("pdf"))
                {
                    using var ms = new MemoryStream(r.FileData!);
                    var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    CopyPages(attachment, outputDocument);
                }
                else if (r.ContentType != null && r.ContentType.Contains("image"))
                {
                    var newPage = outputDocument.AddPage();
                    using var ms = new MemoryStream(r.FileData!);
                    using var img = XImage.FromStream(ms);
                    
                    var graphics = XGraphics.FromPdfPage(newPage);
                    
                    // Fix for CS0618: Use .Point property for conversion in 6.1
                    double pageWidth = newPage.Width.Point;
                    double imageHeight = (pageWidth / img.PixelWidth) * img.PixelHeight;
                    
                    graphics.DrawImage(img, 0, 0, pageWidth, imageHeight);
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"ATTACHMENT_ERR for {r.Provider}: {ex.Message}");
            }
        }

        using var finalMs = new MemoryStream();
        outputDocument.Save(finalMs);
        return finalMs.ToArray();
    }

    private void CopyPages(PdfDocument from, PdfDocument to)
    {
        for (int i = 0; i < from.PageCount; i++) to.AddPage(from.Pages[i]);
    }

    private string BuildLedgerHtml(List<HsaReceipt> receipts)
    {
        var sb = new StringBuilder();
        sb.Append(@"<html><head><style>
            body { font-family: sans-serif; padding: 40px; color: #333; }
            h1 { color: #2563eb; margin-bottom: 5px; font-size: 24px; }
            table { width: 100%; border-collapse: collapse; margin-top: 20px; }
            th { text-align: left; border-bottom: 2px solid #000; padding: 12px 8px; font-size: 12px; }
            td { padding: 10px 8px; border-bottom: 1px solid #eee; font-size: 11px; }
            .amt { text-align: right; font-family: monospace; }
            .total { font-weight: bold; background: #f8fafc; border-top: 2px solid #333; }
        </style></head><body>");

        sb.Append("<h1>HSA Reimbursement Submission</h1>");
        sb.Append($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.Append("<table><thead><tr><th>Date</th><th>Provider</th><th>Category</th><th class='amt'>Amount</th></tr></thead><tbody>");

        foreach (var r in receipts)
        {
            sb.Append($"<tr><td>{r.ServiceDate:yyyy-MM-dd}</td><td>{r.Provider}</td><td>{r.Type}</td><td class='amt'>${r.Amount:N2}</td></tr>");
        }

        sb.Append($"<tr class='total'><td colspan='3' style='text-align:right'>Total Submission:</td><td class='amt'>${receipts.Sum(x => x.Amount):N2}</td></tr>");
        sb.Append("</tbody></table></body></html>");

        return sb.ToString();
    }
}