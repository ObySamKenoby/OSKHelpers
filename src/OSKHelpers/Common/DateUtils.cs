using System;
using System.Globalization;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utility class for converting <see cref="DateTime"/> to and from a configurable string format.
    /// </summary>
    public class DateUtils
    {
        /// <summary>
        /// Date/time format string used for conversions.<br/>
        /// Default value: <c>yyyy-MM-dd HH:mm:ss:ffff</c>.
        /// </summary>
        public static string DateFormat { get; set; }

        /// <summary>
        /// Static constructor. Initialises <see cref="DateFormat"/> to its default value.
        /// </summary>
        static DateUtils()
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss:ffff";
        }

        /// <summary>
        /// Returns the string representation of the given date using <see cref="DateFormat"/>.
        /// </summary>
        /// <param name="date">Date to convert.</param>
        /// <returns>Formatted string, or an empty string if the date is null.</returns>
        public static string GetString(DateTime? date) => date?.ToString(DateFormat) ?? string.Empty;

        /// <summary>
        /// Parses the given string into a <see cref="DateTime"/> using <see cref="DateFormat"/>.
        /// </summary>
        /// <param name="dateString">String to parse.</param>
        /// <param name="acceptNull">When true, returns null if the string is empty; otherwise returns <see cref="DateTime.MinValue"/>.</param>
        /// <param name="exactMatch">When true, requires an exact format match.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or null / <see cref="DateTime.MinValue"/> when parsing fails.</returns>
        public static DateTime? GetDateTime(string dateString, bool acceptNull = true, bool exactMatch = false)
        {
            DateTime? dateTime = acceptNull ? null : (DateTime?)DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(dateString))
            {
                DateTime outDateTime;

                if (exactMatch && DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out outDateTime))
                {
                    dateTime = outDateTime;
                }
                else if (!exactMatch && DateTime.TryParse(dateString, out outDateTime))
                {
                    dateTime = outDateTime;
                }
            }

            return dateTime;
        }

        /// <summary>
        /// Parses the given string into a non-nullable <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateString">String to parse.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.MinValue"/> when parsing fails.</returns>
        public static DateTime GetDateTimeNotNull(string dateString) => (DateTime)GetDateTime(dateString, false);

    }
}
