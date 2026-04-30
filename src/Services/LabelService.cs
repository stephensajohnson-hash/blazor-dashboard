using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dashboard; 

namespace Dashboard.Services
{
    public class LabelService
    {
        public static async Task<byte[]> CreateAveryLabels(
            PPP_Owner owner, 
            byte[]? logoBytes, 
            List<PPP_OrderItem> items, 
            int startPos,
            Dictionary<string, (string Name, string Phone)> customerData)
        {
            // Avery 5163: 2 columns, 5 rows (2" x 4" labels)
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(0.5f, Unit.Inch);
                    page.MarginBottom(0.5f, Unit.Inch);
                    page.MarginLeft(0.16f, Unit.Inch);
                    page.MarginRight(0.16f, Unit.Inch);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // 1. Handle Offset (Skips used labels on the sheet)
                        for (int i = 1; i < startPos; i++)
                        {
                            table.Cell().Height(2, Unit.Inch).Text("");
                        }

                        // 2. Group items by Order so Bag and Container labels stay together
                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;
                            var itemCount = orderGroup.Count();
                            
                            var email = order?.CustomerIdentifier ?? "";
                            var displayName = customerData.ContainsKey(email) ? customerData[email].Name : email;
                            var displayPhone = customerData.ContainsKey(email) ? customerData[email].Phone : "";

                            // Grammar check for item counts
                            var itemText = itemCount == 1 ? "1 Item in Order" : $"{itemCount} Items in Order";

                            // A. MAIN ORDER SUMMARY LABEL
                            table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                            {
                                col.Item().Row(row => {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text($"ORDER #{orderGroup.Key}").FontSize(10).ExtraBold();
                                        c.Item().PaddingTop(1).Text(owner.BusinessName).FontSize(11).Bold();
                                    });
                                    
                                    if (logoBytes != null && logoBytes.Length > 0)
                                    {
                                        row.ConstantItem(35).Height(35).Image(logoBytes).FitArea();
                                    }
                                });

                                col.Item().PaddingTop(4).Text(t => {
                                    t.Line(displayName).FontSize(10).SemiBold();
                                    if (address != null) 
                                        t.Line($"{address.Street}, {address.City}").FontSize(8);
                                    
                                    if (!string.IsNullOrEmpty(displayPhone))
                                        t.Line(displayPhone).FontSize(9).SemiBold().FontColor(Colors.Grey.Darken3);
                                });

                                col.Item().AlignBottom().Text(itemText).FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                            });

                            // B. INDIVIDUAL CONTAINER LABELS
                            foreach (var lineItem in orderGroup)
                            {
                                table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Text($"ORDER #{lineItem.OrderId}").FontSize(10).ExtraBold();
                                        
                                        if (!string.IsNullOrEmpty(lineItem.LabelName))
                                            row.RelativeItem().AlignRight().Text($"FOR: {lineItem.LabelName}").FontSize(9).Bold();
                                    });

                                    col.Item().PaddingTop(4).Text(lineItem.RecipeName.ToUpper()).FontSize(11).Bold();
                                    col.Item().Text(lineItem.SizeName).FontSize(8);

                                    if (lineItem.SelectedOptions != null && lineItem.SelectedOptions.Any())
                                    {
                                        var optString = string.Join(", ", lineItem.SelectedOptions.Select(o => o.OptionName));
                                        // High-readability Red for stand-out instructions
                                        col.Item().Text($"+ {optString}").FontSize(8).Bold().FontColor(Colors.Red.Darken2);
                                    }
                                    
                                    col.Item().AlignBottom().Row(row => {
                                        row.RelativeItem().Text("Prep Date: " + lineItem.ScheduledDate.ToString("MM/dd/yy")).FontSize(7).FontColor(Colors.Grey.Medium);
                                        row.RelativeItem().AlignRight().Text(displayName).FontSize(7).FontColor(Colors.Grey.Darken1);
                                    });
                                });
                            }
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}