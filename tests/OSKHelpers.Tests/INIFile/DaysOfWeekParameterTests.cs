using System;
using System.Collections.Generic;
using System.Linq;
using OSKHelpers.INIFile;

namespace OSKHelpers.Tests.INIFile
{
    /// <summary>
    /// Tests for the <see cref="DaysOfWeekParameter"/> class.
    /// </summary>
    [TestClass]
    public class DaysOfWeekParameterTests
    {
        #region Constructors

        /// <summary>
        /// Verifies that the default constructor creates an empty instance.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_CreatesEmptyList()
        {
            var dow = new DaysOfWeekParameter();

            Assert.AreEqual(0, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies the constructor with a valid string.
        /// </summary>
        [TestMethod]
        public void Constructor_ValidString_ParsesDays()
        {
            var dow = new DaysOfWeekParameter("1 3 5");

            Assert.AreEqual(3, dow.DaysOfWeek.Count);
            Assert.IsTrue(dow.Contains(DayOfWeek.Monday));
            Assert.IsTrue(dow.Contains(DayOfWeek.Wednesday));
            Assert.IsTrue(dow.Contains(DayOfWeek.Friday));
        }

        /// <summary>
        /// Verifies that all days of the week are parsed correctly (0-6).
        /// </summary>
        [TestMethod]
        public void Constructor_AllDays_ParsesAllSeven()
        {
            var dow = new DaysOfWeekParameter("0 1 2 3 4 5 6");

            Assert.AreEqual(7, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that the constructor with a null string throws an exception.
        /// </summary>
        [TestMethod]
        public void Constructor_NullString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DaysOfWeekParameter(null));
        }

        /// <summary>
        /// Verifies that the constructor with an empty string throws an exception.
        /// </summary>
        [TestMethod]
        public void Constructor_EmptyString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DaysOfWeekParameter(""));
        }

        /// <summary>
        /// Verifies that the constructor with an invalid value throws an exception.
        /// </summary>
        [TestMethod]
        public void Constructor_InvalidDay_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DaysOfWeekParameter("7"));
        }

        /// <summary>
        /// Verifies that the constructor with a non-numeric value throws an exception.
        /// </summary>
        [TestMethod]
        public void Constructor_NonNumericDay_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DaysOfWeekParameter("abc"));
        }

        /// <summary>
        /// Verifies that multiple spaces in the string are handled correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_ExtraSpaces_ParsesCorrectly()
        {
            var dow = new DaysOfWeekParameter("  1   3   5  ");

            Assert.AreEqual(3, dow.DaysOfWeek.Count);
        }

        #endregion

        #region Add

        /// <summary>
        /// Verifies that Add(int) adds a valid day.
        /// </summary>
        [TestMethod]
        public void Add_ValidIntDay_AddsDayToList()
        {
            var dow = new DaysOfWeekParameter();
            dow.Add(0);

            Assert.IsTrue(dow.Contains(DayOfWeek.Sunday));
            Assert.AreEqual(1, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that Add(DayOfWeek) adds the day to the list.
        /// </summary>
        [TestMethod]
        public void Add_DayOfWeekEnum_AddsDayToList()
        {
            var dow = new DaysOfWeekParameter();
            dow.Add(DayOfWeek.Monday);

            Assert.IsTrue(dow.Contains(DayOfWeek.Monday));
        }

        /// <summary>
        /// Verifies that duplicates are not added.
        /// </summary>
        [TestMethod]
        public void Add_DuplicateDay_DoesNotAddTwice()
        {
            var dow = new DaysOfWeekParameter();
            dow.Add(DayOfWeek.Monday);
            dow.Add(DayOfWeek.Monday);

            Assert.AreEqual(1, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that Add(int) with an invalid value throws an exception.
        /// </summary>
        [TestMethod]
        public void Add_InvalidIntDay_ThrowsArgumentOutOfRangeException()
        {
            var dow = new DaysOfWeekParameter();

            Assert.Throws<ArgumentOutOfRangeException>(() => dow.Add(7));
            Assert.Throws<ArgumentOutOfRangeException>(() => dow.Add(-1));
        }

        #endregion

        #region Remove

        /// <summary>
        /// Verifies that Remove(int) removes a present day.
        /// </summary>
        [TestMethod]
        public void Remove_ExistingDay_RemovesDayFromList()
        {
            var dow = new DaysOfWeekParameter("0 1 2");
            dow.Remove(1);

            Assert.IsFalse(dow.Contains(DayOfWeek.Monday));
            Assert.AreEqual(2, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that Remove(DayOfWeek) removes a present day.
        /// </summary>
        [TestMethod]
        public void Remove_DayOfWeekEnum_RemovesDayFromList()
        {
            var dow = new DaysOfWeekParameter("0 1");
            dow.Remove(DayOfWeek.Sunday);

            Assert.IsFalse(dow.Contains(DayOfWeek.Sunday));
            Assert.AreEqual(1, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that removing an absent day causes no errors.
        /// </summary>
        [TestMethod]
        public void Remove_NonExistingDay_DoesNothing()
        {
            var dow = new DaysOfWeekParameter("1");
            dow.Remove(DayOfWeek.Sunday);

            Assert.AreEqual(1, dow.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies that Remove(int) with an invalid value throws an exception.
        /// </summary>
        [TestMethod]
        public void Remove_InvalidIntDay_ThrowsArgumentOutOfRangeException()
        {
            var dow = new DaysOfWeekParameter();

            Assert.Throws<ArgumentOutOfRangeException>(() => dow.Remove(7));
        }

        #endregion

        #region Contains

        /// <summary>
        /// Verifies Contains(int) with a present and an absent day.
        /// </summary>
        [TestMethod]
        public void Contains_IntDay_ReturnsCorrectResult()
        {
            var dow = new DaysOfWeekParameter("1 3");

            Assert.IsTrue(dow.Contains(1));
            Assert.IsFalse(dow.Contains(0));
        }

        /// <summary>
        /// Verifies Contains(int) with an out-of-range value.
        /// </summary>
        [TestMethod]
        public void Contains_InvalidIntDay_ReturnsFalse()
        {
            var dow = new DaysOfWeekParameter("1");

            Assert.IsFalse(dow.Contains(7));
            Assert.IsFalse(dow.Contains(-1));
        }

        /// <summary>
        /// Verifies Contains(DayOfWeek).
        /// </summary>
        [TestMethod]
        public void Contains_DayOfWeekEnum_ReturnsCorrectResult()
        {
            var dow = new DaysOfWeekParameter("1");

            Assert.IsTrue(dow.Contains(DayOfWeek.Monday));
            Assert.IsFalse(dow.Contains(DayOfWeek.Sunday));
        }

        #endregion

        #region ContainsToday

        /// <summary>
        /// Verifies ContainsToday with a list containing today.
        /// </summary>
        [TestMethod]
        public void ContainsToday_TodayInList_ReturnsTrue()
        {
            var today   = (int)DateTime.Now.DayOfWeek;
            var dow     = new DaysOfWeekParameter(today.ToString());

            Assert.IsTrue(dow.ContainsToday());
        }

        /// <summary>
        /// Verifies ContainsToday with an empty list.
        /// </summary>
        [TestMethod]
        public void ContainsToday_EmptyList_ReturnsFalse()
        {
            var dow = new DaysOfWeekParameter();

            Assert.IsFalse(dow.ContainsToday());
        }

        #endregion

        #region ToString

        /// <summary>
        /// Verifies that ToString returns the day codes sorted and separated by spaces.
        /// </summary>
        [TestMethod]
        public void ToString_MultipleDays_ReturnsSortedSpaceSeparated()
        {
            var dow = new DaysOfWeekParameter("5 1 3");

            Assert.AreEqual("1 3 5", dow.ToString());
        }

        /// <summary>
        /// Verifies that ToString with an empty list returns an empty string.
        /// </summary>
        [TestMethod]
        public void ToString_EmptyList_ReturnsEmptyString()
        {
            var dow = new DaysOfWeekParameter();

            Assert.AreEqual(string.Empty, dow.ToString());
        }

        /// <summary>
        /// Verifies symmetry: parsing the result of ToString must produce the same object.
        /// </summary>
        [TestMethod]
        public void ToString_RoundTrip_ProducesSameResult()
        {
            var original    = new DaysOfWeekParameter("6 0 3 1");
            var asString    = original.ToString();
            var roundTrip   = new DaysOfWeekParameter(asString);

            Assert.AreEqual(original.DaysOfWeek.Count, roundTrip.DaysOfWeek.Count);
            for (int i = 0; i < original.DaysOfWeek.Count; i++)
            {
                Assert.IsTrue(roundTrip.Contains(original.DaysOfWeek[i]));
            }
        }

        #endregion
    }
}
