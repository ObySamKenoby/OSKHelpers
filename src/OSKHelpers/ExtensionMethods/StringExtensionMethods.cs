using OSKHelpers.Common;

namespace OSKHelpers.ExtensionMethods
{
    /// <summary>
    /// Extension methods for strings.
    /// </summary>
    public static class StringExtensionMethods
    {
        /// <inheritdoc cref="StringUtils.AsASCII(string, bool, bool, int?, bool)"/>
        public static string AsASCII(this string text, bool toUpper = false, bool trim = false, int? length = null, bool padRight = false) => StringUtils.AsASCII(text, toUpper, trim, length, padRight);

    }
}
