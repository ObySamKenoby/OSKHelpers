using OSKHelpers.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Classe base che genera automaticamente gli header e le righe per i file SV utilizzando tutte le proprietà pubbliche dell'oggetto.
    /// </summary>
    public class CSVLogItem : ICSVLogItem
    {
        #region Proprietà

        /// <summary>
        /// Separatore che sarà utilizzato all'interno delle intestazioni e dei dati per dividere i valori.
        /// </summary>
        public string CSVSeparator = ";";

        #endregion

        #region Metodi

        /// <inheritdoc cref="ICSVLogItem.GetCSVData"/>
        public string GetCSVHeader()
        {
            return GetCSVHeader(this, CSVSeparator);
        }

        /// <inheritdoc cref="ICSVLogItem.GetCSVData"/>
        public string GetCSVData()
        {
            return GetCSVData(this, CSVSeparator);
        }

        /// Restituisce l'intestazione di default per il file CSV contenente i nomi di tutte le proprietà ordinati alfabeticamente.
        /// </summary>
        /// <param name="obj">Oggetto per cui creare le intestazioni.</param>
        /// <param name="separator">Separatore da utilizzare.</param>
        /// <returns>La stringa contenente le intestazioni.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static string GetCSVHeader(object obj, string separator)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(separator);
            }
            Type type = obj.GetType();

            return string.Join(separator, type.GetProperties().Select(p => p.Name).OrderBy(n => n));
        }

        /// <summary>
        /// Restituisce la riga dati di default per il file CSV contenente il valore di tutte le proprietà ordinate alfabeticamente.<br/>
        /// I dati saranno così formattati:<br/>
        /// <b>stringhe</b>: racchiuse tra virgolette ("");<br/>
        /// <b>numeri</b>: senza virgolette, senza separatore delle migliaia, utilizzano il punto come separatore decimale);<br/>
        /// <b>date (o data/ora)</b>: senza virgolette, con prefisso "D", in formato ISO UTC;<br/>
        /// <b>altri tipi</b>: saranno trattati come stringhe, racchiusi tra virgolette e rappresentati richiamando il relativo metodo ToString().
        /// </summary>
        /// <param name="obj">Oggetto per cui creare la riga dati.</param>
        /// <param name="separator">Separatore da utilizzare.</param>
        /// <returns>La stringa contenente le intestazioni.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <summary>
        public static string GetCSVData(object obj, string separator)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(separator);
            }
            Type type = obj.GetType();

            var fields = new List<string>();
            foreach (var property in type.GetProperties().OrderBy(p => p.Name))
            {
                try
                {
                    var value = property.GetValue(obj);
                    if (ObjectUtils.IsNumeric(value))
                    {
                        fields.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
                    }
#if NET8_0_OR_GREATER
                else if (value is DateOnly)
                {
                    var v = (DateOnly)value;
                    fields.Add(new DateTime(v.Year, v.Month, v.Day).ToString("s"));
                }
#endif
                    else if (value is DateTime)
                    {
                        fields.Add(((DateTime)value).ToString("s"));
                    }
                    else if (value is string)
                    {
                        fields.Add($"\"{value}\"");
                    }
                    else
                    {
                        fields.Add($"\"{value.ToString()}\"");
                    }
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex);
                    fields.Add($"#ERR ({ex.Message})#");
                }
            }

            return string.Join(separator, fields);
        }

#endregion
    }
}
