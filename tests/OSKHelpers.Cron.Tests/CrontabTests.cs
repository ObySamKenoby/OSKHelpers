using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OSKHelpers.Common;

namespace OSKHelpers.Cron.Tests
{
    /// <summary>
    /// Tests for the reading, the automatic reload and the occurrences of <see cref="Crontab"/>.
    /// </summary>
    [TestClass]
    public partial class CrontabTests
    {
        #region Support classes

        /// <summary>
        /// Crontab with the validation of the actions and the custom check supplied by the test.
        /// </summary>
        public class TestCrontab : Crontab
        {
            public Func<CrontabEntry, string> Validator { get; set; }

            public Func<IReadOnlyList<CrontabEntry>, List<string>, bool> CustomCheck { get; set; }

            public Action<IReadOnlyList<CrontabEntry>, List<string>> CoverageLines { get; set; }

            public TestCrontab(string filePath) : base(filePath, TimeZoneInfo.Utc)
            {
            }

            protected override string ValidateAction(CrontabEntry entry)
                => Validator == null ? base.ValidateAction(entry) : Validator(entry);

            protected override bool CheckCrontabCustomLogic(IReadOnlyList<CrontabEntry> entries, List<string> lines)
                => CustomCheck == null ? base.CheckCrontabCustomLogic(entries, lines) : CustomCheck(entries, lines);

            protected override void AddCoverageLines(IReadOnlyList<CrontabEntry> entries, DateTime nowUtc, TimeZoneInfo zone,
                List<string> lines)
            {
                if (CoverageLines == null)
                {
                    base.AddCoverageLines(entries, nowUtc, zone, lines);
                }
                else
                {
                    CoverageLines(entries, lines);
                }
            }
        }

        #endregion

        #region Members

        private string _directory;

        #endregion

        #region Initialization

        [TestInitialize]
        public void Initialize()
        {
            _directory = Path.Combine(Path.GetTempPath(), "OSKHelpers.Cron.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>
        /// Writes a crontab file in the directory of the test and returns its path.
        /// </summary>
        private string WriteFile(params string[] lines)
        {
            var path = Path.Combine(_directory, Crontab.DEFAULTFILENAME);
            File.WriteAllLines(path, lines);
            return path;
        }

        private static DateTime Utc(int hour, int minute, int second = 0)
            => new DateTime(2026, 1, 5, hour, minute, second, DateTimeKind.Utc);

        #endregion

        #region Parse

        [TestMethod]
        public void Parse_StandardLine_ReturnsEntryWithActionContainingSpaces()
        {
            var result = Crontab.Parse(new[] { "*/15   8-18 * * 1-5   Import orders  " });

            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(1, result.Entries.Count);
            Assert.AreEqual("*/15 8-18 * * 1-5", result.Entries[0].ExpressionText);
            Assert.AreEqual("Import orders", result.Entries[0].Action);
            Assert.AreEqual(1, result.Entries[0].LineNumber);
        }

        [TestMethod]
        public void Parse_Macro_ReturnsEntry()
        {
            var result = Crontab.Parse(new[] { "@daily Cleanup" });

            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual("@daily", result.Entries[0].ExpressionText);
            Assert.AreEqual("Cleanup", result.Entries[0].Action);
        }

        [TestMethod]
        public void Parse_DocumentedMacros_AreValid()
        {
            var macros = new[] { "@yearly", "@annually", "@monthly", "@weekly", "@daily", "@midnight", "@hourly", "@every_minute" };

            var result = Crontab.Parse(macros.Select(m => $"{m} Action"));

            Assert.AreEqual(0, result.Errors.Count, string.Join(" ", result.Errors));
            Assert.AreEqual(macros.Length, result.Entries.Count);
        }

        [TestMethod]
        public void Parse_CommentsAndEmptyLines_AreIgnoredKeepingLineNumbers()
        {
            var result = Crontab.Parse(new[] { "# comment", "", "   ", "0 1 * * * Backup", null });

            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(1, result.Entries.Count);
            Assert.AreEqual(4, result.Entries[0].LineNumber);
        }

        [TestMethod]
        public void Parse_WrongFieldsCount_ReturnsError()
        {
            var result = Crontab.Parse(new[] { "* * * Backup" });

            Assert.AreEqual(0, result.Entries.Count);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.StartsWith(result.Errors[0], "Line 1:");
        }

        [TestMethod]
        public void Parse_MacroWithoutAction_ReturnsError()
        {
            var result = Crontab.Parse(new[] { "@daily" });

            Assert.AreEqual(0, result.Entries.Count);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Parse_InvalidExpression_ReturnsError()
        {
            var result = Crontab.Parse(new[] { "61 * * * * Backup", "@never Backup" });

            Assert.AreEqual(0, result.Entries.Count);
            Assert.AreEqual(2, result.Errors.Count);
            StringAssert.Contains(result.Errors[0], "61 * * * *");
        }

        [TestMethod]
        public void Parse_Null_ReturnsEmpty()
        {
            var result = Crontab.Parse(null);

            Assert.AreEqual(0, result.Entries.Count);
            Assert.AreEqual(0, result.Errors.Count);
        }

        #endregion

        #region Read and Load

        [TestMethod]
        public void Read_MissingFile_ReturnsError()
        {
            var result = Crontab.Read(Path.Combine(_directory, "missing"));

            Assert.AreEqual(0, result.Entries.Count);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Load_ValidFile_ReplacesEntries()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup", "@hourly Import"));

            var result = crontab.Load();

            Assert.IsTrue(result);
            Assert.IsTrue(crontab.FileExists);
            Assert.AreEqual(2, crontab.Entries.Count);
            Assert.AreEqual(0, crontab.Errors.Count);
        }

        [TestMethod]
        public void Load_ValidateActionRejectsLine_ExcludesItAndAddsError()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup", "@hourly Unknown"))
            {
                Validator = e => e.Action == "Unknown" ? $"Line {e.LineNumber}: unknown action." : string.Empty
            };

            var result = crontab.Load();

            Assert.IsFalse(result);
            Assert.AreEqual(1, crontab.Entries.Count);
            Assert.AreEqual("Backup", crontab.Entries[0].Action);
            Assert.AreEqual("Line 2: unknown action.", crontab.Errors.Single());
        }

        [TestMethod]
        public void Load_ValidateActionThrows_RejectsLine()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup"))
            {
                Validator = e => throw new InvalidOperationException("boom")
            };

            var result = crontab.Load();

            Assert.IsFalse(result);
            Assert.AreEqual(0, crontab.Entries.Count);
            StringAssert.Contains(crontab.Errors.Single(), "boom");
        }

        [TestMethod]
        public void Load_MissingFile_ReturnsFalseAndEmptyEntries()
        {
            var crontab = new TestCrontab(Path.Combine(_directory, "missing"));

            var result = crontab.Load();

            Assert.IsFalse(result);
            Assert.IsFalse(crontab.FileExists);
            Assert.AreEqual(0, crontab.Entries.Count);
        }

        #endregion

        #region ReloadIfChanged

        [TestMethod]
        public void ReloadIfChanged_FirstCall_ReadsFile()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup"));

            Assert.IsTrue(crontab.ReloadIfChanged());
            Assert.AreEqual(1, crontab.Entries.Count);
        }

        [TestMethod]
        public void ReloadIfChanged_NotModified_ReturnsFalse()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup"));
            crontab.ReloadIfChanged();

            Assert.IsFalse(crontab.ReloadIfChanged());
        }

        [TestMethod]
        public void ReloadIfChanged_FileModified_ReadsAgain()
        {
            var path    = WriteFile("0 1 * * * Backup");
            var crontab = new TestCrontab(path);
            crontab.ReloadIfChanged();

            File.WriteAllLines(path, new[] { "0 1 * * * Backup", "@daily Cleanup" });
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

            Assert.IsTrue(crontab.ReloadIfChanged());
            Assert.AreEqual(2, crontab.Entries.Count);
        }

        [TestMethod]
        public void ReloadIfChanged_FileCreatedAndDeleted_ReadsAgain()
        {
            var path    = Path.Combine(_directory, Crontab.DEFAULTFILENAME);
            var crontab = new TestCrontab(path);
            crontab.ReloadIfChanged();
            Assert.IsFalse(crontab.FileExists);

            File.WriteAllLines(path, new[] { "0 1 * * * Backup" });
            Assert.IsTrue(crontab.ReloadIfChanged());
            Assert.AreEqual(1, crontab.Entries.Count);

            File.Delete(path);
            Assert.IsTrue(crontab.ReloadIfChanged());
            Assert.IsFalse(crontab.FileExists);
            Assert.AreEqual(0, crontab.Entries.Count);
        }

        [TestMethod]
        public void ReloadIfChanged_AfterInvalidate_ReadsAgain()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup"));
            crontab.ReloadIfChanged();

            crontab.Invalidate();

            Assert.IsTrue(crontab.ReloadIfChanged());
        }

        [TestMethod]
        public void ReloadIfChanged_AfterSetFile_ReadsNewFile()
        {
            var crontab = new TestCrontab(WriteFile("0 1 * * * Backup"));
            crontab.ReloadIfChanged();
            var otherPath = Path.Combine(_directory, "other");
            File.WriteAllLines(otherPath, new[] { "0 1 * * * Backup", "@daily Cleanup" });

            crontab.SetFile(otherPath, TimeZoneInfo.Utc);

            Assert.IsTrue(crontab.ReloadIfChanged());
            Assert.AreEqual(otherPath, crontab.FilePath);
            Assert.AreEqual(2, crontab.Entries.Count);
        }

        #endregion

        #region File and time zone

        [TestMethod]
        public void Constructor_NullPath_UsesDefaultFilePathAndLocalZone()
        {
            var crontab = new Crontab();

            Assert.AreEqual(Crontab.DefaultFilePath, crontab.FilePath);
            Assert.AreEqual(TimeZoneInfo.Local, crontab.Zone);
        }

        [TestMethod]
        public void Constructor_NullActionComparer_UsesDefaultActionComparer()
        {
            var crontab = new Crontab();

            Assert.AreSame(Crontab.DefaultActionComparer, crontab.ActionComparer);
        }

        [TestMethod]
        public void Constructor_ActionComparer_IsKept()
        {
            var comparer    = new WhitespaceInsensitiveComparer();
            var crontab     = new Crontab(null, null, comparer);

            Assert.AreSame(comparer, crontab.ActionComparer);
        }

        [TestMethod]
        public void Constructor_RelativePath_ResolvedOnDefaultConfigsDirectory()
        {
            var crontab = new Crontab("Schedules/crontab");

            Assert.AreEqual(Path.Combine(Paths.DefaultConfigsDirectory, "Schedules/crontab"), crontab.FilePath);
        }

        [TestMethod]
        public void SetFile_InvalidPath_ThrowsArgumentException()
        {
            var crontab = new Crontab();

            Assert.ThrowsExactly<ArgumentException>(() => crontab.SetFile("a|b", null));
        }

        [TestMethod]
        public void TryGetTimeZone_Empty_ReturnsLocal()
        {
            var result = Crontab.TryGetTimeZone(" ", out var zone);

            Assert.IsTrue(result);
            Assert.AreEqual(TimeZoneInfo.Local, zone);
        }

        [TestMethod]
        public void TryGetTimeZone_Unknown_ReturnsFalse()
        {
            var result = Crontab.TryGetTimeZone("Not/AZone", out var zone);

            Assert.IsFalse(result);
            Assert.IsNull(zone);
        }

        [TestMethod]
        public void TryGetTimeZone_Utc_ReturnsZone()
        {
            var result = Crontab.TryGetTimeZone("UTC", out var zone);

            Assert.IsTrue(result);
            Assert.AreEqual(TimeSpan.Zero, zone.BaseUtcOffset);
        }

        #endregion

        #region Occurrences

        [TestMethod]
        public void GetDueEntries_InitialBoundExcludedFinalIncluded()
        {
            var entries = Crontab.Parse(new[] { "0 10 * * * Backup" }).Entries;

            var excluded = Crontab.GetDueEntries(entries, Utc(10, 0), Utc(11, 0), TimeZoneInfo.Utc);
            var included = Crontab.GetDueEntries(entries, Utc(9, 0), Utc(10, 0), TimeZoneInfo.Utc);

            Assert.AreEqual(0, excluded.Count);
            Assert.AreEqual(1, included.Count);
        }

        [TestMethod]
        public void GetDueEntries_EmptyInterval_ReturnsEmpty()
        {
            var entries = Crontab.Parse(new[] { "* * * * * Backup" }).Entries;

            var result = Crontab.GetDueEntries(entries, Utc(10, 5), Utc(10, 0), TimeZoneInfo.Utc);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetDueActions_MoreLinesAndMissedTimes_ReturnsActionOnce()
        {
            var entries = Crontab.Parse(new[] { "* * * * * Import", "*/2 * * * * import", "0 12 * * * Backup" }).Entries;

            var result = Crontab.GetDueActions(entries, Utc(10, 0, 30), Utc(10, 5, 30), TimeZoneInfo.Utc);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Import", result[0].Action);
            Assert.AreEqual(Utc(10, 5), result[0].ScheduledTimeUtc);
            Assert.AreEqual(5, result[0].Occurrences);
        }

        [TestMethod]
        public void GetDueActions_DefaultComparer_ActionsWithDifferentSpacesAreDistinct()
        {
            var entries = Crontab.Parse(new[] { "* * * * * Import orders", "* * * * * import   orders" }).Entries;

            var result = Crontab.GetDueActions(entries, Utc(10, 0, 30), Utc(10, 1, 30), TimeZoneInfo.Utc);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetDueActions_CustomComparer_ActionsWithDifferentSpacesReturnedOnce()
        {
            var entries = Crontab.Parse(new[] { "* * * * * Import orders", "*/2 * * * * import   orders" }).Entries;

            var result = Crontab.GetDueActions(entries, Utc(10, 0, 30), Utc(10, 5, 30), TimeZoneInfo.Utc,
                new WhitespaceInsensitiveComparer());

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Import orders", result[0].Action);
            Assert.AreEqual(5, result[0].Occurrences);
        }

        [TestMethod]
        public void GetNextOccurrence_MoreEntries_ReturnsEarliest()
        {
            var entries = Crontab.Parse(new[] { "0 12 * * * Backup", "30 10 * * * Import" }).Entries;

            var result = Crontab.GetNextOccurrence(entries, Utc(10, 0), TimeZoneInfo.Utc);

            Assert.AreEqual(Utc(10, 30), result);
        }

        [TestMethod]
        public void GetNextOccurrence_NoEntries_ReturnsNull()
        {
            var result = Crontab.GetNextOccurrence(new List<CrontabEntry>(), Utc(10, 0), TimeZoneInfo.Utc);

            Assert.IsNull(result);
        }

        #endregion
    }
}
