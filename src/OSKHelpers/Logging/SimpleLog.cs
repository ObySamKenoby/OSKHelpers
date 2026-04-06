using OSKHelpers.INIFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Semplice classe da utilizzare per generare i log dell'applicativo.
    /// </summary>
    public class SimpleLog
    {
        #region Membri

        private static readonly SimpleLogger _logger;

        #endregion

        #region Proprietà

        /// <summary>
        /// Istanza privata di <see cref="SimpleLogger"/> utilizzata per esporre i metodi e le proprietà.<br/>
        /// E' sconsigliato agire direttamente su questa, in ogni caso utilizzarla equivarrà ad utilizzare<br/>
        /// Le proprietà ed i metodi statici corrispondenti in <see cref="SimpleLog"/>.
        /// </summary>
        public static SimpleLogger Instance => _logger;

        /// <inheritdoc cref="SimpleLogger.DefaultLogPath"/>
        public static string DefaultLogPath => SimpleLogger.DefaultLogPath;

        /// <inheritdoc cref="SimpleLogger.EnableConsoleLog"/>
        public static bool EnableConsoleLog { get => _logger.EnableConsoleLog; set => _logger.EnableConsoleLog = value; }

        /// <inheritdoc cref="SimpleLogger.DisableDebugLog"/>
        public static bool DisableDebugLog { get => _logger.DisableDebugLog; set => _logger.DisableDebugLog = value; }

        /// <inheritdoc cref="SimpleLogger.LogLevel"/>
        public static LogLevel LogLevel { get => _logger.LogLevel; set => _logger.LogLevel = value; }

        /// <inheritdoc cref="SimpleLogger.LogPath"/>
        public static string LogPath { get => _logger.LogPath; set => _logger.LogPath = value; }

        /// <inheritdoc cref="SimpleLogger.Prefix"/>
        public static string Prefix { get => _logger.Prefix; set => _logger.Prefix = value; }

        /// <inheritdoc cref="SimpleLogger.LogFile"/>
        public static string LogFile => _logger.LogFile;

        /// <inheritdoc cref="SimpleLogger.ForceDebug"/>
        public static bool ForceDebug { get => _logger.ForceDebug; set => _logger.ForceDebug = value; }

        /// <inheritdoc cref="SimpleLogger.ForceProtocol"/>
        public static bool ForceProtocol { get => _logger.ForceProtocol; set => _logger.ForceProtocol = value; }

        /// <inheritdoc cref="SimpleLogger.LogLevelDebug"/>
        public static bool LogLevelDebug => _logger.LogLevelDebug;

        /// <inheritdoc cref="SimpleLogger.LogLevelProtocol"/>
        public static bool LogLevelProtocol => _logger.LogLevelProtocol;

        #endregion

        #region Costruttore

        static SimpleLog()
        {
            _logger         = new SimpleLogger();
            _logger.LogPath = DefaultLogPath;
            LogLevel        = LogLevel.Error;
#if DEBUG
            LogLevel        = LogLevel.Debug;
#endif
        }

        #endregion

        #region Metodi

        /// <inheritdoc cref="SimpleLogger.ToLog(LogLevel)"/>
        public static bool ToLog(LogLevel logLevel)
            => _logger.ToLog(LogLevel);

        /// <inheritdoc cref="SimpleLogger.Write(string, LogLevel)"/>
        public static void Write(string line, LogLevel logLevel = LogLevel.None) 
            => _logger.Write(line, LogLevel);

        /// <inheritdoc cref="SimpleLogger.Write(LogLevel, string, object, bool)"/>
        public static void Write(LogLevel logLevel, string line, object obj = null, bool inline = false) 
            => _logger.Write(logLevel, line, obj, inline);

        /// <inheritdoc cref="SimpleLogger.Write(LogLevel, object)"/>
        public static void Write(LogLevel logLevel, object obj)
            => _logger.Write(logLevel, obj);

        ///<inheritdoc cref="SimpleLogger.WriteLine(string)"/>
        public static void WriteLine(string line) 
            => _logger.WriteLine(line);

        /// <inheritdoc cref="SimpleLogger.ErrorWrite(string, object, bool)"/>
        public static void ErrorWrite(string line, object obj = null, bool inline = false)
            => _logger.ErrorWrite(line, obj, inline);

        /// <inheritdoc cref="SimpleLogger.ErrorWrite(object)"/>
        public static void ErrorWrite(object obj)
            => _logger.ErrorWrite(obj);

        /// <inheritdoc cref="SimpleLogger.DebugWrite(string, object, bool)"/>
        public static void DebugWrite(string line, object obj = null, bool inline = false) 
            => _logger.DebugWrite(line, obj, inline);

        /// <inheritdoc cref="SimpleLogger.DebugWrite(object)"/>
        public static void DebugWrite(object obj) 
            => _logger.DebugWrite(obj);

        /// <inheritdoc cref="SimpleLogger.ProtocolWrite(string, object, bool)"/>
        public static void ProtocolWrite(string line, object obj = null, bool inline = false) =>
            _logger.ProtocolWrite(line, obj, inline);

        /// <inheritdoc cref="SimpleLogger.ProtocolWrite(object)"/>
        public static void ProtocolWrite(object obj) 
            => _logger.ProtocolWrite(obj);

        /// <inheritdoc cref="SimpleLogger.LogLines(IEnumerable{string})"/>
        public static void LogLines(IEnumerable<string> lines)
            => _logger.LogLines(lines);

        /// <inheritdoc cref="SimpleLogger.LogLines(LogLevel, IEnumerable{string})"/>
        public static void LogLines(LogLevel logLevel, IEnumerable<string> lines)
            => _logger.LogLines(logLevel, lines);

        /// <inheritdoc cref="SimpleLogger.LogArray(LogLevel, string, string, IEnumerable{object}, bool)"/>
        public static void LogArray(LogLevel logLevel, string message, string arrayName, IEnumerable<object> array, bool serialize = false)
            => _logger.LogArray(logLevel, message, logLevel, arrayName, array, serialize);

        /// <inheritdoc cref="SimpleLogger.LogArray(LogLevel, string, LogLevel, string, IEnumerable{object}, bool)"/>
        public static void LogArray(LogLevel logLevel, string message, LogLevel detailLogLevel, string arrayName, IEnumerable<object> array, bool serialize = false)
            => _logger.LogArray(logLevel, message, detailLogLevel, arrayName, array, serialize);

        /// <inheritdoc cref="SimpleLogger.FormattedException(Exception, bool)"/>
        public static string FormattedException(Exception ex, bool includeStackTrace = false) 
            => _logger.FormattedException(ex, includeStackTrace);

        /// <inheritdoc cref="SimpleLogger.LogError(Exception, bool, string)"/>
        public static void LogError(Exception ex, bool includeStackTrace = false, [CallerMemberName] string callerMethodName = "")
        {
            var callerTypeName = new StackFrame(1).GetMethod().DeclaringType.Name;
            _logger.LogError(ex, callerTypeName, callerMethodName, includeStackTrace);
        }

        /// <inheritdoc cref="SimpleLogger.LogError(Exception, string, string, bool)"/>
        public static void LogError(Exception ex, string callerTypeName, string callerMethodName, bool includeStackTrace = false)
            => _logger.LogError(ex, callerTypeName, callerMethodName, includeStackTrace);

        /// <inheritdoc cref="SimpleLogger.GetCallerTypeName"/>
        public static string GetCallerTypeName() => new StackFrame(1).GetMethod().DeclaringType.Name;

        /// <inheritdoc cref="SimpleLogger.GetCallerMethodName(string)"/>
        public static string GetCallerMethodName([CallerMemberName] string callerMethodName = "") => callerMethodName;

        /// <inheritdoc cref="SimpleLogger.GetCallerTypeMethodName(string)"/>
        public static string GetCallerTypeMethodName([CallerMemberName] string callerMethodName = "") => $"{new StackFrame(1).GetMethod().DeclaringType.Name}.{callerMethodName}";

        ///<inheritdoc cref="SimpleLogger.LogCallerTypeMethodName(LogLevel, string)"/>
        public static void LogCallerTypeMethodName(LogLevel logLevel = LogLevel.Debug, [CallerMemberName] string callerMethodName = "") => Write(logLevel, $"{new StackFrame(1).GetMethod().DeclaringType.Name}.{callerMethodName}");

        /// <inheritdoc cref="SimpleLogger.ConsoleLog(string, LogLevel)"/>
        public void ConsoleLog(string text, LogLevel logLevel = LogLevel.None)
            => _logger.ConsoleLog(text, logLevel);
        
        ///<inheritdoc cref="SimpleLogger.DebugConsoleLog(string)"/>
        public static void DebugConsoleLog(string text)
            => _logger?.DebugConsoleLog(text);

        ///<inheritdoc cref="SimpleLogger.ProtocolConsoleLog(string)"/>
        public static void ProtocolConsoleLog(string text)
            => _logger?.ProtocolConsoleLog(text);

        /// <inheritdoc cref="SimpleLogger.ConditionalLog(string, LogLevel, LogLevel, object, bool, bool?)"/>
        public static void ConditionalLog(string line, LogLevel consoleLogLevel, LogLevel fileLogLevel, object obj = null, bool inline = false, bool? disableConsoleLog = null)
            => _logger?.ConditionalLog(line, consoleLogLevel, fileLogLevel, obj, inline, disableConsoleLog);

        /// <inheritdoc cref="SimpleLogger.DebugProtocolConditionalLog(string, object, bool, bool?)"/>
        public static void DebugProtocolConditionalLog(string line, object obj = null, bool inline = false, bool? disableConsoleLog = null)
            => _logger?.DebugProtocolConditionalLog(line, obj, inline, disableConsoleLog);

        /// <inheritdoc cref="SimpleLogger.SetLogLevel(IniFileHelper)"/>
        public static void SetLogLevel(IniFileHelper iniFile)
            => _logger?.SetLogLevel(iniFile);

        #endregion

    }

}
