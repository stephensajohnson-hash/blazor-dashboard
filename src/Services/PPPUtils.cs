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
}