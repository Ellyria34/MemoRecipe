using System.Text.RegularExpressions;

namespace MemoRecipeIA.Application
{
    /// <summary>
    /// Normalizes OCR-extracted quantity strings by fixing common OCR mistakes
    /// while preserving valid units and values.
    /// 
    /// IMPORTANT:
    /// - Never invent quantities
    /// - Never normalize units (no conversions)
    /// - Only fix obvious OCR character confusions
    /// - Be conservative: if unsure, return the original value
    /// </summary>
    public static class OcrQuantityNormalizer
    {
        private static readonly Regex QuantityRegex =
            new(@"^(?<number>[\[\]I1l0-9]+)\s*(?<unit>[a-zA-Z]+)?$",
                RegexOptions.Compiled);

        public static string? Normalize(string? quantity)
        {
            if (string.IsNullOrWhiteSpace(quantity))
                return quantity;

            var q = quantity.Trim();

            var match = QuantityRegex.Match(q);
            if (!match.Success)
                return q;

            var number = match.Groups["number"].Value;
            var unit = match.Groups["unit"].Value;

            // '[' is almost always '1' in OCR
            if (number.Contains('['))
            {
                number = number.Replace("[", "1");
            }

            // Replace 'l' or 'I' ONLY when followed by at least 2 digits
            // This avoids inventing quantities like I5 -> 15
            if (Regex.IsMatch(number, @"^[lI][0-9]{2,}$"))
            {
                number = "1" + number.Substring(1);
            }

            unit = NormalizeUnit(unit);

            return string.IsNullOrEmpty(unit)
                ? number
                : $"{number}{unit}";
        }


        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return unit;

            unit = unit.ToLowerInvariant();

            return unit switch
            {
                // Common OCR confusions for "ml"
                "m1" => "ml",
                "mi" => "ml",

                // Leave all other units untouched
                _ => unit
            };
        }
    }
}
