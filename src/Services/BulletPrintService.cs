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
            
            // Clean baseUrl to ensure it's just the domain
            var cleanBaseUrl = baseUrl.TrimEnd('/');

            var options = new LaunchOptions
            {
                ExecutablePath = "/usr/bin/chromium", 
                Headless = true,
                Args = new[] { 
                    "--no-sandbox", 
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage"
                }
            };

            await using var browser = await Puppeteer.LaunchAsync(options);
            await using var page = await browser.NewPageAsync();

            // DO NOT set extra headers here - that is what triggered the ReferrerPolicy error.
            await page.SetViewportAsync(new ViewPortOptions { Width = 816, Height = 1056 });

            for (int month = 1; month <= 12; month++)
            {
                // We use a simple string for the URL to avoid any object-encoding issues
                string targetUrl = cleanBaseUrl + "/year-book-export?year=" + year + "&month=" + month;
                
                // Navigate using the most basic 'Load' event
                await page.GoToAsync(targetUrl, WaitUntilNavigation.Load);

                // Give the Blazor Server-side data a moment to actually populate the HTML
                await Task.Delay(2000);
                
                var monthPdfData = await page.PdfDataAsync(new PdfOptions { 
                    Format = PuppeteerSharp.Media.PaperFormat.Letter, 
                    PrintBackground = true,
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions { Top = "0in", Bottom = "0in", Left = "0in", Right = "0in" }
                });

                using var monthStream = new MemoryStream(monthPdfData);
                using var monthDoc = PdfReader.Open(monthStream, PdfDocumentOpenMode.Import);
                
                // Layout spread logic: ensure Left/Right pages align
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