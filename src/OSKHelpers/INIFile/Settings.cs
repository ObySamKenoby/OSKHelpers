using OSKHelpers.INIFile;
using OSKHelpers.Logging;
using System;

namespace OSKHelpers.Templates.INIFile
{
    /// <summary>
    /// Class containing the application settings.
    /// </summary>
    public class Settings
    {
        #region Constants

        public const string PRODUCTNAME = "Applicazione";
        public const string PRODUCTVERSION = "0.0.1";
        public const string PRODUCTDATE = "01/01/1900";
        public const string PRODUCTFULLNAME = PRODUCTNAME + " Ver. " + PRODUCTVERSION + " del" + PRODUCTDATE;

        #endregion

        #region Members


        #endregion

        #region Properties

        /// <summary>
        /// True if the configuration was loaded correctly and is valid.
        /// </summary>
        public static bool IsValid { get; private set; }

        #endregion

        #region Constructor

        static Settings()
        {
            try
            {
                var iniFile = new IniFileHelper(true);

                if (iniFile.IniFileRead)
                {
                    SimpleLog.SetLogLevel(iniFile);

                    CheckIsValid();

                }
                else
                {
                    SimpleLog.Write("Impossible leggere il file Settings.ini o il file è male formattato.");
                    IsValid = false;
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                IsValid = false;
            }
        }

        #endregion

        #region Methods

        private static bool CheckIsValid()
        {
            try
            {

                IsValid = true;
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                IsValid = false;
            }


            if (!IsValid)
            {
                SimpleLog.Write("Il file Settings.ini contiene errori e non è valido.");
            }

            return IsValid;
        }

        #endregion
    }
}
