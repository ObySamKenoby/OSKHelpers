using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Types.IsChanged;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OSKHelpers.Tests.Types.IsChanged
{
    [TestClass]
    // The tests change the static separators and culture: running them in parallel would make them interfere
    [DoNotParallelize]
    public class PropertyStringUtilsTests
    {
        #region Members

        private static string _decimalSeparator;
        private static string _thousandSeparator;
        private static CultureInfo _defaultCultureInfo;
        private static CultureInfo _threadCultureInfo;

        #endregion

        #region Methods

        static PropertyStringUtilsTests()
        {
            _threadCultureInfo  = CultureInfo.CurrentCulture;
            _defaultCultureInfo = PropertyStringUtils.CultureInfo;
            _decimalSeparator   = PropertyStringUtils.DecimalSeparator;
            _thousandSeparator  = PropertyStringUtils.ThousandsSeparator;
        }

        [TestInitialize]
        public void TestInitiallize()
        {
            // The expected values are written for the Italian culture, regardless of the culture of the system
            PropertyStringUtils.CultureInfo        = new CultureInfo("it-IT");
            PropertyStringUtils.DecimalSeparator   = _decimalSeparator;
            PropertyStringUtils.ThousandsSeparator = _thousandSeparator;
        }

        [ClassCleanup]
        public static void TestCleanup()
        {
            PropertyStringUtils.CultureInfo        = _defaultCultureInfo;
            PropertyStringUtils.DecimalSeparator   = _decimalSeparator;
            PropertyStringUtils.ThousandsSeparator = _thousandSeparator;
        }

        [TestMethod]
        public void Separators_SetValue_ReturnsSetValue()
        {
            Assert.AreEqual(",", PropertyStringUtils.DecimalSeparator);
            Assert.AreEqual(".", PropertyStringUtils.ThousandsSeparator);
            PropertyStringUtils.DecimalSeparator = "x";
            Assert.AreEqual("x", PropertyStringUtils.DecimalSeparator);
            PropertyStringUtils.DecimalSeparator = _decimalSeparator;
            Assert.AreEqual(PropertyStringUtils.DecimalSeparator, _decimalSeparator);
            PropertyStringUtils.ThousandsSeparator = "x";
            Assert.AreEqual("x", PropertyStringUtils.ThousandsSeparator);
            PropertyStringUtils.ThousandsSeparator = _decimalSeparator;
            Assert.AreEqual(PropertyStringUtils.ThousandsSeparator, _decimalSeparator);
        }

        [TestMethod]
        [DataRow(null, null)]
        [DynamicData(nameof(DecimalValues))]
        public void DisplayAsCurrency_NullableValues_ReturnsFormattedString(decimal? value, string expected)
        {
            Assert.AreEqual(expected, PropertyStringUtils.DisplayAsCurrency(value));
        }

        [TestMethod]
        [DynamicData(nameof(DecimalValues))]
        public void DisplayAsCurrency_Values_ReturnsFormattedString(decimal value, string expected)
        {
            Assert.AreEqual(expected, PropertyStringUtils.DisplayAsCurrency(value));
        }

        public static IEnumerable<object[]> DecimalValues
        {
            get
            {
                yield return new object[]{ 10m,        "10,00" };
                yield return new object[]{ 10.3m,      "10,30" };
                yield return new object[]{ 10.3049m,   "10,30" };
                yield return new object[]{ 10.3050m,   "10,31" };
                yield return new object[]{ 10.3051m,   "10,31" };
            }
        }

        [TestMethod]
        [DynamicData(nameof(DecimalPropertyValues))]
        public void SetProperty_Decimal_ReturnsExpected(bool result, string value, decimal expected)
        {
            decimal d = 10;
            Assert.AreEqual(result, PropertyStringUtils.SetProperty(ref d, value));
            Assert.AreEqual(expected, d);
        }

        [TestMethod]
        [DynamicData(nameof(NullDecimalPropertyValues))]
        public void SetProperty_NullableDecimal_ReturnsExpected(bool result, decimal? value, string newValue, decimal? expected)
        {
            decimal? d = value;
            Assert.AreEqual(result, PropertyStringUtils.SetProperty(ref d, newValue));
            Assert.AreEqual(expected, d);
        }

        public static IEnumerable<object[]> DecimalPropertyValues
        {
            get
            {
                yield return new object[] { false,  "10",       10m };
                yield return new object[] { false,  "10.0",     10m };
                yield return new object[] { false,  "10,0" ,    10m };
                yield return new object[] { false,  "1.0,0",    10m };
                yield return new object[] { false,  "1,0.0",    10m };
                yield return new object[] { true,   "1.0.11",   10.11m };
                yield return new object[] { true,   "1.0,11",   10.11m };
                yield return new object[] { true,   "1,0.11",   10.11m };
                yield return new object[] { true,   "1,0,11",   10.11m };
                yield return new object[] { true,   ".,10",     0.10m };
                yield return new object[] { true,   ",.10",     0.10m };
            }
        }

        public static IEnumerable<object[]> NullDecimalPropertyValues
        {
            get
            {
                yield return new object[] { false,  (decimal?)null,   null,       null };
                yield return new object[] { true,   (decimal?)null,   "10",       10m };
                yield return new object[] { true,   (decimal?)10m,    null,       null };
                yield return new object[] { false,  (decimal?)10m,    "10",       10m };
                yield return new object[] { false,  (decimal?)10m,    "10.0",     10m };
                yield return new object[] { false,  (decimal?)10m,    "10,0" ,    10m };
                yield return new object[] { false,  (decimal?)10m,    "1.0,0",    10m };
                yield return new object[] { false,  (decimal?)10m,    "1,0.0",    10m };
                yield return new object[] { true,   (decimal?)10m,    "1.0.11",   10.11m };
                yield return new object[] { true,   (decimal?)10m,    "1.0,11",   10.11m };
                yield return new object[] { true,   (decimal?)10m,    "1,0.11",   10.11m };
                yield return new object[] { true,   (decimal?)10m,    "1,0,11",   10.11m };
                yield return new object[] { true,   (decimal?)10m,    ".,10",     0.10m };
                yield return new object[] { true,   (decimal?)10m,    ",.10",     0.10m };
            }
        }

        [TestMethod]
        [DynamicData(nameof(TryParseDecimalPropertyValues))]
        public void TryParse_Decimal_ReturnsExpected(bool result, string value, decimal expected)
        {
            Assert.AreEqual(result, PropertyStringUtils.TryParse(value, out decimal d));
            Assert.AreEqual(expected, d);
        }

        [TestMethod]
        [DynamicData(nameof(TryParseNullDecimalPropertyValues))]
        public void TryParse_NullableDecimal_ReturnsExpected(bool result, string newValue, decimal? expected)
        {
            Assert.AreEqual(result, PropertyStringUtils.TryParse(newValue, out decimal? d));
            Assert.AreEqual(expected, d);
        }

        public static IEnumerable<object[]> TryParseDecimalPropertyValues
        {
            get
            {
                yield return new object[] { false,  null,       0m };
                yield return new object[] { false,  " ",        0m };
                yield return new object[] { false,  "1x0",      0m };
                yield return new object[] { false,  "10x",      0m };
                yield return new object[] { false,  "x10" ,     0m };
                yield return new object[] { false,  " 1.0,0",   0m };
                yield return new object[] { false,  " 1,0.0",   0m };
                yield return new object[] { true,   "10",       10m };
                yield return new object[] { true,   "10.0",     10m };
                yield return new object[] { true,   "10,0" ,    10m };
                yield return new object[] { true,   "1.0.11",   10.11m };
                yield return new object[] { true,   "1.0,11",   10.11m };
                yield return new object[] { true,   "1,0.11",   10.11m };
                yield return new object[] { true,   "1,0,11",   10.11m };
                yield return new object[] { true,   ".,10",     0.10m };
                yield return new object[] { true,   ",.10",     0.10m };
            }
        }

        public static IEnumerable<object[]> TryParseNullDecimalPropertyValues
        {
            get
            {
                yield return new object[] { false,  null,       null };
                yield return new object[] { false,  " ",        null };
                yield return new object[] { true,   "10",       10m };
                yield return new object[] { true,   "10",       10m };
                yield return new object[] { true,   "10.0",     10m };
                yield return new object[] { true,   "10,0" ,    10m };
                yield return new object[] { true,   "1.0,0",    10m };
                yield return new object[] { true,   "1,0.0",    10m };
                yield return new object[] { true,   "1.0.11",   10.11m };
                yield return new object[] { true,   "1.0,11",   10.11m };
                yield return new object[] { true,   "1,0.11",   10.11m };
                yield return new object[] { true,   "1,0,11",   10.11m };
                yield return new object[] { true,   ".,10",     0.10m };
                yield return new object[] { true,   ",.10",     0.10m };
            }
        }

        #region Defaults and checks

        /// <summary>
        /// Verifies that the default culture is the current culture at the first use of the class.
        /// </summary>
        [TestMethod]
        public void CultureInfo_Default_IsCurrentCulture()
        {
            Assert.AreEqual(_threadCultureInfo.Name, _defaultCultureInfo.Name);
        }

        /// <summary>
        /// Verifies that the conversions throw ArgumentNullException when a separator or the culture is not set.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void TryParse_SettingsNotSet_ThrowsArgumentNullException()
        {
            var originalCulture = PropertyStringUtils.CultureInfo;
            try
            {
                PropertyStringUtils.DecimalSeparator = string.Empty;
                Assert.AreEqual(nameof(PropertyStringUtils.DecimalSeparator), Assert.Throws<ArgumentNullException>(() => PropertyStringUtils.TryParse("1", out decimal _)).ParamName);
                PropertyStringUtils.DecimalSeparator = _decimalSeparator;

                PropertyStringUtils.ThousandsSeparator = null;
                Assert.AreEqual(nameof(PropertyStringUtils.ThousandsSeparator), Assert.Throws<ArgumentNullException>(() => PropertyStringUtils.TryParse("1", out DateTime _)).ParamName);
                PropertyStringUtils.ThousandsSeparator = _thousandSeparator;

                PropertyStringUtils.CultureInfo = null;
                Assert.AreEqual(nameof(PropertyStringUtils.CultureInfo), Assert.Throws<ArgumentNullException>(() => PropertyStringUtils.TryParse("1", out decimal? _)).ParamName);
            }
            finally
            {
                PropertyStringUtils.CultureInfo         = originalCulture;
                PropertyStringUtils.DecimalSeparator    = _decimalSeparator;
                PropertyStringUtils.ThousandsSeparator  = _thousandSeparator;
            }
        }

        #endregion

        #region Culture of the thread

        /// <summary>
        /// Verifies that the decimal conversion uses PropertyStringUtils.CultureInfo and not the culture of the thread.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-033")]
        public void TryParse_DecimalWithThreadCultureEnglish_UsesCultureInfo()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                Assert.IsTrue(PropertyStringUtils.TryParse("1.234,5", out decimal value));
                Assert.AreEqual(1234.5m, value);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// Verifies that DisplayAsCurrency uses PropertyStringUtils.CultureInfo and not the culture of the thread.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-033")]
        public void DisplayAsCurrency_ThreadCultureEnglish_UsesCultureInfo()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                Assert.AreEqual("1.234,50", PropertyStringUtils.DisplayAsCurrency(1234.5m));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        #endregion

        #region Nullable decimal

        /// <summary>
        /// Verifies that an invalid string leaves the nullable field unchanged and returns false (test plan decision 3).
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-034")]
        public void SetProperty_NullableDecimalInvalidString_LeavesFieldUnchanged()
        {
            decimal? field = 10m;

            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, "abc"));
            Assert.AreEqual(10m, field);
        }

        /// <summary>
        /// Verifies that an empty string sets the nullable field to null.
        /// </summary>
        [TestMethod]
        [DataRow("")]
        [DataRow("  ")]
        public void SetProperty_NullableDecimalEmptyString_SetsNull(string newValue)
        {
            decimal? field = 10m;

            Assert.IsTrue(PropertyStringUtils.SetProperty(ref field, newValue));
            Assert.IsNull(field);
        }

        /// <summary>
        /// Verifies ParseDecimal and ParseDecimalNullable for valid and invalid strings.
        /// </summary>
        [TestMethod]
        public void ParseDecimal_ValidAndInvalidStrings_ReturnsExpected()
        {
            Assert.AreEqual(1234.5m, PropertyStringUtils.ParseDecimal("1.234,5"));
            Assert.AreEqual(0m, PropertyStringUtils.ParseDecimal("abc"));
            Assert.AreEqual(1234.5m, PropertyStringUtils.ParseDecimalNullable("1.234,5"));
            Assert.IsNull(PropertyStringUtils.ParseDecimalNullable("abc"));
            Assert.IsNull(PropertyStringUtils.ParseDecimalNullable(null));
        }

        #endregion

        #region DateTime

        /// <summary>
        /// Verifies that TryParse(DateTime) uses the Italian culture (day/month/year).
        /// </summary>
        [TestMethod]
        public void TryParse_DateTimeItalianFormat_ReturnsDate()
        {
            Assert.IsTrue(PropertyStringUtils.TryParse("21/10/2026 13:14", out DateTime value));
            Assert.AreEqual(new DateTime(2026, 10, 21, 13, 14, 0), value);
        }

        /// <summary>
        /// Verifies that an invalid string returns false with DateTime.MinValue, or null for the nullable overload.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("31/02/2026")]
        [DataRow("abc")]
        public void TryParse_DateTimeInvalidString_ReturnsFalse(string text)
        {
            Assert.IsFalse(PropertyStringUtils.TryParse(text, out DateTime value));
            Assert.AreEqual(DateTime.MinValue, value);
            Assert.IsFalse(PropertyStringUtils.TryParse(text, out DateTime? nullableValue));
            Assert.IsNull(nullableValue);
        }

        /// <summary>
        /// Verifies SetProperty(DateTime): new value, same value and invalid string.
        /// </summary>
        [TestMethod]
        public void SetProperty_DateTime_ReturnsWhetherChanged()
        {
            var field = new DateTime(2026, 1, 1);

            Assert.IsTrue(PropertyStringUtils.SetProperty(ref field, "21/10/2026"));
            Assert.AreEqual(new DateTime(2026, 10, 21), field);
            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, "21/10/2026"));
            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, "abc"));
            Assert.AreEqual(new DateTime(2026, 10, 21), field);
        }

        /// <summary>
        /// Verifies SetProperty(DateTime?): new value, same value and empty string.
        /// </summary>
        [TestMethod]
        public void SetProperty_NullableDateTime_ReturnsWhetherChanged()
        {
            DateTime? field = null;

            Assert.IsTrue(PropertyStringUtils.SetProperty(ref field, "21/10/2026"));
            Assert.AreEqual(new DateTime(2026, 10, 21), field);
            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, "21/10/2026"));
            Assert.IsTrue(PropertyStringUtils.SetProperty(ref field, ""));
            Assert.IsNull(field);
            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, ""));
        }

        /// <summary>
        /// Verifies that an invalid string leaves the nullable field unchanged and returns false (test plan decision 3).
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-034")]
        public void SetProperty_NullableDateTimeInvalidString_LeavesFieldUnchanged()
        {
            DateTime? field = new DateTime(2026, 10, 21);

            Assert.IsFalse(PropertyStringUtils.SetProperty(ref field, "abc"));
            Assert.AreEqual(new DateTime(2026, 10, 21), field);
        }

        /// <summary>
        /// Verifies ParseDateTime and ParseDateTimeNullable for valid and invalid strings.
        /// </summary>
        [TestMethod]
        public void ParseDateTime_ValidAndInvalidStrings_ReturnsExpected()
        {
            Assert.AreEqual(new DateTime(2026, 10, 21), PropertyStringUtils.ParseDateTime("21/10/2026"));
            Assert.AreEqual(DateTime.MinValue, PropertyStringUtils.ParseDateTime("abc"));
            Assert.AreEqual(new DateTime(2026, 10, 21), PropertyStringUtils.ParseDateTimeNullable("21/10/2026"));
            Assert.IsNull(PropertyStringUtils.ParseDateTimeNullable("abc"));
        }

        #endregion

        #region gRPC conversions

        /// <summary>
        /// Verifies the round trip of the gRPC conversions (ISO 8601 format).
        /// </summary>
        [TestMethod]
        public void ToGRPCStringAndFromGRPCDateTime_Date_RoundTrip()
        {
            var date = new DateTime(2026, 10, 21, 13, 14, 15);

            var text = PropertyStringUtils.ToGRPCString(date);

            Assert.AreEqual("2026-10-21T13:14:15", text);
            Assert.AreEqual(date, PropertyStringUtils.FromGRPCDateTime(text));
            Assert.AreEqual(date, PropertyStringUtils.FromGRPCDateTimeNullable(text));
        }

        /// <summary>
        /// Verifies the gRPC conversions of an invalid string.
        /// </summary>
        [TestMethod]
        public void FromGRPCDateTime_InvalidString_ReturnsMinValueOrNull()
        {
            Assert.AreEqual(DateTime.MinValue, PropertyStringUtils.FromGRPCDateTime("abc"));
            Assert.IsNull(PropertyStringUtils.FromGRPCDateTimeNullable("abc"));
        }

        #endregion

        #endregion

    }
}