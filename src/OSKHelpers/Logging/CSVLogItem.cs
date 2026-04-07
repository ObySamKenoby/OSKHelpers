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
                throw new ArgumentNullException(separator);
            }
            Type type = obj.GetType();

            return string.Join(separator, type.GetProperties().Select(p => p.Name).OrderBy(n => n));
        }

        /// <summary>
        /// Returns the default CSV data row containing the values of all public properties sorted alphabetically.<br/>
        /// Data is formatted as follows:<br/>
        /// <b>strings</b>: enclosed in double quotes ("");<br/>
        /// <b>numbers</b>: no quotes, no thousands separator, dot as decimal separator;<br/>
        /// <b>dates (or date/time)</b>: no quotes, prefixed with "D", ISO UTC format;<br/>
        /// <b>other types</b>: treated as strings, enclosed in double quotes, represented via ToString().
        /// </summary>
        /// <param name="obj">Object for which to create the data row.</param>
        /// <param name="separator">Separator to use.</param>
        /// <returns>The string containing the data row.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <summary>
        public static string GetCSVData(object obj, string separator)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(separator);
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
#if NET8_0_OR_GREATER
                else if (value is DateOnly)
                {
                    var v = (DateOnly)value;
                    fields.Add(new DateTime(v.Year, v.Month, v.Day).ToString("s"));
                }
#endif
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
                        fields.Add($"\"{value.ToString()}\"");
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

#endregion
    }
}
