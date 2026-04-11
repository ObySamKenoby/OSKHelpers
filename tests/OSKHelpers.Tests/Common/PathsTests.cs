using System;
using System.IO;
using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Common
{
    [TestClass]
    public class PathsTests
    {
        #region Constants

        private const string DockerEnvPath = "/.dockerenv";

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
        public void GetNewTempFilenameTests(string extension, string expectedValue)
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
        public void GetDomainDirectoryPathShouldUseAssemblyPathWhenNotDocker(string dir)
        {
#if DEBUG
            Paths.OverrideIsDockerized(false);
            string expected = Path.Combine(_appDataPath, dir);
            string actual   = Paths.GetDataDirectoryPath(dir);
            Assert.AreEqual(expected, actual);
#endif
        }

        [TestMethod]
        public void DefaultDirectoriesShouldBeInitialized()
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
        public void DefaultDirectoriesShouldBeUnderAppdata(string propertyName, string expectedSubdir)
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
        public void DefaultSettingsFileShouldBeUnderAppdata()
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

#endregion

#endregion
    }

}
