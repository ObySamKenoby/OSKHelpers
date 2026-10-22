using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.CollectionUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.Tests.CollectionUtils
{
    [TestClass]
    public class PagingCollectionTests
    {
        #region Constants

        public const int MINPAGESIZE        = PagingCollection<int>.MINPAGESIZE;
        public const int DEFAULTPAGESIZE    = PagingCollection<int>.DEFAULTPAGESIZE;
        public const int MINGROUPSIZE       = PagingCollection<int>.MINGROUPSIZE;
        public const int MAXGROUPSIZE       = PagingCollection<int>.MAXGROUPSIZE;
        public const int DEFAULTGROUPSIZE   = PagingCollection<int>.DEFAULTGROUPSIZE;
        public const int MINBORDERSIZE      = PagingCollection<int>.MINBORDERSIZE;
        public const int MAXBORDERSIZE      = PagingCollection<int>.MAXBORDERSIZE;
        public const int DEFAULTBORDERSIZE  = PagingCollection<int>.DEFAULTBORDERSIZE;

        #endregion

        #region Methods

        private PagingCollection<int> GetTestPaging()
        {
            var intList = new List<int>();
            for (var i = 1; i < 1000; i++)
            {
                intList.Add(i);
            }
            return new PagingCollection<int>(intList);
        }

        #region Test methods

        [TestMethod]
        public void Constructor_Default_InitializesDefaultValues()
        {
            var paging = new PagingCollection<int>();
            Assert.AreEqual(PagingCollection<int>.DEFAULTPAGESIZE, paging.PageSize);
            Assert.AreEqual(1, paging.Page);
            Assert.AreEqual(PagingCollection<int>.DEFAULTGROUPSIZE, paging.GroupSize);
            Assert.AreEqual(PagingCollection<int>.DEFAULTBORDERSIZE, paging.BorderSize);
            Assert.AreEqual(1, paging.Pages);
            Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { 1 }));
        }

        [TestMethod]
        public void Constructor_NullElementsAndZeroSizes_ClampsToMinimums()
        {
            List<int> elements = null;
            var paging = new PagingCollection<int>(elements, 0, 0, 0, 0);
            Assert.AreEqual(PagingCollection<int>.MINPAGESIZE, paging.PageSize);
            Assert.AreEqual(1, paging.Page);
            Assert.AreEqual(PagingCollection<int>.MINGROUPSIZE, paging.GroupSize);
            Assert.AreEqual(PagingCollection<int>.MINBORDERSIZE, paging.BorderSize);
            Assert.AreEqual(1, paging.Pages);
            Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { 1 }));
        }

        [TestMethod]
        public void Page_OutOfRange_ClampsToValidPage()
        {
            var paging = GetTestPaging();
            paging.Page = paging.Pages + 1;
            Assert.AreEqual(paging.Page, paging.Pages);
            paging.Page = 0;
            Assert.AreEqual(1, paging.Page);
            paging.Page = paging.Pages;
            paging.SetElements(Array.Empty<int>());
            Assert.AreEqual(1, paging.Page);
        }

        [TestMethod]
        public void PageSize_OutOfRange_ClampsToMinimum()
        {
            var paging = GetTestPaging();
            paging.PageSize = 1;
            Assert.AreEqual(MINPAGESIZE, paging.PageSize);
            paging.PageSize = 100;
            Assert.AreEqual(100, paging.PageSize);
        }

        [TestMethod]
        public void GroupSize_OutOfRangeOrEven_ClampsToValidOddValue()
        {
            var paging = GetTestPaging();
            paging.GroupSize = MINGROUPSIZE - 1;
            Assert.AreEqual(MINGROUPSIZE, paging.GroupSize);
            paging.GroupSize = MAXGROUPSIZE + 1;
            Assert.AreEqual(MAXGROUPSIZE, paging.GroupSize);
            paging.GroupSize = 4;
            Assert.AreEqual(5, paging.GroupSize);
        }

        [TestMethod]
        public void BorderSize_OutOfRange_ClampsToLimits()
        {
            var paging = GetTestPaging();
            paging.BorderSize = MINBORDERSIZE- 1;
            Assert.AreEqual(MINBORDERSIZE, paging.BorderSize);
            paging.BorderSize= MAXBORDERSIZE+ 1;
            Assert.AreEqual(MAXBORDERSIZE, paging.BorderSize);
        }

        [TestMethod]
        public void GetPageElements_EachPage_ReturnsPageElements()
        {
            var paging = GetTestPaging();
            var elementsForPage = 25;
            var elementsCount = paging.ElementsCount;
            var pages = (int)Math.Ceiling((decimal)elementsCount / elementsForPage);
            if (pages == 0)
            {
                pages = 1;
            }

            List<int> nums = new List<int>();
            
            paging.PageSize = elementsForPage;

            paging.Page = 1;
            for (var i = 1; i <= elementsForPage; i++)
            {
                nums.Add(i);
            }

            Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
            nums.Clear();
            var page = (int)Math.Floor(((decimal)pages / 2));
            paging.Page = page; 
            var startNum = ((page - 1) * elementsForPage) + 1;
            for (var i = 0; i < elementsForPage; i++)
            {
                nums.Add(startNum + i);
            }

            Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
            // Last page: element count is the default minus one (see how the test array is created)
            nums.Clear();
            page = pages;
            paging.Page = paging.Pages;
            startNum = ((page - 1) * elementsForPage) + 1;
            for (var i = 0; i < elementsForPage - 1; i++)
            {
                nums.Add(startNum + i);
            }

            Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
        }

        [TestMethod]
        [DataRow(50, 25, 2, 26, 25)]
        [DataRow(49, 25, 2, 26, 24)]
        [DataRow(25, 25, 1, 1,  25)]
        [DataRow(1,  25, 1, 1,  1)]
        public void GetPageElements_LastPage_ReturnsRemainingElements(int elementsCount, int pageSize, int expectedPages, int expectedFirst, int expectedCount)
        {
            // Arrange
            var paging = new PagingCollection<int>(Enumerable.Range(1, elementsCount), pageSize);
            paging.Page = paging.Pages;

            // Act
            var elements = paging.GetPageElements();

            // Assert
            Assert.AreEqual(expectedPages, paging.Pages);
            Assert.IsTrue(elements.SequenceEqual(Enumerable.Range(expectedFirst, expectedCount)));
        }

        [TestMethod]
        public void GetPagesArray_VariousPages_ReturnsExpectedArray()
        {
            List<int> nums = new List<int>();
            for (var i = 1; i < 101; i++)
            {
                nums.Add(i);
            }

            var paging = new PagingCollection<int>(nums);
            paging.PageSize = 25;
            paging.GroupSize = 3;
            paging.BorderSize = 2;
            paging.Page = 1;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, 4 });
            Assert.AreEqual(4, paging.Pages);
            var numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));
            nums.Clear();
            for (var i = 1; i < 176; i++)
            {
                nums.Add(i);
            }
            paging.SetElements(nums);
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7 });
            Assert.AreEqual(7, paging.Pages);
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging = GetTestPaging();
            paging.PageSize = 25;
            paging.GroupSize = 5;
            paging.BorderSize = 2;

            paging.Page = 1;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = 2;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, 4, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = 3;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, 4, 5, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = 5;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = 10;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, (int)PagingControlChars.SuspensionPoints, 8, 9, 10, 11, 12, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = paging.Pages - 7;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, (int)PagingControlChars.SuspensionPoints, paging.Pages - 9, paging.Pages - 8, paging.Pages - 7, paging.Pages - 6, paging.Pages - 5, (int)PagingControlChars.SuspensionPoints, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = paging.Pages - 3;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, (int)PagingControlChars.SuspensionPoints, paging.Pages - 5, paging.Pages - 4, paging.Pages - 3, paging.Pages - 2, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

            paging.Page = paging.Pages;
            nums.Clear();
            nums.AddRange(new int[] { 1, 2, (int)PagingControlChars.SuspensionPoints, paging.Pages - 2, paging.Pages - 1, paging.Pages });
            numArray = paging.GetPagesArray();
            Assert.IsTrue(numArray.SequenceEqual(nums));

        }

        /// <summary>
        /// Verifies that SetElements replaces the elements and recalculates the pages.
        /// </summary>
        [TestMethod]
        public void SetElements_NewElements_ReplacesElementsAndRecalculatesPages()
        {
            var paging = new PagingCollection<int>(Enumerable.Range(1, 60), 25);
            Assert.AreEqual(3, paging.Pages);

            paging.SetElements(Enumerable.Range(1, 10));

            Assert.AreEqual(10, paging.ElementsCount);
            Assert.AreEqual(1, paging.Pages);
            CollectionAssert.AreEqual(Enumerable.Range(1, 10).ToList(), paging.GetPageElements());
        }

        /// <summary>
        /// Verifies that SetElements with null empties the collection.
        /// </summary>
        [TestMethod]
        public void SetElements_Null_EmptiesCollection()
        {
            var paging = GetTestPaging();

            paging.SetElements(null);

            Assert.AreEqual(0, paging.ElementsCount);
            Assert.AreEqual(1, paging.Pages);
            Assert.AreEqual(1, paging.Page);
            Assert.HasCount(0, paging.GetPageElements());
        }

        /// <summary>
        /// Verifies that a page beyond the last one in the constructor is clamped to the last page.
        /// </summary>
        [TestMethod]
        public void Constructor_PageBeyondLast_ClampsToLastPage()
        {
            var paging = new PagingCollection<int>(Enumerable.Range(1, 30), 10, 99);

            Assert.AreEqual(3, paging.Page);
            CollectionAssert.AreEqual(Enumerable.Range(21, 10).ToList(), paging.GetPageElements());
        }

    }

    #endregion

    #endregion
}
