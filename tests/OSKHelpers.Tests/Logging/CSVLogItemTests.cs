using System;
using System.Globalization;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Logging
{
    /// <summary>
    /// Tests for the <see cref="CSVLogItem"/> class.<br/>
    /// Expected format from ModelloDominio.md: numbers in invariant culture, DateTime/DateOnly in ISO format ("s"),
    /// strings and other types in double quotes (test plan decisions 1 and 2).
    /// </summary>
    [TestClass]
    public class CSVLogItemTests
    {
        #region Support classes

        /// <summary>
        /// Log item with properties of the various supported types.
        /// </summary>
        private class SampleItem : CSVLogItem
        {
            public string Name { get; set; }
            public decimal Amount { get; set; }
            public int Count { get; set; }
            public DateTime Date { get; set; }
            public DateOnly Day { get; set; }
            public bool Flag { get; set; }
        }

        /// <summary>
        /// Log item with a nullable string property.
        /// </summary>
        private class NullableItem : CSVLogItem
        {
            public string Name { get; set; }
            public string Note { get; set; }
        }

        #endregion

        #region Members

        private static readonly DateTime _date = new DateTime(2026, 10, 21, 13, 14, 15);

        #endregion

        #region GetCSVHeader

        /// <summary>
        /// Verifies that the header contains the public property names in alphabetical order, separated by CSVSeparator.
        /// </summary>
        [TestMethod]
        public void GetCSVHeader_Item_ReturnsSortedPropertyNames()
        {
            Assert.AreEqual("Amount;Count;Date;Day;Flag;Name", new SampleItem().GetCSVHeader());
        }

        /// <summary>
        /// Verifies that a custom separator is used in the header.
        /// </summary>
        [TestMethod]
        public void GetCSVHeader_CustomSeparator_UsesSeparator()
        {
            var item = new SampleItem { CSVSeparator = "|" };

            Assert.AreEqual("Amount|Count|Date|Day|Flag|Name", item.GetCSVHeader());
        }

        /// <summary>
        /// Verifies that the static method works on any object.
        /// </summary>
        [TestMethod]
        public void GetCSVHeader_AnyObject_ReturnsSortedPropertyNames()
        {
            Assert.AreEqual("Major,Minor", CSVLogItem.GetCSVHeader(new { Minor = 1, Major = 2 }, ","));
        }

        /// <summary>
        /// Verifies that a null object throws ArgumentNullException for the obj parameter.
        /// </summary>
        [TestMethod]
        public void GetCSVHeader_NullObject_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CSVLogItem.GetCSVHeader(null, ";"));

            Assert.AreEqual("obj", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a null separator throws ArgumentNullException for the separator parameter.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-026")]
        public void GetCSVHeader_NullSeparator_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CSVLogItem.GetCSVHeader(new SampleItem(), null));

            Assert.AreEqual("separator", ex.ParamName);
        }

        #endregion

        #region GetCSVData

        /// <summary>
        /// Verifies the format of the values: invariant numbers, ISO dates, quoted strings and other types.
        /// </summary>
        [TestMethod]
        public void GetCSVData_VariousTypes_FormatsValues()
        {
            var item = new SampleItem
            {
                Name    = "Test",
                Amount  = 1234.5m,
                Count   = 3,
                Date    = _date,
                Day     = new DateOnly(2026, 10, 21),
                Flag    = true
            };

            Assert.AreEqual("1234.5;3;2026-10-21T13:14:15;2026-10-21T00:00:00;\"True\";\"Test\"", item.GetCSVData());
        }

        /// <summary>
        /// Verifies that numbers do not depend on the current culture.
        /// </summary>
        [TestMethod]
        public void GetCSVData_ItalianCulture_UsesInvariantNumbers()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("it-IT");

                var data = new SampleItem { Amount = 1234.5m }.GetCSVData();

                Assert.StartsWith("1234.5;", data);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// Verifies that a custom separator is used in the data row.
        /// </summary>
        [TestMethod]
        public void GetCSVData_CustomSeparator_UsesSeparator()
        {
            var item = new NullableItem { Name = "A", Note = "B", CSVSeparator = "|" };

            Assert.AreEqual("\"A\"|\"B\"", item.GetCSVData());
        }

        /// <summary>
        /// Verifies that a null property is written as an empty quoted field (test plan decision 2).
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-028")]
        public void GetCSVData_NullProperty_WritesEmptyQuotedField()
        {
            var item = new NullableItem { Name = "A", Note = null };

            Assert.AreEqual("\"A\";\"\"", item.GetCSVData());
        }

        /// <summary>
        /// Verifies that a null object throws ArgumentNullException for the obj parameter.
        /// </summary>
        [TestMethod]
        public void GetCSVData_NullObject_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CSVLogItem.GetCSVData(null, ";"));

            Assert.AreEqual("obj", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a null separator throws ArgumentNullException for the separator parameter.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-026")]
        public void GetCSVData_NullSeparator_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CSVLogItem.GetCSVData(new SampleItem(), null));

            Assert.AreEqual("separator", ex.ParamName);
        }

        /// <summary>
        /// Verifies that header and data have the same number of fields.
        /// </summary>
        [TestMethod]
        public void GetCSVData_Item_HasSameFieldCountAsHeader()
        {
            var item = new SampleItem { Name = "Test" };

            Assert.HasCount(item.GetCSVHeader().Split(';').Length, item.GetCSVData().Split(';'));
        }

        #endregion
    }
}
