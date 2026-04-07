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

        public static bool SetProperty(ref decimal field, string newValue)
        {
            var changed = TryParse(newValue, out decimal value) && field != value;
            if (changed)
            {
                field = value;
            }
            return changed;
        }

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

        public static bool SetProperty(ref DateTime field, string newValue)
        {
            var changed = TryParse(newValue, out DateTime value) && !field.Equals(value);
            if (changed)
            {
                field = value;
            }
            return changed;
        }

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

        public static bool TryParse(string s, out DateTime result)
        {
            CheckProperties();

            return DateTime.TryParse(s, CultureInfo, DateTimeStyles.None, out result);
        }

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

        public static decimal ParseDecimal(string s) => TryParse(s, out decimal result) ? result : result;
        public static decimal? ParseDecimalNullable(string s) => TryParse(s, out decimal? result) ? result : null;
        public static DateTime ParseDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        public static DateTime? ParseDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #region gRPC Conversions

        public static string ToGRPCString(DateTime d) => $"{d:s}";
        public static DateTime FromGRPCDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        public static DateTime? FromGRPCDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #endregion
    }
}
