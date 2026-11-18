using System;
using System.IO;
using System.Linq;

namespace OSKHelpers.Cron.Tests
{
    /// <summary>
    /// Tests for the check of the file (<see cref="Crontab.CheckCrontab"/>) and for the coverage of the actions.
    /// </summary>
    public partial class CrontabTests
    {
        #region CheckCrontab

        [TestMethod]
        public void CheckCrontab_ValidFile_IsValidWithCoverage()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import", "0 1 * * * Backup"));

            var result = crontab.CheckCrontab();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("The crontab file is valid.", result.Lines.Last());
            Assert.IsFalse(result.Lines.Any(l => l.StartsWith(Crontab.ERRORPREFIX)));
            Assert.IsTrue(result.Lines.Any(l => l.Contains("Backup") && l.Contains("[LEAST FREQUENT]")));
        }

        [TestMethod]
        public void CheckCrontab_InvalidLine_IsNotValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import", "bad line"));

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Lines.Count(l => l.StartsWith(Crontab.ERRORPREFIX)));
        }

        [TestMethod]
        public void CheckCrontab_MissingFile_IsNotValid()
        {
            var crontab = new TestCrontab(Path.Combine(_directory, "missing"));

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void CheckCrontab_ValidateActionRejectsLine_IsNotValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Unknown"))
            {
                Validator = e => "rejected"
            };

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Lines.Contains(Crontab.ERRORPREFIX + "rejected"));
        }

        [TestMethod]
        public void CheckCrontab_CoverageLinesOverridden_ReplacesDefaultTable()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import", "0 1 * * * Backup"))
            {
                CoverageLines = (entries, lines) => lines.Add($"custom table: {entries.Count} lines")
            };

            var result = crontab.CheckCrontab();

            Assert.IsTrue(result.IsValid);
            CollectionAssert.Contains(result.Lines, "custom table: 2 lines");
            Assert.IsFalse(result.Lines.Any(l => l.StartsWith("Coverage of the actions")));
        }

        [TestMethod]
        public void CheckCrontab_CoverageLinesThrows_IsNotValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import"))
            {
                CoverageLines = (entries, lines) => throw new InvalidOperationException("Test")
            };

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Lines.Any(l => l.StartsWith(Crontab.ERRORPREFIX)));
        }

        [TestMethod]
        public void CheckCrontab_CustomLogicAddsWarning_RemainsValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import"))
            {
                CustomCheck = (entries, lines) =>
                {
                    lines.Add(Crontab.WARNINGPREFIX + "custom warning");
                    return true;
                }
            };

            var result = crontab.CheckCrontab();

            Assert.IsTrue(result.IsValid);
            CollectionAssert.Contains(result.Lines, Crontab.WARNINGPREFIX + "custom warning");
        }

        [TestMethod]
        public void CheckCrontab_CustomLogicReturnsFalse_IsNotValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import"))
            {
                CustomCheck = (entries, lines) =>
                {
                    lines.Add(Crontab.ERRORPREFIX + "custom error");
                    return false;
                }
            };

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
            CollectionAssert.Contains(result.Lines, Crontab.ERRORPREFIX + "custom error");
        }

        [TestMethod]
        public void CheckCrontab_CustomLogicThrows_IsNotValid()
        {
            var crontab = new TestCrontab(WriteFile("* * * * * Import"))
            {
                CustomCheck = (entries, lines) => throw new InvalidOperationException("boom")
            };

            var result = crontab.CheckCrontab();

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Lines.Any(l => l.StartsWith(Crontab.ERRORPREFIX) && l.Contains("boom")));
        }

        [TestMethod]
        public void CheckCrontab_DoesNotReplaceEntriesInUse()
        {
            var path    = WriteFile("* * * * * Import");
            var crontab = new TestCrontab(path);
            crontab.Load();
            File.WriteAllLines(path, new[] { "* * * * * Import", "@daily Cleanup" });

            crontab.CheckCrontab();

            Assert.AreEqual(1, crontab.Entries.Count);
        }

        #endregion

        #region Coverage

        [TestMethod]
        public void GetCoverage_WithActions_IncludesActionsNotScheduled()
        {
            var entries = Crontab.Parse(new[] { "0 * * * * Import", "30 * * * * import" }).Entries;

            var result = Crontab.GetCoverage(entries, Utc(10, 0), TimeZoneInfo.Utc, new[] { "IMPORT", "Backup" });

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[0].EntriesCount);
            Assert.AreEqual(Utc(10, 30), result[0].NextOccurrenceUtc);
            Assert.AreEqual(48, result[0].OccurrencesNextDay);
            Assert.AreEqual(48 * 7, result[0].OccurrencesNextWeek);
            Assert.IsFalse(result[1].IsScheduled);
            Assert.IsNull(result[1].NextOccurrenceUtc);
        }

        [TestMethod]
        public void GetCoverage_CustomComparer_GroupsActionsWrittenDifferently()
        {
            var entries = Crontab.Parse(new[] { "0 * * * * Import orders", "30 * * * * import   orders" }).Entries;

            var result = Crontab.GetCoverage(entries, Utc(10, 0), TimeZoneInfo.Utc, null, new WhitespaceInsensitiveComparer());

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Import orders", result[0].Action);
            Assert.AreEqual(2, result[0].EntriesCount);
            Assert.AreEqual(48, result[0].OccurrencesNextDay);
        }

        [TestMethod]
        public void CheckCrontab_CustomComparer_CoverageGroupsActions()
        {
            var path    = WriteFile("0 * * * * Import orders", "30 * * * * import   orders");
            var crontab = new Crontab(path, TimeZoneInfo.Utc, new WhitespaceInsensitiveComparer());

            var result = crontab.CheckCrontab();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Lines.Count(l => l.Contains("orders")));
        }

        [TestMethod]
        public void GetLeastCovered_DifferentFrequencies_ReturnsLeastFrequent()
        {
            var entries     = Crontab.Parse(new[] { "0 * * * * Import", "0 1 * * * Backup" }).Entries;
            var coverage    = Crontab.GetCoverage(entries, Utc(10, 0), TimeZoneInfo.Utc);

            var result = Crontab.GetLeastCovered(coverage);

            Assert.AreEqual("Backup", result.Single().Action);
        }

        [TestMethod]
        public void GetLeastCovered_SameFrequency_ReturnsEmpty()
        {
            var entries     = Crontab.Parse(new[] { "0 1 * * * Import", "0 2 * * * Backup" }).Entries;
            var coverage    = Crontab.GetCoverage(entries, Utc(10, 0), TimeZoneInfo.Utc);

            var result = Crontab.GetLeastCovered(coverage);

            Assert.AreEqual(0, result.Count);
        }

        #endregion
    }
}
