using System;
using System.Globalization;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="DateUtils"/> class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DateUtilsTests
    {
        #region Constants

        /// <summary>
        /// Default value of <see cref="DateUtils.DateFormat"/>.
        /// </summary>
        private const string DEFAULTFORMAT = "yyyy-MM-dd HH:mm:ss:ffff";

        #endregion

        #region Members

        private string _originalFormat;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Saves the current date format.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _originalFormat = DateUtils.DateFormat;
        }

        /// <summary>
        /// Restores the date format changed by the tests.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            DateUtils.DateFormat = _originalFormat;
        }

        #endregion

        #region DateFormat

        /// <summary>
        /// Verifies the documented default value of DateFormat.
        /// </summary>
        [TestMethod]
        public void DateFormat_Default_IsDocumentedFormat()
        {
            Assert.AreEqual(DEFAULTFORMAT, DateUtils.DateFormat);
        }

        #endregion

        #region GetString

        /// <summary>
        /// Verifies that a null date returns an empty string.
        /// </summary>
        [TestMethod]
        public void GetString_NullDate_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, DateUtils.GetString(null));
        }

        /// <summary>
        /// Verifies that the date is formatted with DateFormat.
        /// </summary>
        [TestMethod]
        public void GetString_ValidDate_UsesDateFormat()
        {
            var date = new DateTime(2026, 10, 21, 13, 14, 15, 678);

            Assert.AreEqual(date.ToString(DEFAULTFORMAT), DateUtils.GetString(date));
        }

        /// <summary>
        /// Verifies that a custom DateFormat is used.
        /// </summary>
        [TestMethod]
        public void GetString_CustomFormat_UsesCustomFormat()
        {
            DateUtils.DateFormat = "dd/MM/yyyy";

            Assert.AreEqual("21/10/2026", DateUtils.GetString(new DateTime(2026, 10, 21)));
        }

        #endregion

        #region GetDateTime

        /// <summary>
        /// Verifies that an empty string returns null when acceptNull is true.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void GetDateTime_EmptyStringAcceptNull_ReturnsNull(string dateString)
        {
            Assert.IsNull(DateUtils.GetDateTime(dateString));
        }

        /// <summary>
        /// Verifies that an empty string returns DateTime.MinValue when acceptNull is false.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GetDateTime_EmptyStringNotAcceptNull_ReturnsMinValue(string dateString)
        {
            Assert.AreEqual(DateTime.MinValue, DateUtils.GetDateTime(dateString, false));
        }

        /// <summary>
        /// Verifies that an invalid string returns null, or DateTime.MinValue when acceptNull is false.
        /// </summary>
        [TestMethod]
        public void GetDateTime_InvalidString_ReturnsNullOrMinValue()
        {
            Assert.IsNull(DateUtils.GetDateTime("not a date"));
            Assert.AreEqual(DateTime.MinValue, DateUtils.GetDateTime("not a date", false));
        }

        /// <summary>
        /// Verifies that the exact match parses a string produced by GetString.
        /// </summary>
        [TestMethod]
        public void GetDateTime_ExactMatchWithDateFormat_ReturnsDate()
        {
            var date = new DateTime(2026, 10, 21, 13, 14, 15).AddTicks(1234000);

            Assert.AreEqual(date, DateUtils.GetDateTime(DateUtils.GetString(date), true, true));
        }

        /// <summary>
        /// Verifies that the exact match rejects a string that does not follow DateFormat.
        /// </summary>
        [TestMethod]
        public void GetDateTime_ExactMatchDifferentFormat_ReturnsNull()
        {
            Assert.IsNull(DateUtils.GetDateTime("2026-10-21", true, true));
        }

        /// <summary>
        /// Verifies that the non-exact match parses a standard date string.
        /// </summary>
        [TestMethod]
        public void GetDateTime_NotExactMatchStandardString_ReturnsDate()
        {
            var text = new DateTime(2026, 10, 21, 13, 14, 15).ToString(CultureInfo.CurrentCulture);

            Assert.AreEqual(new DateTime(2026, 10, 21, 13, 14, 15), DateUtils.GetDateTime(text));
        }

        #endregion

        #region GetDateTimeNotNull

        /// <summary>
        /// Verifies that an invalid or empty string returns DateTime.MinValue.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not a date")]
        public void GetDateTimeNotNull_InvalidString_ReturnsMinValue(string dateString)
        {
            Assert.AreEqual(DateTime.MinValue, DateUtils.GetDateTimeNotNull(dateString));
        }

        /// <summary>
        /// Verifies that a valid string returns the parsed date.
        /// </summary>
        [TestMethod]
        public void GetDateTimeNotNull_ValidString_ReturnsDate()
        {
            var text = new DateTime(2026, 10, 21).ToString(CultureInfo.CurrentCulture);

            Assert.AreEqual(new DateTime(2026, 10, 21), DateUtils.GetDateTimeNotNull(text));
        }

        #endregion
    }
}
