using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKHelpers.CollectionUtils
{
	/// <summary>
	/// Generic wrapper that holds a <see cref="List{T}"/> together with all pagination state,
	/// making it easy to expose paged data directly from a view-model or API response.
	/// </summary>
	public class PagingCollection<T>
	{
		#region Constants

		/// <summary>Minimum page size.</summary>
		public const int MINPAGESIZE		= Paging.MINPAGESIZE;
		/// <summary>Default page size.</summary>
		public const int DEFAULTPAGESIZE	= Paging.DEFAULTPAGESIZE;
		/// <summary>Minimum group size.</summary>
		public const int MINGROUPSIZE		= Paging.MINGROUPSIZE;
		/// <summary>Maximum group size.</summary>
		public const int MAXGROUPSIZE		= Paging.MAXGROUPSIZE;
		/// <summary>Default group size.</summary>
		public const int DEFAULTGROUPSIZE	= Paging.DEFAULTGROUPSIZE;
		/// <summary>Minimum border size.</summary>
		public const int MINBORDERSIZE		= Paging.MINBORDERSIZE;
		/// <summary>Maximum border size.</summary>
		public const int MAXBORDERSIZE		= Paging.MAXBORDERSIZE;
		/// <summary>Default border size.</summary>
		public const int DEFAULTBORDERSIZE	= Paging.DEFAULTBORDERSIZE;

		#endregion

		#region Members

		/// <summary>Internal list of elements.</summary>
		private List<T> _elements;

		/// <summary>Current page number.</summary>
		private int _page;
		/// <summary>Page size.</summary>
		private int _pageSize;
		/// <summary>Group size.</summary>
		private int _groupSize;
		/// <summary>Border size.</summary>
		private int _borderSize;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the current page number.
		/// </summary>
		public int Page
		{
			get => _page > 0 ? _page : 1;
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
		/// Gets the total number of elements.
		/// </summary>
		public int ElementsCount => _elements?.Count ?? 0;

		/// <summary>
		/// Gets or sets the page size.
		/// </summary>
		public int PageSize
		{
			get => _pageSize;
			set
			{
				_pageSize = value >= MINPAGESIZE ? value : MINPAGESIZE;
				CalculatePages();
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

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of <see cref="PagingCollection{T}"/> with default values.
		/// </summary>
		public PagingCollection()
		{
			_elements = new List<T>();
			PageSize = DEFAULTPAGESIZE;
			Page = 1;
			GroupSize = DEFAULTGROUPSIZE;
			BorderSize = DEFAULTBORDERSIZE;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="PagingCollection{T}"/> with the specified values.
		/// </summary>
		/// <param name="elements">Elements to paginate.</param>
		/// <param name="pageSize">Number of elements per page.</param>
		/// <param name="page">Desired page number.</param>
		/// <param name="groupNumbers">Number of page numbers in the central group (around the current page).</param>
		/// <param name="startEndNumbers">Number of page numbers in the initial and final border groups.</param>
		public PagingCollection(IEnumerable<T> elements, int pageSize = DEFAULTPAGESIZE, int page = 1, int groupNumbers = DEFAULTGROUPSIZE, int startEndNumbers = DEFAULTBORDERSIZE) : this()
		{
			SetElements(elements);
			PageSize = pageSize;
			Page = page;
			GroupSize = groupNumbers;
			BorderSize = startEndNumbers;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Replaces the current element collection with the provided enumerable.
		/// </summary>
		/// <param name="elements">New elements to paginate.</param>
		public void SetElements(IEnumerable<T> elements)
		{
			_elements.Clear();
			if (elements?.Any() ?? false)
			{
				_elements.AddRange(elements);
			}
			CalculatePages();
		}

		/// <summary>
		/// Returns the list of elements belonging to the current page.
		/// </summary>
		/// <returns>Elements for the current page.</returns>
		public List<T> GetPageElements()
		{
			var elements = new List<T>();
			var page = Page <= Pages ? Page - 1 : Pages - 1;
			if (page < 0)
				page = 0;
			if (_elements.Any())
			{
				if (Page == Pages)
				{
					elements.AddRange(_elements.GetRange(page * PageSize, _elements.Count % PageSize));
				}
				else
				{
					elements.AddRange(_elements.GetRange(page * PageSize, PageSize));
				}
			}

			return elements;
		}

		/// <summary>
		/// Returns the array containing the list of pages to display with the related suspension points.
		/// </summary>
		/// <returns>List of page numbers and control characters.</returns>
		public List<int> GetPagesArray()
		{
			var numbers = new List<int>();
			var surroundingNums = (int)Math.Floor((decimal)GroupSize / 2); // Numbers around the selected page
			if (Pages <= (BorderSize * 2) + GroupSize)
			{
				// If the number of pages is less than the sum of initial/final numbers plus the group size, create a single uninterrupted list
				for (var p = 1; p <= Pages; p++)
				{
					numbers.Add(p);
				}
			}
			else
			{
				// Calculate the start and end positions of the numbers to display for the selected page group
				var groupStart = Page - surroundingNums;
				var groupEnd = Page + surroundingNums;
				// Calculate the first page number of the final group
				var lastStart = Pages - BorderSize + 1;
				// Add the header numbers
				for (var p = 1; p <= BorderSize; p++)
				{
					numbers.Add(p);
				}
				if (groupStart <= BorderSize + 1)
				{
					groupStart = BorderSize + 1;
				}
				else
				{
					numbers.Add((int)PagingControlChars.SuspensionPoints);
				}
				if (groupEnd >= lastStart - 1)
				{
					for (var p = groupStart; p <= Pages; p++)
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
					for (var p = lastStart; p <= Pages; p++)
					{
						numbers.Add(p);
					}
				}
			}
			return numbers;
		}

		/// <summary>
		/// Calculates the number of pages needed to display all elements.
		/// </summary>
		private void CalculatePages()
		{
			var pages = (int)Math.Ceiling((decimal)_elements.Count / PageSize);
			Pages = pages > 0 ? pages : 1;
			if (Page < 1)
				Page = 1;
			if (Page > Pages)
				Page = Pages;
		}

		#endregion
	}
}
