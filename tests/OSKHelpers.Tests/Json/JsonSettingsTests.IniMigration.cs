using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OSKHelpers.Common;
using OSKHelpers.INIFile;
using OSKHelpers.Json;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Json
{
    /// <summary>
    /// Tests for the migration of an INI file to <see cref="JsonSettings{T}"/> (<see cref="JsonSettings{T}.MigrateFromIniFile"/>).
    /// </summary>
    public partial class JsonSettingsTests
    {
        #region Support classes

        /// <summary>
        /// Enum used by the migration tests.
        /// </summary>
        public enum MigrationColor
        {
            Red     = 1,
            Green   = 2
        }

        /// <summary>
        /// Flags enum used by the migration tests.
        /// </summary>
        [Flags]
        public enum MigrationFlags
        {
            None    = 0,
            First   = 1,
            Second  = 2
        }

        /// <summary>
        /// Object not supported by the default conversion.
        /// </summary>
        public class MigrationNested
        {
            public string Value { get; set; }
        }

        /// <summary>
        /// Settings class with a property for every type supported by the default conversion, one not supported and one marked
        /// with JsonIgnore.
        /// </summary>
        public class MigrationSettings : JsonSettings<MigrationSettings>
        {
            public const string DEFAULTTEXT = "Default";
            public const int    DEFAULTCOUNT = 7;

            public string Text { get; set; }
            public bool Enabled { get; set; }
            public int Count { get; set; }
            public long Big { get; set; }
            public byte Small { get; set; }
            public decimal Amount { get; set; }
            public double Ratio { get; set; }
            public float Factor { get; set; }
            public MigrationColor Color { get; set; }
            public MigrationFlags Flags { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan Interval { get; set; }
            public Guid Id { get; set; }
            public int? OptionalCount { get; set; }
            public LogLevel LogLevel { get; set; }
            public List<string> Names { get; set; }
            public int[] Numbers { get; set; }
            public IEnumerable<MigrationColor> Colors { get; set; }
            public List<DayOfWeek> Days { get; set; }
            public IReadOnlyList<DayOfWeek> WorkDays { get; set; }
            public MigrationNested Nested { get; set; }

            [JsonIgnore]
            public string Ignored { get; set; }

            /// <summary>
            /// Current settings: must not be assigned by the migration.
            /// </summary>
            public static MigrationSettings Current { get; private set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;

            protected override void CreateDefaultData()
            {
                Text    = DEFAULTTEXT;
                Count   = DEFAULTCOUNT;
                Names   = new List<string> { DEFAULTTEXT };
            }

            /// <summary>
            /// Resets the static state shared by the tests.
            /// </summary>
            public static void ResetStaticState()
            {
                Current = null;
            }
        }

        /// <summary>
        /// Settings class with two properties, used for the outcome and the custom logic of the migration.
        /// </summary>
        public class SimpleMigrationSettings : JsonSettings<SimpleMigrationSettings>
        {
            public const string DEFAULTTEXT = "Default";

            public string Text { get; set; }
            public int Count { get; set; }

            /// <summary>
            /// Custom logic executed by <see cref="MigrateFromIniFileCustomLogic"/>; null to apply the default behaviour.
            /// </summary>
            public static Func<SimpleMigrationSettings, IniFileHelper, ICollection<string>, List<string>, bool> CustomLogic { get; set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;

            protected override void CreateDefaultData()
            {
                Text = DEFAULTTEXT;
            }

            protected override bool MigrateFromIniFileCustomLogic(IniFileHelper iniFile, ICollection<string> notMigratedProperties,
                List<string> problems)
                => CustomLogic == null || CustomLogic(this, iniFile, notMigratedProperties, problems);
        }

        /// <summary>
        /// Settings class with two properties whose names differ only in case.
        /// </summary>
        public class AmbiguousMigrationSettings : JsonSettings<AmbiguousMigrationSettings>
        {
            public string Value { get; set; }
            public string VALUE { get; set; }
            public string Other { get; set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;
        }

        #endregion

        #region Migration support methods

        /// <summary>
        /// Lines of an INI file with a value for every property of <see cref="MigrationSettings"/>, preceded by a pragma.
        /// </summary>
        private static string[] GetFullMigrationIniLines()
        {
            return new[]
            {
                "!!DEBUG!!",
                "!!PROTOCOL!!",
                "# Settings to migrate",
                "Text = Hello world",
                "ENABLED = true",
                "count = 42",
                "Big = 9000000000",
                "Small = 200",
                "Amount = 12.50",
                "Ratio = 0.25",
                "Factor = 1.5",
                "Color = green",
                "Flags = First, Second",
                "Date = 2026-12-31",
                "Interval = 01:30:00",
                "Id = 0f8fad5b-d9cb-469f-a165-70867728950e",
                "OptionalCount = 3",
                "LogLevel = 4",
                "*Names = first",
                "*Names = second",
                "*Numbers = 1",
                "*Numbers = 2",
                "*Colors = Red",
                "*Colors = 2",
                "*Days = 1",
                "*Days = 5",
                "WorkDays = 1 2 3",
                "Nested = something",
                "Ignored = something"
            };
        }

        /// <summary>
        /// Writes an INI file in the temporary directory and returns its path.
        /// </summary>
        private string WriteMigrationIniFile(params string[] lines)
        {
            var iniFilePath = Path.Combine(_tempDirectory, "Settings.ini");
            File.WriteAllLines(iniFilePath, lines);
            return iniFilePath;
        }

        /// <summary>
        /// Returns the path of the JSON file of the migration in the temporary directory.
        /// </summary>
        private string GetMigrationJsonFilePath() => Path.Combine(_tempDirectory, "Settings.json");

        #endregion

        #region MigrateFromIniFile

        /// <summary>
        /// Verifies that every supported type is converted and that the JSON file can be read with Load.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_AllSupportedTypes_ConvertsValues()
        {
            MigrationSettings.ResetStaticState();
            var iniFilePath     = WriteMigrationIniFile(GetFullMigrationIniLines());
            var jsonFilePath    = GetMigrationJsonFilePath();

            var result = MigrationSettings.MigrateFromIniFile(iniFilePath, jsonFilePath);

            Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
            CollectionAssert.AreEqual(new[] { nameof(MigrationSettings.Nested) }, result.NotMigratedProperties.ToList());
            Assert.IsEmpty(result.Problems);
            Assert.AreEqual(iniFilePath, result.IniFilePath);
            Assert.AreEqual(jsonFilePath, result.JsonFilePath);

            var settings = MigrationSettings.Load(jsonFilePath, false).Object;
            Assert.AreEqual("Hello world", settings.Text);
            Assert.IsTrue(settings.Enabled);
            Assert.AreEqual(42, settings.Count);
            Assert.AreEqual(9000000000L, settings.Big);
            Assert.AreEqual((byte)200, settings.Small);
            Assert.AreEqual(12.50m, settings.Amount);
            Assert.AreEqual(0.25d, settings.Ratio);
            Assert.AreEqual(1.5f, settings.Factor);
            Assert.AreEqual(MigrationColor.Green, settings.Color);
            Assert.AreEqual(MigrationFlags.First | MigrationFlags.Second, settings.Flags);
            Assert.AreEqual(new DateTime(2026, 12, 31), settings.Date);
            Assert.AreEqual(new TimeSpan(1, 30, 0), settings.Interval);
            Assert.AreEqual(new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), settings.Id);
            Assert.AreEqual(3, settings.OptionalCount);
            Assert.AreEqual(LogLevel.Debug, settings.LogLevel);
            CollectionAssert.AreEqual(new[] { "first", "second" }, settings.Names);
            CollectionAssert.AreEqual(new[] { 1, 2 }, settings.Numbers);
            CollectionAssert.AreEqual(new[] { MigrationColor.Red, MigrationColor.Green }, settings.Colors.ToList());
            CollectionAssert.AreEqual(new[] { DayOfWeek.Monday, DayOfWeek.Friday }, settings.Days);
            CollectionAssert.AreEqual(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday }, settings.WorkDays.ToList());
            Assert.IsNull(settings.Nested);
            Assert.IsNull(settings.Ignored);
            Assert.AreEqual(1, settings.Version);
        }

        /// <summary>
        /// Verifies that the instance returned is not initialised as the current settings and that the INI file is not modified.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_ValidFile_DoesNotAssignCurrentNorModifyIniFile()
        {
            MigrationSettings.ResetStaticState();
            var iniFilePath = WriteMigrationIniFile(GetFullMigrationIniLines());
            var iniContent  = File.ReadAllText(iniFilePath);

            var result = MigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

            Assert.IsNotNull(result.Settings);
            Assert.IsTrue(result.Settings.IsNew);
            Assert.IsNull(MigrationSettings.Current);
            Assert.AreEqual(iniContent, File.ReadAllText(iniFilePath));
        }

        /// <summary>
        /// Verifies that the initial pragmas of the INI file are not applied to SimpleLog.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_InitialPragmas_AreNotApplied()
        {
            var forceDebug      = SimpleLog.ForceDebug;
            var forceProtocol   = SimpleLog.ForceProtocol;
            var iniFilePath     = WriteMigrationIniFile(GetFullMigrationIniLines());

            try
            {
                var result = MigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

                Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
                Assert.AreEqual(forceDebug, SimpleLog.ForceDebug);
                Assert.AreEqual(forceProtocol, SimpleLog.ForceProtocol);
            }
            finally
            {
                SimpleLog.ForceDebug    = forceDebug;
                SimpleLog.ForceProtocol = forceProtocol;
            }
        }

        /// <summary>
        /// Verifies that, with every property migrated and keys written with a different case, the outcome is Migrated.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_AllPropertiesMigrated_ReturnsMigrated()
        {
            var iniFilePath = WriteMigrationIniFile("TEXT = Value", "cOuNt = 3");

            var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

            Assert.AreEqual(JsonSettingsMigrationStatus.Migrated, result.Status);
            CollectionAssert.AreEquivalent(new[] { "Text", "Count" }, result.MigratedProperties.ToList());
            Assert.IsEmpty(result.NotMigratedProperties);
            Assert.AreEqual("Value", result.Settings.Text);
            Assert.AreEqual(3, result.Settings.Count);
        }

        /// <summary>
        /// Verifies that a missing key keeps the default data and is reported among the properties not migrated.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_MissingKey_KeepsDefaultData()
        {
            var iniFilePath = WriteMigrationIniFile("Count = 3");

            var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

            Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
            CollectionAssert.AreEqual(new[] { "Text" }, result.NotMigratedProperties.ToList());
            Assert.IsEmpty(result.Problems);
            Assert.AreEqual(SimpleMigrationSettings.DEFAULTTEXT, result.Settings.Text);
        }

        /// <summary>
        /// Verifies that a value that cannot be converted keeps the default value and is reported in Problems.
        /// </summary>
        [TestMethod]
        [DataRow("Count = abc", "Count")]
        [DataRow("Enabled = yes", "Enabled")]
        [DataRow("Color = 99", "Color")]
        [DataRow("Color = Blue", "Color")]
        [DataRow("Amount = 1,5", "Amount")]
        [DataRow("*Numbers = x", "Numbers")]
        [DataRow("WorkDays = 9", "WorkDays")]
        [DataRow("Names = single", "Names")]
        [DataRow("*Text = element", "Text")]
        public void MigrateFromIniFile_InvalidValue_KeepsDefaultAndReportsProblem(string line, string propertyName)
        {
            var iniFilePath = WriteMigrationIniFile(line);

            var result = MigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

            Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
            Assert.Contains(propertyName, result.NotMigratedProperties);
            Assert.HasCount(1, result.Problems);
            Assert.Contains(propertyName, result.Problems[0]);
            Assert.AreEqual(MigrationSettings.DEFAULTCOUNT, result.Settings.Count);
            Assert.AreEqual(MigrationSettings.DEFAULTTEXT, result.Settings.Text);
        }

        /// <summary>
        /// Verifies that properties whose names differ only in case are not migrated and are reported.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_AmbiguousProperties_AreNotMigrated()
        {
            var iniFilePath = WriteMigrationIniFile("Value = x", "Other = y");
            // The default options are case insensitive and would not allow serialising the ambiguous properties.
            var options     = new JsonSerializerOptions();

            var result = AmbiguousMigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath(), options: options);

            Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
            CollectionAssert.AreEquivalent(new[] { "Value", "VALUE" }, result.NotMigratedProperties.ToList());
            Assert.HasCount(1, result.Problems);
            Assert.IsNull(result.Settings.Value);
            Assert.IsNull(result.Settings.VALUE);
            Assert.AreEqual("y", result.Settings.Other);
        }

        /// <summary>
        /// Verifies that an existing JSON file is not overwritten without overwrite.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_DestinationExists_DoesNothing()
        {
            var iniFilePath     = WriteMigrationIniFile("Text = Value");
            var jsonFilePath    = GetMigrationJsonFilePath();
            File.WriteAllText(jsonFilePath, "{ \"Version\": 1, \"Text\": \"Existing\" }");

            var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, jsonFilePath);

            Assert.AreEqual(JsonSettingsMigrationStatus.DestinationExists, result.Status);
            Assert.IsNull(result.Settings);
            Assert.Contains("Existing", File.ReadAllText(jsonFilePath));
        }

        /// <summary>
        /// Verifies that with overwrite the existing JSON file is replaced after saving a backup copy.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_DestinationExistsOverwrite_ReplacesFileWithBackup()
        {
            var iniFilePath     = WriteMigrationIniFile("Text = Value", "Count = 1");
            var baseName        = $"Settings_{Guid.NewGuid():N}";
            var jsonFilePath    = Path.Combine(_tempDirectory, $"{baseName}.json");
            File.WriteAllText(jsonFilePath, "{ \"Version\": 1, \"Text\": \"Existing\" }");

            var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, jsonFilePath, true);

            var backups = Directory.GetFiles(Paths.DefaultBackupDirectory, $"{baseName}_*").ToList();
            try
            {
                Assert.AreEqual(JsonSettingsMigrationStatus.Migrated, result.Status);
                Assert.Contains("Value", File.ReadAllText(jsonFilePath));
                Assert.HasCount(1, backups);
                Assert.Contains("Existing", File.ReadAllText(backups[0]));
            }
            finally
            {
                backups.ForEach(File.Delete);
            }
        }

        /// <summary>
        /// Verifies that a missing INI file gives SourceNotFound without writing the JSON file.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_MissingIniFile_ReturnsSourceNotFound()
        {
            var jsonFilePath = GetMigrationJsonFilePath();

            var result = SimpleMigrationSettings.MigrateFromIniFile(Path.Combine(_tempDirectory, "Missing.ini"), jsonFilePath);

            Assert.AreEqual(JsonSettingsMigrationStatus.SourceNotFound, result.Status);
            Assert.IsFalse(File.Exists(jsonFilePath));
        }

        /// <summary>
        /// Verifies that an invalid INI file gives Failed without writing the JSON file.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_InvalidIniFile_ReturnsFailed()
        {
            var iniFilePath     = WriteMigrationIniFile("Text = Value", "Count =");
            var jsonFilePath    = GetMigrationJsonFilePath();

            var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, jsonFilePath);

            Assert.AreEqual(JsonSettingsMigrationStatus.Failed, result.Status);
            Assert.IsNull(result.Settings);
            Assert.IsNotEmpty(result.Problems);
            Assert.IsFalse(File.Exists(jsonFilePath));
        }

        /// <summary>
        /// Verifies that invalid destination paths give Failed without exceptions.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_InvalidDestination_ReturnsFailedWithoutExceptions()
        {
            var iniFilePath = WriteMigrationIniFile("Text = Value");

            var missingDirectory    = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath,
                Path.Combine(_tempDirectory, "Missing", "Settings.json"));
            var invalidCharacters   = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath,
                Path.Combine(_tempDirectory, "Set\0tings.json"));

            Assert.AreEqual(JsonSettingsMigrationStatus.Failed, missingDirectory.Status);
            Assert.AreEqual(JsonSettingsMigrationStatus.Failed, invalidCharacters.Status);
            Assert.IsNotEmpty(missingDirectory.Problems);
            Assert.IsFalse(Directory.Exists(Path.Combine(_tempDirectory, "Missing")));
        }

        /// <summary>
        /// Verifies that the custom logic receives the properties not migrated and that the ones it handles are reported as
        /// migrated.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_CustomLogicHandlesProperty_ReturnsMigrated()
        {
            var iniFilePath = WriteMigrationIniFile("Text = Value", "Counter = 5");
            List<string> received = null;
            SimpleMigrationSettings.CustomLogic = (settings, iniFile, notMigrated, problems) =>
            {
                received        = notMigrated.ToList();
                settings.Count  = iniFile.GetInt("Counter");
                notMigrated.Remove("Count");
                return true;
            };

            try
            {
                var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

                CollectionAssert.AreEqual(new[] { "Count" }, received);
                Assert.AreEqual(JsonSettingsMigrationStatus.Migrated, result.Status);
                Assert.Contains("Count", result.MigratedProperties);
                Assert.AreEqual(5, SimpleMigrationSettings.Load(GetMigrationJsonFilePath(), false).Object.Count);
            }
            finally
            {
                SimpleMigrationSettings.CustomLogic = null;
            }
        }

        /// <summary>
        /// Verifies that a problem added by the custom logic gives MigratedWithWarnings.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_CustomLogicAddsProblem_ReturnsMigratedWithWarnings()
        {
            var iniFilePath = WriteMigrationIniFile("Text = Value", "Count = 1");
            SimpleMigrationSettings.CustomLogic = (settings, iniFile, notMigrated, problems) =>
            {
                problems.Add("Custom problem");
                return true;
            };

            try
            {
                var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, GetMigrationJsonFilePath());

                Assert.AreEqual(JsonSettingsMigrationStatus.MigratedWithWarnings, result.Status);
                CollectionAssert.AreEqual(new[] { "Custom problem" }, result.Problems.ToList());
            }
            finally
            {
                SimpleMigrationSettings.CustomLogic = null;
            }
        }

        /// <summary>
        /// Verifies that the custom logic can refuse the migration: Failed and no JSON file written.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_CustomLogicRefuses_ReturnsFailedWithoutFile()
        {
            var iniFilePath     = WriteMigrationIniFile("Text = Value", "Count = 1");
            var jsonFilePath    = GetMigrationJsonFilePath();
            SimpleMigrationSettings.CustomLogic = (settings, iniFile, notMigrated, problems) => false;

            try
            {
                var result = SimpleMigrationSettings.MigrateFromIniFile(iniFilePath, jsonFilePath);

                Assert.AreEqual(JsonSettingsMigrationStatus.Failed, result.Status);
                Assert.IsNull(result.Settings);
                Assert.IsFalse(File.Exists(jsonFilePath));
            }
            finally
            {
                SimpleMigrationSettings.CustomLogic = null;
            }
        }

        /// <summary>
        /// Verifies that, without paths, the default INI file and the default JSON file are used.
        /// </summary>
        [TestMethod]
        public void MigrateFromIniFile_DefaultPaths_UsesDefaultSettingsFileAndConfigsDirectory()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                Paths.OverrideAppdataDirectory(_tempDirectory);
                // INIFILEFULLPATH (assembly directory) has no Settings.ini during the tests: the fallback is used.
                File.WriteAllLines(Paths.DefaultSettingsFile, new[] { "Text = Value", "Count = 1" });

                var result = SimpleMigrationSettings.MigrateFromIniFile();

                Assert.AreEqual(JsonSettingsMigrationStatus.Migrated, result.Status);
                Assert.AreEqual(Paths.DefaultSettingsFile, result.IniFilePath);
                Assert.IsTrue(File.Exists(Path.Combine(_tempDirectory, "Configs", SimpleMigrationSettings.DEFAULTFILENAME)));
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
