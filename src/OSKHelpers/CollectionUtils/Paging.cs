using System;
using System.Collections.Generic;

namespace OSKHelpers.CollectionUtils
{
    /// <summary>
    /// Calculates pagination metadata (pages, groups, borders) for a given element count and page size.
    /// </summary>
    public class Paging
	{
        #region Constants

		/// <summary>
		/// Minimum page size.
		/// </summary>
		public const int MINPAGESIZE = 5;
		/// <summary>
		/// Default page size.
		/// </summary>
		public const int DEFAULTPAGESIZE = 25;
		/// <summary>
		/// Minimum group size.
		/// </summary>
		public const int MINGROUPSIZE = 3;
		/// <summary>
		/// Maximum group size.
		/// </summary>
		public const int MAXGROUPSIZE = 7;
		/// <summary>
		/// Default group size.
		/// </summary>
		public const int DEFAULTGROUPSIZE = 5;
		/// <summary>
		/// Minimum border size.
		/// </summary>
		public const int MINBORDERSIZE = 2;
		/// <summary>
		/// Maximum border size.
		/// </summary>
		public const int MAXBORDERSIZE = 5;
		/// <summary>
		/// Default border size.
		/// </summary>
		public const int DEFAULTBORDERSIZE = 3;

		#endregion

		#region Members

		/// <summary>
		/// Number of elements.
		/// </summary>
		private int _elements;
		/// <summary>
		/// Current page number.
		/// </summary>
		private int _page;
		/// <summary>
		/// Page size.
		/// </summary>
		private int _pageSize;
		/// <summary>
		/// Group size.
		/// </summary>
		private int _groupSize;
		/// <summary>
		/// Border size.
		/// </summary>
		private int _borderSize;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the number of elements.
		/// </summary>
		public int Elements
		{
			get => _elements;
			set
			{
				_elements = value > 0 ? value : 0;
				Pages = GetPages(_elements, _pageSize);
			}
		}

		/// <summary>
		/// Gets or sets the current page number.
		/// </summary>
		public int Page
		{
			get
			{
				if (_page < 1)
				{
					_page = 1;
				}
				else if (_page > Pages)
				{
					_page = Pages;
				}
				return _page;
			}
			set
			{
				_page = value > 0 ? value : 1;
				if (_page > Pages)
				{
					_page = Pages;
				}
			}
		}

		/// <summary>
		/// Gets or sets the page size.
		/// </summary>
		public int PageSize
		{
			get => _pageSize;
			set
			{
				_pageSize = value >= MINPAGESIZE ? value : MINPAGESIZE;
				Pages = GetPages(_elements, _pageSize);
			}
		}

		/// <summary>
		/// Gets or sets the group size.
		/// </summary>
		public int GroupSize
		{
			get => _groupSize;
			set
			{
				if (value < MINGROUPSIZE)
				{
					_groupSize = MINGROUPSIZE;
				}
				else if (value > MAXGROUPSIZE)
				{
					_groupSize = MAXGROUPSIZE;
				}
				else
				{
					_groupSize = value;
				}
				if (_groupSize % 2 == 0)
				{
					_groupSize++;
				}
			}
		}

		/// <summary>
		/// Gets or sets the border size.
		/// </summary>
		public int BorderSize
		{
			get => _borderSize;
			set
			{
				if (value < MINBORDERSIZE)
				{
					_borderSize = MINBORDERSIZE;
				}
				else if (value > MAXBORDERSIZE)
				{
					_borderSize = MAXBORDERSIZE;
				}
				else
				{
					_borderSize = value;
				}
			}
		}

		/// <summary>
		/// Gets the total number of pages.
		/// </summary>
		public int Pages { get; private set; }

		/// <summary>
		/// Gets the index of the first element on the current page.
		/// </summary>
		public int PageFirstElementIndex => GetPageFirstElementIndex(Elements, PageSize, Page);

		/// <summary>
		/// Gets the index of the last element on the current page.
		/// </summary>
		public int PageLastElementIndex => GetPageLastElementIndex(Elements, PageSize, Page);

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Paging"/> class with default values.
		/// </summary>
		public Paging()
		{
			Elements	= 0;
			PageSize	= DEFAULTPAGESIZE;
			Page		= 1;
			GroupSize	= DEFAULTGROUPSIZE;
			BorderSize	= DEFAULTBORDERSIZE;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Paging"/> class with the specified values.
		/// </summary>
		/// <param name="elements">Number of elements.</param>
		/// <param name="pageSize">Page size.</param>
		/// <param name="page">Current page number.</param>
		/// <param name="borderSize">Border size.</param>
		/// <param name="groupSize">Group size.</param>
		public Paging(int elements, int pageSize, int page, int borderSize, int groupSize)
		{
			Elements	= elements;
			GroupSize	= groupSize;
			BorderSize	= borderSize;
			Elements	= elements;
			PageSize	= pageSize;
			Page		= page;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns the array containing the list of pages to display with the related suspension points.
		/// </summary>
		/// <returns>List of page numbers and control characters.</returns>
		public List<int> GetPagesArray() => GetPagesArray(Elements, PageSize, Page, BorderSize, GroupSize);

		/// <summary>
		/// Returns the array containing the list of pages to display with the related suspension points.
		/// </summary>
		/// <param name="elements">Number of elements.</param>
		/// <param name="pageSize">Page size.</param>
		/// <param name="page">Current page number.</param>
		/// <param name="borderSize">Border size.</param>
		/// <param name="groupSize">Group size.</param>
		/// <returns>List of page numbers and control characters.</returns>
		public static List<int> GetPagesArray(int elements, int pageSize, int page, int borderSize, int groupSize)
		{
			if (elements < 0)
				elements = 0;
			if (pageSize < MINPAGESIZE)
				pageSize = MINPAGESIZE;
			if (page < 1)
				page = 1;
			if (borderSize < MINBORDERSIZE)
				borderSize = MINBORDERSIZE;
			else if (borderSize > MAXBORDERSIZE)
				borderSize = MAXBORDERSIZE;
			if (groupSize < MINGROUPSIZE)
				groupSize = MINGROUPSIZE;
			else if (groupSize > MAXGROUPSIZE)
				groupSize = MAXGROUPSIZE;
			if (groupSize % 2 == 0)
				groupSize++;

			var numbers = new List<int>();
            var surroundingNums = (int)Math.Floor((decimal)groupSize / 2); // Numbers around the selected page
			var pages = GetPages(elements, pageSize);
			if (page > pages)
			{
				page = pages;
			}

			if (pages <= (borderSize * 2) + groupSize)
			{
				// If the number of pages is less than the sum of the initial and final numbers plus those of the selected group, a single uninterrupted list is created
				for (var p = 1; p <= pages; p++)
				{
					numbers.Add(p);
				}
			}
			else
			{
				// Calculate the start and end positions of the numbers to display for the selected page group
				var groupStart = page - surroundingNums;
				var groupEnd = page + surroundingNums;
				// Calculate the first page number of the final group
				var lastStart = pages - borderSize + 1;

				// Add controls to go to the previous page, skip a group, or go to the first page
				if (page > 1)
				{
					numbers.Add((int)PagingControlChars.First);
				}
				if (groupStart > borderSize + 1)
				{
					numbers.Add((int)PagingControlChars.FastRewind);
				}
				if (page > 2)
				{
					numbers.Add((int)PagingControlChars.Previous);
				}

				// Add the header numbers
				for (var p = 1; p <= borderSize; p++)
				{
					numbers.Add(p);
				}
				if (groupStart <= borderSize + 1)
				{
					groupStart = borderSize + 1;
				}
				else
				{
					numbers.Add((int)PagingControlChars.SuspensionPoints);
				}
				if (groupEnd >= lastStart - 1)
				{
					for (var p = groupStart; p <= pages; p++)
					{
						numbers.Add(p);
					}
				}
				else
				{
					for (var p = groupStart; p <= groupEnd; p++)
					{
						numbers.Add(p);
					}
					numbers.Add((int)PagingControlChars.SuspensionPoints);
					for (var p = lastStart; p <= pages; p++)
					{
						numbers.Add(p);
					}
				}

				// Add controls to go to the next page, skip a group, or go to the last page
				if (page < pages - 1)
				{
					numbers.Add((int)PagingControlChars.Next);
				}
				if (groupEnd < lastStart - 1)
				{
					numbers.Add((int)PagingControlChars.FastForward);
				}
				if (page < pages)
				{
					numbers.Add((int)PagingControlChars.Last);
				}
			}

			return numbers;
		}

		/// <summary>
		/// Returns the number of pages in which to divide the elements.
		/// </summary>
		/// <param name="elements">Number of elements.</param>
		/// <param name="pageSize">Page size.</param>
		/// <returns>Number of pages.</returns>
		public static int GetPages(int elements, int pageSize)
		{
			return elements > 0 ? (int)Math.Ceiling((decimal)elements / (pageSize > 0 ? pageSize : MINPAGESIZE)) : 1;
		}

		/// <summary>
		/// Returns the index of the first element for the page.
		/// </summary>
		/// <param name="elements">Number of elements.</param>
		/// <param name="pageSize">Page size.</param>
		/// <param name="page">Current page number.</param>
		/// <returns>Index of the first element for the page.</returns>
		public static int GetPageFirstElementIndex(int elements, int pageSize, int page)
		{
			var pages = GetPages(elements, pageSize);
			if (page > pages)
			{
				page = pages;
			}
			return (pageSize < MINPAGESIZE ? MINPAGESIZE : pageSize) * (page > 0 ? page - 1 : 0);
		}

		/// <summary>
		/// Returns the index of the last element for the page.
		/// </summary>
		/// <param name="elements">Number of elements.</param>
		/// <param name="pageSize">Page size.</param>
		/// <param name="page">Current page number.</param>
		/// <returns>Index of the last element for the page.</returns>
		public static int GetPageLastElementIndex(int elements, int pageSize, int page)
		{
			// Initialize the return value as the last element
			var index = elements - 1;

			if (index <= 0)
			{
				index = 0;
			}
			else
			{
				if (pageSize < MINPAGESIZE)
				{
					pageSize = MINPAGESIZE;
				}

				var pages = GetPages(elements, pageSize);

				if (page < 1)
				{
					page = 1;
				}
				else if (page > pages)
				{
					page = pages;
				}

				// If the requested page is not the last, calculate the last element of the page
				if (page != pages)
				{
					index = (pageSize * page) - 1;
				}
			}

			return index;
		}

		#endregion
	}
}
