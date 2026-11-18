using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using OSKHelpers.Common;
using OSKHelpers.Json;

namespace OSKHelpers.Tests.Json
{
    /// <summary>
    /// Tests for the automatic update of <see cref="JsonSettings{T}"/> (<see cref="JsonSettings{T}.AutoUpdate"/>,
    /// <see cref="AutoUpdateAttribute"/>, <see cref="CurrentSettingsAttribute"/>, <see cref="JsonSettings{T}.StartAutoUpdate"/>).
    /// </summary>
    public partial class JsonSettingsTests
    {
        #region Support classes

        /// <summary>
        /// Settings class with properties applied by the automatic update and a property applied only at startup.<br/>
        /// Valid if <see cref="DisplayName"/> is set and differs from <see cref="StartupOnly"/>, so that a file valid by itself
        /// can produce an invalid configuration once applied to the instance in use.
        /// </summary>
        public class AutoUpdateSettings : JsonSettings<AutoUpdateSettings>
        {
            public const int CURRENTVERSION = 2;

            protected override int LASTVERSION => CURRENTVERSION;

            [AutoUpdate]
            public string DisplayName { get; set; }

            [AutoUpdate]
            public List<string> Items { get; set; }

            public string StartupOnly { get; set; }

            [AutoUpdate]
            [JsonIgnore]
            public string NotPersisted { get; set; }

            [JsonIgnore]
            public int InitCount { get; private set; }

            /// <summary>
            /// True if, at the first call of <see cref="Init"/>, the instance was already the current settings.
            /// </summary>
            [JsonIgnore]
            public bool WasCurrentAtInit { get; private set; }

            /// <summary>
            /// Custom logic executed by <see cref="AutoUpdateCustomLogic"/>; null to apply the default behaviour.
            /// </summary>
            [JsonIgnore]
            public Func<AutoUpdateSettings, List<string>, bool> CustomLogic { get; set; }

            /// <summary>
            /// Reasons of an invalid file added by <see cref="AutoUpdateInvalidFileProblems"/>; null to apply the default
            /// behaviour.
            /// </summary>
            [JsonIgnore]
            public Action<AutoUpdateSettings, List<string>> InvalidFileProblems { get; set; }

            /// <summary>
            /// Current settings, assigned by Load because of the default name.
            /// </summary>
            public static AutoUpdateSettings Current { get; private set; }

            /// <summary>
            /// True to assign every initialised instance to <see cref="Current"/>, as derived classes may do.
            /// </summary>
            public static bool AssignCurrentInInit { get; set; }

            public AutoUpdateSettings() : base()
            {
                Items = new List<string>();
            }

            public override void Init()
            {
                if (InitCount == 0)
                {
                    WasCurrentAtInit = ReferenceEquals(Current, this);
                }
                InitCount++;
                if (AssignCurrentInInit)
                {
                    Current = this;
                }
            }

            public override bool CheckIsValid()
            {
                IsValid = !string.IsNullOrWhiteSpace(DisplayName) && DisplayName != StartupOnly;
                return IsValid;
            }

            protected override bool AutoUpdateCustomLogic(AutoUpdateSettings loaded, List<string> problems)
                => CustomLogic == null || CustomLogic(loaded, problems);

            protected override void AutoUpdateInvalidFileProblems(AutoUpdateSettings loaded, List<string> problems)
            {
                if (InvalidFileProblems == null)
                {
                    base.AutoUpdateInvalidFileProblems(loaded, problems);
                }
                else
                {
                    InvalidFileProblems(loaded, problems);
                }
            }

            /// <summary>
            /// Resets the static state shared by the tests.
            /// </summary>
            public static void ResetStaticState()
            {
                Current             = null;
                AssignCurrentInInit = false;
            }
        }

        /// <summary>
        /// Settings class whose current settings are held by a property marked with CurrentSettings, which takes precedence
        /// over the property named Current.
        /// </summary>
        public class MarkedCurrentSettings : JsonSettings<MarkedCurrentSettings>
        {
            [CurrentSettings]
            public static MarkedCurrentSettings Instance { get; private set; }

            public static MarkedCurrentSettings Current { get; set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;

            /// <summary>
            /// Resets the static state shared by the tests.
            /// </summary>
            public static void ResetStaticState()
            {
                Instance    = null;
                Current     = null;
            }
        }

        #endregion

        #region AutoUpdate support methods

        /// <summary>
        /// Writes a settings file for <see cref="AutoUpdateSettings"/>, making sure its modification date changes.
        /// </summary>
        private static void WriteAutoUpdateFile(string fileName, string displayName, string startupOnly, string[] items,
            int version = AutoUpdateSettings.CURRENTVERSION)
        {
            var itemsJson = string.Join(", ", items.Select(i => $"\"{i}\""));
            WriteFileWithNewDate(fileName, $"{{ \"Version\": {version}, \"DisplayName\": \"{displayName}\", "
                + $"\"StartupOnly\": \"{startupOnly}\", \"Items\": [ {itemsJson} ] }}");
        }

        /// <summary>
        /// Writes a file and, if its modification date did not change (fast consecutive writes), moves it forward.
        /// </summary>
        private static void WriteFileWithNewDate(string fileName, string content)
        {
            DateTime? previous = File.Exists(fileName) ? File.GetLastWriteTimeUtc(fileName) : (DateTime?)null;
            File.WriteAllText(fileName, content);
            if (previous.HasValue && File.GetLastWriteTimeUtc(fileName) <= previous.Value)
            {
                File.SetLastWriteTimeUtc(fileName, previous.Value.AddSeconds(1));
            }
        }

        /// <summary>
        /// Creates the file and reads the settings that the tests update.
        /// </summary>
        private AutoUpdateSettings LoadAutoUpdateSettings(string fileName)
        {
            WriteAutoUpdateFile(fileName, "First", "Startup", new[] { "A" });
            return AutoUpdateSettings.Load(fileName, false).Object;
        }

        #endregion

        #region GetAutoUpdatePropertyNames / Current settings / Load

        /// <summary>
        /// Verifies that only the persisted properties marked with AutoUpdate are applied by the automatic update.
        /// </summary>
        [TestMethod]
        public void GetAutoUpdatePropertyNames_MarkedProperties_ExcludesUnmarkedAndJsonIgnore()
        {
            var names = AutoUpdateSettings.GetAutoUpdatePropertyNames();

            CollectionAssert.AreEquivalent(new[] { nameof(AutoUpdateSettings.DisplayName), nameof(AutoUpdateSettings.Items) },
                names.ToList());
        }

        /// <summary>
        /// Verifies that a static property named Current is used for the current settings without the attribute.
        /// </summary>
        [TestMethod]
        public void GetCurrentSettingsPropertyName_PropertyNamedCurrent_ReturnsCurrent()
        {
            Assert.AreEqual(CurrentSettingsAttribute.DEFAULTPROPERTYNAME, AutoUpdateSettings.GetCurrentSettingsPropertyName());
        }

        /// <summary>
        /// Verifies that the property marked with CurrentSettings takes precedence over the one named Current.
        /// </summary>
        [TestMethod]
        public void GetCurrentSettingsPropertyName_MarkedProperty_TakesPrecedence()
        {
            Assert.AreEqual(nameof(MarkedCurrentSettings.Instance), MarkedCurrentSettings.GetCurrentSettingsPropertyName());
        }

        /// <summary>
        /// Verifies that no property is used when the derived class does not declare it.
        /// </summary>
        [TestMethod]
        public void GetCurrentSettingsPropertyName_NotDeclared_ReturnsNull()
        {
            Assert.IsNull(TestSettings.GetCurrentSettingsPropertyName());
        }

        /// <summary>
        /// Verifies that Load assigns the instance read to the property Current before Init.
        /// </summary>
        [TestMethod]
        public void Load_PropertyNamedCurrent_AssignsInstanceBeforeInit()
        {
            try
            {
                AutoUpdateSettings.ResetStaticState();

                var settings = LoadAutoUpdateSettings(Path.Combine(_tempDirectory, "Settings.json"));

                Assert.AreSame(settings, AutoUpdateSettings.Current);
                Assert.IsTrue(settings.WasCurrentAtInit);
            }
            finally
            {
                AutoUpdateSettings.ResetStaticState();
            }
        }

        /// <summary>
        /// Verifies that the overload for the default file with doNotAssignCurrent reads and initialises the file without
        /// changing the current settings.
        /// </summary>
        [TestMethod]
        public void Load_DefaultFileDoNotAssignCurrent_KeepsCurrentSettings()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                Paths.OverrideAppdataDirectory(_tempDirectory);
                Directory.CreateDirectory(Paths.DefaultConfigsDirectory);
                var fileName    = Path.Combine(Paths.DefaultConfigsDirectory, AutoUpdateSettings.DEFAULTFILENAME);
                var current     = LoadAutoUpdateSettings(fileName);
                WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

                var other = AutoUpdateSettings.Load(false, null, doNotAssignCurrent: true).Object;

                Assert.AreNotSame(current, other);
                Assert.AreSame(current, AutoUpdateSettings.Current);
                Assert.AreEqual("First", current.DisplayName);
                Assert.AreEqual("Second", other.DisplayName);
                Assert.AreEqual(1, other.InitCount);
                Assert.IsFalse(other.WasCurrentAtInit);
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
            }
#endif
        }

        /// <summary>
        /// Verifies that the overload for the default file with doNotAssignCurrent false behaves as the other overloads of
        /// Load: the current settings already assigned are updated, not replaced.
        /// </summary>
        [TestMethod]
        public void Load_DefaultFileDoNotAssignCurrentFalse_UpdatesCurrent()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                Paths.OverrideAppdataDirectory(_tempDirectory);
                Directory.CreateDirectory(Paths.DefaultConfigsDirectory);
                var fileName    = Path.Combine(Paths.DefaultConfigsDirectory, AutoUpdateSettings.DEFAULTFILENAME);
                var current     = LoadAutoUpdateSettings(fileName);
                WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

                var other = AutoUpdateSettings.Load(false, null, doNotAssignCurrent: false).Object;

                Assert.AreSame(current, other);
                Assert.AreSame(current, AutoUpdateSettings.Current);
                Assert.AreEqual("Second", current.DisplayName);
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
            }
#endif
        }

        /// <summary>
        /// Verifies that Load with the current settings already assigned returns the instance in use, updating only the
        /// properties marked with AutoUpdate.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssigned_UpdatesOnlyAutoUpdateProperties()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var current     = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, "Second", "Changed", new[] { "A", "B" });

            var (settings, isNew) = AutoUpdateSettings.Load(fileName, false);

            Assert.AreSame(current, settings);
            Assert.AreSame(current, AutoUpdateSettings.Current);
            Assert.IsFalse(isNew);
            Assert.AreEqual("Second", current.DisplayName);
            CollectionAssert.AreEqual(new[] { "A", "B" }, current.Items);
            Assert.AreEqual("Startup", current.StartupOnly);
            Assert.IsTrue(current.IsValid);
        }

        /// <summary>
        /// Verifies that Load with the current settings already assigned and an invalid file keeps the current values.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssignedInvalidFile_KeepsCurrentValues()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var current     = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "B" });

            var settings = AutoUpdateSettings.Load(fileName, false).Object;

            Assert.AreSame(current, settings);
            Assert.AreEqual("First", current.DisplayName);
            CollectionAssert.AreEqual(new[] { "A" }, current.Items);
            Assert.IsTrue(current.IsValid);
        }

        /// <summary>
        /// Verifies that Load with the current settings already assigned restores the previous values when the custom logic
        /// refuses the update.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssignedCustomLogicRefuses_KeepsCurrentValues()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var current     = LoadAutoUpdateSettings(fileName);
            current.CustomLogic = (loaded, problems) => false;
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

            var settings = AutoUpdateSettings.Load(fileName, false).Object;

            Assert.AreSame(current, settings);
            Assert.AreEqual("First", current.DisplayName);
        }

        /// <summary>
        /// Verifies that Load of the source file of the current settings records its modification date, so that AutoUpdate
        /// does not read it again.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssignedSameFile_AutoUpdateReturnsNoChange()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var current     = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

            AutoUpdateSettings.Load(fileName, false);

            Assert.AreEqual(File.GetLastWriteTimeUtc(fileName), current.SourceFileWriteTimeUtc);
            Assert.AreEqual(JsonSettingsAutoUpdateStatus.NoChange, current.AutoUpdate().Status);
        }

        /// <summary>
        /// Verifies that Load of another file with the current settings already assigned applies its values without changing
        /// the source file of the instance in use.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssignedOtherFile_KeepsSourceFile()
        {
            var fileName        = Path.Combine(_tempDirectory, "Settings.json");
            var otherFileName   = Path.Combine(_tempDirectory, "Other.json");
            var current         = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(otherFileName, "Other", "Startup", new[] { "A" });

            var settings = AutoUpdateSettings.Load(otherFileName, false).Object;

            Assert.AreSame(current, settings);
            Assert.AreEqual("Other", current.DisplayName);
            Assert.AreEqual(fileName, current.SourceFilePath);
        }

        /// <summary>
        /// Verifies that Load with the current settings already assigned keeps the instance in use even if Init of the derived
        /// class assigns every instance read.
        /// </summary>
        [TestMethod]
        public void Load_CurrentAssignedInitAssignsCurrent_KeepsInstanceInUse()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            AutoUpdateSettings.AssignCurrentInInit = true;
            var current = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

            var settings = AutoUpdateSettings.Load(fileName, false).Object;

            Assert.AreSame(current, settings);
            Assert.AreSame(current, AutoUpdateSettings.Current);
            Assert.AreEqual("Second", current.DisplayName);
        }

        /// <summary>
        /// Verifies that Load assigns also a newly created instance.
        /// </summary>
        [TestMethod]
        public void Load_MissingFileCreate_AssignsNewInstance()
        {
            try
            {
                MarkedCurrentSettings.ResetStaticState();

                var (settings, isNew) = MarkedCurrentSettings.Load(Path.Combine(_tempDirectory, "Settings.json"));

                Assert.IsTrue(isNew);
                Assert.AreSame(settings, MarkedCurrentSettings.Instance);
            }
            finally
            {
                MarkedCurrentSettings.ResetStaticState();
            }
        }

        /// <summary>
        /// Verifies that only the property marked with CurrentSettings is assigned, not the one named Current.
        /// </summary>
        [TestMethod]
        public void Load_MarkedProperty_AssignsOnlyMarkedProperty()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            try
            {
                MarkedCurrentSettings.ResetStaticState();
                File.WriteAllText(fileName, "{ \"Version\": 1 }");

                var settings = MarkedCurrentSettings.Load(fileName, false).Object;

                Assert.AreSame(settings, MarkedCurrentSettings.Instance);
                Assert.IsNull(MarkedCurrentSettings.Current);
            }
            finally
            {
                MarkedCurrentSettings.ResetStaticState();
            }
        }

        /// <summary>
        /// Verifies that CheckTemplate does not replace the current settings with the template.
        /// </summary>
        [TestMethod]
        public void CheckTemplate_CurrentDeclared_DoesNotAssignCurrent()
        {
#if DEBUG
            var originalAppdata = Paths.AppdataDirectory;
            try
            {
                AutoUpdateSettings.ResetStaticState();
                var settings = LoadAutoUpdateSettings(Path.Combine(_tempDirectory, "Settings.json"));
                Paths.OverrideAppdataDirectory(_tempDirectory);
                Directory.CreateDirectory(Paths.DefaultConfigsDirectory);

                settings.CheckTemplate();

                Assert.IsTrue(File.Exists(Path.Combine(Paths.DefaultConfigsDirectory, "SettingsTemplate.json")));
                Assert.AreSame(settings, AutoUpdateSettings.Current);
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
                AutoUpdateSettings.ResetStaticState();
            }
#endif
        }

        /// <summary>
        /// Verifies that Load records the full path and the modification date of the file read.
        /// </summary>
        [TestMethod]
        public void Load_ExistingFile_RecordsSourceFile()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");

            var settings = LoadAutoUpdateSettings(fileName);

            Assert.AreEqual(fileName, settings.SourceFilePath);
            Assert.AreEqual(File.GetLastWriteTimeUtc(fileName), settings.SourceFileWriteTimeUtc);
        }

        #endregion

        #region AutoUpdate

        /// <summary>
        /// Verifies that an instance not read from a file cannot be updated.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_NotLoadedFromFile_ReturnsRejected()
        {
            var settings = new AutoUpdateSettings();

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.HasCount(1, result.Problems);
        }

        /// <summary>
        /// Verifies that an unmodified file is not read again.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_FileUnchanged_ReturnsNoChange()
        {
            var settings    = LoadAutoUpdateSettings(Path.Combine(_tempDirectory, "Settings.json"));
            var initCount   = settings.InitCount;

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.NoChange, result.Status);
            Assert.IsFalse(result.FileChanged);
            Assert.IsEmpty(result.ChangedProperties);
            Assert.IsEmpty(result.Problems);
            Assert.AreEqual(initCount, settings.InitCount);
        }

        /// <summary>
        /// Verifies that only the marked properties of a modified file are applied, on the same instance, which is
        /// initialised again; a second call finds no changes.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_MarkedPropertiesChanged_AppliesOnlyMarkedProperties()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var settings    = LoadAutoUpdateSettings(fileName);
            var initCount   = settings.InitCount;
            WriteAutoUpdateFile(fileName, "Second", "Changed", new[] { "A", "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, result.Status);
            Assert.IsTrue(result.FileChanged);
            Assert.IsFalse(result.FileSaved);
            Assert.IsEmpty(result.Problems);
            CollectionAssert.AreEquivalent(new[] { nameof(AutoUpdateSettings.DisplayName), nameof(AutoUpdateSettings.Items) },
                result.ChangedProperties.ToList());
            Assert.AreEqual("Second", settings.DisplayName);
            CollectionAssert.AreEqual(new[] { "A", "B" }, settings.Items);
            Assert.AreEqual("Startup", settings.StartupOnly);
            Assert.AreEqual(initCount + 1, settings.InitCount);
            Assert.IsTrue(settings.IsValid);
            Assert.AreEqual(JsonSettingsAutoUpdateStatus.NoChange, settings.AutoUpdate().Status);
        }

        /// <summary>
        /// Verifies that marked properties with the same content keep their value (and instance) and are not reported.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_MarkedValuesUnchanged_KeepsInstancesAndReportsNoProperty()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var settings    = LoadAutoUpdateSettings(fileName);
            var items       = settings.Items;
            WriteAutoUpdateFile(fileName, "First", "Changed", new[] { "A" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, result.Status);
            Assert.IsEmpty(result.ChangedProperties);
            Assert.AreSame(items, settings.Items);
            Assert.AreEqual("Startup", settings.StartupOnly);
        }

        /// <summary>
        /// Verifies that an invalid file keeps the current values and is read again at every call, reporting FileChanged only
        /// the first time, until it is corrected.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_InvalidFile_KeepsValuesAndRetriesUntilCorrected()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "B" });

            var first   = settings.AutoUpdate();
            var retry   = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, first.Status);
            Assert.IsTrue(first.FileChanged);
            Assert.IsNotEmpty(first.Problems);
            Assert.IsEmpty(first.ChangedProperties);
            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, retry.Status);
            Assert.IsFalse(retry.FileChanged);
            Assert.AreEqual("First", settings.DisplayName);
            CollectionAssert.AreEqual(new[] { "A" }, settings.Items);
            Assert.IsTrue(settings.IsValid);

            WriteAutoUpdateFile(fileName, "Corrected", "Startup", new[] { "A" });
            var corrected = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, corrected.Status);
            Assert.AreEqual("Corrected", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that the default problem of an invalid file names the file.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_InvalidFile_ReportsDefaultProblem()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual($"The file {fileName} is not valid.", result.Problems[0]);
        }

        /// <summary>
        /// Verifies that the derived class can report the reasons of an invalid file in place of the default problem, reading
        /// them from the instance read.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_InvalidFileProblemsOverridden_ReportsReasonsOfInstanceRead()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            settings.InvalidFileProblems = (loaded, problems) => problems.Add($"Invalid display name: '{loaded.DisplayName}'.");
            WriteAutoUpdateFile(fileName, "Startup", "Startup", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.AreEqual("Invalid display name: 'Startup'.", result.Problems[0]);
            Assert.DoesNotContain($"The file {fileName} is not valid.", result.Problems);
            Assert.AreEqual("First", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that an exception of the override rejects the update without being thrown.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_InvalidFileProblemsThrows_ReturnsRejected()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            settings.InvalidFileProblems = (loaded, problems) => throw new InvalidOperationException("Test");
            WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.AreEqual("First", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that a file that is not valid JSON keeps the current values.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_MalformedJson_ReturnsRejected()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            WriteFileWithNewDate(fileName, "{ \"DisplayName\": ");

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.IsNotEmpty(result.Problems);
            Assert.AreEqual("First", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that a deleted file keeps the current values.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_DeletedFile_ReturnsRejected()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            File.Delete(fileName);

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.IsTrue(result.FileChanged);
            Assert.AreEqual("First", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that the custom logic can refuse the update, keeping the current values.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_CustomLogicRefuses_KeepsValues()
        {
            var fileName            = Path.Combine(_tempDirectory, "Settings.json");
            var settings            = LoadAutoUpdateSettings(fileName);
            settings.CustomLogic    = (loaded, problems) =>
            {
                problems.Add("Refused");
                return false;
            };
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.Contains("Refused", result.Problems);
            Assert.AreEqual("First", settings.DisplayName);
            CollectionAssert.AreEqual(new[] { "A" }, settings.Items);
        }

        /// <summary>
        /// Verifies that the custom logic receives the instance read, can modify it before the marked properties are applied
        /// and can report messages also when the update is applied.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_CustomLogicModifiesLoaded_AppliesModifiedValues()
        {
            var fileName            = Path.Combine(_tempDirectory, "Settings.json");
            var settings            = LoadAutoUpdateSettings(fileName);
            settings.CustomLogic    = (loaded, problems) =>
            {
                // Keeps the previous items, as for an invalid item of a collection.
                loaded.Items = settings.Items;
                problems.Add("Items kept");
                return true;
            };
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, result.Status);
            CollectionAssert.AreEqual(new[] { "Items kept" }, result.Problems.ToList());
            CollectionAssert.AreEqual(new[] { nameof(AutoUpdateSettings.DisplayName) }, result.ChangedProperties.ToList());
            Assert.AreEqual("Second", settings.DisplayName);
            CollectionAssert.AreEqual(new[] { "A" }, settings.Items);
        }

        /// <summary>
        /// Verifies that, if the configuration obtained by applying a valid file is not valid, the previous values are
        /// restored.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_ResultingSettingsInvalid_RestoresPreviousValues()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            // Valid by itself, but DisplayName equals StartupOnly of the instance in use, which is not applied.
            WriteAutoUpdateFile(fileName, "Startup", "Other", new[] { "B" });

            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, result.Status);
            Assert.IsEmpty(result.ChangedProperties);
            Assert.AreEqual("First", settings.DisplayName);
            CollectionAssert.AreEqual(new[] { "A" }, settings.Items);
            Assert.IsTrue(settings.IsValid);
        }

        /// <summary>
        /// Verifies that Current keeps the instance in use even if Init of the derived class assigns every instance read,
        /// both when the update is applied and when it is rejected.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_InitAssignsCurrent_KeepsInstanceInUse()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            try
            {
                AutoUpdateSettings.ResetStaticState();
                AutoUpdateSettings.AssignCurrentInInit = true;
                var settings = LoadAutoUpdateSettings(fileName);
                Assert.AreSame(settings, AutoUpdateSettings.Current);

                WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });
                var applied = settings.AutoUpdate();

                Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, applied.Status);
                Assert.AreSame(settings, AutoUpdateSettings.Current);
                Assert.AreEqual("Second", AutoUpdateSettings.Current.DisplayName);

                WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "A" });
                var rejected = settings.AutoUpdate();

                Assert.AreEqual(JsonSettingsAutoUpdateStatus.Rejected, rejected.Status);
                Assert.AreSame(settings, AutoUpdateSettings.Current);
            }
            finally
            {
                AutoUpdateSettings.ResetStaticState();
            }
        }

        /// <summary>
        /// Verifies that a file at an obsolete version is updated and saved again, and that the new modification date is
        /// recorded, so that the save is not detected as a modification.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_ObsoleteVersion_ReportsFileSavedAndRecordsNewDate()
        {
            var fileName = Path.Combine(_tempDirectory, $"Settings_{Guid.NewGuid():N}.json");
            try
            {
                var settings = LoadAutoUpdateSettings(fileName);
                WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" }, AutoUpdateSettings.CURRENTVERSION - 1);

                var result = settings.AutoUpdate();

                Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, result.Status);
                Assert.IsTrue(result.FileSaved);
                Assert.AreEqual("Second", settings.DisplayName);
                Assert.AreEqual(File.GetLastWriteTimeUtc(fileName), settings.SourceFileWriteTimeUtc);
                Assert.AreEqual(JsonSettingsAutoUpdateStatus.NoChange, settings.AutoUpdate().Status);
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
        /// Verifies that saving the instance on its own file is not detected as a modification.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_AfterSaveOnSourceFile_ReturnsNoChange()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            File.SetLastWriteTimeUtc(fileName, DateTime.UtcNow.AddMinutes(-1));
            settings = AutoUpdateSettings.Load(fileName, false).Object;

            settings.DisplayName = "Saved";
            settings.Save(fileName, ignoreBackup: true);
            var result = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.NoChange, result.Status);
            Assert.AreEqual(File.GetLastWriteTimeUtc(fileName), settings.SourceFileWriteTimeUtc);
        }

        /// <summary>
        /// Verifies that a file locked by another process (e.g. an editor that is saving it) is reported as busy without
        /// problems, and read at the next call. File locks are mandatory only on Windows.
        /// </summary>
        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        public void AutoUpdate_FileLocked_ReturnsFileBusyAndRetries()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

            JsonSettingsAutoUpdateResult busy;
            using (new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                busy = settings.AutoUpdate();
            }
            var retry = settings.AutoUpdate();

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.FileBusy, busy.Status);
            Assert.IsEmpty(busy.Problems);
            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, retry.Status);
            Assert.IsTrue(retry.FileChanged);
            Assert.AreEqual("Second", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that the update does not assign the instance read to Current, which keeps the instance in use.
        /// </summary>
        [TestMethod]
        public void AutoUpdate_Applied_CurrentKeepsInstanceInUse()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            try
            {
                AutoUpdateSettings.ResetStaticState();
                var settings = LoadAutoUpdateSettings(fileName);
                WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

                var result = settings.AutoUpdate();

                Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, result.Status);
                Assert.AreSame(settings, AutoUpdateSettings.Current);
                Assert.AreEqual("Second", AutoUpdateSettings.Current.DisplayName);
            }
            finally
            {
                AutoUpdateSettings.ResetStaticState();
            }
        }

        #endregion

        #region StartAutoUpdate / StopAutoUpdate

        /// <summary>
        /// Maximum wait for a check of the timer in the tests, in milliseconds.
        /// </summary>
        private const int TIMERWAITMILLISECONDS = 10000;

        /// <summary>
        /// Verifies that the timer does not start for an instance not read from a file.
        /// </summary>
        [TestMethod]
        public void StartAutoUpdate_NotLoadedFromFile_ReturnsFalse()
        {
            var settings = new AutoUpdateSettings();

            Assert.IsFalse(settings.StartAutoUpdate());
            Assert.IsFalse(settings.IsAutoUpdateRunning);
        }

        /// <summary>
        /// Verifies that the timer does not start with an interval of less than one second.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void StartAutoUpdate_InvalidInterval_ReturnsFalse(int intervalSeconds)
        {
            var settings = LoadAutoUpdateSettings(Path.Combine(_tempDirectory, "Settings.json"));

            Assert.IsFalse(settings.StartAutoUpdate(intervalSeconds));
            Assert.IsFalse(settings.IsAutoUpdateRunning);
        }

        /// <summary>
        /// Verifies that the timer starts with the default interval and stops with StopAutoUpdate.
        /// </summary>
        [TestMethod]
        public void StartAutoUpdate_DefaultInterval_StartsAndStops()
        {
            var settings = LoadAutoUpdateSettings(Path.Combine(_tempDirectory, "Settings.json"));
            try
            {
                Assert.IsTrue(settings.StartAutoUpdate());
                Assert.IsTrue(settings.IsAutoUpdateRunning);
                Assert.AreEqual(AutoUpdateSettings.DEFAULTAUTOUPDATEINTERVALSECONDS, settings.AutoUpdateIntervalSeconds);
            }
            finally
            {
                settings.StopAutoUpdate();
            }

            Assert.IsFalse(settings.IsAutoUpdateRunning);
        }

        /// <summary>
        /// Verifies that the timer applies the modification of the file and raises AutoUpdated with the outcome.
        /// </summary>
        [TestMethod]
        public void StartAutoUpdate_FileModified_AppliesAndRaisesAutoUpdated()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            JsonSettingsAutoUpdateResult received = null;
            using (var raised = new System.Threading.ManualResetEventSlim(false))
            {
                settings.AutoUpdated += (sender, result) =>
                {
                    received = result;
                    raised.Set();
                };
                try
                {
                    settings.StartAutoUpdate();
                    WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });

                    Assert.IsTrue(raised.Wait(TIMERWAITMILLISECONDS));
                }
                finally
                {
                    settings.StopAutoUpdate();
                }
            }

            Assert.AreEqual(JsonSettingsAutoUpdateStatus.Applied, received.Status);
            CollectionAssert.AreEqual(new[] { nameof(AutoUpdateSettings.DisplayName) }, received.ChangedProperties.ToList());
            Assert.AreEqual("Second", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that the timer keeps working after an exception of an event handler.
        /// </summary>
        [TestMethod]
        public void StartAutoUpdate_HandlerThrows_KeepsChecking()
        {
            var fileName    = Path.Combine(_tempDirectory, "Settings.json");
            var settings    = LoadAutoUpdateSettings(fileName);
            var calls       = 0;
            using (var secondCall = new System.Threading.ManualResetEventSlim(false))
            {
                settings.AutoUpdated += (sender, result) =>
                {
                    if (System.Threading.Interlocked.Increment(ref calls) >= 2)
                    {
                        secondCall.Set();
                    }
                    throw new InvalidOperationException("Handler error");
                };
                try
                {
                    settings.StartAutoUpdate();
                    WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });
                    // Rejected outcome: the file is read (and the event raised) at every check until it is corrected.
                    WriteAutoUpdateFile(fileName, string.Empty, "Startup", new[] { "A" });

                    Assert.IsTrue(secondCall.Wait(TIMERWAITMILLISECONDS));
                }
                finally
                {
                    settings.StopAutoUpdate();
                }
            }

            Assert.IsTrue(calls >= 2);
        }

        /// <summary>
        /// Verifies that after StopAutoUpdate the modifications of the file are no longer applied.
        /// </summary>
        [TestMethod]
        public void StopAutoUpdate_Running_ModificationsNotApplied()
        {
            var fileName = Path.Combine(_tempDirectory, "Settings.json");
            var settings = LoadAutoUpdateSettings(fileName);
            settings.StartAutoUpdate();

            settings.StopAutoUpdate();
            WriteAutoUpdateFile(fileName, "Second", "Startup", new[] { "A" });
            System.Threading.Thread.Sleep(2500);

            Assert.IsFalse(settings.IsAutoUpdateRunning);
            Assert.AreEqual("First", settings.DisplayName);
        }

        /// <summary>
        /// Verifies that StopAutoUpdate without an active timer has no effect.
        /// </summary>
        [TestMethod]
        public void StopAutoUpdate_NotRunning_DoesNothing()
        {
            var settings = new AutoUpdateSettings();

            settings.StopAutoUpdate();

            Assert.IsFalse(settings.IsAutoUpdateRunning);
        }

        #endregion
    }
}
