using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using OSKHelpers.Common;
using OSKHelpers.Json;

namespace OSKHelpers.Tests.Json
{
    /// <summary>
    /// Tests for the <see cref="JsonSettings{T}"/> class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class JsonSettingsTests
    {
        #region Support classes

        /// <summary>
        /// Minimal concrete settings class used by the tests.
        /// </summary>
        public class TestSettings : JsonSettings<TestSettings>
        {
            public string DisplayName { get; set; }

            public override void Init() { }

            public override bool CheckIsValid()
            {
                IsValid = true;
                return IsValid;
            }

            /// <summary>
            /// Exposes the protected static <see cref="JsonSettings{T}.DefaultSerializerOptions"/> to the tests.
            /// </summary>
            public static JsonSerializerOptions SerializerOptions
            {
                get => DefaultSerializerOptions;
                set => DefaultSerializerOptions = value;
            }
        }

        /// <summary>
        /// Second settings class, used to verify that the default options are distinct for each type.
        /// </summary>
        public class OtherSettings : JsonSettings<OtherSettings>
        {
            public override void Init() { }

            public override bool CheckIsValid() => true;

            /// <inheritdoc cref="TestSettings.SerializerOptions"/>
            public static JsonSerializerOptions SerializerOptions => DefaultSerializerOptions;
        }

        /// <summary>
        /// Settings class at version 2, with default data and custom update code, that records the calls of the
        /// methods overridden from <see cref="JsonSettings{T}"/>.
        /// </summary>
        public class VersionedSettings : JsonSettings<VersionedSettings>
        {
            public const string DEFAULTNAME = "Default";

            protected override int LASTVERSION => 2;

            public string DisplayName { get; set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public bool InitCalled { get; private set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public bool CustomCodeCalled { get; private set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public bool CustomCodeResult { get; set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public int LastVersion => LASTVERSION;

            public override void Init()
            {
                InitCalled = true;
            }

            public override bool CheckIsValid()
            {
                IsValid = !string.IsNullOrWhiteSpace(DisplayName);
                return IsValid;
            }

            protected override void CreateDefaultData()
            {
                DisplayName = DEFAULTNAME;
            }

            protected override bool UpdateVersionCustomCode()
            {
                CustomCodeCalled = true;
                return CustomCodeResult;
            }
        }

        #endregion

        #region Members

        private string _tempDirectory;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Creates a temporary directory for the settings files.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Removes the temporary directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors during tests.
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// Verifies that the backup of an existing file keeps the original extension, without a double dot.
        /// </summary>
        [TestMethod]
        public void Save_ExistingFile_CreatesBackupWithOriginalExtension()
        {
            var baseName    = $"Settings_{Guid.NewGuid():N}";
            var fileName    = Path.Combine(_tempDirectory, $"{baseName}.json");
            var settings    = new TestSettings();
            settings.Save(fileName);
            settings.Save(fileName);

            var backups = Directory.GetFiles(Paths.DefaultBackupDirectory, $"{baseName}_*").ToList();
            try
            {
                Assert.HasCount(1, backups);
                Assert.EndsWith(".json", backups[0]);
                Assert.DoesNotContain("..", Path.GetFileName(backups[0]));
            }
            finally
            {
                backups.ForEach(File.Delete);
            }
        }

        #endregion

        #region Load

        /// <summary>
        /// Verifies that Load without options uses DefaultSerializerOptions.
        /// </summary>
        [TestMethod]
        public void Load_NullOptions_UsesDefaultSerializerOptions()
        {
            var fileName        = Path.Combine(_tempDirectory, "Settings.json");
            var originalOptions = TestSettings.SerializerOptions;
            File.WriteAllText(fileName, "{ \"version\": 1, \"display_name\": \"Test\" }");

            try
            {
                TestSettings.SerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

                var (settings, isNew) = TestSettings.Load(fileName, false);

                Assert.IsFalse(isNew);
                Assert.AreEqual("Test", settings.DisplayName);
            }
            finally
            {
                TestSettings.SerializerOptions = originalOptions;
            }
        }

        /// <summary>
        /// Verifies that Load without options falls back to the default options when DefaultSerializerOptions is null.
        /// </summary>
        [TestMethod]
        public void Load_NullDefaultSerializerOptions_UsesGetDefaultSerializerOptions()
        {
            var fileName        = Path.Combine(_tempDirectory, "Settings.json");
            var originalOptions = TestSettings.SerializerOptions;
            File.WriteAllText(fileName, "{ \"version\": 1, \"displayName\": \"Test\" }");

            try
            {
                TestSettings.SerializerOptions = null;

                var (settings, _) = TestSettings.Load(fileName, false);

                Assert.AreEqual("Test", settings.DisplayName);
            }
            finally
            {
                TestSettings.SerializerOptions = originalOptions;
            }
        }

        #endregion

        #region DefaultSerializerOptions

        /// <summary>
        /// Verifies that DefaultSerializerOptions is distinct for each derived type.
        /// </summary>
        [TestMethod]
        public void DefaultSerializerOptions_DifferentDerivedTypes_AreDistinct()
        {
            Assert.IsNotNull(TestSettings.SerializerOptions);
            Assert.IsNotNull(OtherSettings.SerializerOptions);
            Assert.AreNotSame(TestSettings.SerializerOptions, OtherSettings.SerializerOptions);
        }

        #endregion

        #region Constructor / GetTemplate

        /// <summary>
        /// Verifies that a new instance is at the latest version, not obsolete, not new and not updated.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_InitializesAtLastVersion()
        {
            var settings = new VersionedSettings();

            Assert.AreEqual(settings.LastVersion, settings.Version);
            Assert.IsFalse(settings.Obsolete);
            Assert.IsFalse(settings.IsNew);
            Assert.IsFalse(settings.Updated);
        }

        /// <summary>
        /// Verifies that GetTemplate returns an instance populated by CreateDefaultData.
        /// </summary>
        [TestMethod]
        public void GetTemplate_Default_ReturnsDefaultData()
        {
            var template = VersionedSettings.GetTemplate();

            Assert.AreEqual(VersionedSettings.DEFAULTNAME, template.DisplayName);
            Assert.IsFalse(template.IsNew);
        }

        #endregion

        #region UpdateVersion

        /// <summary>
        /// Verifies that an obsolete version is brought to the latest one, executing the custom code.
        /// </summary>
        [TestMethod]
        public void UpdateVersion_ObsoleteVersion_UpdatesVersionAndSetsUpdated()
        {
            var settings = new VersionedSettings { Version = 1 };

            Assert.IsTrue(settings.Obsolete);
            Assert.IsTrue(settings.UpdateVersion());

            Assert.IsTrue(settings.CustomCodeCalled);
            Assert.AreEqual(settings.LastVersion, settings.Version);
            Assert.IsFalse(settings.Obsolete);
            Assert.IsTrue(settings.Updated);
        }

        /// <summary>
        /// Verifies that, at the latest version, the result is the one of the custom code and Updated is not set.
        /// </summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UpdateVersion_LastVersion_ReturnsCustomCodeResult(bool customCodeResult)
        {
            var settings = new VersionedSettings { CustomCodeResult = customCodeResult };

            Assert.AreEqual(customCodeResult, settings.UpdateVersion());
            Assert.IsTrue(settings.CustomCodeCalled);
            Assert.IsFalse(settings.Updated);
        }

        #endregion

        #region Load (additional cases)

        /// <summary>
        /// Verifies that a missing file is created with the default data, IsNew and Updated set, Init and CheckIsValid called.
        /// </summary>
        [TestMethod]
        public void Load_MissingFileCreate_CreatesFileWithDefaultData()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");

            var (settings, isNew) = VersionedSettings.Load(fileName);

            Assert.IsTrue(isNew);
            Assert.IsTrue(settings.IsNew);
            Assert.IsTrue(settings.Updated);
            Assert.IsTrue(settings.InitCalled);
            Assert.IsTrue(settings.IsValid);
            Assert.AreEqual(VersionedSettings.DEFAULTNAME, settings.DisplayName);
            Assert.IsTrue(File.Exists(fileName));
        }

        /// <summary>
        /// Verifies that a missing file without createIfNotExists throws FileNotFoundException.
        /// </summary>
        [TestMethod]
        public void Load_MissingFileNotCreate_ThrowsFileNotFoundException()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");

            Assert.Throws<FileNotFoundException>(() => VersionedSettings.Load(fileName, false));
            Assert.IsFalse(File.Exists(fileName));
        }

        /// <summary>
        /// Verifies that an existing file at the latest version is read without being saved again.
        /// </summary>
        [TestMethod]
        public void Load_ExistingFileLastVersion_ReadsValues()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            File.WriteAllText(fileName, "{ \"Version\": 2, \"DisplayName\": \"Stored\" }");

            var (settings, isNew) = VersionedSettings.Load(fileName);

            Assert.IsFalse(isNew);
            Assert.IsFalse(settings.IsNew);
            Assert.IsFalse(settings.Updated);
            Assert.IsTrue(settings.InitCalled);
            Assert.IsTrue(settings.IsValid);
            Assert.AreEqual("Stored", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that a file at an obsolete version is updated and saved again (with a backup of the original).
        /// </summary>
        [TestMethod]
        public void Load_ExistingFileObsoleteVersion_UpdatesAndSavesFile()
        {
            var fileName = Path.Combine(_tempDirectory, $"Settings_{Guid.NewGuid():N}.json");
            File.WriteAllText(fileName, "{ \"Version\": 1, \"DisplayName\": \"Stored\" }");
            try
            {
                var (settings, _) = VersionedSettings.Load(fileName);

                Assert.IsTrue(settings.Updated);
                Assert.AreEqual(2, settings.Version);
                Assert.AreEqual(2, VersionedSettings.Load(fileName, false).Object.Version);
            }
            finally
            {
                foreach (var backup in Directory.GetFiles(Paths.DefaultBackupDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}_*"))
                {
                    File.Delete(backup);
                }
            }
        }

        /// <summary>
        /// Verifies that a missing directory throws DirectoryNotFoundException and is not created.
        /// </summary>
        [TestMethod]
        public void Load_MissingDirectory_ThrowsDirectoryNotFoundException()
        {
            var directory = Path.Combine(_tempDirectory, "Missing");

            Assert.Throws<DirectoryNotFoundException>(() => VersionedSettings.Load(Path.Combine(directory, "Settings.json")));
            Assert.IsFalse(Directory.Exists(directory));
        }

        /// <summary>
        /// Verifies that an empty file name throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void Load_EmptyFileName_ThrowsArgumentNullException(string fileName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => VersionedSettings.Load(fileName));

            Assert.AreEqual("filename", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a file name with invalid characters throws FormatException.
        /// </summary>
        [TestMethod]
        public void Load_InvalidCharacters_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => VersionedSettings.Load(Path.Combine(_tempDirectory, "Set\u0001tings.json")));
        }

        /// <summary>
        /// Verifies that a relative file name is resolved on Paths.DefaultConfigsDirectory.
        /// </summary>
        [TestMethod]
        public void Load_RelativeFileName_UsesDefaultConfigsDirectory()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                Paths.OverrideAppdataDirectory(_tempDirectory);
                Directory.CreateDirectory(Paths.DefaultConfigsDirectory);

                VersionedSettings.Load("Relative.json");

                Assert.IsTrue(File.Exists(Path.Combine(_tempDirectory, "Configs", "Relative.json")));
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
            }
#endif
        }

        #endregion

        #region Save (additional cases)

        /// <summary>
        /// Verifies that Save writes the serialised object, readable by Load.
        /// </summary>
        [TestMethod]
        public void Save_NewFile_WritesSerializedObject()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var settings    = new VersionedSettings { DisplayName = "Saved" };

            settings.Save(fileName);

            Assert.AreEqual("Saved", VersionedSettings.Load(fileName, false).Object.DisplayName);
        }

        /// <summary>
        /// Verifies the argument checks of Save.
        /// </summary>
        [TestMethod]
        public void Save_InvalidFileName_ThrowsExpectedExceptions()
        {
            var settings = new VersionedSettings();

            Assert.AreEqual("filename", Assert.Throws<ArgumentNullException>(() => settings.Save((string)null)).ParamName);
            Assert.Throws<FormatException>(() => settings.Save(Path.Combine(_tempDirectory, "Set\u0001tings.json")));
            Assert.Throws<DirectoryNotFoundException>(() => settings.Save(Path.Combine(_tempDirectory, "Missing", "Settings.json")));
        }

        #endregion

        #region CheckTemplate

        /// <summary>
        /// Verifies that CheckTemplate creates SettingsTemplate.json in Paths.DefaultConfigsDirectory.
        /// </summary>
        [TestMethod]
        public void CheckTemplate_MissingTemplate_CreatesTemplateFile()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                Paths.OverrideAppdataDirectory(_tempDirectory);
                Directory.CreateDirectory(Paths.DefaultConfigsDirectory);

                new VersionedSettings().CheckTemplate();

                var templateFile = Path.Combine(Paths.DefaultConfigsDirectory, "SettingsTemplate.json");
                Assert.IsTrue(File.Exists(templateFile));
                Assert.AreEqual(VersionedSettings.DEFAULTNAME, VersionedSettings.Load(templateFile, false).Object.DisplayName);
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
            }
#endif
        }

        #endregion
    }
}
