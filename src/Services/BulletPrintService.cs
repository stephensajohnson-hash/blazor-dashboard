using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dashboard.Services
{
    public class BulletPrintService
    {
        public async Task<byte[]> GenerateYearBookPdf(int year, string baseUrl)
        {
            // Set up the output stream and final PDF document
            using var finalDocument = new PdfDocument();
            
            // 1. Launch Browser using the path defined in your Dockerfile
            var options = new LaunchOptions
            {
                ExecutablePath = "/usr/bin/chromium", 
                Headless = true,
                Args = new[] { 
                    "--no-sandbox", 
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage", 
                    "--single-process" 
                }
            };

            await using var browser = await Puppeteer.LaunchAsync(options);
            await using var page = await browser.NewPageAsync();

            // 2. Loop Through Months to keep memory footprint low
            for (int month = 1; month <= 12; month++)
            {
                // We will build this route next
                string targetUrl = $"{baseUrl.TrimEnd('/')}/export/year-book/{year}/{month}";
                
                await page.GoToAsync(targetUrl, WaitUntilNavigation.Networkidle2);
                
                var monthPdfData = await page.PdfDataAsync(new PdfOptions { 
                    Format = PuppeteerSharp.Media.PaperFormat.Letter, 
                    PrintBackground = true, // Captures Tailwind colors and images
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions
                    {
                        Top = "0in",
                        Bottom = "0in",
                        Left = "0in",
                        Right = "0in"
                    }
                });

                // 3. Import this month's pages into the final document
                using var monthStream = new MemoryStream(monthPdfData);
                using var monthDoc = PdfReader.Open(monthStream, PdfDocumentOpenMode.Import);
                
                /* SPREAD LOGIC: 
                   In professional printing, Page 1 is always the Right side.
                   We want our Monthly Spread (2 pages) to be Left + Right.
                   Therefore, the Month Spread must start on an EVEN page (2, 4, 6...).
                */
                if (finalDocument.PageCount > 0 && finalDocument.PageCount % 2 != 0)
                {
                    // Add a blank "Notes" or filler page to shift the start to the next Left page
                    finalDocument.AddPage(); 
                }

                foreach (PdfPage p in monthDoc.Pages)
                {
                    finalDocument.AddPage(p);
                }
            }

            // 4. Save and return the combined byte array
            using var outStream = new MemoryStream();
            finalDocument.Save(outStream);
            return outStream.ToArray();
        }
    }
}