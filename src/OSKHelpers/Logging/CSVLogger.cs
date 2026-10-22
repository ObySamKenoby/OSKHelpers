using System;
using System.IO;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Saves the content of objects of type T to a CSV log file.
    /// </summary>
    public class CSVLogger<T> where T : ICSVLogItem, new()
    {
        #region Members

        private DateTime? _lastDate;

        private string _logPath;
        private string _logFilename;
        private string _logFileDirectory;

        private string _prefix;
        private bool _usePrefixAsLogFile;
        private readonly object _lock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Path of the log file.<br/>
        /// The folder is checked at write time; if it does not exist, a creation attempt is made.<br/>
        /// Until a value is assigned, <see cref="SimpleLogger.DefaultLogPath"/> (<see cref="Common.Paths.DefaultLogsDirectory"/>)
        /// is used and its changes are followed;<br/>
        /// assigning null or an empty string restores this behaviour.
        /// </summary>
        public string LogPath
        {
            get => _logPath ?? SimpleLogger.DefaultLogPath;
            set
            {
                _logPath = string.IsNullOrWhiteSpace(value) ? null : value;
                // _lastDate is set to null to force file name regeneration
                _lastDate = null;
            }
        }

        /// <summary>
        /// Prefix to add to the log file name.<br/>
        /// Default value: null.
        /// </summary>
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                // _lastDate is set to null to force file name regeneration
                _lastDate = null;
            }
        }

        /// <summary>
        /// Forces the log name to be the prefix only.<br/>
        /// Used to have a unique log name that spans multiple days.<br/>
        /// Note: this property has no equivalent in <see cref="SimpleLog"/>.
        /// </summary>
        public bool UsePrefixAsLogFile
        {
            get => _usePrefixAsLogFile;
            set
            {
                _usePrefixAsLogFile = value;
                // _lastDate is set to null to force file name regeneration
                _lastDate = null;
            }
        }

        /// <summary>
        /// Log file name.<br/>
        /// The name follows the pattern PREFIXLogYYYYMMDD.csv; different logs in the same folder can be distinguished by changing Prefix.<br/>
        /// The name is regenerated when the date or the log folder changes.
        /// </summary>
        public string LogFile
        {
            get
            {
                var logPath = LogPath;
                if (DateTime.Now.Date != (_lastDate?.Date ?? DateTime.MinValue) || logPath != _logFileDirectory)
                {
                    _logFilename        = Path.Combine(logPath, _usePrefixAsLogFile ? $"{_prefix}.csv" : $"{_prefix}Log{DateTime.Now:yyyyMMdd}.csv");
                    _logFileDirectory   = logPath;
                    _lastDate           = DateTime.Now.Date;
                }

                return _logFilename;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CSVLogger()
        {
            _usePrefixAsLogFile = false;
            _lastDate           = null;
            _logPath            = null;
            _logFilename        = null;
            _logFileDirectory   = null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes the given object to the CSV log file.
        /// </summary>
        /// <param name="obj">Object to log.</param>
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
                            if (!Directory.Exists(LogPath))
                            {
                                Directory.CreateDirectory(LogPath);
                            }
                            var t = new T();
                            File.AppendAllText(_logFilename, $"{t.GetCSVHeader()}\r\n");
                        }

                        File.AppendAllText(LogFile, $"{obj.GetCSVData()}\r\n");
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.WriteErrorsLog(
                            $"Error writing log.\r\n  Log file name: {LogFile}\r\n  Error: {SimpleLog.FormattedException(ex, true)}\r\n");
                    }
                }
            }
        }


        #endregion
    }
}
