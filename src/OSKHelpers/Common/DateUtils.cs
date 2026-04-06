using System;
using System.Globalization;

namespace OSKHelpers.Common
{
    public class DateUtils
    {
        public static string DateFormat { get; set; }

        static DateUtils()
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss:ffff";
        }

        public static string GetString(DateTime? date) => date?.ToString(DateFormat) ?? string.Empty;
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
        public static DateTime GetDateTimeNotNull(string dateString) => (DateTime)GetDateTime(dateString, false);

    }
}
