using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.CollectionUtils;
using System;
using System.Linq;

namespace OSKHelpers.Tests.CollectionUtils
{
    [TestClass]
    public class PagingTests
    {
		#region Constants

		public const int FIRST				= (int)PagingControlChars.First;
		public const int FASTREWINND		= (int)PagingControlChars.FastRewind;
		public const int PREVIOUS			= (int)PagingControlChars.Previous;
		public const int SUSPENSIONPOINTS	= (int)PagingControlChars.SuspensionPoints;
		public const int NEXT				= (int)PagingControlChars.Next;
		public const int FASTFORWARD		= (int)PagingControlChars.FastForward;
		public const int LAST				= (int)PagingControlChars.Last;

		public const int MINPAGESIZE		= Paging.MINPAGESIZE;
		public const int DEFAULTPAGESIZE	= Paging.DEFAULTPAGESIZE;
		public const int MINGROUPSIZE		= Paging.MINGROUPSIZE;
		public const int MAXGROUPSIZE		= Paging.MAXGROUPSIZE;
		public const int DEFAULTGROUPSIZE	= Paging.DEFAULTGROUPSIZE;
		public const int MINBORDERSIZE		= Paging.MINBORDERSIZE;
		public const int MAXBORDERSIZE		= Paging.MAXBORDERSIZE;
		public const int DEFAULTBORDERSIZE	= Paging.DEFAULTBORDERSIZE;

		#endregion

		#region Methods

		#region Test methods
		#region Instance usage tests 

		private Paging GetTestPaging() => new Paging() { Elements = 100 };

		[TestMethod]
		public void DefaultConstructor()
		{
			var paging = new Paging();
			Assert.AreEqual(0, paging.Elements);
			Assert.AreEqual(Paging.DEFAULTPAGESIZE, paging.PageSize);
			Assert.AreEqual(1, paging.Page);
			Assert.AreEqual(Paging.DEFAULTGROUPSIZE, paging.GroupSize);
			Assert.AreEqual(Paging.DEFAULTBORDERSIZE, paging.BorderSize);
			Assert.AreEqual(1, paging.Pages);
			Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { 1 }));
		}

		[TestMethod]
		public void ParametrizedConstructorTests()
		{
			var paging = new Paging(-1, MINPAGESIZE - 1, 0, MINBORDERSIZE - 1, MINGROUPSIZE -1);
			Assert.AreEqual(0, paging.Elements);
			Assert.AreEqual(Paging.MINPAGESIZE, paging.PageSize);
			Assert.AreEqual(1, paging.Page);
			Assert.AreEqual(Paging.MINGROUPSIZE, paging.GroupSize);
			Assert.AreEqual(Paging.MINBORDERSIZE, paging.BorderSize);
			Assert.AreEqual(1, paging.Pages);
			Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { 1 }));
			paging = new Paging(0, 10, 2, MAXBORDERSIZE + 1, MAXGROUPSIZE + 1);
			Assert.AreEqual(0, paging.Elements);
			Assert.AreEqual(10, paging.PageSize);
			Assert.AreEqual(1, paging.Page);
			Assert.AreEqual(Paging.MAXGROUPSIZE, paging.GroupSize);
			Assert.AreEqual(Paging.MAXBORDERSIZE, paging.BorderSize);
			Assert.AreEqual(1, paging.Pages);
			Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { 1 }));
			paging = new Paging(109, 10, 5, 2, 3);
			Assert.AreEqual(109, paging.Elements);
			Assert.AreEqual(10, paging.PageSize);
			Assert.AreEqual(5, paging.Page);
			Assert.AreEqual(3, paging.GroupSize);
			Assert.AreEqual(2, paging.BorderSize);
			Assert.AreEqual(11, paging.Pages);
			Assert.IsTrue(paging.GetPagesArray().SequenceEqual(new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 4, 5, 6, SUSPENSIONPOINTS, 10, 11, NEXT, FASTFORWARD, LAST }));
		}

		[TestMethod]
		public void PageTests()
		{
			var paging = GetTestPaging();
			paging.Page = paging.Pages + 1;
			Assert.AreEqual(paging.Pages, paging.Page);
			paging.Page = 0;
			Assert.AreEqual(1, paging.Page);
			paging.Page = paging.Pages;
			paging.Elements = 0;
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
			paging.BorderSize = MINBORDERSIZE - 1;
			Assert.AreEqual(MINBORDERSIZE, paging.BorderSize);
			paging.BorderSize = MAXBORDERSIZE + 1;
			Assert.AreEqual(MAXBORDERSIZE, paging.BorderSize);
		}

        #endregion

        [TestMethod]
        public void GetPagesTests()
        {
			Assert.AreEqual(1, Paging.GetPages(-5, 5));
			Assert.AreEqual(1, Paging.GetPages(5, -5));
			Assert.AreEqual(1, Paging.GetPages(1, 5));
			Assert.AreEqual(1, Paging.GetPages(5, 5));
			Assert.AreEqual(2, Paging.GetPages(6, 5));
			Assert.AreEqual(1, Paging.GetPages(9, 10));
			Assert.AreEqual(2, Paging.GetPages(19, 10));
			Assert.AreEqual(2, Paging.GetPages(20, 10));
			Assert.AreEqual(3, Paging.GetPages(21, 10));
		}

		[TestMethod]
		public void GetPageFirstElementIndex()
        {
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(-1, 10, 10));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(0, 10, 10));
			Assert.AreEqual(45, Paging.GetPageFirstElementIndex(100, -1, 10));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(100, 10, -1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(100, 10, 1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(3, 10, 1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(9, 10, 2));
			Assert.AreEqual(10, Paging.GetPageFirstElementIndex(100, 10, 2));
			Assert.AreEqual(10, Paging.GetPageFirstElementIndex(11, 10, 2));
			Assert.AreEqual(10, Paging.GetPageFirstElementIndex(11, 10, 2));
			Assert.AreEqual(90, Paging.GetPageFirstElementIndex(100, 10, 11));
		}

		[TestMethod]
		public void GetPageLastElementIndex()
        {
			Assert.AreEqual(0, Paging.GetPageLastElementIndex(-1, 10, 10));
			Assert.AreEqual(0, Paging.GetPageLastElementIndex(0, 10, 10));
			Assert.AreEqual(0, Paging.GetPageLastElementIndex(1, 10, 10));
			Assert.AreEqual(4, Paging.GetPageLastElementIndex(10, 4, 1));
			Assert.AreEqual(4, Paging.GetPageLastElementIndex(10, 5, -1));
			Assert.AreEqual(9, Paging.GetPageLastElementIndex(10, 10, 2));
			Assert.AreEqual(19, Paging.GetPageLastElementIndex(20, 10, 2));
			Assert.AreEqual(19, Paging.GetPageLastElementIndex(20, 10, 3));
			Assert.AreEqual(93, Paging.GetPageLastElementIndex(94, 10, 10));
		}

		[TestMethod]
		public void GetPagesArrayTests()
		{
			int[] test = new int[] { 1 };
			Assert.IsTrue(Paging.GetPagesArray(10, 10, 1, 2, 3).SequenceEqual(test));

			test = new int[] { 1, 2, 3 };
			Assert.IsTrue(Paging.GetPagesArray(25, 10, 4, 2, 3).SequenceEqual(test));

			test = new int[] { 1, 2, 3, 4, 5};
			Assert.IsTrue(Paging.GetPagesArray(45, 10, 3, 2, 3).SequenceEqual(test));

			test = new int[] { 1, 2, 3, 4, 5, 6, 7};
			Assert.IsTrue(Paging.GetPagesArray(65, 10, 5, 2, 5).SequenceEqual(test));

			test = new int[] { 1, 2, 3, 4, 5, 6, 7 };
			Assert.IsTrue(Paging.GetPagesArray(65, 10, 2, 2, 3).SequenceEqual(test));

			test = new int[] { 1, 2, SUSPENSIONPOINTS, 7, 8, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(75, 10, -1, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, 1, 2, 3, SUSPENSIONPOINTS, 7, 8, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(75, 10, 2, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 6, 7, 8, SUSPENSIONPOINTS, 11, 12, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 7, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 5, 6, 7, 8, 9, SUSPENSIONPOINTS, 11, 12, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 7, 2, 5).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, 3, SUSPENSIONPOINTS, 6, 7, 8, SUSPENSIONPOINTS, 10, 11, 12, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 7, 3, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 8, 9, 10, 11, 12, NEXT, LAST };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 9, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 10, 11, 12, LAST };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 11, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 11, 12 };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 12, 2, 3).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 11, 12 };
			Assert.IsTrue(Paging.GetPagesArray(115, 10, 15, 2, 3).SequenceEqual(test));

			// Test out-of-bounds parameters
			// Default limits: 
			// PageSize:	5 - 
			// BorderSize:	2 - 5
			// GroupSize:	3 - 7, odd numbers only
			// Page:		1 - max page
			test = new int[] { 1 };
			Assert.IsTrue(Paging.GetPagesArray(-1, 10, 1, 2, 3).SequenceEqual(test));

			test = new int[] { 1, 2, 3, 4, 5, 6, 7 };
			Assert.IsTrue(Paging.GetPagesArray(65, 10, 5, 1, 5).SequenceEqual(test));
			Assert.IsTrue(Paging.GetPagesArray(65, 10, -1, 1, 5).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 6, 7, 8, SUSPENSIONPOINTS, 11, 12, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(59,  1, 7, 2, 3).SequenceEqual(test));
			Assert.IsTrue(Paging.GetPagesArray(59, -1, 7, 2, 3).SequenceEqual(test));
			Assert.IsTrue(Paging.GetPagesArray(59,  5, 7, 2, 1).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, 3, 4, 5, SUSPENSIONPOINTS, 7, 8, 9, SUSPENSIONPOINTS, 11, 12, 13, 14, 15, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(74, -1, 8, 6, 1).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 4, 5, 6, 7, 8, 9, 10, SUSPENSIONPOINTS, 12, 13, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(61,  5, 7, 1, 8).SequenceEqual(test));
			Assert.IsTrue(Paging.GetPagesArray(61,  5, 7, 1, 6).SequenceEqual(test));

			test = new int[] { FIRST, FASTREWINND, PREVIOUS, 1, 2, SUSPENSIONPOINTS, 5, 6, 7, 8, 9, SUSPENSIONPOINTS, 12, 13, NEXT, FASTFORWARD, LAST };
			Assert.IsTrue(Paging.GetPagesArray(61, 5, 7, 1, 4).SequenceEqual(test));
		}

		[TestMethod]
		public void GetPageFirstElementIndexTests()
        {
			// Test out-of-bounds parameters
			// Default limits: 
			// PageSize:	5 -
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(-1, Paging.MINPAGESIZE, 1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(10, -1, 1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(10, Paging.MINPAGESIZE, -1));
			// Verifica dei risultati con parametri corretti
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(10, 10, 1));
			Assert.AreEqual(0, Paging.GetPageFirstElementIndex(20, 10, 1));
			Assert.AreEqual(10, Paging.GetPageFirstElementIndex(20, 10, 2));
			Assert.AreEqual(21, Paging.GetPageFirstElementIndex(22, 7, 4));
		}

		[TestMethod]
		public void GetPageLastElementIndexTests()
		{
			// Test out-of-bounds parameters
			// Default limits: 
			// PageSize:	5 -
				Assert.AreEqual(0, Paging.GetPageLastElementIndex(-1, -1, -1));
			Assert.AreEqual(0, Paging.GetPageLastElementIndex(-1, Paging.MINPAGESIZE, 1));
			Assert.AreEqual(4, Paging.GetPageLastElementIndex(10, -1, 1));
			Assert.AreEqual(4, Paging.GetPageLastElementIndex(10, Paging.MINPAGESIZE, -1));
			Assert.AreEqual(19, Paging.GetPageLastElementIndex(31, 3, 4));
			// Verifica dei risultati con parametri corretti
			Assert.AreEqual(9, Paging.GetPageLastElementIndex(10, 10, 1));
			Assert.AreEqual(9, Paging.GetPageLastElementIndex(20, 10, 1));
			Assert.AreEqual(19, Paging.GetPageLastElementIndex(20, 10, 2));
			Assert.AreEqual(19, Paging.GetPageLastElementIndex(31, 5, 4));
			Assert.AreEqual(14, Paging.GetPageLastElementIndex(24, 5, 3));
			Assert.AreEqual(20, Paging.GetPageLastElementIndex(24, 7, 3));
			Assert.AreEqual(23, Paging.GetPageLastElementIndex(24, 7, 4));
		}

		[TestMethod]
		public void InstanceGetPageFirstElementIndexTests()
		{
			// Test correct results,
			// the property is only an interface to the static method
				Paging paging = new Paging(10, 10, 1, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
				Assert.AreEqual(0, paging.PageFirstElementIndex);
				paging = new Paging(20, 10, 1, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
				Assert.AreEqual(0, paging.PageFirstElementIndex);
				paging = new Paging(20, 10, 2, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE); 
				Assert.AreEqual(10, paging.PageFirstElementIndex);
				paging = new Paging(22, 7, 4, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
				Assert.AreEqual(21, paging.PageFirstElementIndex);
		}

		[TestMethod]
		public void InstanceGetPageLastElementIndexTests()
		{
			// Test correct results,
			// the property is only an interface to the static method
				Paging paging = new Paging(10, 10, 1, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
				Assert.AreEqual(9, paging.PageLastElementIndex);
			paging = new Paging(20, 10, 1, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(9, paging.PageLastElementIndex);
			paging = new Paging(20, 10, 2, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(19, paging.PageLastElementIndex);
			paging = new Paging(31,  5, 4, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(19, paging.PageLastElementIndex);
			paging = new Paging(24,  5, 3, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(14, paging.PageLastElementIndex);
			paging = new Paging(24,  7, 3, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(20, paging.PageLastElementIndex);
			paging = new Paging(24,  7, 4, DEFAULTBORDERSIZE, DEFAULTGROUPSIZE);
			Assert.AreEqual(23, paging.PageLastElementIndex);
		}

		#endregion

		#endregion
	}
}
