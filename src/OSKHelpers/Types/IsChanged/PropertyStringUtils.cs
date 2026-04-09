using System;
using System.Globalization;

namespace OSKHelpers.Types.IsChanged
{
    /// <summary>
    /// Utilities for assigning string values to various data types.<br/>
    /// Primarily intended for web projects (Blazor, MVC) where a string containing a number may produce unexpected conversion results.<br/>
    /// Provides methods for field assignment and custom format converters used by the other methods.<br/>
    /// The static properties <see cref="DecimalSeparator"/> <see cref="ThousandsSeparator"/> and <see cref="CultureInfo"/> can be used to customise the behaviour.
    /// </summary>
    public class PropertyStringUtils
    {
        #region Properties

        /// <summary>
        /// Decimal separator (default ',').
        /// </summary>
        public static string DecimalSeparator { get; set; }
        /// <summary>
        /// Thousands separator (default '.').
        /// </summary>
        public static string ThousandsSeparator { get; set; }

        /// <summary>
        /// CultureInfo used internally for conversions;<br/>
        /// defaults to Italian culture.
        /// </summary>
        public static CultureInfo CultureInfo { get; set; }

        #endregion

        #region Constructor

        static PropertyStringUtils()
        {
            CultureInfo         = new CultureInfo("it-IT");
            DecimalSeparator    = ",";
            ThousandsSeparator  = ".";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates that <see cref="DecimalSeparator"/>, <see cref="ThousandsSeparator"/> and <see cref="CultureInfo"/> are set.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        private static void CheckProperties()
        {
            if (string.IsNullOrEmpty(DecimalSeparator))
                throw new ArgumentNullException(nameof(DecimalSeparator));

            if (string.IsNullOrEmpty(ThousandsSeparator))
                throw new ArgumentNullException(nameof(ThousandsSeparator));

            if (CultureInfo == null)
                throw new ArgumentNullException(nameof(CultureInfo));
        }

        #region Display

        /// <summary>
        /// Returns the currency-formatted representation of the value passed as parameter.
        /// </summary>
        public static string DisplayAsCurrency(decimal? value) => value != null ? $"{value:#,##0.00}" : null;

        #endregion

        #region SetProperty

        /// <summary>
        /// Sets the value of <paramref name="field"/> by parsing <paramref name="newValue"/> as a decimal.
        /// </summary>
        /// <param name="field">Field to update.</param>
        /// <param name="newValue">String representation of the new decimal value.</param>
        /// <returns>True if the field value changed.</returns>
        public static bool SetProperty(ref decimal field, string newValue)
        {
            var changed = TryParse(newValue, out decimal value) && field != value;
            if (changed)
            {
                field = value;
            }
            return changed;
        }

        /// <summary>
        /// Sets the value of <paramref name="field"/> by parsing <paramref name="newValue"/> as a nullable decimal.
        /// </summary>
        /// <param name="field">Field to update.</param>
        /// <param name="newValue">String representation of the new decimal value, or null/empty to set the field to null.</param>
        /// <returns>True if the field value changed.</returns>
        public static bool SetProperty(ref decimal? field, string newValue)
        {
            bool changed = false;

            if (string.IsNullOrWhiteSpace(newValue) && field != null)
            {
                field = null;
                changed = true;
            }
            else
            {
                TryParse(newValue, out decimal? value);
                changed = field != value;
                if (changed)
                {
                    field = value;
                }
            }

            return changed;
        }

        /// <summary>
        /// Sets the value of <paramref name="field"/> by parsing <paramref name="newValue"/> as a DateTime.
        /// </summary>
        /// <param name="field">Field to update.</param>
        /// <param name="newValue">String representation of the new DateTime value.</param>
        /// <returns>True if the field value changed.</returns>
        public static bool SetProperty(ref DateTime field, string newValue)
        {
            var changed = TryParse(newValue, out DateTime value) && !field.Equals(value);
            if (changed)
            {
                field = value;
            }
            return changed;
        }

        /// <summary>
        /// Sets the value of <paramref name="field"/> by parsing <paramref name="newValue"/> as a nullable DateTime.
        /// </summary>
        /// <param name="field">Field to update.</param>
        /// <param name="newValue">String representation of the new DateTime value, or null/empty to set the field to null.</param>
        /// <returns>True if the field value changed.</returns>
        public static bool SetProperty(ref DateTime? field, string newValue)
        {
            bool changed = false;

            if (string.IsNullOrWhiteSpace(newValue) && field != null)
            {
                field = null;
                changed = true;
            }
            else
            {
                TryParse(newValue, out DateTime? value);
                changed = !field.Equals(value);
                if (changed)
                {
                    field = value;
                }
            }

            return changed;
        }

        #endregion

        #region TryParse

        /// <summary>
        /// Attempts to parse <paramref name="s"/> as a decimal, handling locale-specific separators.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="result">Parsed decimal value, or 0 on failure.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParse(string s, out decimal result)
        {
            CheckProperties();

            bool parsed = false;

            result = 0;
            if (!string.IsNullOrWhiteSpace(s))
            {
                // Check whether the last character to be used as the decimal separator
                // is a comma or a dot...
                var lastTpos = s.LastIndexOf(ThousandsSeparator);
                var lastDpos = s.LastIndexOf(DecimalSeparator);
                if (lastTpos >= 0 || lastDpos >= 0)
                {
                    // ... if at least one of the two is present, sanitise the string as much as possible
                    // after prepending and appending a zero so there is always something before and after the separator...
                    s = $"0{s}0";
                    // ... detect the last separator, adding 2 to account for the leading zero and the decimal separator character...
                    var p = (lastTpos > lastDpos ? lastTpos : lastDpos) + 2;
                    // ... and finally build the sanitised string
                    s = $"{s.Substring(0, p).Trim().Replace(ThousandsSeparator, string.Empty).Replace(DecimalSeparator, string.Empty)},{s.Substring(p)}";
                }
                // ... finally, if the resulting string is a valid number, update the property to the new value.
                parsed = decimal.TryParse(s, out result);
            }

            return parsed;
        }

        /// <summary>
        /// Attempts to parse <paramref name="s"/> as a nullable decimal.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="result">Parsed decimal value, or null on failure.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParse(string s, out decimal? result)
        {
            CheckProperties();

            result = null;
            var parsed = TryParse(s, out decimal res);
            if (parsed)
            {
                result = res;
            }
            return parsed;
        }

        /// <summary>
        /// Attempts to parse <paramref name="s"/> as a DateTime using <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="result">Parsed DateTime value, or <see cref="DateTime.MinValue"/> on failure.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParse(string s, out DateTime result)
        {
            CheckProperties();

            return DateTime.TryParse(s, CultureInfo, DateTimeStyles.None, out result);
        }

        /// <summary>
        /// Attempts to parse <paramref name="s"/> as a nullable DateTime.
        /// </summary>
        /// <param name="s">String to parse.</param>
        /// <param name="result">Parsed DateTime value, or null on failure.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParse(string s, out DateTime? result)
        {
            CheckProperties();

            result = null;
            var parsed = TryParse(s, out DateTime res);
            if (parsed)
            {
                result = res;
            }
            return parsed;
        }

        #endregion

        #region Parse

        /// <summary>Parses <paramref name="s"/> as a decimal; returns 0 on failure.</summary>
        /// <param name="s">String to parse.</param>
        public static decimal ParseDecimal(string s) => TryParse(s, out decimal result) ? result : result;
        /// <summary>Parses <paramref name="s"/> as a nullable decimal; returns null on failure.</summary>
        /// <param name="s">String to parse.</param>
        public static decimal? ParseDecimalNullable(string s) => TryParse(s, out decimal? result) ? result : null;
        /// <summary>Parses <paramref name="s"/> as a DateTime; returns <see cref="DateTime.MinValue"/> on failure.</summary>
        /// <param name="s">String to parse.</param>
        public static DateTime ParseDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        /// <summary>Parses <paramref name="s"/> as a nullable DateTime; returns null on failure.</summary>
        /// <param name="s">String to parse.</param>
        public static DateTime? ParseDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #region gRPC Conversions

        /// <summary>Converts a <see cref="DateTime"/> to a gRPC-compatible ISO 8601 string.</summary>
        /// <param name="d">Date to convert.</param>
        public static string ToGRPCString(DateTime d) => $"{d:s}";
        /// <summary>Converts a gRPC ISO 8601 string to a <see cref="DateTime"/>.</summary>
        /// <param name="s">String to convert.</param>
        public static DateTime FromGRPCDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        /// <summary>Converts a gRPC ISO 8601 string to a nullable <see cref="DateTime"/>.</summary>
        /// <param name="s">String to convert.</param>
        public static DateTime? FromGRPCDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #endregion
    }
}
