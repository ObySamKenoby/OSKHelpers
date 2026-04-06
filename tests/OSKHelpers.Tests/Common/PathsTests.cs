using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Common
{
    [TestClass]
    public class PathsTests
    {
        #region Costanti

        private const string DockerEnvPath = "/.dockerenv";

        #endregion

        #region MEmbri

        private static string _appDataPath = string.Empty;

        #endregion

        #region Metodi

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

        #region Test per Dockerization

        [TestMethod]
        [DataRow("Backups")]
        [DataRow("Configs")]
        [DataRow("Database")]
        [DataRow("Logs")]
        [DataRow("Output")]
        [DataRow("Temp")]
        public void GetDomainDirectoryPath_ShouldUseAssemblyPath_WhenNotDocker(string dir)
        {
#if DEBUG
            Paths.OverrideIsDockerized(false);
            string expected = Path.Combine(_appDataPath, dir);
            string actual   = Paths.GetDataDirectoryPath(dir);
            Assert.AreEqual(expected, actual);
#endif
        }

        [TestMethod]
        public void DefaultDirectories_ShouldBeInitialized()
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
        public void DefaultDirectories_ShouldBeUnderAppdata(string propertyName, string expectedSubdir)
        {
            // Verifica che la directory venga creata se non esistente
            var appDir = Paths.GetDataDirectoryPath(expectedSubdir);
            Assert.IsFalse(Directory.Exists(appDir));

            Paths.CheckDirectory(appDir);
            Assert.IsTrue(Directory.Exists(appDir));

            // Usa reflection per leggere la proprietà statica
            var prop = typeof(Paths).GetProperty(propertyName);
            Assert.IsNotNull(prop, $"Property {propertyName} not found");

            var value = prop.GetValue(null) as string;
            Assert.IsNotNull(value, $"{propertyName} should not be null");

            // Deve iniziare con la sandbox
            Assert.StartsWith(value, _appDataPath);

            // Deve contenere la sottodirectory attesa
            Assert.Contains(value, expectedSubdir);

            // Deve esistere sul filesystem
            Assert.IsTrue(Directory.Exists(value), $"{propertyName} directory should exist");
        }

        [TestMethod]
        public void DefaultSettingsFile_ShouldBeUnderAppdata()
        {
#if DEBUG
            Paths.OverrideIsDockerized(true);

            string settingsFile = Paths.DefaultSettingsFile;
            Assert.StartsWith(settingsFile, _appDataPath);
            Assert.Contains(settingsFile, "Settings.ini");

            // Il file Settings.ini non deve esistere di default.
            var file = Path.GetDirectoryName(settingsFile);
            
            Assert.IsFalse(File.Exists(file));

            Paths.OverrideIsDockerized(false);
#endif
        }

#endregion

#endregion
    }

}
