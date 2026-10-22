using System;
using System.IO;
using System.Threading;
using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Common
{
    [TestClass]
    public class PathsTests
    {
        #region Constants

        private const string DOCKERENVPATH = "/.dockerenv";

        #endregion

        #region Members

        private static string _appDataPath = string.Empty;

        #endregion

        #region Methods

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            _appDataPath = dir;
#if DEBUG
            Paths.OverrideIsDockerized(false);
            Paths.OverrideAppdataDirectory(_appDataPath);
#endif
            try
            {
                if (Directory.Exists(_appDataPath))
                {
                    Directory.Delete(_appDataPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(SimpleLog.FormattedException(ex, true));
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                if (Directory.Exists(_appDataPath))
                {
                    Directory.Delete(_appDataPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(SimpleLog.FormattedException(ex, true));
            }
        }

        [TestInitialize]
        public void Setup()
        {
#if DEBUG
            Paths.OverrideIsDockerized(false);
            Paths.OverrideAppdataDirectory(_appDataPath);
#endif
        }

        [TestMethod]
        [DataRow(null,      "")]
        [DataRow("",        "")]
        [DataRow(" ",       "")]
        [DataRow("c.sv",    "")]
        [DataRow("C.SV",    "")]
        [DataRow("c sv",    "")]
        [DataRow("csv#",    "")]
        [DataRow("Csv#",    "")]
        [DataRow("..csv",   "")]
        [DataRow(".c.sv",   "")]
        [DataRow("csv.",    "")]
        [DataRow("csv",     ".csv")]
        [DataRow("csv ",    ".csv")]
        [DataRow("Csv",     ".csv")]
        [DataRow("CSV",     ".csv")]
        [DataRow("c12",     ".c12")]
        [DataRow("C12",     ".c12")]
        [DataRow(".csv",    ".csv")]
        [DataRow(".CsV",    ".csv")]
        [DataRow(".c12",    ".c12")]
        public void GetNewTempFilename_VariousExtensions_ReturnsExpectedExtension(string extension, string expectedValue)
        {
            Assert.AreEqual(expectedValue, Path.GetExtension(Paths.GetNewTempFilename(false, extension)));
        }

        #region Dockerization tests

        [TestMethod]
        [DataRow("Backups")]
        [DataRow("Configs")]
        [DataRow("Database")]
        [DataRow("Logs")]
        [DataRow("Output")]
        [DataRow("Temp")]
        public void GetDomainDirectoryPath_NotDockerized_UsesAssemblyPath(string dir)
        {
#if DEBUG
            Paths.OverrideIsDockerized(false);
            string expected = Path.Combine(_appDataPath, dir);
            string actual   = Paths.GetDataDirectoryPath(dir);
            Assert.AreEqual(expected, actual);
#endif
        }

        [TestMethod]
        public void DefaultDirectories_AfterInitialization_AreNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.DefaultBackupDirectory));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.DefaultConfigsDirectory));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.DefaultDatabaseDirectory));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.DefaultLogsDirectory));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.DefaultOutputDirectory));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Paths.TempDirectory));
        }

        [TestMethod]
        [DataRow(nameof(Paths.DefaultBackupDirectory),      "Backups")]
        [DataRow(nameof(Paths.DefaultConfigsDirectory),     "Configs")]
        [DataRow(nameof(Paths.DefaultDatabaseDirectory),    "Database")]
        [DataRow(nameof(Paths.DefaultLogsDirectory),        "Logs")]
        [DataRow(nameof(Paths.DefaultOutputDirectory),      "Output")]
        [DataRow(nameof(Paths.TempDirectory),               "Temp")]
        public void DefaultDirectories_AfterInitialization_AreUnderAppdata(string propertyName, string expectedSubdir)
        {
            // Verify the directory is created if not existing
            var appDir = Paths.GetDataDirectoryPath(expectedSubdir);
            Assert.IsFalse(Directory.Exists(appDir));

            Paths.CheckDirectory(appDir);
            Assert.IsTrue(Directory.Exists(appDir));

            // Use reflection to read the static property
            var prop = typeof(Paths).GetProperty(propertyName);
            Assert.IsNotNull(prop, $"Property {propertyName} not found");

            var value = prop.GetValue(null) as string;
            Assert.IsNotNull(value, $"{propertyName} should not be null");

            // Must start with the sandbox
            Assert.StartsWith(_appDataPath, value);

            // Must contain the expected subdirectory
            Assert.Contains(expectedSubdir, value);

            // Must exist on the filesystem
            Assert.IsTrue(Directory.Exists(value), $"{propertyName} directory should exist");
        }

        [TestMethod]
        [DoNotParallelize]
        public void CheckDefaultLogsDirectory_MissingDirectory_CreatesLogsDirectory()
        {
#if DEBUG
            // Dedicated sandbox, so the shared one keeps the directories that other tests expect to be missing
            var appDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Paths.OverrideAppdataDirectory(appDataPath);

                var result = Paths.CheckDefaultLogsDirectory();

                Assert.IsTrue(result);
                Assert.IsTrue(Directory.Exists(Paths.DefaultLogsDirectory));
                Assert.IsFalse(Directory.Exists(Paths.DefaultDatabaseDirectory));
            }
            finally
            {
                Paths.OverrideAppdataDirectory(_appDataPath);
                if (Directory.Exists(appDataPath))
                {
                    Directory.Delete(appDataPath, true);
                }
            }
#endif
        }

        [TestMethod]
        public void DefaultSettingsFile_Dockerized_IsUnderAppdata()
        {
#if DEBUG
            Paths.OverrideIsDockerized(true);

            string settingsFile = Paths.DefaultSettingsFile;
            Assert.StartsWith(_appDataPath, settingsFile);
            Assert.Contains("Settings.ini", settingsFile);

            // The Settings.ini file should not exist by default.
            var file = Path.GetDirectoryName(settingsFile);
            
            Assert.IsFalse(File.Exists(file));

            Paths.OverrideIsDockerized(false);
#endif
        }

        #region CheckDirectory / GetDataDirectoryPath

        /// <summary>
        /// Verifies that CheckDirectory creates a missing directory and returns true.
        /// </summary>
        [TestMethod]
        public void CheckDirectory_MissingDirectory_CreatesDirectoryAndReturnsTrue()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Assert.IsTrue(Paths.CheckDirectory(directory));
                Assert.IsTrue(Directory.Exists(directory));
                Assert.IsTrue(Paths.CheckDirectory(directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Verifies that CheckDirectory returns false, without exceptions, when the directory cannot be created.
        /// </summary>
        [TestMethod]
        public void CheckDirectory_PathUnderFile_ReturnsFalse()
        {
            var file = Path.GetTempFileName();
            try
            {
                Assert.IsFalse(Paths.CheckDirectory(Path.Combine(file, "Sub")));
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// Verifies that GetDataDirectoryPath combines the name with AppdataDirectory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void GetDataDirectoryPath_Name_CombinesWithAppdataDirectory()
        {
            Assert.AreEqual(Path.Combine(Paths.AppdataDirectory, "Custom"), Paths.GetDataDirectoryPath("Custom"));
        }

        #endregion

        #region AppdataDirectory / UseLocalAppdataDirectory

        /// <summary>
        /// Verifies that changing AppdataDirectory updates all the default directories.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void AppdataDirectory_SetValue_UpdatesDefaultDirectories()
        {
            var appdata = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Paths.AppdataDirectory = appdata;

                Assert.AreEqual(appdata, Paths.AppdataDirectory);
                Assert.AreEqual(Path.Combine(appdata, "Logs"), Paths.DefaultLogsDirectory);
                Assert.AreEqual(Path.Combine(appdata, "Configs"), Paths.DefaultConfigsDirectory);
                Assert.AreEqual(Path.Combine(appdata, "Temp"), Paths.TempDirectory);
                Assert.AreEqual(Path.Combine(appdata, "Settings.ini"), Paths.DefaultSettingsFile);
                Assert.IsFalse(Directory.Exists(appdata));
            }
            finally
            {
#if DEBUG
                Paths.OverrideAppdataDirectory(_appDataPath);
#endif
            }
        }

        /// <summary>
        /// Verifies that UseLocalAppdataDirectory moves the base to AssemblyPath\Appdata and creates the default directories.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void UseLocalAppdataDirectory_NotDockerized_CreatesDirectoriesUnderAssemblyPath()
        {
            var localAppdata    = Path.Combine(Paths.AssemblyPath, "Appdata");
            var existed         = Directory.Exists(localAppdata);
            try
            {
                Paths.UseLocalAppdataDirectory();

                Assert.AreEqual(localAppdata, Paths.AppdataDirectory);
                Assert.IsTrue(Directory.Exists(Paths.DefaultLogsDirectory));
                Assert.IsTrue(Directory.Exists(Paths.DefaultBackupDirectory));
                Assert.IsTrue(Directory.Exists(Paths.TempDirectory));
            }
            finally
            {
#if DEBUG
                Paths.OverrideAppdataDirectory(_appDataPath);
#endif
                if (!existed && Directory.Exists(localAppdata))
                {
                    Directory.Delete(localAppdata, true);
                }
            }
        }

        #endregion

        #region Temporary files

        /// <summary>
        /// Verifies that CheckTempDirectory creates the temporary directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void CheckTempDirectory_MissingDirectory_CreatesDirectoryAndReturnsTrue()
        {
            Assert.IsTrue(Paths.CheckTempDirectory());
            Assert.IsTrue(Directory.Exists(Paths.TempDirectory));
        }

        /// <summary>
        /// Verifies that CleanTempFiles deletes only the files older than StaleFilesInterval minutes.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void CleanTempFiles_StaleAndRecentFiles_DeletesOnlyStaleFiles()
        {
            var originalInterval = Paths.StaleFilesInterval;
            try
            {
                Paths.StaleFilesInterval    = 60;
                Paths.CheckTempDirectory();
                var staleFile               = Paths.GetNewTempFilename(true);
                var recentFile              = Paths.GetNewTempFilename(true);
                File.SetLastWriteTime(staleFile, DateTime.Now.AddMinutes(-61));

                Assert.IsTrue(Paths.CleanTempFiles());

                Assert.IsFalse(File.Exists(staleFile));
                Assert.IsTrue(File.Exists(recentFile));
            }
            finally
            {
                Paths.StaleFilesInterval = originalInterval;
            }
        }

        /// <summary>
        /// Verifies that a StaleFilesInterval of zero or less disables the cleanup.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [DataRow(0)]
        [DataRow(-1)]
        public void CleanTempFiles_IntervalNotPositive_DeletesNothing(int interval)
        {
            var originalInterval = Paths.StaleFilesInterval;
            try
            {
                Paths.StaleFilesInterval    = interval;
                Paths.CheckTempDirectory();
                var staleFile               = Paths.GetNewTempFilename(true);
                File.SetLastWriteTime(staleFile, DateTime.Now.AddDays(-1));

                Assert.IsTrue(Paths.CleanTempFiles());

                Assert.IsTrue(File.Exists(staleFile));
            }
            finally
            {
                Paths.StaleFilesInterval = originalInterval;
            }
        }

        /// <summary>
        /// Verifies that, with createFile, an empty placeholder file is created in the temporary directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void GetNewTempFilename_CreateFile_CreatesEmptyFile()
        {
            Paths.CheckTempDirectory();

            var filename = Paths.GetNewTempFilename(true, ".CSV");

            Assert.AreEqual(Paths.TempDirectory, Path.GetDirectoryName(filename));
            Assert.EndsWith(".csv", filename);
            Assert.IsTrue(File.Exists(filename));
            Assert.AreEqual(0, new FileInfo(filename).Length);
        }

        /// <summary>
        /// Verifies that two calls return different names and, without createFile, no file is created.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void GetNewTempFilename_TwoCalls_ReturnsDifferentNamesWithoutFiles()
        {
            var first   = Paths.GetNewTempFilename();
            var second  = Paths.GetNewTempFilename();

            Assert.AreNotEqual(first, second);
            Assert.IsFalse(File.Exists(first));
            Assert.IsFalse(File.Exists(second));
        }

        /// <summary>
        /// Verifies that the cleanup timer can be started and stopped.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void StartAndStopTempDirectoryCleanTimer_TimerInitialized_ReturnTrue()
        {
            Assert.IsTrue(Paths.StartTempDirectoryCleanTimer());
            Assert.IsTrue(Paths.StopTempDirectoryCleanTimer());
        }

#if DEBUG

        /// <summary>
        /// Verifies that the started cleanup timer removes the stale temporary files (interval shortened for the test).
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-039")]
        public void StartTempDirectoryCleanTimer_StaleFile_DeletesFileOnElapsed()
        {
            const int SHORTINTERVAL = 50;
            const int TIMEOUT       = 5000;

            var originalInterval = Paths.StaleFilesInterval;
            try
            {
                Paths.StaleFilesInterval    = 60;
                Paths.CheckTempDirectory();
                var staleFile               = Paths.GetNewTempFilename(true);
                File.SetLastWriteTime(staleFile, DateTime.Now.AddMinutes(-61));
                Paths.OverrideTempFilesCleanInterval(SHORTINTERVAL);

                Assert.IsTrue(Paths.StartTempDirectoryCleanTimer());

                var deadline = DateTime.Now.AddMilliseconds(TIMEOUT);
                while (File.Exists(staleFile) && DateTime.Now < deadline)
                {
                    Thread.Sleep(SHORTINTERVAL);
                }
                Assert.IsFalse(File.Exists(staleFile));
            }
            finally
            {
                Paths.StopTempDirectoryCleanTimer();
                Paths.OverrideTempFilesCleanInterval(null);
                Paths.StaleFilesInterval = originalInterval;
            }
        }

#endif

        #endregion

#endregion

#endregion
    }

}
