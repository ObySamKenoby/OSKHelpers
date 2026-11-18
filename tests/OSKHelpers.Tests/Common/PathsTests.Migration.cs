#if DEBUG

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests of <see cref="Paths.MigrateToAppdata"/>, which use the Debug-only parameter sourceDirectory to work in a temporary
    /// application directory: they change the shared state of <see cref="Paths"/>, so they never run in parallel.
    /// </summary>
    public partial class PathsTests
    {
        #region Constants

        private const string APPDATANAME        = "Appdata";
        private const string SETTINGSINI        = "Settings.ini";
        private const string SOURCECONTENT      = "source";
        private const string DESTINATIONCONTENT = "destination";

        #endregion

        #region Properties

        /// <summary>
        /// Names of the default directories moved by the migration.
        /// </summary>
        private static string[] DefaultDirectories => new[] { "Backups", "Configs", "Database", "Logs", "Output", "Scripts", "Temp" };

        #endregion

        #region Methods

        #region Migration helper methods

        /// <summary>
        /// Creates an empty temporary application directory.
        /// </summary>
        private static string CreateSourceDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// Writes a file, creating its directory, under the given root.
        /// </summary>
        private static string WriteMigrationFile(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return path;
        }

        /// <summary>
        /// Restores the shared sandbox of the class and removes the temporary application directory.
        /// </summary>
        private static void RestoreAfterMigration(string sourceDirectory)
        {
            Paths.OverrideIsDockerized(false);
            Paths.OverrideAppdataDirectory(_appDataPath);
            if (Directory.Exists(sourceDirectory))
            {
                Directory.Delete(sourceDirectory, true);
            }
        }

        #endregion

        #region MigrateToAppdata

        /// <summary>
        /// Verifies that the whole old structure is moved, unchanged, and the data directory becomes the base.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_FullOldStructure_MovesEverythingAndActivatesAppdata()
        {
            var source = CreateSourceDirectory();
            try
            {
                foreach (var directory in DefaultDirectories)
                {
                    WriteMigrationFile(source, Path.Combine(directory, "File.txt"), directory);
                }
                WriteMigrationFile(source, Path.Combine("Logs", "Archive", "Old.zip"), "archive");
                WriteMigrationFile(source, SETTINGSINI, "KEY = value");
                var appdata = Path.Combine(source, APPDATANAME);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.Migrated, result.Status);
                Assert.AreEqual(source, result.SourceDirectory);
                Assert.AreEqual(appdata, result.DestinationDirectory);
                Assert.HasCount(DefaultDirectories.Length + 1, result.MovedItems);
                Assert.IsEmpty(result.SkippedItems);
                Assert.IsEmpty(result.Problems);
                foreach (var directory in DefaultDirectories)
                {
                    Assert.IsFalse(Directory.Exists(Path.Combine(source, directory)), $"{directory} left in the source");
                    Assert.AreEqual(directory, File.ReadAllText(Path.Combine(appdata, directory, "File.txt")));
                }
                Assert.IsTrue(File.Exists(Path.Combine(appdata, "Logs", "Archive", "Old.zip")));
                Assert.IsFalse(File.Exists(Path.Combine(source, SETTINGSINI)));
                Assert.AreEqual("KEY = value", File.ReadAllText(Path.Combine(appdata, SETTINGSINI)));
                Assert.AreEqual(appdata, Paths.AppdataDirectory);
                Assert.AreEqual(Path.Combine(appdata, "Logs"), Paths.DefaultLogsDirectory);
                Assert.AreEqual(Path.Combine(appdata, SETTINGSINI), Paths.DefaultSettingsFile);
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that a second call finds nothing to move and changes nothing.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_SecondCall_ReturnsNothingToMigrate()
        {
            var source = CreateSourceDirectory();
            try
            {
                WriteMigrationFile(source, Path.Combine("Configs", "Settings.json"), "{}");
                Paths.MigrateToAppdata(sourceDirectory: source);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.NothingToMigrate, result.Status);
                Assert.IsEmpty(result.MovedItems);
                Assert.AreEqual("{}", File.ReadAllText(Path.Combine(source, APPDATANAME, "Configs", "Settings.json")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that a new installation gets the data directory with all the default directories.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_NewInstallation_ReturnsNothingToMigrateAndCreatesDirectories()
        {
            var source = CreateSourceDirectory();
            try
            {
                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.NothingToMigrate, result.Status);
                foreach (var directory in DefaultDirectories)
                {
                    Assert.IsTrue(Directory.Exists(Path.Combine(source, APPDATANAME, directory)), $"{directory} not created");
                }
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that a file already present in the data directory is kept and the source file is left and reported.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_FileAlreadyInDestination_SkipsFileAndKeepsDestination()
        {
            var source = CreateSourceDirectory();
            try
            {
                var conflict    = Path.Combine("Configs", "Settings.json");
                var sourceFile  = WriteMigrationFile(source, conflict, SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Configs", "Other.json"), SOURCECONTENT);
                var destination = WriteMigrationFile(Path.Combine(source, APPDATANAME), conflict, DESTINATIONCONTENT);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.MigratedWithWarnings, result.Status);
                CollectionAssert.AreEqual(new[] { conflict }, result.SkippedItems.ToArray());
                CollectionAssert.Contains(result.MovedItems.ToArray(), Path.Combine("Configs", "Other.json"));
                Assert.AreEqual(DESTINATIONCONTENT, File.ReadAllText(destination));
                Assert.AreEqual(SOURCECONTENT, File.ReadAllText(sourceFile));
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Configs", "Other.json")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that, with overwrite, the source file replaces the destination one and the empty source directory is removed.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_FileAlreadyInDestinationWithOverwrite_ReplacesFile()
        {
            var source = CreateSourceDirectory();
            try
            {
                var conflict    = Path.Combine("Configs", "Settings.json");
                WriteMigrationFile(source, conflict, SOURCECONTENT);
                var destination = WriteMigrationFile(Path.Combine(source, APPDATANAME), conflict, DESTINATIONCONTENT);

                var result = Paths.MigrateToAppdata(overwrite: true, sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.Migrated, result.Status);
                CollectionAssert.AreEqual(new[] { conflict }, result.MovedItems.ToArray());
                Assert.AreEqual(SOURCECONTENT, File.ReadAllText(destination));
                Assert.IsFalse(Directory.Exists(Path.Combine(source, "Configs")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that a source directory whose destination is a file is left in place and reported.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_DirectoryWithFileInDestination_SkipsDirectory()
        {
            var source = CreateSourceDirectory();
            try
            {
                WriteMigrationFile(source, Path.Combine("Output", "Report.pdf"), SOURCECONTENT);
                WriteMigrationFile(Path.Combine(source, APPDATANAME), "Output", DESTINATIONCONTENT);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.MigratedWithWarnings, result.Status);
                CollectionAssert.Contains(result.SkippedItems.ToArray(), "Output");
                Assert.IsTrue(File.Exists(Path.Combine(source, "Output", "Report.pdf")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that the additional items of the application, files and directories, also nested, are moved.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_AdditionalItems_MovesFilesAndDirectories()
        {
            var source = CreateSourceDirectory();
            try
            {
                WriteMigrationFile(source, Path.Combine("Templates", "Mail.txt"), SOURCECONTENT);
                WriteMigrationFile(source, "Data.db", SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Data", "Archive.db"), SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Data", "Keep.db"), SOURCECONTENT);

                var result = Paths.MigrateToAppdata(new[] { "Templates/", " Data.db ", "Data/Archive.db", "templates" },
                    sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.Migrated, result.Status);
                Assert.HasCount(3, result.MovedItems);
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Templates", "Mail.txt")));
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Data.db")));
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Data", "Archive.db")));
                Assert.IsTrue(File.Exists(Path.Combine(source, "Data", "Keep.db")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that the additional items with a name that is not valid are ignored and reported.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("..")]
        [DataRow("../Outside.txt")]
        [DataRow("Data/../Outside.txt")]
        [DataRow("./Data.db")]
        [DataRow("/Absolute.txt")]
        [DataRow("Appdata")]
        [DataRow("appdata/Logs")]
        [DataRow("Data//Archive.db")]
        public void MigrateToAppdata_InvalidAdditionalItem_IgnoresItemAndReportsProblem(string item)
        {
            var source = CreateSourceDirectory();
            try
            {
                var outside = WriteMigrationFile(Path.GetDirectoryName(source), $"{Guid.NewGuid()}.txt", SOURCECONTENT);
                try
                {
                    var result = Paths.MigrateToAppdata(new[] { item }, sourceDirectory: source);

                    Assert.AreEqual(PathsMigrationStatus.NothingToMigrate, result.Status);
                    Assert.HasCount(1, result.Problems);
                    Assert.IsEmpty(result.MovedItems);
                    Assert.IsTrue(File.Exists(outside));
                }
                finally
                {
                    File.Delete(outside);
                }
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that the files distributed with the application and the files not listed are not moved.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_DistributedFiles_AreNotMoved()
        {
            var source = CreateSourceDirectory();
            try
            {
                WriteMigrationFile(source, "SettingsTemplate.ini", SOURCECONTENT);
                WriteMigrationFile(source, "Application.dll", SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("MailTemplates", "0Index.txt"), SOURCECONTENT);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.NothingToMigrate, result.Status);
                Assert.IsTrue(File.Exists(Path.Combine(source, "SettingsTemplate.ini")));
                Assert.IsTrue(File.Exists(Path.Combine(source, "Application.dll")));
                Assert.IsTrue(File.Exists(Path.Combine(source, "MailTemplates", "0Index.txt")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that a file in use is left in the application directory and reported, while the other files are moved
        /// (Windows only: elsewhere file locks do not prevent moving a file).
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_FileInUse_LeavesFileAndReportsProblem()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Inconclusive("File locks prevent moving a file only on Windows.");
            }

            var source = CreateSourceDirectory();
            try
            {
                var lockedFile = WriteMigrationFile(source, Path.Combine("Logs", "Locked.txt"), SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Logs", "Free.txt"), SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Configs", "Settings.json"), SOURCECONTENT);

                PathsMigrationResult result;
                using (new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    result = Paths.MigrateToAppdata(sourceDirectory: source);
                }

                Assert.AreEqual(PathsMigrationStatus.MigratedWithWarnings, result.Status);
                Assert.HasCount(1, result.Problems);
                Assert.Contains("Locked.txt", result.Problems[0]);
                Assert.IsTrue(File.Exists(lockedFile));
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Logs", "Free.txt")));
                Assert.IsTrue(File.Exists(Path.Combine(source, APPDATANAME, "Configs", "Settings.json")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that, when the data directory cannot be created, nothing is moved and the base does not change.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_DestinationCannotBeCreated_ReturnsFailedAndKeepsBase()
        {
            var source = CreateSourceDirectory();
            try
            {
                WriteMigrationFile(source, APPDATANAME, SOURCECONTENT);
                WriteMigrationFile(source, Path.Combine("Logs", "Log.txt"), SOURCECONTENT);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.Failed, result.Status);
                Assert.HasCount(1, result.Problems);
                Assert.IsEmpty(result.MovedItems);
                Assert.IsTrue(File.Exists(Path.Combine(source, "Logs", "Log.txt")));
                Assert.AreEqual(_appDataPath, Paths.AppdataDirectory);
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        /// <summary>
        /// Verifies that in a container nothing is moved and the default directories are created under the current base.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void MigrateToAppdata_Dockerized_ReturnsNotApplicable()
        {
            var source  = CreateSourceDirectory();
            var appdata = Path.Combine(source, "ContainerAppdata");
            try
            {
                WriteMigrationFile(source, Path.Combine("Logs", "Log.txt"), SOURCECONTENT);
                Paths.OverrideAppdataDirectory(appdata);
                Paths.OverrideIsDockerized(true);

                var result = Paths.MigrateToAppdata(sourceDirectory: source);

                Assert.AreEqual(PathsMigrationStatus.NotApplicable, result.Status);
                Assert.AreEqual(appdata, result.DestinationDirectory);
                Assert.IsEmpty(result.MovedItems);
                Assert.IsTrue(File.Exists(Path.Combine(source, "Logs", "Log.txt")));
                Assert.IsFalse(Directory.Exists(Path.Combine(source, APPDATANAME)));
                Assert.IsTrue(Directory.Exists(Path.Combine(appdata, "Logs")));
            }
            finally
            {
                RestoreAfterMigration(source);
            }
        }

        #endregion

        #endregion
    }
}

#endif
