using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKHelpers.CollectionUtils
{
	public class Paging
	{
		#region Costanti

		public const int MINPAGESIZE = 5;
		public const int DEFAULTPAGESIZE = 25;
		public const int MINGROUPSIZE = 3;
		public const int MAXGROUPSIZE = 7;
		public const int DEFAULTGROUPSIZE = 5;
		public const int MINBORDERSIZE = 2;
		public const int MAXBORDERSIZE = 5;
		public const int DEFAULTBORDERSIZE = 3;

		#endregion

		#region Membri

		private int _elements;
		private int _page;
		private int _pageSize;
		private int _groupSize;
		private int _borderSize;

		#endregion

		#region Proprietà

		public int Elements
		{
			get => _elements;
			set
			{
				_elements = value > 0 ? value : 0;
				Pages = GetPages(_elements, _pageSize);
			}
		}

		public int Page
		{
			get
			{
				if (_page < 1)
					_page = 1;
				else if (_page > Pages)
					_page = Pages;
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

		public int PageSize
		{
			get => _pageSize;
			set
			{
				_pageSize = value >= MINPAGESIZE ? value : MINPAGESIZE;
				Pages = GetPages(_elements, _pageSize);
			}
		}

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

		public int Pages { get; private set; }

		public int PageFirstElementIndex => GetPageFirstElementIndex(Elements, PageSize, Page);

		public int PageLastElementIndex => GetPageLastElementIndex(Elements, PageSize, Page);

		#endregion

		#region Costruttori

		public Paging()
		{
			Elements = 0;
			PageSize = DEFAULTPAGESIZE;
			Page = 1;
			GroupSize = DEFAULTGROUPSIZE;
			BorderSize = DEFAULTBORDERSIZE;
		}

		public Paging(int elements, int pageSize, int page, int borderSize, int groupSize)
		{
			Elements = elements;
			GroupSize = groupSize;
			BorderSize = borderSize;
			Elements = elements;
			PageSize = pageSize;
			Page = page;
		}

		#endregion

		#region Metodi

		/// <summary>
		/// Restituisce l'array contenente la lista delle pagine da visualizzare con i relativi punti di sospensione
		/// </summary>
		public List<int> GetPagesArray() => GetPagesArray(Elements, PageSize, Page, BorderSize, GroupSize);

		/// <summary>
		/// Restituisce l'array contenente la lista delle pagine da visualizzare con i relativi punti di sospensione
		/// </summary>
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
			var surroundingNums = (int)Math.Floor((decimal)groupSize / 2); // Numeri intorno alla pagina selezionata
			var pages = GetPages(elements, pageSize);
			if (page > pages)
				page = pages;

			if (pages <= (borderSize * 2) + groupSize)
			{
				// Se il numero di pagine è inferiore a quello dei numeri iniziali e finali più quelli del gruppo selezionato si crea una unica lista ininterrotta
				for (var p = 1; p <= pages; p++)
				{
					numbers.Add(p);
				}
			}
			else
			{
				// Si calcolano la posizione inizale e finale dei numeri da visualizzare per il gruppo della pagina selezionata
				var groupStart = page - surroundingNums;
				var groupEnd = page + surroundingNums;
				// Si calcola il primo numero di pagina del gruppo finale
				var lastStart = pages - borderSize + 1;

				// Aggiunta dei controlli per andare alla pagina precedente, saltarne un gruppo o andare alla prima
				if (page > 1)
					numbers.Add((int)PagingControlChars.First);
				if (groupStart > borderSize + 1)
					numbers.Add((int)PagingControlChars.FastRewind);
				if (page > 2)
					numbers.Add((int)PagingControlChars.Previous);

				// Si aggiungono i numeri in testata
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

				// Aggiunta dei controlli per andare alla pagina successiva, saltarne un gruppo o andare all'ultima
				if (page < pages - 1)
					numbers.Add((int)PagingControlChars.Next);
				if (groupEnd < lastStart - 1)
					numbers.Add((int)PagingControlChars.FastForward);
				if (page < pages)
					numbers.Add((int)PagingControlChars.Last);
			}

			return numbers;
		}

		/// <summary>
		/// Restituisce il numero di pagine in cui suddividere gli elementi
		/// </summary>
		public static int GetPages(int elements, int pageSize)
		{
			return elements > 0 ? (int)Math.Ceiling((decimal)elements / (pageSize > 0 ? pageSize : MINPAGESIZE)) : 1;
		}

		/// <summary>
		/// Restituisce l'indice del primo elemento per la pagina
		/// </summary>
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
		/// Restituisce l'indice dell'ultimo elemento per la pagina
		/// </summary>
		public static int GetPageLastElementIndex(int elements, int pageSize, int page)
		{
			// Si inizializza come valore di ritorno l'ultimo elemento
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

				// Nel caso in cui la pagina richiesta non sia l'ultima si procede con il calcolo dell'ultimo elemento della pagina
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
