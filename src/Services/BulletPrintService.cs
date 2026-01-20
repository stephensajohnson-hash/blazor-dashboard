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
        public async Task<byte[]> GenerateYearlyCalendarAsync(int year, string baseUrl = "http://localhost")
        {
            using var finalDocument = new PdfDocument();
            
            // Clean the base URL to ensure no trailing slashes interfere with the path
            var cleanBaseUrl = baseUrl.TrimEnd('/');

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

            // Set viewport to standard Letter size (at 96 DPI)
            await page.SetViewportAsync(new ViewPortOptions { Width = 816, Height = 1056 });

            for (int month = 1; month <= 12; month++)
            {
                // SURGICAL: Simplified URL construction to avoid protocol parsing errors
                string targetUrl = $"{cleanBaseUrl}/year-book-export?year={year}&month={month}";
                
                // Use Networkidle2 to wait for all background API calls and images to load
                await page.GoToAsync(targetUrl, new NavigationOptions { 
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                    Timeout = 60000 
                });
                
                var monthPdfData = await page.PdfDataAsync(new PdfOptions { 
                    Format = PuppeteerSharp.Media.PaperFormat.Letter, 
                    PrintBackground = true,
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions
                    {
                        Top = "0in", Bottom = "0in", Left = "0in", Right = "0in"
                    }
                });

                using var monthStream = new MemoryStream(monthPdfData);
                using var monthDoc = PdfReader.Open(monthStream, PdfDocumentOpenMode.Import);
                
                // Ensure Monthly Spread starts on an even page (Left side)
                if (finalDocument.PageCount > 0 && finalDocument.PageCount % 2 != 0)
                {
                    finalDocument.AddPage(); 
                }

                foreach (PdfPage p in monthDoc.Pages)
                {
                    finalDocument.AddPage(p);
                }
            }

            using var outStream = new MemoryStream();
            finalDocument.Save(outStream);
            return outStream.ToArray();
        }
    }
}