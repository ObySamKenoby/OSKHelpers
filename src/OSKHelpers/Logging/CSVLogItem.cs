using OSKHelpers.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Base class that automatically generates CSV headers and rows from all public properties of the object.
    /// </summary>
    public class CSVLogItem : ICSVLogItem
    {
        #region Constants

        /// <summary>
        /// Full name of the System.DateOnly type, recognized by name so that the netstandard2.0 build, which does not know
        /// the type, formats it like the .NET 8 build.
        /// </summary>
        private const string DATEONLYTYPENAME = "System.DateOnly";

        /// <summary>
        /// Custom format of the date part of a DateOnly value: together with DATEONLYTIME it produces the same result as the
        /// sortable format "s" of a DateTime at midnight.
        /// </summary>
        private const string DATEONLYFORMAT = "yyyy-MM-dd";

        /// <summary>
        /// Time part appended to a DateOnly value, which has no time (DateOnly rejects the time specifiers in the format).
        /// </summary>
        private const string DATEONLYTIME = "T00:00:00";

        #endregion

        #region Properties

        /// <summary>
        /// Separator used in headers and data to divide values.
        /// </summary>
        public string CSVSeparator = ";";

        #endregion

        #region Methods

        /// <inheritdoc cref="ICSVLogItem.GetCSVData"/>
        public string GetCSVHeader()
        {
            return GetCSVHeader(this, CSVSeparator);
        }

        /// <inheritdoc cref="ICSVLogItem.GetCSVData"/>
        public string GetCSVData()
        {
            return GetCSVData(this, CSVSeparator);
        }

        /// <summary>
        /// Returns the default CSV header string containing the names of all public properties sorted alphabetically.
        /// </summary>
        /// <param name="obj">Object for which to create the headers.</param>
        /// <param name="separator">Separator to use.</param>
        /// <returns>The string containing the headers.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static string GetCSVHeader(object obj, string separator)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            Type type = obj.GetType();

            return string.Join(separator, type.GetProperties().Select(p => p.Name).OrderBy(n => n));
        }

        /// <summary>
        /// Returns the default CSV data row containing the values of all public properties sorted alphabetically.<br/>
        /// Data is formatted as follows:<br/>
        /// <b>strings</b>: enclosed in double quotes ("");<br/>
        /// <b>numbers</b>: no quotes, no thousands separator, dot as decimal separator;<br/>
        /// <b>dates (DateTime and DateOnly)</b>: no quotes, ISO 8601 sortable format ("s", yyyy-MM-ddTHH:mm:ss),<br/>
        /// without conversion to UTC; DateOnly is written at midnight, with every target framework of the library;<br/>
        /// <b>other types</b>: treated as strings, enclosed in double quotes, represented via ToString().
        /// </summary>
        /// <param name="obj">Object for which to create the data row.</param>
        /// <param name="separator">Separator to use.</param>
        /// <returns>The string containing the data row.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static string GetCSVData(object obj, string separator)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            Type type = obj.GetType();

            var fields = new List<string>();
            foreach (var property in type.GetProperties().OrderBy(p => p.Name))
            {
                try
                {
                    var value = property.GetValue(obj);
                    if (ObjectUtils.IsNumeric(value))
                    {
                        fields.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
                    }
                    else if (IsDateOnly(value))
                    {
                        fields.Add(((IFormattable)value).ToString(DATEONLYFORMAT, CultureInfo.InvariantCulture) + DATEONLYTIME);
                    }
                    else if (value is DateTime)
                    {
                        fields.Add(((DateTime)value).ToString("s"));
                    }
                    else if (value is string)
                    {
                        fields.Add($"\"{value}\"");
                    }
                    else
                    {
                        // A null value is written as an empty quoted field: it is not a read error
                        fields.Add($"\"{value?.ToString()}\"");
                    }
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex);
                    fields.Add($"#ERR ({ex.Message})#");
                }
            }

            return string.Join(separator, fields);
        }

        /// <summary>
        /// Checks whether the value is a System.DateOnly.
        /// </summary>
        /// <remarks>
        /// The type is recognized by its full name instead of with "is DateOnly" because the netstandard2.0 build does not
        /// know it, while an application running on .NET 6 or later can still pass it (intervento 045).
        /// </remarks>
        /// <param name="value">Value to check; it can be null.</param>
        /// <returns><c>true</c> if the value is a DateOnly that can be formatted through IFormattable.</returns>
        private static bool IsDateOnly(object value)
        {
            return value is IFormattable && value.GetType().FullName == DATEONLYTYPENAME;
        }

        #endregion
    }
}
