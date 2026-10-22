using System;
using System.Collections.Generic;
using System.IO;
using OSKHelpers.INIFile;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Logging
{
    /// <summary>
    /// Tests for the <see cref="SimpleLog"/> class, static façade over <see cref="SimpleLog.Instance"/>.<br/>
    /// The behaviour of the single methods is verified in <see cref="SimpleLoggerTests"/>; here the delegation is verified.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SimpleLogTests
    {
        #region Members

        private string _tempLogPath;
        private LogLevel _originalLogLevel;
        private bool _originalDisableDebugLog;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Redirects the default log to a temporary directory.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath                = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _originalLogLevel           = SimpleLog.LogLevel;
            _originalDisableDebugLog    = SimpleLog.DisableDebugLog;
            SimpleLog.LogPath           = _tempLogPath;
            SimpleLog.DisableDebugLog   = true;
            SimpleLog.LogLevel          = LogLevel.Protocol;
        }

        /// <summary>
        /// Restores the default log and removes the temporary directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            SimpleLog.LogPath           = null;
            SimpleLog.Prefix            = null;
            SimpleLog.ForceDebug        = false;
            SimpleLog.ForceProtocol     = false;
            SimpleLog.EnableConsoleLog  = false;
            SimpleLog.LogLevel          = _originalLogLevel;
            SimpleLog.DisableDebugLog   = _originalDisableDebugLog;
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

        #region Properties

        /// <summary>
        /// Verifies that the static properties read and write the properties of Instance.
        /// </summary>
        [TestMethod]
        public void Properties_SetValues_DelegatedToInstance()
        {
            SimpleLog.Prefix            = "Facade";
            SimpleLog.EnableConsoleLog  = true;
            SimpleLog.LogLevel          = LogLevel.Warning;

            Assert.IsNotNull(SimpleLog.Instance);
            Assert.AreEqual("Facade", SimpleLog.Instance.Prefix);
            Assert.IsTrue(SimpleLog.Instance.EnableConsoleLog);
            Assert.AreEqual(LogLevel.Warning, SimpleLog.Instance.LogLevel);
            Assert.AreEqual(_tempLogPath, SimpleLog.Instance.LogPath);
            Assert.AreEqual(SimpleLog.Instance.LogFile, SimpleLog.LogFile);
            Assert.AreEqual(SimpleLogger.DefaultLogPath, SimpleLog.DefaultLogPath);
        }

        /// <summary>
        /// Verifies ForceDebug, ForceProtocol and the derived properties.
        /// </summary>
        [TestMethod]
        public void ForceDebugAndForceProtocol_SetTrue_ForceLevels()
        {
            SimpleLog.LogLevel = LogLevel.Error;

            SimpleLog.ForceDebug = true;
            Assert.IsTrue(SimpleLog.ForceDebug);
            Assert.IsTrue(SimpleLog.LogLevelDebug);
            Assert.IsFalse(SimpleLog.LogLevelProtocol);

            SimpleLog.ForceProtocol = true;
            Assert.IsTrue(SimpleLog.ForceProtocol);
            Assert.IsTrue(SimpleLog.LogLevelProtocol);
            Assert.IsTrue(SimpleLog.ToLog(LogLevel.Protocol));
        }

        #endregion

        #region Write methods

        /// <summary>
        /// Verifies that the write methods write to the log file of Instance.
        /// </summary>
        [TestMethod]
        public void WriteMethods_Messages_WriteToInstanceLogFile()
        {
            SimpleLog.Write("Write");
            SimpleLog.Write(LogLevel.Error, "WriteLevel");
            SimpleLog.Write(LogLevel.Error, new { Id = 1 });
            SimpleLog.WriteLine("WriteLine");
            SimpleLog.ErrorWrite("ErrorWrite");
            SimpleLog.ErrorWrite(new { Id = 2 });
            SimpleLog.LogLines(new[] { "LogLines" });
            SimpleLog.LogLines(LogLevel.Error, new[] { "LogLinesLevel" });
            SimpleLog.LogArray(LogLevel.Error, "LogArray", "arr", new object[] { "a" });
            SimpleLog.LogArray(LogLevel.Error, "LogArrayDetail", LogLevel.Error, "det", new object[] { "b" });

            var content = File.ReadAllText(SimpleLog.Instance.LogFile);
            foreach (var expected in new[] { "Write", "WriteLevel", "{\"Id\":1}", "WriteLine", "ErrorWrite", "{\"Id\":2}", "LogLines",
                                             "LogLinesLevel", "LogArray", "arr[0]: a", "LogArrayDetail", "det[0]: b" })
            {
                Assert.Contains(expected, content);
            }
        }

        /// <summary>
        /// Verifies that DebugWrite and ProtocolWrite write to the log file of Instance.
        /// </summary>
        [TestMethod]
        public void DebugWriteAndProtocolWrite_Messages_WriteToInstanceLogFile()
        {
            SimpleLog.DebugWrite("DebugMessage");
            SimpleLog.DebugWrite(new { Id = 3 });
            SimpleLog.ProtocolWrite("ProtocolMessage");
            SimpleLog.ProtocolWrite(new { Id = 4 });
            SimpleLog.ConditionalLog("Conditional", LogLevel.Error, LogLevel.Error, disableConsoleLog: true);
            SimpleLog.DebugProtocolConditionalLog("DebugProtocol", disableConsoleLog: true);

            var content = File.ReadAllText(SimpleLog.LogFile);
            foreach (var expected in new[] { "DebugMessage", "{\"Id\":3}", "ProtocolMessage", "{\"Id\":4}", "Conditional", "DebugProtocol" })
            {
                Assert.Contains(expected, content);
            }
        }

        /// <summary>
        /// Verifies that LogError writes the caller type and method of the façade caller.
        /// </summary>
        [TestMethod]
        public void LogError_Exception_WritesCallerTypeAndMethod()
        {
            SimpleLog.LogError(new InvalidOperationException("Failure"));
            SimpleLog.LogError(null, "MyType", "MyMethod");

            var content = File.ReadAllText(SimpleLog.LogFile);
            Assert.Contains($"Error in {nameof(SimpleLogTests)}.{nameof(LogError_Exception_WritesCallerTypeAndMethod)}: System.InvalidOperationException Failure", content);
            Assert.Contains("Error in MyType.MyMethod: ", content);
        }

        /// <summary>
        /// Verifies FormattedException and LogCallerTypeMethodName.
        /// </summary>
        [TestMethod]
        public void FormattedExceptionAndLogCallerTypeMethodName_Calls_DelegatedToInstance()
        {
            var ex = new InvalidOperationException("Failure");

            SimpleLog.LogCallerTypeMethodName();

            Assert.AreEqual(SimpleLog.Instance.FormattedException(ex, true), SimpleLog.FormattedException(ex, true));
            Assert.Contains($"{nameof(SimpleLogTests)}.{nameof(FormattedExceptionAndLogCallerTypeMethodName_Calls_DelegatedToInstance)}", File.ReadAllText(SimpleLog.LogFile));
        }

        #endregion

        #region Caller names

        /// <summary>
        /// Verifies that the caller names refer to the façade caller.
        /// </summary>
        [TestMethod]
        public void GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod()
        {
            Assert.AreEqual(nameof(SimpleLogTests), SimpleLog.GetCallerTypeName());
            Assert.AreEqual(nameof(GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod), SimpleLog.GetCallerMethodName());
            Assert.AreEqual($"{nameof(SimpleLogTests)}.{nameof(GetCallerNames_DirectCall_ReturnsCallerTypeAndMethod)}", SimpleLog.GetCallerTypeMethodName());
        }

        #endregion

        #region Console

        /// <summary>
        /// Verifies that ConsoleLog prints when EnableConsoleLog is true, and that the debug console methods print nothing
        /// when DisableDebugLog is true.
        /// </summary>
        [TestMethod]
        public void ConsoleMethods_EnableConsoleLog_PrintExpected()
        {
            SimpleLog.EnableConsoleLog  = true;
            SimpleLog.LogLevel          = LogLevel.Error;
            var originalOut             = Console.Out;
            var writer                  = new StringWriter();
            try
            {
                Console.SetOut(writer);
                SimpleLog.ConsoleLog("Console message", LogLevel.Error);
                SimpleLog.DebugConsoleLog("Debug message");
                SimpleLog.ProtocolConsoleLog("Protocol message");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = writer.ToString();
            Assert.Contains("Console message", output);
            Assert.DoesNotContain("Debug message", output);
            Assert.DoesNotContain("Protocol message", output);
        }

        #endregion

        #region SetLogLevel

        /// <summary>
        /// Verifies the SetLogLevel overloads.
        /// </summary>
        [TestMethod]
        public void SetLogLevel_Overloads_SetLevel()
        {
            var settings = new Moq.Mock<OSKHelpers.Interfaces.ISettingsBase>();
            settings.SetupGet(s => s.LogLevel).Returns((int)LogLevel.Debug);
            var iniFile = new IniFileHelper();
            iniFile.Parse(new List<string> { "LogLevel = 5" });

            SimpleLog.SetLogLevel((int)LogLevel.Protocol);
            Assert.AreEqual(LogLevel.Protocol, SimpleLog.LogLevel);

            SimpleLog.SetLogLevel(settings.Object);
            Assert.AreEqual(LogLevel.Debug, SimpleLog.LogLevel);

            SimpleLog.SetLogLevel(iniFile);
            Assert.AreEqual(LogLevel.Protocol, SimpleLog.LogLevel);
        }

        #endregion
    }
}
