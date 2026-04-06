using OSKHelpers.INIFile;
using OSKHelpers.Logging;
using System;

namespace OSKHelpers.Templates.INIFile
{
    /// <summary>
    /// Classe contenente le impostazioni per l'applicativo.
    /// </summary>
    public class Settings
    {
        #region Costanti

        public const string PRODUCTNAME = "Applicazione";
        public const string PRODUCTVERSION = "0.0.1";
        public const string PRODUCTDATE = "01/01/1900";
        public const string PRODUCTFULLNAME = PRODUCTNAME + " Ver. " + PRODUCTVERSION + " del" + PRODUCTDATE;

        #endregion

        #region Membri


        #endregion

        #region Proprietà

        /// <summary>
        /// Se True la configurazione è stata correttamente caricata ed è valida.
        /// </summary>
        public static bool IsValid { get; private set; }

        #endregion

        #region Costruttore

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

        #region Metodi

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
