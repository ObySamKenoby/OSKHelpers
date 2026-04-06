using System;
using System.Globalization;

namespace OSKHelpers.Types.IsChanged
{
    /// <summary>
    /// Utilità per l'attribuzione di valori stringa a vari tipi di dato.<br/>
    /// L'utilizzo è pensato principalmente per i progetti web (Blazor, MVC) dove una stringa contenente un numero può dare risultati inaspettati in fase di conversione.<br/>
    /// Vengono forniti i metodi per l'attribuzione ad un campo ed i convertitori di formato personalizzati sfruttati dagli altri metodi.<br/>
    /// Le proprietà statiche <see cref="DecimalSeparator"/> <see cref="ThousandsSeparator"/> e <see cref="CultureInfo"/> possono essere utilizzate per personalizzare il comportamento.
    /// </summary>
    public class PropertyStringUtils
    {
        #region Proprietà

        /// <summary>
        /// Separatore dei decimali (default ',')
        /// </summary>
        public static string DecimalSeparator { get; set; }
        /// <summary>
        /// separatore delle migliaia (default '.')
        /// </summary>
        public static string ThousandsSeparator { get; set; }

        /// <summary>
        /// CultureInfo da utilizzare all'interno del metodo per le conversioni,<br/>
        /// impostato di default a quella italiana.
        /// </summary>
        public static CultureInfo CultureInfo { get; set; }

        #endregion

        #region Costruttore

        static PropertyStringUtils()
        {
            CultureInfo         = new CultureInfo("it-IT");
            DecimalSeparator    = ",";
            ThousandsSeparator  = ".";
        }

        #endregion

        #region Metodi

        private static void CheckProperties()
        {
            if (string.IsNullOrEmpty(DecimalSeparator))
                throw new ArgumentNullException(nameof(DecimalSeparator));

            if (string.IsNullOrEmpty(ThousandsSeparator))
                throw new ArgumentNullException(nameof(ThousandsSeparator));

            if (CultureInfo == null)
                throw new ArgumentNullException(nameof(CultureInfo));
        }

        #region Display

        /// <summary>
        /// Restituisce la visualizzazione come valuta del valore passato come parametro.
        /// </summary>
        public static string DisplayAsCurrency(decimal? value) => value != null ? $"{value:#,##0.00}" : null;

        #endregion

        #region SetProperty

        public static bool SetProperty(ref decimal field, string newValue)
        {
            var changed = TryParse(newValue, out decimal value) && field != value;
            if (changed)
            {
                field = value;
            }
            return changed;
        }

        public static bool SetProperty(ref decimal? field, string newValue)
        {
            bool changed = false;

            if (string.IsNullOrWhiteSpace(newValue) && field != null)
            {
                field = null;
                changed = true;
            }
            else
            {
                TryParse(newValue, out decimal? value);
                changed = field != value;
                if (changed)
                {
                    field = value;
                }
            }

            return changed;
        }

        public static bool SetProperty(ref DateTime field, string newValue)
        {
            var changed = TryParse(newValue, out DateTime value) && !field.Equals(value);
            if (changed)
            {
                field = value;
            }
            return changed;
        }

        public static bool SetProperty(ref DateTime? field, string newValue)
        {
            bool changed = false;

            if (string.IsNullOrWhiteSpace(newValue) && field != null)
            {
                field = null;
                changed = true;
            }
            else
            {
                TryParse(newValue, out DateTime? value);
                changed = !field.Equals(value);
                if (changed)
                {
                    field = value;
                }
            }

            return changed;
        }

        #endregion

        #region TryParse

        public static bool TryParse(string s, out decimal result)
        {
            CheckProperties();

            bool parsed = false;

            result = 0;
            if (!string.IsNullOrWhiteSpace(s))
            {
                // Si verifica se l'ultimo carattere che sarà preso a riferimento come separatore decimale,
                // è una virgola o un punto...
                var lastTpos = s.LastIndexOf(ThousandsSeparator);
                var lastDpos = s.LastIndexOf(DecimalSeparator);
                if (lastTpos >= 0 || lastDpos >= 0)
                {
                    // ... e nel caso in cui sia presente almeno uno dei due si sanifica per quanto possibile la stringa
                    // dopo aver aggiunto uno zero iniziale ed uno finale in modo da avere sempre qualcosa prima e dopo il separatore...
                    s = $"0{s}0";
                    // ... si rileva l'ultimo separatore, aggiungendo 2 per bilanciare lo spazio aggiunto all'inizio della
                    // stringa ed il carattere di separazione dei decimali...
                    var p = (lastTpos > lastDpos ? lastTpos : lastDpos) + 2;
                    // ... ed infine si genera la stringa sanificata
                    s = $"{s.Substring(0, p).Trim().Replace(ThousandsSeparator, string.Empty).Replace(DecimalSeparator, string.Empty)},{s.Substring(p)}";
                }
                // ... infine se la stringa risultante è un numero valido si aggiorna la proprietà al nuovo valore.
                parsed = decimal.TryParse(s, out result);
            }

            return parsed;
        }

        public static bool TryParse(string s, out decimal? result)
        {
            CheckProperties();

            result = null;
            var parsed = TryParse(s, out decimal res);
            if (parsed)
            {
                result = res;
            }
            return parsed;
        }

        public static bool TryParse(string s, out DateTime result)
        {
            CheckProperties();

            return DateTime.TryParse(s, CultureInfo, DateTimeStyles.None, out result);
        }

        public static bool TryParse(string s, out DateTime? result)
        {
            CheckProperties();

            result = null;
            var parsed = TryParse(s, out DateTime res);
            if (parsed)
            {
                result = res;
            }
            return parsed;
        }

        #endregion

        #region Parse

        public static decimal ParseDecimal(string s) => TryParse(s, out decimal result) ? result : result;
        public static decimal? ParseDecimalNullable(string s) => TryParse(s, out decimal? result) ? result : null;
        public static DateTime ParseDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        public static DateTime? ParseDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #region Conversioni gRPC

        public static string ToGRPCString(DateTime d) => $"{d:s}";
        public static DateTime FromGRPCDateTime(string s) => TryParse(s, out DateTime result) ? result : result;
        public static DateTime? FromGRPCDateTimeNullable(string s) => TryParse(s, out DateTime? result) ? result : null;

        #endregion

        #endregion
    }
}
