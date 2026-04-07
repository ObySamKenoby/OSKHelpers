using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Additional utilities for handling JSON files.
    /// </summary>
    public class JsonUtils
    {
        #region Members

        private static readonly JsonSerializerOptions _defaultOptions;

        #endregion

        #region Constructor

        static JsonUtils()
        {
            _defaultOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition  = JsonIgnoreCondition.Never,
                WriteIndented           = true
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Serialises an object using its actual runtime type.
        /// </summary>
        /// <param name="obj">Object to serialise.</param>
        /// <param name="options">
        /// Serialisation options; if null the default options are used<br/>
        /// (includes all properties, formats the serialised string).
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
