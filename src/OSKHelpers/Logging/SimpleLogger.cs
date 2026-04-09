using OSKHelpers.Common;
using OSKHelpers.INIFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Simple class used to generate application logs.
    /// </summary>
    public class SimpleLogger
    {
        #region Members

        private DateTime? _lastDate;

        private static readonly string _defaultLogPath;
        private string _logPath;
        private string _logFilename;

        private LogLevel _logLevel;
        private bool _forceDebug;
        private bool _forceProtocol;
        
        private string _prefix;
        private bool _usePrefixAsLogFile;
        private readonly object _lock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Default log folder.
        /// </summary>
        public static string DefaultLogPath { get => _defaultLogPath; }

        /// <summary>
        /// Used to display messages
        /// Does not affect the behaviour of <see cref="DebugConsoleLog(string)"/> and <see cref="ProtocolConsoleLog(string)"/>.<br/>
        /// Default: false.
        /// </summary>
        public bool EnableConsoleLog { get; set; }

        /// <summary>
        /// Used to disable debug calls; does not affect the normal behaviour of the LogLevel.Debug level.
        /// </summary>
        public bool DisableDebugLog { get; set; }

        /// <summary>
        /// Minimum log level
        /// Default value: LogLevel.Error.
        /// </summary>
        public LogLevel LogLevel
        { 
            get => _logLevel; 
            set
            {
                if (_forceProtocol || value == LogLevel.Protocol)
                    _logLevel = LogLevel.Protocol;
                else if (_forceDebug || value == LogLevel.Debug)
                    _logLevel = LogLevel.Debug;
                else
                    _logLevel = value;
            }
        }

        /// <summary>
        /// Path of the log file.<br/>
        /// The folder is checked at write time; if it does not exist, a creation attempt is made.<br/>
        /// By default the "Log" folder inside the application path is used.
        /// </summary>
        public string LogPath
        {
            get => _logPath;
            set
            {
                _logPath = value;
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
        /// Used to have a unique log name that spans multiple days.
        /// First implemented for the KDucer class in ProFanStd.<br/>
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
        /// The name follows the pattern
        /// </summary>
        public string LogFile
        {
            get
            {
                if (DateTime.Now.Date != (_lastDate?.Date ?? DateTime.MinValue))
                {
                    _logFilename    = Path.Combine(_logPath, _usePrefixAsLogFile ? $"{_prefix}.txt" : $"{_prefix}Log{DateTime.Now:yyyyMMdd}.txt");
                    _lastDate       = DateTime.Now.Date;
                }

                return _logFilename;
            }
        }

        /// <summary>
        /// Forces the log level to Debug.<br/>
        /// Useful for enabling debug logging during program initialisation (before the settings file is loaded).<br/>
        /// Ignored when ForceProtocol is also true.
        /// </summary>
        public bool ForceDebug
        {
            get => _forceDebug;
            set
            {
                _forceDebug = value;
                if (_forceDebug)
                {
                    LogLevel = LogLevel.Debug;
                }
            }
        }

        /// <summary>
        /// Forces the log level to Protocol.<br/>
        /// Useful for enabling protocol logging during program initialisation (before the settings file is loaded).<br/>
        /// When true, ForceDebug is ignored.
        /// </summary>
        public bool ForceProtocol
        {
            get => _forceProtocol;
            set
            {
                _forceProtocol = value;
                if (_forceProtocol)
                {
                    LogLevel = LogLevel.Protocol;
                }
            }
        }

        /// <summary>
        /// True if the log level is Debug or higher.
        /// </summary>
        public bool LogLevelDebug => LogLevel >= LogLevel.Debug;

        /// <summary>
        /// True if the log level is Protocol or higher.
        /// </summary>
        public bool LogLevelProtocol => LogLevel >= LogLevel.Protocol;

        #endregion

        #region Constructor

        static SimpleLogger()
        {
            _defaultLogPath = Paths.DefaultLogsDirectory;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SimpleLogger()
        {
            EnableConsoleLog    = false;
            DisableDebugLog     = true;
            _usePrefixAsLogFile = false;
            _lastDate           = null;
            LogPath             = _defaultLogPath;
            _logFilename        = null;
            _forceDebug         = false;
            _forceProtocol      = false;

            LogLevel            = LogLevel.Error;
#if !RELEASE
            LogLevel            = LogLevel.Debug;
            DisableDebugLog     = false;
#endif
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns true if the specified log level allows writing to the log.
        /// </summary>
        /// <param name="logLevel">Log level to check.</param>
        /// <returns>True if the specified log level allows writing to the log.</returns>
        public bool ToLog(LogLevel logLevel)
        {
            return logLevel <= LogLevel;
        }

        /// <summary>
        /// Writes the specified line to the log file and, by default, prints it to the console when debugging.
        /// </summary>
        /// <param name="line">Line to write to the log.</param>
        /// <param name="logLevel">Used to correctly format the prefix when the log level is DEBUG or PROTOCOL.<br/>
        /// Does not prevent the line from being written. When omitted, the standard prefix (date/time) is used.</param>
        public void Write(string line, LogLevel logLevel = LogLevel.None)
        {
            if (!string.IsNullOrWhiteSpace(line) && ToLog(logLevel))
            {
                lock (_lock)
                {
                    var prefix = GetLogLinePrefix(logLevel);
                    try
                    {
                        DebugConsoleLog(line);
                        ConsoleLog(line, logLevel);
                        if (!Directory.Exists(_logPath))
                        {
                            Directory.CreateDirectory(_logPath);
                        }
                        File.AppendAllText(LogFile, $"{prefix} {line}\r\n");
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // If an error occurs during writing, a fallback log is created in the application folder, if possible.
                            line = $"Error writing log.\r\n  Log file name: {LogFile}\r\n  Error: {FormattedException(ex, true)}\r\n  Original message: {line}";
                            File.AppendAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ERRORSLOG.txt"), $"{prefix} {line}\r\n");
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Logs the line with optional object serialisation when the log level is compatible.
        /// </summary>
        /// <param name="logLevel">Minimum log level.</param>
        /// <param name="line">Log text.</param>
        /// <param name="obj">Optional object to serialise.</param>
        /// <param name="inline">If true the serialised object is appended to the text; otherwise it is written on a new line.</param>
        public void Write(LogLevel logLevel, string line, object obj = null, bool inline = false)
        {
            if (ToLog(LogLevel))
            {
                try
                {
                    if (obj != null && inline)
                    {
                        line = $"{line} {JsonSerializer.Serialize(obj)}";
                    }
                    Write(line);
                    if (obj != null && !inline)
                    {
                        Write(JsonSerializer.Serialize(obj));
                    }
                }
                catch (Exception ex)
                {
                    Write($"Error in {nameof(SimpleLogger)}.{nameof(Write)}(LogLevel.{logLevel},'{line}', obj, {inline}): {SimpleLog.FormattedException(ex, true)}");
                }
            }
        }

        /// <summary>
        /// Logs the serialisation of the given object.
        /// </summary>
        public void Write(LogLevel logLevel, object obj) 
            => Write(logLevel, string.Empty, obj, true);

        /// <summary>
        /// Writes a line.<br/>
        /// Created to allow use as a logging delegate, e.g. for EF Core logging.<br/>
        /// Writes the specified line to the log file and, by default, prints it to the console when debugging.
        /// </summary>
        /// <param name="line">Line to write.</param>
        public void WriteLine(string line) => Write(line);

        /// <summary>
        /// Logs the line with optional object serialisation when the log level is at least Error.
        /// </summary>
        /// <param name="line">Log text.</param>
        /// <param name="obj">Optional object to serialise.</param>
        /// <param name="inline">If true the serialised object is appended to the text; otherwise it is written on a new line.</param>
        public void ErrorWrite(string line, object obj = null, bool inline = false)
            => Write(LogLevel.Error, line, obj, inline);

        ///<inheritdoc cref="DebugWrite(string, object, bool)"/>
        public void ErrorWrite(object obj)
            => ErrorWrite(string.Empty, obj, true);

        /// <summary>
        /// Logs the line with optional object serialisation when the log level is at least Debug.
        /// </summary>
        /// <param name="line">Log text.</param>
        /// <param name="obj">Optional object to serialise.</param>
        /// <param name="inline">If true the serialised object is appended to the text; otherwise it is written on a new line.</param>
        public void DebugWrite(string line, object obj = null, bool inline = false)
            => Write(LogLevel.Debug, line, obj, inline);

        ///<inheritdoc cref="DebugWrite(string, object, bool)"/>
        public void DebugWrite(object obj)
            => DebugWrite(string.Empty, obj, true);

        /// <summary>
        /// Logs the line with optional object serialisation when the log level is at least Protocol.
        /// </summary>
        /// <param name="line">Log text.</param>
        /// <param name="obj">Optional object to serialise.</param>
        /// <param name="inline">If true the serialised object is appended to the text; otherwise it is written on a new line.</param>
        public void ProtocolWrite(string line, object obj = null, bool inline = false)
            => Write(LogLevel.Protocol, line, obj, inline);

        /// <summary>
        /// Logs the serialisation of the given object at Protocol level.
        /// </summary>
        public void ProtocolWrite(object obj)
            => ProtocolWrite(string.Empty, obj, true);

        ///<inheritdoc cref="LogLines(LogLevel, IEnumerable{string})"/>
        public void LogLines(IEnumerable<string> lines)
            => LogLines(LogLevel.None, lines);

        /// <summary>
        /// Writes the given strings to the log when the log level is compatible.
        /// </summary>
        /// <param name="logLevel">Minimum log level.</param>
        /// <param name="lines">Lines to log.</param>
        public void LogLines(LogLevel logLevel, IEnumerable<string> lines)
        {
            try
            {
                if (ToLog(LogLevel) && (lines?.Any() ?? false))
                {
                    string prefix = GetLogLinePrefix(logLevel);

                    lock (_lock)
                    {
                        if (!Directory.Exists(_logPath))
                        {
                            Directory.CreateDirectory(_logPath);
                        }
                        File.AppendAllLines(LogFile, lines.Select(l => $"{prefix} {l}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Error in {nameof(SimpleLogger)}.{nameof(LogArray)}: {FormattedException(ex)}");
            }
        }

        ///<inheritdoc cref="LogArray(LogLevel, string, LogLevel, string, IEnumerable{object}, bool)"/>
        public void LogArray(LogLevel logLevel, string message, string arrayName, IEnumerable<object> array, bool serialize = false)
            => LogArray(logLevel, message, logLevel, arrayName, array, serialize);

        /// <summary>
        /// Logs the array, optionally recording the content of all elements when the log level is compatible with detailLogLevel.
        /// </summary>
        /// <param name="logLevel">Minimum log level.</param>
        /// <param name="message">Log text.</param>
        /// <param name="detailLogLevel">Minimum log level required to log each element's content.</param>
        /// <param name="arrayName">Name of the array.</param>
        /// <param name="array">Array to log.</param>
        /// <param name="serialize">When true, each element is JSON-serialised.</param>
        public void LogArray(LogLevel logLevel, string message, LogLevel detailLogLevel, string arrayName, IEnumerable<object> array, bool serialize = false)
        {
            if (ToLog(logLevel) && array != null && (array?.Any() ?? false))
            {
                var prefix = GetLogLinePrefix(logLevel);

                var lines = new List<string>();

                try
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        DebugConsoleLog(message);
                        lines.Add($"{prefix} {message}");
                    }
                    if (ToLog(detailLogLevel))
                    {
                        prefix = GetLogLinePrefix(detailLogLevel);

                        var i = 0;
                        foreach (var element in array)
                        {
                            lines.Add($"{prefix} {arrayName}[{i}]: {(serialize ? JsonSerializer.Serialize(element) : element)}");
                            i++;
                        }
                    }
                    lock (_lock)
                    {
                        if (!Directory.Exists(_logPath))
                        {
                            Directory.CreateDirectory(_logPath);
                        }
                        File.AppendAllLines(LogFile, lines);
                    }
                }
                catch (Exception ex)
                {
                    Write($"Error in {nameof(SimpleLogger)}.{nameof(LogArray)}: {FormattedException(ex)}");
                }
            }
        }

        /// <summary>
        /// Returns a standard string for displaying and logging exceptions and their inner exceptions.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="includeStackTrace">Whether to force inclusion of the stack trace (always included when log level is Debug or higher).</param>
        public string FormattedException(Exception ex, bool includeStackTrace = false)
        {
            includeStackTrace = includeStackTrace || LogLevelDebug;
            string formattedException = string.Empty;
            if (ex != null)
            {
                formattedException = $"{ex.GetType()} {ex.Message}"
                    + (ex.InnerException != null ? $"\r\n{ex.InnerException.GetType()} {ex.InnerException.Message}" : string.Empty)
                    + (includeStackTrace ? $"\r\nStack: {ex.StackTrace}" : string.Empty);
            }
            return formattedException;
        }

        /// <summary>
        /// Logs the given exception with a message of the form "Error in ClassName.MethodName: Exception".<br/>
        /// If a null exception is passed, the error message is still recorded.<br/>
        /// Logging is performed for LogLevel.Error or higher.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="includeStackTrace">Whether to force inclusion of the stack trace (always included when log level is Debug or higher).</param>
        public void LogError(Exception ex, bool includeStackTrace = false, [CallerMemberName] string callerMethodName = "")
        {
            var callerTypeName = new StackFrame(1).GetMethod().DeclaringType.Name;
            LogError(ex, callerTypeName, callerMethodName, includeStackTrace);
        }

        /// <summary>
        /// Logs the given exception with a message of the form "Error in ClassName.MethodName: Exception".<br/>
        /// If a null exception is passed, the error message is still recorded.<br/>
        /// Logging is performed for LogLevel.Error or higher.
        /// </summary>
        /// <param name="ex">Exception to log.</param>
        /// <param name="callerTypeName">Name of the calling type.</param>
        /// <param name="callerMethodName">Name of the calling method.</param>
        /// <param name="includeStackTrace">Whether to force inclusion of the stack trace (always included when log level is Debug or higher).</param>
        public void LogError(Exception ex, string callerTypeName, string callerMethodName , bool includeStackTrace = false)
        {
            Write(LogLevel.Error, $"Error in {callerTypeName}.{callerMethodName}: {FormattedException(ex, includeStackTrace)}");
        }

        /// <summary>
        /// Returns the name of the calling type.
        /// </summary>
        public string GetCallerTypeName() => new StackFrame(1).GetMethod().DeclaringType.Name;

        /// <summary>
        /// Returns the name of the calling method.
        /// </summary>
        public string GetCallerMethodName([CallerMemberName] string callerMethodName = "") => callerMethodName;

        /// <summary>
        /// Returns the calling type and method name in the form ClassName.MethodName.
        /// </summary>
        public string GetCallerTypeMethodName([CallerMemberName] string callerMethodName = "") => $"{new StackFrame(1).GetMethod().DeclaringType.Name}.{callerMethodName}";


        /// <summary>
        /// Logs the calling class and method name when the log level is adequate.
        /// </summary>
        public void LogCallerTypeMethodName(LogLevel logLevel = LogLevel.Debug, [CallerMemberName] string callerMethodName = "") => Write(logLevel, $"{new StackFrame(1).GetMethod().DeclaringType.Name}.{callerMethodName}");

        /// <summary>
        /// Displays <paramref name="text"/>
        /// </summary>
        public void ConsoleLog(string text, LogLevel logLevel = LogLevel.None)
        {
            // The LogLevelDebug check is there to prevent messages from being printed to the console twice.
            if (EnableConsoleLog && ToLog(logLevel) && (!LogLevelDebug || DisableDebugLog))
            {
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// Prints the given line to the console when <see cref="LogLevel"/> is at least Debug; does nothing otherwise.<br/>
        /// Output is produced even in production when <see cref="DisableDebugLog"/> is false.<br/>
        /// Can be suppressed by setting <see cref="DisableDebugLog"/> to true (default in production).
        /// </summary>
        public void DebugConsoleLog(string text)
        {
            if (!DisableDebugLog && LogLevelDebug && Environment.UserInteractive)
            {
                Console.WriteLine($"{text}");
            }
        }

        /// <summary>
        /// Prints the given line to the console when <see cref="LogLevel"/> is at least Protocol; does nothing otherwise.<br/>
        /// Output is produced even in production when <see cref="DisableDebugLog"/> is false.<br/>
        /// Can be suppressed by setting <see cref="DisableDebugLog"/> to true (default in production).
        /// </summary>
        public void ProtocolConsoleLog(string text)
        {
            if (!DisableDebugLog && LogLevelProtocol && Environment.UserInteractive)
            {
                Console.WriteLine($"{text}");
            }
        }

        /// <summary>
        /// Writes text to the console and/or to the log file according to the two independent level thresholds.
        /// </summary>
        /// <param name="line">Text to display or record in the log file.</param>
        /// <param name="consoleLogLevel">Log level required to display the message on screen.</param>
        /// <param name="fileLogLevel">Log level required to record the message in the log file.</param>
        /// <param name="obj">Object to serialise.</param>
        /// <param name="inline">The object serialisation is appended to the text on the same line.</param>
        /// <param name="disableConsoleLog">
        /// Suppresses console output. When null, the value of <see cref="DisableDebugLog"/> is used,
        /// regardless of the actual value of consoleLogLevel.
        /// </param>
        public void ConditionalLog(string line, LogLevel consoleLogLevel, LogLevel fileLogLevel, object obj = null, bool inline = false, bool? disableConsoleLog = null)
        {
            try
            {
                // Console output
                if (Environment.UserInteractive && ToLog(consoleLogLevel))
                {
                    var consoleLog = !(disableConsoleLog ?? DisableDebugLog);
                    // If DisableDebugLog is false and consoleLogLevel is at least Debug, suppress console output to avoid duplicate lines.
                    consoleLog &= !(fileLogLevel <= LogLevel.Debug && !DisableDebugLog);

                    if (consoleLog)
                    {
                        if (obj != null && inline)
                        {
                            line = $"{line} {JsonSerializer.Serialize(obj)}";
                        }
                        Console.WriteLine(line);
                        if (obj != null && !inline)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(obj));
                        }
                    }
                    Write(fileLogLevel, line, obj, inline);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        ///<summary>
        /// Writes text to the console when the log level is Debug, and to the file when it is Protocol.
        /// </summary>
        /// <inheritdoc cref="ConditionalLog(string, LogLevel, LogLevel, object, bool, bool?)"/>
        public void DebugProtocolConditionalLog(string line, object obj = null, bool inline = false, bool? disableConsoleLog = null)
            => ConditionalLog(line, LogLevel.Debug, LogLevel.Protocol, obj, inline, disableConsoleLog);

        /// <summary>
        /// Reads the log level from the "LogLevel" key in the given IniFileHelper object.
        /// If the key does not exist or has an invalid value, the default (Error) is used.
        /// In Debug builds, the level is always set to at least Debug.
        /// </summary>
        /// <param name="iniFile">IniFileHelper object from which to read LogLevel.</param>
        public void SetLogLevel(IniFileHelper iniFile)
        {
            var logLevel = LogLevel.Error;

            if (iniFile != null)
            {
                if (iniFile.HasKey(nameof(LogLevel)))
                {
                    var iniLogLevel = iniFile.GetInt(nameof(LogLevel));
                    if (Enum.IsDefined(typeof(LogLevel), iniLogLevel))
                    {
                        logLevel = (LogLevel)iniLogLevel;
                    }
                }
            }
#if DEBUG
            if (logLevel != LogLevel.Debug && logLevel != LogLevel.Protocol)
            {
                logLevel = LogLevel.Debug;
            }
#endif
            LogLevel = logLevel;
        }

        /// <summary>
        /// Returns the prefix string for log lines:<br/>
        /// - Date in dd/MM/yyyy HH:mm:ss format.<br/>
        /// - Optional label for DEBUG or PROTOCOL log levels.<br/>
        /// The prefix does not include a trailing space.
        /// </summary>
        /// <param name="logLevel">Log level used to generate the prefix.</param>
        private string GetLogLinePrefix(LogLevel logLevel = LogLevel.None)
        {
            string prefix = $"{DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            if (logLevel == LogLevel.Debug)
                prefix = $"{prefix} DEBUG";
            else if (logLevel == LogLevel.Protocol)
                prefix = $"{prefix} PROTOCOL";
            return prefix;
        }

        #endregion

    }

}
