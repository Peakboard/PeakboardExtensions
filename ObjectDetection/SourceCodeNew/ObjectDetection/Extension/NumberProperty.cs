using System.Globalization;

namespace PeakboardExtensionObjectDetection.Extension
{
    /// <summary>
    /// A number typed into a data source property. The defaults are written "0.4", but on a
    /// German Designer the same field is just as naturally filled in as "0,4" - and an
    /// invariant-culture parse of that fails. Failing used to mean 0: a confidence threshold of 0
    /// turns every raw candidate of the model into a detection.
    /// </summary>
    internal static class NumberProperty
    {
        /// <summary>The value, with "," or "." as decimal separator; the fallback when it is empty or not a number.</summary>
        public static double Parse(string text, double fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            var normalized = text.Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && !double.IsNaN(value) && !double.IsInfinity(value)
                ? value
                : fallback;
        }
    }
}
