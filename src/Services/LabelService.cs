using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dashboard; 

namespace Dashboard.Services
{
    public class LabelService
    {
        public static async Task<byte[]> CreateAveryLabels(PPP_Owner owner, byte[]? logoBytes, List<PPP_OrderItem> items, int startPos)
        {
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

                        // 1. Handle Offset (Empty Labels)
                        for (int i = 1; i < startPos; i++)
                        {
                            table.Cell().Height(2, Unit.Inch).Padding(10).Text("");
                        }

                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;

                            // A. BAG LABEL
                            table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                            {
                                col.Item().Row(row => {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text(owner.BusinessName).FontSize(12).Bold();
                                        c.Item().Text("BAG LABEL").FontSize(8).SemiBold().FontColor(Colors.Green.Medium);
                                    });
                                    
                                    if (logoBytes != null && logoBytes.Length > 0)
                                    {
                                        // Constrain logo to prevent layout overflow
                                        row.ConstantItem(35).Height(35).Image(logoBytes).FitArea();
                                    }
                                });

                                col.Item().PaddingTop(2).Text(t => {
                                    t.Line($"Order #{orderGroup.Key}").FontSize(9).Bold();
                                    t.Line($"{order?.CustomerIdentifier}").FontSize(8);
                                    if (address != null) 
                                        t.Line($"{address.Street}, {address.City}").FontSize(7);
                                });

                                col.Item().AlignBottom().Text($"{orderGroup.Count()} ITEMS IN BAG").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                            });

                            // B. CONTAINER LABELS
                            foreach (var lineItem in orderGroup)
                            {
                                table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Text($"Order #{lineItem.OrderId}").FontSize(8).Bold();
                                        if (!string.IsNullOrEmpty(lineItem.LabelName))
                                            row.ConstantItem(80).Background(Colors.Grey.Lighten4).PaddingHorizontal(5).Text($"FOR: {lineItem.LabelName}").FontSize(8).Bold();
                                    });

                                    col.Item().PaddingTop(2).Text(lineItem.RecipeName.ToUpper()).FontSize(11).Bold();
                                    col.Item().Text(lineItem.SizeName).FontSize(8);

                                    if (lineItem.SelectedOptions != null && lineItem.SelectedOptions.Any())
                                    {
                                        var optString = string.Join(", ", lineItem.SelectedOptions.Select(o => o.OptionName));
                                        col.Item().Text($"+ {optString}").FontSize(7).FontColor(Colors.Blue.Medium);
                                    }
                                    
                                    col.Item().AlignBottom().Row(row => {
                                        row.RelativeItem().Text("Prep Date: " + lineItem.ScheduledDate.ToString("MM/dd/yy")).FontSize(7).FontColor(Colors.Grey.Medium);
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