using System;
using System.IO;
using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Logging
{
    /// <summary>
    /// Tests for the <see cref="SimpleLogger"/> class.
    /// </summary>
    [TestClass]
    public class SimpleLoggerTests
    {
        #region Members

        private string _tempLogPath;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Initialization: creates a temporary directory for log files.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempLogPath);
        }

        /// <summary>
        /// Cleanup: removes the temporary directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempLogPath))
                {
                    Directory.Delete(_tempLogPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors during tests.
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Verifies that the default constructor initializes properties correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_InitializesProperties()
        {
            var logger = new SimpleLogger();

            Assert.IsNotNull(logger.LogPath);
            Assert.IsFalse(logger.EnableConsoleLog);
            Assert.IsFalse(logger.ForceDebug);
            Assert.IsFalse(logger.ForceProtocol);
        }

        #endregion

        #region LogPath

        /// <summary>
        /// Verifies that, until LogPath is assigned, the default log path is used.
        /// </summary>
        [TestMethod]
        public void LogPath_NotAssigned_ReturnsDefaultLogPath()
        {
            var logger = new SimpleLogger();

            Assert.AreEqual(SimpleLogger.DefaultLogPath, logger.LogPath);
        }

        /// <summary>
        /// Verifies that assigning null to LogPath restores the default log path.
        /// </summary>
        [TestMethod]
        public void LogPath_SetNull_RestoresDefaultLogPath()
        {
            var logger      = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.LogPath  = null;

            Assert.AreEqual(SimpleLogger.DefaultLogPath, logger.LogPath);
        }

#if DEBUG

        /// <summary>
        /// Verifies that a logger without an explicit LogPath, SimpleLog included, follows a change of
        /// Paths.AppdataDirectory made after its creation, while a logger with an explicit LogPath keeps it.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void LogFile_AppdataDirectoryChanged_FollowsDefaultLogsDirectory()
        {
            var originalAppdata         = Paths.AppdataDirectory;
            var defaultLogger           = new SimpleLogger();
            var customLogger            = new SimpleLogger();
            customLogger.LogPath        = _tempLogPath;
            _                           = defaultLogger.LogFile;
            _                           = SimpleLog.LogFile;

            try
            {
                Paths.OverrideAppdataDirectory(Path.Combine(_tempLogPath, "Appdata"));
                var expectedDirectory   = Path.Combine(_tempLogPath, "Appdata", "Logs");

                Assert.AreEqual(expectedDirectory, Path.GetDirectoryName(defaultLogger.LogFile));
                Assert.AreEqual(expectedDirectory, Path.GetDirectoryName(SimpleLog.LogFile));
                Assert.AreEqual(_tempLogPath, Path.GetDirectoryName(customLogger.LogFile));
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
            }
        }

#endif

        #endregion

        #region LogLevel

        /// <summary>
        /// Verifies that the log level can be set and read.
        /// </summary>
        [TestMethod]
        public void LogLevel_SetValue_ReturnsSetValue()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Warning;

            Assert.AreEqual(LogLevel.Warning, logger.LogLevel);
        }

        /// <summary>
        /// Verifies that ForceDebug forces the level to Debug.
        /// </summary>
        [TestMethod]
        public void ForceDebug_SetTrue_ForcesDebugLevel()
        {
            var logger = new SimpleLogger();
            logger.ForceDebug = true;
            logger.LogLevel = LogLevel.Error;

            Assert.AreEqual(LogLevel.Debug, logger.LogLevel);
        }

        /// <summary>
        /// Verifies that ForceProtocol forces the level to Protocol.
        /// </summary>
        [TestMethod]
        public void ForceProtocol_SetTrue_ForcesProtocolLevel()
        {
            var logger = new SimpleLogger();
            logger.ForceProtocol = true;
            logger.LogLevel = LogLevel.Error;

            Assert.AreEqual(LogLevel.Protocol, logger.LogLevel);
        }

        /// <summary>
        /// Verifies that ForceProtocol takes precedence over ForceDebug.
        /// </summary>
        [TestMethod]
        public void ForceProtocol_WithForceDebug_ProtocolWins()
        {
            var logger = new SimpleLogger();
            logger.ForceDebug = true;
            logger.ForceProtocol = true;

            Assert.AreEqual(LogLevel.Protocol, logger.LogLevel);
        }

        #endregion

        #region ToLog

        /// <summary>
        /// Verifies that ToLog returns true for compatible levels.
        /// </summary>
        [TestMethod]
        [DataRow(LogLevel.Error,    LogLevel.Error,     true)]
        [DataRow(LogLevel.Error,    LogLevel.Debug,     true)]
        [DataRow(LogLevel.Debug,    LogLevel.Error,     false)]
        [DataRow(LogLevel.None,     LogLevel.Error,     true)]
        [DataRow(LogLevel.Protocol, LogLevel.Protocol,  true)]
        [DataRow(LogLevel.Protocol, LogLevel.Debug,     false)]
        public void ToLog_VariousLevels_ReturnsExpected(LogLevel messageLevel, LogLevel currentLevel, bool expected)
        {
            var logger = new SimpleLogger();
            logger.LogLevel = currentLevel;

            Assert.AreEqual(expected, logger.ToLog(messageLevel));
        }

        #endregion

        #region LogLevelDebug / LogLevelProtocol

        /// <summary>
        /// Verifies LogLevelDebug.
        /// </summary>
        [TestMethod]
        public void LogLevelDebug_LevelDebug_ReturnsTrue()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Debug;

            Assert.IsTrue(logger.LogLevelDebug);
        }

        /// <summary>
        /// Verifies that LogLevelDebug is false at Error level.
        /// </summary>
        [TestMethod]
        public void LogLevelDebug_LevelError_ReturnsFalse()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Error;

            Assert.IsFalse(logger.LogLevelDebug);
        }

        /// <summary>
        /// Verifies LogLevelProtocol.
        /// </summary>
        [TestMethod]
        public void LogLevelProtocol_LevelProtocol_ReturnsTrue()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Protocol;

            Assert.IsTrue(logger.LogLevelProtocol);
        }

        #endregion

        #region Prefix / LogFile

        /// <summary>
        /// Verifies that Prefix is applied to the log file name.
        /// </summary>
        [TestMethod]
        public void Prefix_SetValue_AffectsLogFileName()
        {
            var logger = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.Prefix   = "MyApp_";

            Assert.Contains("MyApp_", logger.LogFile);
        }

        /// <summary>
        /// Verifies that UsePrefixAsLogFile uses the prefix as the file name.
        /// </summary>
        [TestMethod]
        public void UsePrefixAsLogFile_SetTrue_UsesOnlyPrefix()
        {
            var logger = new SimpleLogger();
            logger.LogPath              = _tempLogPath;
            logger.Prefix               = "Custom";
            logger.UsePrefixAsLogFile   = true;

            var expectedFile = Path.Combine(_tempLogPath, "Custom.txt");
            Assert.AreEqual(expectedFile, logger.LogFile);
        }

        /// <summary>
        /// Verifies that the log file is in the specified directory.
        /// </summary>
        [TestMethod]
        public void LogFile_CustomLogPath_UsesCustomPath()
        {
            var logger = new SimpleLogger();
            logger.LogPath = _tempLogPath;

            Assert.StartsWith(_tempLogPath, logger.LogFile);
        }

        #endregion

        #region FormattedException

        /// <summary>
        /// Verifies that FormattedException correctly formats an exception.
        /// </summary>
        [TestMethod]
        public void FormattedException_ValidException_ContainsMessage()
        {
            var logger  = new SimpleLogger();
            var ex      = new InvalidOperationException("Test error");
            var result  = logger.FormattedException(ex);

            Assert.Contains("Test error", result);
            Assert.Contains("InvalidOperationException", result);
        }

        /// <summary>
        /// Verifies that FormattedException with InnerException includes both messages.
        /// </summary>
        [TestMethod]
        public void FormattedException_InnerException_ContainsBothMessages()
        {
            var logger  = new SimpleLogger();
            var inner   = new ArgumentException("Inner error");
            var ex      = new InvalidOperationException("Outer error", inner);
            var result  = logger.FormattedException(ex);

            Assert.Contains("Outer error", result);
            Assert.Contains("Inner error", result);
        }

        /// <summary>
        /// Verifies that FormattedException with null returns an empty string.
        /// </summary>
        [TestMethod]
        public void FormattedException_NullException_ReturnsEmpty()
        {
            var logger  = new SimpleLogger();
            var result  = logger.FormattedException(null);

            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Verifies that includeStackTrace adds the stack trace.
        /// </summary>
        [TestMethod]
        public void FormattedException_IncludeStackTrace_ContainsStackInfo()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Error;

            try
            {
                throw new Exception("Stack test");
            }
            catch (Exception ex)
            {
                var result = logger.FormattedException(ex, true);
                Assert.Contains("Stack:", result);
            }
        }

        #endregion

        #region Write

        /// <summary>
        /// Verifies that Write creates the log file and writes the message.
        /// </summary>
        [TestMethod]
        public void Write_ValidMessage_CreatesLogFile()
        {
            var logger = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.LogLevel = LogLevel.Debug;
            logger.Write("Test message");

            Assert.IsTrue(File.Exists(logger.LogFile));
            var content = File.ReadAllText(logger.LogFile);
            Assert.Contains("Test message", content);
        }

        /// <summary>
        /// Verifies that Write with an empty string writes nothing.
        /// </summary>
        [TestMethod]
        public void Write_EmptyMessage_DoesNotCreateLogFile()
        {
            var logger = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.LogLevel = LogLevel.Debug;
            logger.Write(string.Empty);

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that Write with a null string writes nothing.
        /// </summary>
        [TestMethod]
        public void Write_NullMessage_DoesNotCreateLogFile()
        {
            var logger = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.LogLevel = LogLevel.Debug;
            logger.Write(null);

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that the line is written with the date/time prefix and, for Debug and Protocol, the level label.
        /// </summary>
        [TestMethod]
        [DataRow(LogLevel.None, "")]
        [DataRow(LogLevel.Error, "")]
        [DataRow(LogLevel.Debug, " DEBUG")]
        [DataRow(LogLevel.Protocol, " PROTOCOL")]
        public void Write_LogLevel_WritesPrefixWithLabel(LogLevel logLevel, string label)
        {
            var logger = CreateLogger(LogLevel.Protocol);

            logger.Write("Message", logLevel);

            var line = File.ReadAllLines(logger.LogFile)[0];
            Assert.MatchesRegex($@"^\d{{2}}/\d{{2}}/\d{{4}} \d{{2}}:\d{{2}}:\d{{2}}{label} Message$", line);
        }

        /// <summary>
        /// Verifies that a line with a level higher than LogLevel is not written.
        /// </summary>
        [TestMethod]
        public void Write_LevelHigherThanLogLevel_WritesNothing()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.Write("Message", LogLevel.Debug);
            logger.Write(LogLevel.Debug, "Message");
            logger.DebugWrite("Message");
            logger.ProtocolWrite("Message");

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that the object is serialised on the same line with inline, on the following line otherwise.
        /// </summary>
        [TestMethod]
        public void Write_ObjectInlineAndNotInline_SerializesObject()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.Write(LogLevel.Error, "Inline", new { Id = 1 }, true);
            logger.Write(LogLevel.Error, "NotInline", new { Id = 2 });
            logger.Write(LogLevel.Error, new { Id = 3 });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(4, lines);
            Assert.EndsWith("Inline {\"Id\":1}", lines[0]);
            Assert.EndsWith("NotInline", lines[1]);
            Assert.EndsWith("{\"Id\":2}", lines[2]);
            Assert.EndsWith(" {\"Id\":3}", lines[3]);
        }

        /// <summary>
        /// Verifies that WriteLine and ErrorWrite write the line.
        /// </summary>
        [TestMethod]
        public void WriteLineAndErrorWrite_Message_WritesLines()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.WriteLine("Line");
            logger.ErrorWrite("Error");
            logger.ErrorWrite(new { Id = 1 });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(3, lines);
            Assert.EndsWith(" Line", lines[0]);
            Assert.EndsWith(" Error", lines[1]);
            Assert.EndsWith(" {\"Id\":1}", lines[2]);
        }

        /// <summary>
        /// Verifies that DebugWrite writes the line with the DEBUG label, as described in ModelloDominio.md.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-029")]
        public void DebugWrite_LogLevelDebug_WritesLineWithDebugLabel()
        {
            var logger = CreateLogger(LogLevel.Debug);

            logger.DebugWrite("Message");
            logger.DebugWrite(new { Id = 1 });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(2, lines);
            Assert.Contains(" DEBUG Message", lines[0]);
            Assert.Contains(" DEBUG {\"Id\":1}", lines[1]);
        }

        /// <summary>
        /// Verifies that ProtocolWrite writes the line with the PROTOCOL label, as described in ModelloDominio.md.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-029")]
        public void ProtocolWrite_LogLevelProtocol_WritesLineWithProtocolLabel()
        {
            var logger = CreateLogger(LogLevel.Protocol);

            logger.ProtocolWrite("Message");
            logger.ProtocolWrite(new { Id = 1 });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(2, lines);
            Assert.Contains(" PROTOCOL Message", lines[0]);
            Assert.Contains(" PROTOCOL {\"Id\":1}", lines[1]);
        }

        /// <summary>
        /// Verifies that, when the log file cannot be written, the line is written to the fallback log ERRORSLOG.txt
        /// in the assembly folder (the test output folder).
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-039")]
        public void Write_LogPathNotWritable_WritesToErrorsLog()
        {
            var errorsLogFile   = Path.Combine(Path.GetDirectoryName(typeof(SimpleLogger).Assembly.Location), "ERRORSLOG.txt");
            var blockingFile    = Path.Combine(_tempLogPath, "Blocking.txt");
            var message         = $"Message {Guid.NewGuid()}";
            File.WriteAllText(blockingFile, string.Empty);
            File.Delete(errorsLogFile);
            try
            {
                // The log directory would be created under an existing file, so writing the log fails.
                var logger = CreateLogger(LogLevel.Error);
                logger.LogPath = Path.Combine(blockingFile, "Logs");

                logger.Write(message);

                var content = File.ReadAllText(errorsLogFile);
                Assert.Contains("Error writing log.", content);
                Assert.Contains($"Original message: {message}", content);
            }
            finally
            {
                File.Delete(errorsLogFile);
            }
        }

        #endregion

        #region LogLines / LogArray

        /// <summary>
        /// Verifies that LogLines writes all the lines with the prefix.
        /// </summary>
        [TestMethod]
        public void LogLines_Lines_WritesAllLines()
        {
            var logger = CreateLogger(LogLevel.Debug);

            logger.LogLines(new[] { "First", "Second" });
            logger.LogLines(LogLevel.Debug, new[] { "Third" });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(3, lines);
            Assert.EndsWith(" First", lines[0]);
            Assert.EndsWith(" Second", lines[1]);
            Assert.EndsWith(" DEBUG Third", lines[2]);
        }

        /// <summary>
        /// Verifies that LogLines writes nothing for empty or null lines or an insufficient level.
        /// </summary>
        [TestMethod]
        public void LogLines_EmptyLinesOrLevelNotSufficient_WritesNothing()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogLines(null);
            logger.LogLines(Array.Empty<string>());
            logger.LogLines(LogLevel.Debug, new[] { "Line" });

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that LogArray writes the message and each element with its index.
        /// </summary>
        [TestMethod]
        public void LogArray_Elements_WritesMessageAndElements()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogArray(LogLevel.Error, "Values", "arr", new object[] { "a", 2 });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(3, lines);
            Assert.EndsWith(" Values", lines[0]);
            Assert.EndsWith(" arr[0]: a", lines[1]);
            Assert.EndsWith(" arr[1]: 2", lines[2]);
        }

        /// <summary>
        /// Verifies that, with serialize, the elements are written as JSON.
        /// </summary>
        [TestMethod]
        public void LogArray_Serialize_WritesJsonElements()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogArray(LogLevel.Error, null, "arr", new object[] { new { Id = 1 } }, true);

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(1, lines);
            Assert.EndsWith(" arr[0]: {\"Id\":1}", lines[0]);
        }

        /// <summary>
        /// Verifies that the elements are not written when the detail level is not sufficient.
        /// </summary>
        [TestMethod]
        public void LogArray_DetailLevelNotSufficient_WritesOnlyMessage()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogArray(LogLevel.Error, "Values", LogLevel.Debug, "arr", new object[] { "a" });

            var lines = File.ReadAllLines(logger.LogFile);
            Assert.HasCount(1, lines);
            Assert.EndsWith(" Values", lines[0]);
        }

        /// <summary>
        /// Verifies that LogArray writes nothing for an empty or null array or an insufficient level.
        /// </summary>
        [TestMethod]
        public void LogArray_EmptyArrayOrLevelNotSufficient_WritesNothing()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogArray(LogLevel.Error, "Values", "arr", null);
            logger.LogArray(LogLevel.Error, "Values", "arr", Array.Empty<object>());
            logger.LogArray(LogLevel.Debug, "Values", "arr", new object[] { "a" });

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        #endregion

        #region LogError

        /// <summary>
        /// Verifies that LogError writes "Error in Class.Method: exception" with the caller type and method.
        /// </summary>
        [TestMethod]
        public void LogError_Exception_WritesCallerAndException()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogError(new InvalidOperationException("Failure"));

            var content = File.ReadAllText(logger.LogFile);
            Assert.Contains($"Error in {nameof(SimpleLoggerTests)}.{nameof(LogError_Exception_WritesCallerAndException)}: System.InvalidOperationException Failure", content);
        }

        /// <summary>
        /// Verifies that LogError with explicit names uses them, and that a null exception is still logged.
        /// </summary>
        [TestMethod]
        public void LogError_ExplicitNamesAndNullException_WritesMessage()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.LogError(null, "MyType", "MyMethod");

            Assert.Contains("Error in MyType.MyMethod: ", File.ReadAllText(logger.LogFile));
        }

        #endregion

        #region ConditionalLog

        /// <summary>
        /// Verifies that the line is written to the file when the file level is sufficient.
        /// </summary>
        [TestMethod]
        public void ConditionalLog_BothLevelsSufficient_WritesToFile()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.ConditionalLog("Message", LogLevel.Error, LogLevel.Error, disableConsoleLog: true);

            Assert.EndsWith(" Message", File.ReadAllLines(logger.LogFile)[0]);
        }

        /// <summary>
        /// Verifies that the file threshold is independent of the console threshold: the line is written to the file
        /// even when the console level is not sufficient.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-030")]
        public void ConditionalLog_ConsoleLevelNotSufficient_WritesToFile()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.ConditionalLog("Message", LogLevel.Protocol, LogLevel.Error, disableConsoleLog: true);

            Assert.IsTrue(File.Exists(logger.LogFile));
            Assert.EndsWith(" Message", File.ReadAllLines(logger.LogFile)[0]);
        }

        /// <summary>
        /// Verifies that nothing is written to the file when the file level is not sufficient.
        /// </summary>
        [TestMethod]
        public void ConditionalLog_FileLevelNotSufficient_WritesNothing()
        {
            var logger = CreateLogger(LogLevel.Error);

            logger.ConditionalLog("Message", LogLevel.Error, LogLevel.Debug, disableConsoleLog: true);

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that, with the console enabled and an inline object, the object is written to the file only once.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-031")]
        public void ConditionalLog_ConsoleEnabledInlineObject_SerializesObjectOnce()
        {
            var logger              = CreateLogger(LogLevel.Error);
            logger.DisableDebugLog  = true;
            var originalOut         = Console.Out;
            try
            {
                Console.SetOut(new StringWriter());

                logger.ConditionalLog("Message", LogLevel.Error, LogLevel.Error, new { Id = 1 }, true, false);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var line = File.ReadAllLines(logger.LogFile)[0];
            Assert.EndsWith(" Message {\"Id\":1}", line);
        }

        /// <summary>
        /// Verifies that DebugProtocolConditionalLog writes to the file only with level Protocol.
        /// </summary>
        [TestMethod]
        public void DebugProtocolConditionalLog_LogLevelProtocol_WritesToFile()
        {
            var logger = CreateLogger(LogLevel.Protocol);

            logger.DebugProtocolConditionalLog("Message", disableConsoleLog: true);

            Assert.Contains("Message", File.ReadAllText(logger.LogFile));
        }

        #endregion

        #region Console

        /// <summary>
        /// Verifies that ConsoleLog prints only when EnableConsoleLog is true and the level is sufficient.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [DataRow(true, LogLevel.Error, true)]
        [DataRow(false, LogLevel.Error, false)]
        [DataRow(true, LogLevel.Debug, false)]
        public void ConsoleLog_EnableConsoleLogAndLevel_PrintsExpected(bool enableConsoleLog, LogLevel messageLevel, bool printed)
        {
            var logger                  = CreateLogger(LogLevel.Error);
            logger.EnableConsoleLog     = enableConsoleLog;
            logger.DisableDebugLog      = true;
            var output                  = CaptureConsole(() => logger.ConsoleLog("Console message", messageLevel));

            Assert.AreEqual(printed, output.Contains("Console message"));
        }

        /// <summary>
        /// Verifies that DebugConsoleLog and ProtocolConsoleLog print nothing when DisableDebugLog is true.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void DebugConsoleLogAndProtocolConsoleLog_DisableDebugLog_PrintNothing()
        {
            var logger              = CreateLogger(LogLevel.Protocol);
            logger.DisableDebugLog  = true;
            var output              = CaptureConsole(() =>
            {
                logger.DebugConsoleLog("Debug message");
                logger.ProtocolConsoleLog("Protocol message");
            });

            Assert.AreEqual(string.Empty, output);
        }

        #endregion

        #region SetLogLevel

        /// <summary>
        /// Verifies that SetLogLevel sets a valid level; in Debug the minimum level is Debug.
        /// </summary>
        [TestMethod]
        [DataRow(5, LogLevel.Protocol)]
        [DataRow(4, LogLevel.Debug)]
#if DEBUG
        [DataRow(1, LogLevel.Debug)]
        [DataRow(99, LogLevel.Debug)]
#else
        [DataRow(1, LogLevel.Error)]
        [DataRow(99, LogLevel.Error)]
#endif
        public void SetLogLevel_Int_SetsLevel(int logLevel, LogLevel expected)
        {
            var logger = new SimpleLogger();

            logger.SetLogLevel(logLevel);

            Assert.AreEqual(expected, logger.LogLevel);
        }

        /// <summary>
        /// Verifies that SetLogLevel reads the level from ISettingsBase.
        /// </summary>
        [TestMethod]
        public void SetLogLevel_Settings_SetsLevelFromSettings()
        {
            var settings    = new Moq.Mock<OSKHelpers.Interfaces.ISettingsBase>();
            settings.SetupGet(s => s.LogLevel).Returns((int)LogLevel.Protocol);
            var logger      = new SimpleLogger();

            logger.SetLogLevel(settings.Object);

            Assert.AreEqual(LogLevel.Protocol, logger.LogLevel);
            settings.VerifyGet(s => s.LogLevel, Moq.Times.Once());
        }

        /// <summary>
        /// Verifies that SetLogLevel with null settings uses the default level.
        /// </summary>
        [TestMethod]
        public void SetLogLevel_NullSettings_SetsDefaultLevel()
        {
            var logger      = new SimpleLogger();
            logger.LogLevel = LogLevel.Protocol;

            logger.SetLogLevel((OSKHelpers.Interfaces.ISettingsBase)null);

#if DEBUG
            Assert.AreEqual(LogLevel.Debug, logger.LogLevel);
#else
            Assert.AreEqual(LogLevel.Error, logger.LogLevel);
#endif
        }

        /// <summary>
        /// Verifies that SetLogLevel reads the LogLevel key from the INI file.
        /// </summary>
        [TestMethod]
        public void SetLogLevel_IniFile_SetsLevelFromKey()
        {
            var iniFile = new OSKHelpers.INIFile.IniFileHelper();
            iniFile.Parse(new System.Collections.Generic.List<string> { "LogLevel = 5" });
            var logger  = new SimpleLogger();

            logger.SetLogLevel(iniFile);

            Assert.AreEqual(LogLevel.Protocol, logger.LogLevel);
        }

        #endregion

        #region Caller names

        /// <summary>
        /// Verifies the caller type and method names.
        /// </summary>
        [TestMethod]
        public void GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod()
        {
            var logger = new SimpleLogger();

            Assert.AreEqual(nameof(SimpleLoggerTests), logger.GetCallerTypeName());
            Assert.AreEqual(nameof(GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod), logger.GetCallerMethodName());
            Assert.AreEqual($"{nameof(SimpleLoggerTests)}.{nameof(GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod)}", logger.GetCallerTypeMethodName());
        }

        /// <summary>
        /// Verifies that LogCallerTypeMethodName writes the caller type and method.
        /// </summary>
        [TestMethod]
        public void LogCallerTypeMethodName_LevelSufficient_WritesCaller()
        {
            var logger = CreateLogger(LogLevel.Debug);

            logger.LogCallerTypeMethodName();

            Assert.Contains($"{nameof(SimpleLoggerTests)}.{nameof(LogCallerTypeMethodName_LevelSufficient_WritesCaller)}", File.ReadAllText(logger.LogFile));
        }

        #endregion

        #region Support methods

        /// <summary>
        /// Creates a logger on the temporary directory, with the given level and console output disabled.
        /// </summary>
        private SimpleLogger CreateLogger(LogLevel logLevel)
        {
            var logger              = new SimpleLogger();
            logger.LogPath          = _tempLogPath;
            logger.LogLevel         = logLevel;
            logger.DisableDebugLog  = true;

            return logger;
        }

        /// <summary>
        /// Executes the action capturing the console output.
        /// </summary>
        private static string CaptureConsole(Action action)
        {
            var originalOut = Console.Out;
            var writer      = new StringWriter();
            try
            {
                Console.SetOut(writer);
                action();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return writer.ToString();
        }

        #endregion
    }
}
