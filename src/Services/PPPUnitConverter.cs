using System;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard.Services
{
    public static class PPPUnitConverter
    {
        private enum UnitFamily { Weight, Volume, Count, Unknown }

        private static readonly Dictionary<string, (UnitFamily Family, double ToBaseFactor)> Units = new(StringComparer.OrdinalIgnoreCase)
        {
            // Weight (Base: Grams)
            { "g", (UnitFamily.Weight, 1.0) },
            { "gram", (UnitFamily.Weight, 1.0) },
            { "grams", (UnitFamily.Weight, 1.0) },
            { "oz", (UnitFamily.Weight, 28.3495) },
            { "ounce", (UnitFamily.Weight, 28.3495) },
            { "lb", (UnitFamily.Weight, 453.592) },
            { "lbs", (UnitFamily.Weight, 453.592) },
            { "kg", (UnitFamily.Weight, 1000.0) },

            // Volume (Base: Milliliters)
            { "ml", (UnitFamily.Volume, 1.0) },
            { "tsp", (UnitFamily.Volume, 4.92892) },
            { "tbsp", (UnitFamily.Volume, 14.7868) },
            { "cup", (UnitFamily.Volume, 236.588) },
            { "cups", (UnitFamily.Volume, 236.588) },
            { "fl oz", (UnitFamily.Volume, 29.5735) },

            // Count
            { "each", (UnitFamily.Count, 1.0) },
            { "piece", (UnitFamily.Count, 1.0) },
            { "slice", (UnitFamily.Count, 1.0) }
        };

        public static bool TryConvert(double qty, string fromUnit, string toUnit, out double newQty)
        {
            newQty = qty;
            if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit)) return false;
            
            if (!Units.ContainsKey(fromUnit) || !Units.ContainsKey(toUnit)) return false;

            var from = Units[fromUnit];
            var to = Units[toUnit];

            if (from.Family != to.Family) return false;

            newQty = (qty * from.ToBaseFactor) / to.ToBaseFactor;
            return true;
        }

        public static List<string> GetCompatibleUnits(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit) || !Units.ContainsKey(unit)) return new List<string>();
            var family = Units[unit].Family;
            return Units.Where(u => u.Value.Family == family).Select(u => u.Key).ToList();
        }
    }
}