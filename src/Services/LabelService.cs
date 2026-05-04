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
            Dictionary<string, (string Name, string Phone)> customerData,
            bool printSummary,
            bool printContainers,
            bool includeMacros) // NEW: 8th parameter
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

                        // Handle Avery Start Position Offset
                        for (int i = 1; i < startPos; i++)
                        {
                            table.Cell().Height(2, Unit.Inch).Text("");
                        }

                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;
                            var email = order?.CustomerIdentifier ?? "";
                            var displayName = customerData.ContainsKey(email) ? customerData[email].Name : email;
                            var displayPhone = customerData.ContainsKey(email) ? customerData[email].Phone : "";

                            // 1. BAG / ORDER SUMMARY LABELS
                            if (printSummary)
                            {
                                var totalServings = orderGroup.Sum(i => i.Servings.Count);
                                var itemText = totalServings == 1 ? "1 Item in Order" : $"{totalServings} Items in Order";

                                table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Column(c => {
                                            c.Item().Text($"ORDER #{orderGroup.Key}").FontSize(10).ExtraBold();
                                            c.Item().PaddingTop(1).Text(owner.BusinessName).FontSize(11).Bold();
                                        });
                                        if (logoBytes != null && logoBytes.Length > 0)
                                            row.ConstantItem(35).Height(35).Image(logoBytes).FitArea();
                                    });

                                    col.Item().PaddingTop(4).Text(t => {
                                        t.Line(displayName).FontSize(10).SemiBold();
                                        if (address != null) t.Line($"{address.Street}, {address.City}").FontSize(8);
                                        if (!string.IsNullOrEmpty(displayPhone)) t.Line(displayPhone).FontSize(9).SemiBold().FontColor(Colors.Grey.Darken3);
                                    });

                                    col.Item().AlignBottom().Text(itemText).FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                                });
                            }

                            // 2. INDIVIDUAL CONTAINER LABELS
                            if (printContainers)
                            {
                                foreach (var lineItem in orderGroup)
                                {
                                    foreach (var serving in lineItem.Servings)
                                    {
                                        table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                                        {
                                            col.Item().Row(row => {
                                                row.RelativeItem().Text($"ORDER #{lineItem.OrderId}").FontSize(10).ExtraBold();
                                                if (!string.IsNullOrEmpty(serving.LabelName))
                                                    row.RelativeItem().AlignRight().Text($"FOR: {serving.LabelName}").FontSize(9).Bold();
                                            });

                                            col.Item().PaddingTop(2).Text(lineItem.RecipeName.ToUpper()).FontSize(11).Bold();
                                            col.Item().Text(lineItem.SizeName).FontSize(8).FontColor(Colors.Grey.Darken2);

                                            if (serving.SelectedOptions != null && serving.SelectedOptions.Any())
                                            {
                                                var optString = string.Join(", ", serving.SelectedOptions.Select(o => o.OptionName));
                                                col.Item().Text($"+ {optString}").FontSize(8).Bold().FontColor(Colors.Red.Darken2);
                                            }

                                            // NEW: NUTRITION SECTION
                                            if (includeMacros && lineItem.MenuItem?.Recipe != null)
                                            {
                                                var m = GetMacros(lineItem.MenuItem.Recipe);
                                                col.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten3).Row(row =>
                                                {
                                                    row.RelativeItem().Column(c => {
                                                        c.Item().Text("CALS").FontSize(6).Bold().FontColor(Colors.Grey.Medium);
                                                        c.Item().Text($"{m.Cals}").FontSize(8).Bold();
                                                    });
                                                    row.RelativeItem().Column(c => {
                                                        c.Item().Text("PRO").FontSize(6).Bold().FontColor(Colors.Grey.Medium);
                                                        c.Item().Text($"{m.Prot}g").FontSize(8).Bold();
                                                    });
                                                    row.RelativeItem().Column(c => {
                                                        c.Item().Text("FAT").FontSize(6).Bold().FontColor(Colors.Grey.Medium);
                                                        c.Item().Text($"{m.Fat}g").FontSize(8).Bold();
                                                    });
                                                    row.RelativeItem().Column(c => {
                                                        c.Item().Text("NETC").FontSize(6).Bold().FontColor(Colors.Grey.Medium);
                                                        c.Item().Text($"{m.Net}g").FontSize(8).Bold();
                                                    });
                                                });
                                            }
                                            
                                            col.Item().AlignBottom().Row(row => {
                                                row.RelativeItem().Text("Prep Date: " + lineItem.ScheduledDate.ToString("MM/dd/yy")).FontSize(7).FontColor(Colors.Grey.Medium);
                                                row.RelativeItem().AlignRight().Text(displayName).FontSize(7).FontColor(Colors.Grey.Darken1);
                                            });
                                        });
                                    }
                                }
                            }
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static (double Cals, double Prot, double Fat, double Net) GetMacros(PPP_Recipe r)
        {
            if (r == null || r.Servings <= 0) return (0, 0, 0, 0);

            double cals = 0, prot = 0, fat = 0, net = 0;

            foreach (var group in r.IngredientGroups)
            {
                foreach (var mapping in group.Ingredients)
                {
                    if (mapping.Ingredient != null)
                    {
                        cals += mapping.Ingredient.Calories * mapping.Quantity;
                        prot += mapping.Ingredient.Protein * mapping.Quantity;
                        fat += mapping.Ingredient.Fat * mapping.Quantity;
                        net += (mapping.Ingredient.Carbs - mapping.Ingredient.Fiber) * mapping.Quantity;
                    }
                }
            }

            return (
                Math.Round(cals / r.Servings, 0),
                Math.Round(prot / r.Servings, 1),
                Math.Round(fat / r.Servings, 1),
                Math.Round(net / r.Servings, 1)
            );
        }
    }
}