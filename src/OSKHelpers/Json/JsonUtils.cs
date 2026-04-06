using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Utilità aggiuntive per la gestione dei file  JSON.
    /// </summary>
    public class JsonUtils
    {
        #region Membri

        private static readonly JsonSerializerOptions _defaultOptions;

        #endregion

        #region Costruttore

        static JsonUtils()
        {
            _defaultOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition  = JsonIgnoreCondition.Never,
                WriteIndented           = true
            };
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Serializza un oggetto usando il suo tipo runtime effettivo.
        /// </summary>
        /// <param name="obj">Oggetto da serializzare.</param>
        /// <param name="options">
        /// Opzioni di serializzazione, se nullo utilizza le opzioni di default<br/>
        /// (include tutte le proprietà, formatta la stringa serializzata).
        /// </param>
        public static string Serialize(object obj, JsonSerializerOptions options = null)
        {
            string result = "{}";

            if (obj != null)
            {
                if (options == null)
                {
                    options = _defaultOptions;
                }
                result = JsonSerializer.Serialize(obj, obj.GetType(), options);
            }

            return result;
        }

        #endregion
    }

}
