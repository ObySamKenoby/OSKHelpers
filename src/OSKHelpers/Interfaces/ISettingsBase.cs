using OSKHelpers.INIFile;
using OSKHelpers.Json;
using OSKHelpers.Logging;

namespace OSKHelpers.Interfaces
{
    /// <summary>
    /// Base Settings interface, compatible with <see cref="IniFileHelper"/> and <br/>
    /// <see cref="JsonSettings{T}"/> for initializing <see cref="LogLevel"/> in <br/>
    /// <see cref="SimpleLog"/> and <see cref="SimpleLogger"/>.
    /// </summary>
    public interface ISettingsBase
    {
        #region Costanti

        /// <summary>
        /// Product name
        /// </summary>
        string PRODUCTNAME { get; }
        /// <summary>
        /// Product version
        /// </summary>
        string PRODUCTVERSION { get; }
        /// <summary>
        /// Product name
        /// </summary>
        string PRODUCTDATE { get; }
        /// <summary>
        /// Full product name / version
        /// </summary>
        string PRODUCTFULLNAME { get; }

        #endregion

        #region Proprietà

        /// <summary>
        /// Log level<br/>
        /// 0 - None<br/>
        /// 1 - Error<br/>
        /// 2 - Warning<br/>
        /// 3 - Info<br/>
        /// 4 - Debug<br/>
        /// 5 - Protocol
        /// </summary>
        int LogLevel { get; set; }

        #endregion
    }
}
