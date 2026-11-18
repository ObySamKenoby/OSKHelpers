using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace OSKHelpers.INIFile
{
    /// <summary>
    /// Conversion of the values of an INI file into the types of the properties of a settings class, used by
    /// <see cref="Json.JsonSettings{T}.MigrateFromIniFile"/>.<br/>
    /// Numbers and dates are converted with <see cref="CultureInfo.InvariantCulture"/>, booleans with the rules of
    /// <see cref="IniFileHelper"/> (TRUE, FALSE, 1, 0).
    /// </summary>
    internal static class IniValueConverter
    {
        #region Members

        /// <summary>
        /// Converts a text into a value of a given type.
        /// </summary>
        /// <param name="text">Text to convert.</param>
        /// <param name="value">The value converted.</param>
        /// <returns>True if the text is valid for the type.</returns>
        private delegate bool TryParseValue(string text, out object value);

        /// <summary>
        /// Conversion functions of the supported scalar types, enums excluded (handled by <see cref="TryParseEnum"/>).
        /// </summary>
        private static readonly Dictionary<Type, TryParseValue> _parsers;

        /// <summary>
        /// Generic collection types that can be built from an array of the INI file, besides the one-dimensional arrays.
        /// </summary>
        private static readonly HashSet<Type> _supportedCollectionTypes;

        #endregion

        #region Constructors

        static IniValueConverter()
        {
            _parsers = new Dictionary<Type, TryParseValue>
            {
                { typeof(string),   (string t, out object v) => { v = t; return true; } },
                { typeof(bool),     (string t, out object v) => { var ok = IniFileHelper.TryParseBool(t, out bool r); v = r; return ok; } },
                { typeof(int),      (string t, out object v) => { var ok = int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int r); v = r; return ok; } },
                { typeof(long),     (string t, out object v) => { var ok = long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out long r); v = r; return ok; } },
                { typeof(short),    (string t, out object v) => { var ok = short.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out short r); v = r; return ok; } },
                { typeof(byte),     (string t, out object v) => { var ok = byte.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r); v = r; return ok; } },
                { typeof(sbyte),    (string t, out object v) => { var ok = sbyte.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte r); v = r; return ok; } },
                { typeof(uint),     (string t, out object v) => { var ok = uint.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint r); v = r; return ok; } },
                { typeof(ulong),    (string t, out object v) => { var ok = ulong.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong r); v = r; return ok; } },
                { typeof(ushort),   (string t, out object v) => { var ok = ushort.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort r); v = r; return ok; } },
                // NumberStyles.Float (not Number) for decimal too: thousands separators are rejected, so that "1,5" is not read as 15.
                { typeof(decimal),  (string t, out object v) => { var ok = decimal.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal r); v = r; return ok; } },
                { typeof(double),   (string t, out object v) => { var ok = double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double r); v = r; return ok; } },
                { typeof(float),    (string t, out object v) => { var ok = float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out float r); v = r; return ok; } },
                { typeof(DateTime), (string t, out object v) => { var ok = DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime r); v = r; return ok; } },
                { typeof(TimeSpan), (string t, out object v) => { var ok = TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out TimeSpan r); v = r; return ok; } },
                { typeof(Guid),     (string t, out object v) => { var ok = Guid.TryParse(t, out Guid r); v = r; return ok; } }
            };

            _supportedCollectionTypes = new HashSet<Type>
            {
                typeof(List<>),
                typeof(IList<>),
                typeof(ICollection<>),
                typeof(IEnumerable<>),
                typeof(IReadOnlyList<>),
                typeof(IReadOnlyCollection<>)
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns true if values of <paramref name="type"/> can be converted from a single key of the INI file.
        /// </summary>
        /// <param name="type">Type of the property; a <see cref="Nullable{T}"/> is handled as its underlying type.</param>
        /// <returns>True if the type is a supported scalar type.</returns>
        public static bool IsSupportedScalar(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.IsEnum || _parsers.ContainsKey(underlyingType);
        }

        /// <summary>
        /// Returns true if values of <paramref name="type"/> can be built from an array of the INI file: one-dimensional arrays
        /// and <see cref="List{T}"/>, <see cref="IList{T}"/>, <see cref="ICollection{T}"/>, <see cref="IEnumerable{T}"/>,
        /// <see cref="IReadOnlyList{T}"/>, <see cref="IReadOnlyCollection{T}"/> of a supported scalar type.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="elementType">Type of the elements; null if the result is false.</param>
        /// <returns>True if the type is a supported collection.</returns>
        public static bool IsSupportedCollection(Type type, out Type elementType)
        {
            elementType = null;

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType && _supportedCollectionTypes.Contains(type.GetGenericTypeDefinition()))
            {
                elementType = type.GetGenericArguments()[0];
            }

            if (elementType != null && !IsSupportedScalar(elementType))
            {
                elementType = null;
            }

            return elementType != null;
        }

        /// <summary>
        /// Returns true if <paramref name="elementType"/> is <see cref="DayOfWeek"/> (or its nullable form): such a collection
        /// can also be read from a single key in the format of <see cref="DaysOfWeekParameter"/> (e.g. <c>1 3 5</c>).
        /// </summary>
        /// <param name="elementType">Type of the elements of the collection.</param>
        /// <returns>True for collections of days of the week.</returns>
        public static bool IsDayOfWeek(Type elementType) => (Nullable.GetUnderlyingType(elementType) ?? elementType) == typeof(DayOfWeek);

        /// <summary>
        /// Converts the value of a single key into <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Supported scalar type (see <see cref="IsSupportedScalar"/>).</param>
        /// <param name="text">Value of the key.</param>
        /// <param name="value">The value converted; null if the result is false.</param>
        /// <returns>True if the value has been converted.</returns>
        public static bool TryConvertValue(Type type, string text, out object value)
        {
            var underlyingType  = Nullable.GetUnderlyingType(type) ?? type;
            var converted       = false;
            value               = null;

            if (text != null)
            {
                if (underlyingType.IsEnum)
                {
                    converted = TryParseEnum(underlyingType, text.Trim(), out value);
                }
                else if (_parsers.TryGetValue(underlyingType, out TryParseValue parser))
                {
                    // Strings keep the value as read (already trimmed by IniFileHelper), the other types are trimmed again.
                    converted = parser(underlyingType == typeof(string) ? text : text.Trim(), out value);
                }
            }

            if (!converted)
            {
                value = null;
            }

            return converted;
        }

        /// <summary>
        /// Builds a collection of type <paramref name="collectionType"/> from the elements of an array of the INI file.
        /// </summary>
        /// <param name="collectionType">Supported collection type (see <see cref="IsSupportedCollection"/>).</param>
        /// <param name="elementType">Type of the elements.</param>
        /// <param name="elements">Elements of the array.</param>
        /// <param name="value">The collection built; null if the result is false.</param>
        /// <param name="invalidElement">The first element that cannot be converted; null if the result is true.</param>
        /// <returns>True if every element has been converted.</returns>
        public static bool TryConvertArray(Type collectionType, Type elementType, IEnumerable<string> elements, out object value,
            out string invalidElement)
        {
            var list        = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            invalidElement  = null;
            value           = null;

            foreach (var element in elements)
            {
                if (invalidElement == null)
                {
                    if (TryConvertValue(elementType, element, out object elementValue))
                    {
                        list.Add(elementValue);
                    }
                    else
                    {
                        invalidElement = element ?? string.Empty;
                    }
                }
            }

            if (invalidElement == null)
            {
                value = ToCollectionType(collectionType, elementType, list);
            }

            return invalidElement == null;
        }

        /// <summary>
        /// Builds a collection of days of the week from a single key in the format of <see cref="DaysOfWeekParameter"/>
        /// (integer codes separated by spaces, 0 = Sunday … 6 = Saturday).
        /// </summary>
        /// <param name="collectionType">Supported collection type of <see cref="DayOfWeek"/>.</param>
        /// <param name="elementType"><see cref="DayOfWeek"/> or its nullable form.</param>
        /// <param name="text">Value of the key.</param>
        /// <param name="value">The collection built; null if the result is false.</param>
        /// <returns>True if the value is valid.</returns>
        public static bool TryConvertDaysOfWeek(Type collectionType, Type elementType, string text, out object value)
        {
            var converted   = false;
            value           = null;

            try
            {
                var days = new DaysOfWeekParameter(text);
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                foreach (var day in days.DaysOfWeek)
                {
                    list.Add(day);
                }
                value       = ToCollectionType(collectionType, elementType, list);
                converted   = true;
            }
            catch (ArgumentException)
            {
                // Empty value or invalid day code (ArgumentNullException, ArgumentOutOfRangeException): not converted.
                value = null;
            }

            return converted;
        }

        /// <summary>
        /// Converts a text into a value of an enum: name or numeric value, not case sensitive. The value must be defined,
        /// unless the enum is marked with <see cref="FlagsAttribute"/>.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="text">Text to convert, already trimmed.</param>
        /// <param name="value">The value converted; null if the result is false.</param>
        /// <returns>True if the text is a valid value of the enum.</returns>
        private static bool TryParseEnum(Type enumType, string text, out object value)
        {
            var converted   = false;
            value           = null;

            try
            {
                // Enum.TryParse(Type, ...) is not available in netstandard2.0.
                var parsed  = Enum.Parse(enumType, text, true);
                converted   = enumType.IsDefined(typeof(FlagsAttribute), false) || Enum.IsDefined(enumType, parsed);
                value       = converted ? parsed : null;
            }
            catch (ArgumentException)
            {
                value = null;
            }
            catch (OverflowException)
            {
                value = null;
            }

            return converted;
        }

        /// <summary>
        /// Returns <paramref name="list"/> as an array, if <paramref name="collectionType"/> is an array, otherwise the list
        /// itself, which implements all the supported generic collection interfaces.
        /// </summary>
        /// <param name="collectionType">Type of the property.</param>
        /// <param name="elementType">Type of the elements.</param>
        /// <param name="list">List of the converted elements (<see cref="List{T}"/> of <paramref name="elementType"/>).</param>
        /// <returns>The value to assign to the property.</returns>
        private static object ToCollectionType(Type collectionType, Type elementType, IList list)
        {
            object result = list;

            if (collectionType.IsArray)
            {
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                result = array;
            }

            return result;
        }

        #endregion
    }
}
