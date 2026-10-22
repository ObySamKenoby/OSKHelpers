using System;
using System.IO;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Logging
{
    /// <summary>
    /// Tests for the <see cref="CSVLogger{T}"/> class.
    /// </summary>
    [TestClass]
    public class CSVLoggerTests
    {
        #region Support classes

        /// <summary>
        /// Log item used by the tests.
        /// </summary>
        public class SampleItem : CSVLogItem
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        #endregion

        #region Members

        private string _tempLogPath;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Creates a temporary path for the log files (the directory is created by the logger).
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Removes the temporary directory.
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

        #region LogPath / LogFile

        /// <summary>
        /// Verifies that, until LogPath is assigned, the default log path is used.
        /// </summary>
        [TestMethod]
        public void LogPath_NotAssigned_ReturnsDefaultLogPath()
        {
            var logger = new CSVLogger<SampleItem>();

            Assert.AreEqual(SimpleLogger.DefaultLogPath, logger.LogPath);
        }

        /// <summary>
        /// Verifies that assigning null or an empty string to LogPath restores the default log path.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void LogPath_SetEmpty_RestoresDefaultLogPath(string value)
        {
            var logger      = new CSVLogger<SampleItem>();
            logger.LogPath  = _tempLogPath;
            logger.LogPath  = value;

            Assert.AreEqual(SimpleLogger.DefaultLogPath, logger.LogPath);
        }

        /// <summary>
        /// Verifies the default file name: PREFIXLogYYYYMMDD.csv in LogPath.
        /// </summary>
        [TestMethod]
        public void LogFile_Prefix_ReturnsDailyFileName()
        {
            var logger      = new CSVLogger<SampleItem>();
            logger.LogPath  = _tempLogPath;
            logger.Prefix   = "Device";

            Assert.AreEqual(Path.Combine(_tempLogPath, $"DeviceLog{DateTime.Now:yyyyMMdd}.csv"), logger.LogFile);
        }

        /// <summary>
        /// Verifies that, with UsePrefixAsLogFile, the file name is the prefix only.
        /// </summary>
        [TestMethod]
        public void LogFile_UsePrefixAsLogFile_ReturnsPrefixFileName()
        {
            var logger                  = new CSVLogger<SampleItem>();
            logger.LogPath              = _tempLogPath;
            logger.Prefix               = "Device";
            logger.UsePrefixAsLogFile   = true;

            Assert.IsTrue(logger.UsePrefixAsLogFile);
            Assert.AreEqual(Path.Combine(_tempLogPath, "Device.csv"), logger.LogFile);
        }

        /// <summary>
        /// Verifies that changing LogPath or Prefix regenerates the file name.
        /// </summary>
        [TestMethod]
        public void LogFile_LogPathAndPrefixChanged_RegeneratesFileName()
        {
            var logger      = new CSVLogger<SampleItem>();
            logger.LogPath  = _tempLogPath;
            var first       = logger.LogFile;
            logger.Prefix   = "Other";
            var second      = logger.LogFile;
            logger.LogPath  = Path.Combine(_tempLogPath, "Sub");

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(Path.Combine(_tempLogPath, "Sub", $"OtherLog{DateTime.Now:yyyyMMdd}.csv"), logger.LogFile);
        }

        #endregion

        #region Log

        /// <summary>
        /// Verifies that the first write creates the directory and the file with the header, followed by the data rows.
        /// </summary>
        [TestMethod]
        public void Log_NewFile_WritesHeaderAndRows()
        {
            var logger      = new CSVLogger<SampleItem>();
            logger.LogPath  = _tempLogPath;

            logger.Log(new SampleItem { Name = "First", Count = 1 });
            logger.Log(new SampleItem { Name = "Second", Count = 2 });

            var lines = File.ReadAllLines(logger.LogFile);
            CollectionAssert.AreEqual(new[] { "Count;Name", "1;\"First\"", "2;\"Second\"" }, lines);
        }

        /// <summary>
        /// Verifies that a second logger on the same existing file does not repeat the header.
        /// </summary>
        [TestMethod]
        public void Log_ExistingFile_DoesNotRepeatHeader()
        {
            var first       = new CSVLogger<SampleItem>();
            first.LogPath   = _tempLogPath;
            first.Log(new SampleItem { Name = "First", Count = 1 });
            var second      = new CSVLogger<SampleItem>();
            second.LogPath  = _tempLogPath;

            second.Log(new SampleItem { Name = "Second", Count = 2 });

            Assert.HasCount(3, File.ReadAllLines(second.LogFile));
        }

        /// <summary>
        /// Verifies that a null object writes nothing.
        /// </summary>
        [TestMethod]
        public void Log_NullObject_WritesNothing()
        {
            var logger      = new CSVLogger<SampleItem>();
            logger.LogPath  = _tempLogPath;

            logger.Log(null);

            Assert.IsFalse(File.Exists(logger.LogFile));
        }

        /// <summary>
        /// Verifies that, when the log file cannot be written, the error is written to the fallback log ERRORSLOG.txt
        /// in the assembly folder (the test output folder).
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-039")]
        public void Log_LogPathNotWritable_WritesToErrorsLog()
        {
            var errorsLogFile   = Path.Combine(Path.GetDirectoryName(typeof(SimpleLogger).Assembly.Location), "ERRORSLOG.txt");
            var blockingFile    = Path.Combine(_tempLogPath, "Blocking.txt");
            Directory.CreateDirectory(_tempLogPath);
            File.WriteAllText(blockingFile, string.Empty);
            File.Delete(errorsLogFile);
            try
            {
                // The log directory would be created under an existing file, so writing the log fails.
                var logger      = new CSVLogger<SampleItem>();
                logger.LogPath  = Path.Combine(blockingFile, "Logs");

                logger.Log(new SampleItem { Name = "First", Count = 1 });

                var content = File.ReadAllText(errorsLogFile);
                Assert.Contains("Error writing log.", content);
                Assert.Contains($"Log file name: {logger.LogFile}", content);
            }
            finally
            {
                File.Delete(errorsLogFile);
            }
        }

        #endregion
    }
}
