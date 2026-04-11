using System;
using System.IO;
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
        public void ConstructorDefaultInitializesCorrectly()
        {
            var logger = new SimpleLogger();

            Assert.IsNotNull(logger.LogPath);
            Assert.IsFalse(logger.EnableConsoleLog);
            Assert.IsFalse(logger.ForceDebug);
            Assert.IsFalse(logger.ForceProtocol);
        }

        #endregion

        #region LogLevel

        /// <summary>
        /// Verifies that the log level can be set and read.
        /// </summary>
        [TestMethod]
        public void LogLevelSetValueReturnsSetValue()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Warning;

            Assert.AreEqual(LogLevel.Warning, logger.LogLevel);
        }

        /// <summary>
        /// Verifies that ForceDebug forces the level to Debug.
        /// </summary>
        [TestMethod]
        public void ForceDebugSetTrueForcesDebugLevel()
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
        public void ForceProtocolSetTrueForcesProtocolLevel()
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
        public void ForceProtocolWithForceDebugProtocolWins()
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
        public void ToLogVariousLevelsReturnsExpected(LogLevel messageLevel, LogLevel currentLevel, bool expected)
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
        public void LogLevelDebugWhenDebugReturnsTrue()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Debug;

            Assert.IsTrue(logger.LogLevelDebug);
        }

        /// <summary>
        /// Verifies that LogLevelDebug is false at Error level.
        /// </summary>
        [TestMethod]
        public void LogLevelDebugWhenErrorReturnsFalse()
        {
            var logger = new SimpleLogger();
            logger.LogLevel = LogLevel.Error;

            Assert.IsFalse(logger.LogLevelDebug);
        }

        /// <summary>
        /// Verifies LogLevelProtocol.
        /// </summary>
        [TestMethod]
        public void LogLevelProtocolWhenProtocolReturnsTrue()
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
        public void PrefixSetValueAffectsLogFileName()
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
        public void UsePrefixAsLogFileSetTrueUsesOnlyPrefix()
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
        public void LogFileCustomLogPathUsesCustomPath()
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
        public void FormattedExceptionValidExceptionContainsMessage()
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
        public void FormattedExceptionWithInnerExceptionContainsBothMessages()
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
        public void FormattedExceptionNullExceptionReturnsEmpty()
        {
            var logger  = new SimpleLogger();
            var result  = logger.FormattedException(null);

            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Verifies that includeStackTrace adds the stack trace.
        /// </summary>
        [TestMethod]
        public void FormattedExceptionWithStackTraceContainsStackInfo()
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
        public void WriteValidMessageCreatesLogFile()
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
        public void WriteEmptyMessageDoesNotCreateLogFile()
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
        public void WriteNullMessageDoesNotCreateLogFile()
        {
            var logger = new SimpleLogger();
            logger.LogPath  = _tempLogPath;
            logger.LogLevel = LogLevel.Debug;
            logger.Write(null);

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        #endregion
    }
}
