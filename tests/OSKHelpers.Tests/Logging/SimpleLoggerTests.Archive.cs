using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Logging
{
    /// <summary>
    /// Tests for the archiving of the old log files of <see cref="SimpleLogger"/> (SimpleLogger.Archive.cs).
    /// </summary>
    public partial class SimpleLoggerTests
    {
        #region Constants

        private const int ARCHIVETESTTIMEOUTSECONDS = 15;
        private const int NOARCHIVEWAITMILLISECONDS = 1000;

        #endregion

        #region Archive properties

        /// <summary>
        /// Verifies the default values of the archiving properties.
        /// </summary>
        [TestMethod]
        public void ArchiveProperties_Default_InitializedToDefaults()
        {
            var logger = new SimpleLogger();

            Assert.IsFalse(logger.EnableArchiving);
            Assert.AreEqual(SimpleLogger.DEFAULTARCHIVERETENTIONMONTHS, logger.ArchiveRetentionMonths);
        }

        /// <summary>
        /// Verifies that values lower than the minimum are replaced by the minimum.
        /// </summary>
        [TestMethod]
        public void ArchiveProperties_ValuesBelowMinimum_ReplacedByMinimum()
        {
            var logger = CreateArchivingLogger(null);

            logger.ArchiveRetentionMonths = 0;

            Assert.AreEqual(SimpleLogger.MINARCHIVERETENTIONMONTHS, logger.ArchiveRetentionMonths);
        }

        #endregion

        #region ArchiveLogs

        /// <summary>
        /// Verifies that the files of the months before the retention months are moved into the zip file of their month,
        /// while the files of the current month and of the retention months stay in the log folder.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_OldAndRecentFiles_ArchivesOnlyOldMonths()
        {
            var logger      = CreateArchivingLogger("App");
            var keptDate    = FirstDayOfCurrentMonth().AddMonths(-SimpleLogger.DEFAULTARCHIVERETENTIONMONTHS);
            var oldDate     = keptDate.AddDays(-1);
            var todayFile   = CreateDailyLogFile("App", DateTime.Today, "today");
            var keptFile    = CreateDailyLogFile("App", keptDate, "kept");
            var oldFile     = CreateDailyLogFile("App", oldDate, "old content");

            var result = logger.ArchiveLogs();

            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(todayFile));
            Assert.IsTrue(File.Exists(keptFile));
            Assert.IsFalse(File.Exists(oldFile));
            Assert.AreEqual("old content", ReadZipEntry(GetZipFile("App", oldDate), Path.GetFileName(oldFile)));
            Assert.IsFalse(File.Exists(GetZipFile("App", keptDate)));
            Assert.IsEmpty(Directory.GetFiles(_tempLogPath, "*.lock"));
            Assert.IsEmpty(Directory.GetFiles(_tempLogPath, "*.tmp"));
        }

        /// <summary>
        /// Verifies that the zip file name has the form PREFIXLog_YYYY_MM.zip.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_OldFile_ZipNamedWithYearAndMonth()
        {
            var logger  = CreateArchivingLogger("App");
            var date    = new DateTime(2020, 3, 15);
            CreateDailyLogFile("App", date, "old");

            logger.ArchiveLogs();

            Assert.IsTrue(File.Exists(Path.Combine(_tempLogPath, "AppLog_2020_03.zip")));
        }

        /// <summary>
        /// Verifies that the files are added to an existing zip file, keeping its entries.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_ExistingZip_AddsEntriesKeepingExisting()
        {
            var logger  = CreateArchivingLogger(null);
            var date    = FirstDayOfCurrentMonth().AddMonths(-3);
            var zipFile = GetZipFile(null, date);
            using (var archive = ZipFile.Open(zipFile, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("Previous.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("previous");
                }
            }
            var oldFile = CreateDailyLogFile(null, date, "new");

            var result = logger.ArchiveLogs();

            Assert.IsTrue(result);
            Assert.AreEqual("previous", ReadZipEntry(zipFile, "Previous.txt"));
            Assert.AreEqual("new", ReadZipEntry(zipFile, Path.GetFileName(oldFile)));
        }

        /// <summary>
        /// Verifies that a file with the name of an existing entry is added with a numeric suffix, without overwriting it.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_DuplicateEntryName_AddsEntryWithSuffix()
        {
            var logger  = CreateArchivingLogger(null);
            var date    = FirstDayOfCurrentMonth().AddMonths(-3);
            var oldFile = CreateDailyLogFile(null, date, "first");
            logger.ArchiveLogs();
            CreateDailyLogFile(null, date, "second");

            var result = logger.ArchiveLogs();

            var zipFile     = GetZipFile(null, date);
            var name        = Path.GetFileNameWithoutExtension(oldFile);
            Assert.IsTrue(result);
            Assert.AreEqual("first", ReadZipEntry(zipFile, $"{name}.txt"));
            Assert.AreEqual("second", ReadZipEntry(zipFile, $"{name}_1.txt"));
        }

        /// <summary>
        /// Verifies that all the files of a month go into the same zip file, and the files of different months into
        /// different zip files.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_FilesOfDifferentMonths_OneZipPerMonth()
        {
            var logger          = CreateArchivingLogger(null);
            var firstMonth      = FirstDayOfCurrentMonth().AddMonths(-4);
            var secondMonth     = FirstDayOfCurrentMonth().AddMonths(-3);
            var firstDayFile    = CreateDailyLogFile(null, firstMonth, "first day");
            var lastDayFile     = CreateDailyLogFile(null, secondMonth.AddDays(-1), "last day");
            var secondMonthFile = CreateDailyLogFile(null, secondMonth, "second month");

            var result = logger.ArchiveLogs();

            Assert.IsTrue(result);
            Assert.AreEqual("first day", ReadZipEntry(GetZipFile(null, firstMonth), Path.GetFileName(firstDayFile)));
            Assert.AreEqual("last day", ReadZipEntry(GetZipFile(null, firstMonth), Path.GetFileName(lastDayFile)));
            Assert.AreEqual("second month", ReadZipEntry(GetZipFile(null, secondMonth), Path.GetFileName(secondMonthFile)));
        }

        /// <summary>
        /// Verifies that only the files of the logger prefix are archived.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_OtherPrefixesAndNames_IgnoresOtherFiles()
        {
            var logger      = CreateArchivingLogger("Dev");
            var date        = FirstDayOfCurrentMonth().AddMonths(-3);
            var devFile     = CreateDailyLogFile("Dev", date, "dev");
            var otherFile   = CreateDailyLogFile("DevX", date, "other");
            var noPrefix    = CreateDailyLogFile(null, date, "no prefix");
            var customFile  = Path.Combine(_tempLogPath, "Dev.txt");
            File.WriteAllText(customFile, "custom");

            logger.ArchiveLogs();

            Assert.IsFalse(File.Exists(devFile));
            Assert.IsTrue(File.Exists(otherFile));
            Assert.IsTrue(File.Exists(noPrefix));
            Assert.IsTrue(File.Exists(customFile));
            Assert.IsFalse(File.Exists(GetZipFile("DevX", date)));
            Assert.IsFalse(File.Exists(GetZipFile(null, date)));
        }

        /// <summary>
        /// Verifies that ArchiveRetentionMonths changes the number of months kept uncompressed.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_CustomRetentionMonths_ArchivesAccordingly()
        {
            var logger                      = CreateArchivingLogger(null);
            logger.ArchiveRetentionMonths   = 3;
            var keptDate                    = FirstDayOfCurrentMonth().AddMonths(-3);
            var keptFile                    = CreateDailyLogFile(null, keptDate, "kept");
            var oldFile                     = CreateDailyLogFile(null, keptDate.AddDays(-1), "old");

            logger.ArchiveLogs();

            Assert.IsTrue(File.Exists(keptFile));
            Assert.IsFalse(File.Exists(oldFile));
        }

        /// <summary>
        /// Verifies that, when the zip file is locked by another process, no file is archived nor deleted.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_ZipLocked_KeepsFilesAndReturnsFalse()
        {
            var logger  = CreateArchivingLogger(null);
            var date    = FirstDayOfCurrentMonth().AddMonths(-3);
            var oldFile = CreateDailyLogFile(null, date, "old");
            var zipFile = GetZipFile(null, date);

            bool result;
            using (new FileStream(zipFile + ".lock", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                result = logger.ArchiveLogs();
            }

            Assert.IsFalse(result);
            Assert.IsTrue(File.Exists(oldFile));
            Assert.IsFalse(File.Exists(zipFile));
        }

        /// <summary>
        /// Verifies that a missing log folder is not created.
        /// </summary>
        [TestMethod]
        public void ArchiveLogs_MissingLogPath_DoesNotCreateFolder()
        {
            var logger      = CreateArchivingLogger(null);
            logger.LogPath  = Path.Combine(_tempLogPath, "Missing");

            var result = logger.ArchiveLogs();

            Assert.IsTrue(result);
            Assert.IsFalse(Directory.Exists(logger.LogPath));
        }

        #endregion

        #region Automatic archiving

        /// <summary>
        /// Verifies that, with archiving enabled, the regeneration of the log file name on the first write archives the old
        /// files in the background.
        /// </summary>
        [TestMethod]
        public void EnableArchiving_True_WriteArchivesOldFiles()
        {
            var logger              = CreateArchivingLogger(null);
            var oldFile             = CreateDailyLogFile(null, FirstDayOfCurrentMonth().AddMonths(-3), "old");
            logger.EnableArchiving  = true;

            logger.Write("Line");

            Assert.IsTrue(WaitUntil(() => !File.Exists(oldFile)));
        }

        /// <summary>
        /// Verifies that, with archiving disabled, writing to the log does not archive the files.
        /// </summary>
        [TestMethod]
        public void EnableArchiving_False_WriteDoesNotArchive()
        {
            var logger  = CreateArchivingLogger(null);
            var oldFile = CreateDailyLogFile(null, FirstDayOfCurrentMonth().AddMonths(-3), "old");

            logger.Write("Line");
            Thread.Sleep(NOARCHIVEWAITMILLISECONDS);

            Assert.IsTrue(File.Exists(oldFile));
        }

        /// <summary>
        /// Verifies that, after the first check, a new check is started only when the log file name changes (here, at the
        /// change of prefix), not at every write.
        /// </summary>
        [TestMethod]
        public void EnableArchiving_True_ChecksOnlyWhenLogFileChanges()
        {
            var date                = FirstDayOfCurrentMonth().AddMonths(-3);
            var logger              = CreateArchivingLogger(null);
            logger.EnableArchiving  = true;
            logger.Write("First");
            // Waits for the end of the check started by the first write, which ran on an empty folder
            Thread.Sleep(NOARCHIVEWAITMILLISECONDS);
            var sameLogFile         = CreateDailyLogFile(null, date, "same");
            var appFile             = CreateDailyLogFile("App", date, "app");

            logger.Write("Second");
            Thread.Sleep(NOARCHIVEWAITMILLISECONDS);
            var keptOnSameLogFile   = File.Exists(sameLogFile);
            logger.Prefix           = "App";
            logger.Write("Third");

            Assert.IsTrue(keptOnSameLogFile);
            Assert.IsTrue(WaitUntil(() => !File.Exists(appFile)));
        }

        #endregion

        #region Archive support methods

        /// <summary>
        /// Creates a logger on the temporary directory with the given prefix; the automatic archiving is disabled by
        /// default, so the archiving happens only when requested by the test.
        /// </summary>
        private SimpleLogger CreateArchivingLogger(string prefix)
        {
            var logger      = CreateLogger(LogLevel.Error);
            logger.Prefix   = prefix;

            return logger;
        }

        /// <summary>
        /// Creates in the temporary directory the daily log file of the given prefix and date.
        /// </summary>
        private string CreateDailyLogFile(string prefix, DateTime date, string content)
        {
            var file = Path.Combine(_tempLogPath, $"{prefix}Log{date:yyyyMMdd}.txt");
            File.WriteAllText(file, content);

            return file;
        }

        /// <summary>
        /// Returns the path of the zip file of the given prefix and of the month of the given date.
        /// </summary>
        private string GetZipFile(string prefix, DateTime date)
            => Path.Combine(_tempLogPath, $"{prefix}Log_{date.Year:0000}_{date.Month:00}.zip");

        /// <summary>
        /// Returns the first day of the current month.
        /// </summary>
        private static DateTime FirstDayOfCurrentMonth()
            => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        /// <summary>
        /// Returns the content of the given entry of the zip file, or null if the entry does not exist.
        /// </summary>
        private static string ReadZipEntry(string zipFile, string entryName)
        {
            string result = null;

            using (var archive = ZipFile.OpenRead(zipFile))
            {
                var entry = archive.Entries.FirstOrDefault(e => e.FullName == entryName);
                if (entry != null)
                {
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Waits until the condition is true or the timeout expires.
        /// </summary>
        private static bool WaitUntil(Func<bool> condition)
        {
            var stopwatch   = Stopwatch.StartNew();
            var result      = condition();

            while (!result && stopwatch.Elapsed < TimeSpan.FromSeconds(ARCHIVETESTTIMEOUTSECONDS))
            {
                Thread.Sleep(100);
                result = condition();
            }

            return result;
        }

        #endregion
    }
}
