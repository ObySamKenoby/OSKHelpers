using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Types.IsChanged;
using System.Collections.Generic;

namespace AffidamentiWebBlazor.Tests.Utils
{
    [TestClass]
    public class PropertyStringUtilsTests
    {
        #region Members

        private static string _decimalSeparator;
        private static string _thousandSeparator;

        #endregion

        #region Methods

        static PropertyStringUtilsTests()
        {
            _decimalSeparator = PropertyStringUtils.DecimalSeparator;
            _thousandSeparator =  PropertyStringUtils.ThousandsSeparator;
        }

        [TestInitialize]
        public void TestInitiallize()
        {
            PropertyStringUtils.DecimalSeparator   = _decimalSeparator;
            PropertyStringUtils.ThousandsSeparator = _thousandSeparator;
        }

        [ClassCleanup]
        public static void TestCleanup()
        {
            PropertyStringUtils.DecimalSeparator   = _decimalSeparator;
            PropertyStringUtils.ThousandsSeparator = _thousandSeparator;
        }

        [TestMethod]
        public void CheckSeparators()
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
        public void DisplayValueNullTests(decimal? value, string expected)
        {
            Assert.AreEqual(expected, PropertyStringUtils.DisplayAsCurrency(value));
        }

        [TestMethod]
        [DynamicData(nameof(DecimalValues))]
        public void DisplayValueTests(decimal value, string expected)
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
        public void SetPropertyDecimalTests(bool result, string value, decimal expected)
        {
            decimal d = 10;
            Assert.AreEqual(result, PropertyStringUtils.SetProperty(ref d, value));
            Assert.AreEqual(expected, d);
        }

        [TestMethod]
        [DynamicData(nameof(NullDecimalPropertyValues))]
        public void SetPropertyNullDecimalTests(bool result, decimal? value, string newValue, decimal? expected)
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
        public void TryParseDecimalTests(bool result, string value, decimal expected)
        {
            Assert.AreEqual(result, PropertyStringUtils.TryParse(value, out decimal d));
            Assert.AreEqual(expected, d);
        }

        [TestMethod]
        [DynamicData(nameof(TryParseNullDecimalPropertyValues))]
        public void TryParseNullDecimalTests(bool result, string newValue, decimal? expected)
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


        #endregion

    }
}