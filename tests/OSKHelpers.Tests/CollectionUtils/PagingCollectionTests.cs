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

		public const int MINPAGESIZE		= PagingCollection<int>.MINPAGESIZE;
		public const int DEFAULTPAGESIZE	= PagingCollection<int>.DEFAULTPAGESIZE;
		public const int MINGROUPSIZE		= PagingCollection<int>.MINGROUPSIZE;
		public const int MAXGROUPSIZE		= PagingCollection<int>.MAXGROUPSIZE;
		public const int DEFAULTGROUPSIZE	= PagingCollection<int>.DEFAULTGROUPSIZE;
		public const int MINBORDERSIZE		= PagingCollection<int>.MINBORDERSIZE;
		public const int MAXBORDERSIZE		= PagingCollection<int>.MAXBORDERSIZE;
		public const int DEFAULTBORDERSIZE	= PagingCollection<int>.DEFAULTBORDERSIZE;

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
		public void DefaultConstructor()
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
		public void ParametrizedConstructor()
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
		public void PageTests()
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
		public void PageSizeTests()
		{
			var paging = GetTestPaging();
			paging.PageSize = 1;
			Assert.AreEqual(MINPAGESIZE, paging.PageSize);
			paging.PageSize = 100;
			Assert.AreEqual(100, paging.PageSize);
		}

		[TestMethod]
		public void GroupSizeTests()
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
		public void BorderSizeTests()
		{
			var paging = GetTestPaging();
			paging.BorderSize = MINBORDERSIZE- 1;
			Assert.AreEqual(MINBORDERSIZE, paging.BorderSize);
			paging.BorderSize= MAXBORDERSIZE+ 1;
			Assert.AreEqual(MAXBORDERSIZE, paging.BorderSize);
		}

		[TestMethod]
		public void GetPageElementsTests()
        {
			var paging = GetTestPaging();
			var elementsForPage = 25;
			var elementsCount = paging.ElementsCount;
			var pages = (int)Math.Ceiling((decimal)elementsCount / elementsForPage);
			if (pages == 0)
				pages = 1;
			List<int> nums = new List<int>();
			
			paging.PageSize = elementsForPage;

			paging.Page = 1;
			for (var i = 1; i <= elementsForPage; i++)
				nums.Add(i);
			Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
			nums.Clear();
			var page = (int)Math.Floor(((decimal)pages / 2));
			paging.Page = page;	
			var startNum = ((page - 1) * elementsForPage) + 1;
			for (var i = 0; i < elementsForPage; i++)
				nums.Add(startNum + i);
			Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
			// Last page: element count is the default minus one (see how the test array is created)
			nums.Clear();
			page = pages;
			paging.Page = paging.Pages;
			startNum = ((page - 1) * elementsForPage) + 1;
			for (var i = 0; i < elementsForPage - 1; i++)
				nums.Add(startNum + i);
			Assert.IsTrue(paging.GetPageElements().SequenceEqual(nums));
		}

		[TestMethod]
		public void GetPagesArrayTests()
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

	}

	#endregion

	#endregion
}
