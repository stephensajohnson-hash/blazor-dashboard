using System.Linq;

namespace Dashboard.Services
{
    public static class PPPUtils
    {
        /// <summary>
        /// Formats a string into ###-###-####. Strips all non-numeric characters first.
        /// Returns empty string if input is null or doesn't result in digits.
        /// </summary>
        public static string FormatPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";

            // Strip all non-digits
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // If we have exactly 10 digits, format as 903-634-7525
            if (digits.Length == 10)
            {
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6)}";
            }

            // If it's not 10, return just the digits (validation logic elsewhere will handle the length error)
            return digits;
        }

        /// <summary>
        /// Utility to get only numbers from a string. Useful for raw DB storage or comparisons.
        /// </summary>
        public static string StripNonDigits(string? input)
        {
            return string.IsNullOrWhiteSpace(input) 
                ? "" 
                : new string(input.Where(char.IsDigit).ToArray());
        }
    }

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
            if (!Units.ContainsKey(fromUnit) || !Units.ContainsKey(toUnit)) return false;

            var from = Units[fromUnit];
            var to = Units[toUnit];

            if (from.Family != to.Family) return false;

            newQty = (qty * from.ToBaseFactor) / to.ToBaseFactor;
            return true;
        }

        public static List<string> GetCompatibleUnits(string unit)
        {
            if (!Units.ContainsKey(unit)) return new List<string>();
            var family = Units[unit].Family;
            return Units.Where(u => u.Value.Family == family).Select(u => u.Key).ToList();
        }
    }
}