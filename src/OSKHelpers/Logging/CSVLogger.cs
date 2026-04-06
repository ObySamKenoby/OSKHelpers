using System;
using System.IO;
using System.Reflection;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Permette di salvare il contenuto di un tipo T di oggetto all'interno di un file CSV.
    /// </summary>
    public class CSVLogger<T> where T : ICSVLogItem, new()
    {
        #region Membri

        private DateTime? _lastDate;

        private static readonly string _defaultLogPath;
        private string _logPath;
        private string _logFilename;

        private string _prefix;
        private bool _usePrefixAsLogFile;
        private readonly object _lock = new object();

        #endregion

        #region Proprietà

        /// <summary>
        /// Percorso del file di log.<br/>
        /// La presenza della cartella viene verificato al momento della scrittura di un messaggio, se il percorso non viene trovato se ne tenta la creazione.<br />
        /// Di default viene utilizzata la cartella "Log" all'interno del percorso dell'applicativo.
        /// </summary>
        public string LogPath
        {
            get => _logPath;
            set
            {
                _logPath = value;
                // _lastDate viene posto a null per forzare la rigenerazione del nome file
                _lastDate = null;
            }
        }

        /// <summary>
        /// Prefisso da aggiungere al nome del file log.<br/>
        /// Valore di default: Null.
        /// </summary>
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                // _lastDate viene posto a null per forzare la rigenerazione del nome file
                _lastDate = null;
            }
        }

        /// <summary>
        /// Forza il nome del log al solo prefisso.<br/>
        /// Utilizzato epr ptoer avere un nome log univoco che racchiuda più giorni.<br/>
        /// Implementato la priam volta per l'utilizzo nella classe KDucer di ProFanStd.<br/>
        /// NB: la proprietà non ha u equivalente all'interno di SimpleLog.
        /// </summary>
        public bool UsePrefixAsLogFile
        {
            get => _usePrefixAsLogFile;
            set
            {
                _usePrefixAsLogFile = value;
                // _lastDate viene posto a null per forzare la rigenerazione del nome file
                _lastDate = null;
            }
        }

        /// <summary>
        /// Nome del file di log.<br/>
        /// Il nome è composto secondo il pattern PREFIXLogYYYYMMDD.txt, è possibile differenziare i vari log all'interno della stessa cartella modificando Prefix
        /// </summary>
        public string LogFile
        {
            get
            {
                if (DateTime.Now.Date != (_lastDate?.Date ?? DateTime.MinValue))
                {
                    _logFilename = Path.Combine(_logPath, _usePrefixAsLogFile ? $"{_prefix}.csv" : $"{_prefix}Log{DateTime.Now:yyyyMMdd}.csv");
                    _lastDate = DateTime.Now.Date;
                }

                return _logFilename;
            }
        }

        #endregion

        #region Costruttori

        static CSVLogger()
        {
            _defaultLogPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Logs");
        }

        public CSVLogger()
        {
            _usePrefixAsLogFile = false;
            _lastDate           = null;
            LogPath             = _defaultLogPath;
            _logFilename        = null;
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Scrive la linea passata come parametro nel file di log e, di default, la visualizza a video quando in fase di debug
        /// </summary>
        /// <param name="obj">Oggetto da cui ricavare la rga di Log (deve e</param>
        /// <param name="logLevel">Utilizzato per formattare correttamente il prefisso nel caso in cui il livello di log sia DEBUG o PROTOCOL,<br/>
        /// non impedisce in alcun caso la scrittura della riga. Se omesso viene utilizzato il prefisso standard (data/ora)</param>
        public void Log(T obj)
        {
            if (obj != null)
            {
                lock (_lock)
                {
                    try
                    {
                        if (!File.Exists(LogFile))
                        {
                            if (!Directory.Exists(_logPath))
                            {
                                Directory.CreateDirectory(_logPath);
                            }
                            var t = new T();
                            File.AppendAllText(_logFilename, $"{t.GetCSVHeader()}\r\n");
                        }

                        File.AppendAllText(LogFile, $"{obj.GetCSVData()}\r\n");
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // Se si verificano errori in fase di scrittura viene creato un log all'interno della cartella dell'applicazione, se possibile.
                            var line = $"Errore in fase di scrittura del log.\r\n  Nome del file di log: {LogFile}\r\n  Errore: {SimpleLog.FormattedException(ex, true)}\r\n";
                            File.AppendAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ERRORSLOG.txt"), $"{line}\r\n");
                        }
                        catch { }
                    }
                }
            }
        }


        #endregion
    }
}
