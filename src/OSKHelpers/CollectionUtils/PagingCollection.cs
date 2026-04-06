using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKHelpers.CollectionUtils
{
	public class PagingCollection<T>
	{
        #region Costanti

        public const int MINPAGESIZE		= Paging.MINPAGESIZE;
        public const int DEFAULTPAGESIZE	= Paging.DEFAULTPAGESIZE;
        public const int MINGROUPSIZE		= Paging.MINGROUPSIZE;
        public const int MAXGROUPSIZE		= Paging.MAXGROUPSIZE;
        public const int DEFAULTGROUPSIZE	= Paging.DEFAULTGROUPSIZE;
        public const int MINBORDERSIZE		= Paging.MINBORDERSIZE;
        public const int MAXBORDERSIZE		= Paging.MAXBORDERSIZE;
        public const int DEFAULTBORDERSIZE	= Paging.DEFAULTBORDERSIZE;

        #endregion

        #region Membri

        private List<T> _elements;

		private int _page;
		private int _pageSize;
		private int _groupSize;
		private int _borderSize;

		#endregion

		#region Proprietà

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

		public int ElementsCount => _elements?.Count ?? 0;

		public int PageSize
		{
			get => _pageSize;
			set
			{
				_pageSize = value >= MINPAGESIZE ? value : MINPAGESIZE;
				CalculatePages();
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

		#endregion

		#region Costruttori

		/// <summary>
		/// Inizializza l'istanza di PagingResult
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
		/// Inizializza l'istanza di PagingResults con i valori passati come parametro
		/// </summary>
		/// <param name="elements">Elementi di cui fare la paginazione</param>
		/// <param name="pageSize">Elementi per pagina</param>
		/// <param name="page">Pagina desiderata</param>
		/// <param name="groupNumbers">Quantità di numeri costituenti il gruppo centrale (quello della pagina desiderata)</param>
		/// <param name="startEndNumbers">Quantità di numeri costituenti i gruppi iniziale e finale (quelli contenenti la prima e l'ultima pagina) </param>
		public PagingCollection(IEnumerable<T> elements, int pageSize = DEFAULTPAGESIZE, int page = 1, int groupNumbers = DEFAULTGROUPSIZE, int startEndNumbers = DEFAULTBORDERSIZE) : this()
		{
			SetElements(elements);
			PageSize = pageSize;
			Page = page;
			GroupSize = groupNumbers;
			BorderSize = startEndNumbers;
		}

		#endregion

		#region Metodi

		public void SetElements(IEnumerable<T> elements)
		{
			_elements.Clear();
			if (elements?.Any() ?? false)
			{
				_elements.AddRange(elements);
			}
			CalculatePages();
		}

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
		/// Restituisce l'array contenente la lista delle pagine da visualizzare con i relativi punti di sospensione
		/// </summary>
		public List<int> GetPagesArray()
		{
			var numbers = new List<int>();
			var surroundingNums = (int)Math.Floor((decimal)GroupSize / 2); // Numeri intorno alla pagina selezionata
			if (Pages <= (BorderSize * 2) + GroupSize)
			{
				// Se il numero di pagine è inferiore a quello dei numeri iniziali e finali più quelli del gruppo selezionato si crea una unica lista ininterrotta
				for (var p = 1; p <= Pages; p++)
				{
					numbers.Add(p);
				}
			}
			else
			{
				// Si calcolano la posizione inizale e finale dei numeri da visualizzare per il gruppo della pagina selezionata
				var groupStart = Page - surroundingNums;
				var groupEnd = Page + surroundingNums;
				// Si calcola il primo numero di pagina del gruppo finale
				var lastStart = Pages - BorderSize + 1;
				// Si aggiungono i numeri in testata
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
		/// Calcola il numero di pagine in cui suddividere gli elementi
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
