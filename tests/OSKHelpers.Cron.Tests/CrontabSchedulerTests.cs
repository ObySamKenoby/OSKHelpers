using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OSKHelpers.Cron.Interfaces;

namespace OSKHelpers.Cron.Tests
{
    /// <summary>
    /// Tests for <see cref="CrontabScheduler"/>: single wake-ups driven with <see cref="CrontabScheduler.RunCycle"/>, start,
    /// stop and settings.
    /// </summary>
    [TestClass]
    public class CrontabSchedulerTests
    {
        #region Support classes

        /// <summary>
        /// Implementation of <see cref="ISettingsCron"/> for the tests.
        /// </summary>
        public class TestSettings : ISettingsCron
        {
            public string CronFilePath { get; set; }

            public int CronCheckIntervalSeconds { get; set; }

            public int? CronStopTimeoutSeconds { get; set; }

            public string CronTimeZone { get; set; }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Maximum wait, in seconds, of the tests for the actions started in a task of the thread pool.
        /// </summary>
        private const int WAITTIMEOUTSECONDS = 10;

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
        /// Creates a scheduler of a crontab file written in the directory of the test, in UTC.
        /// </summary>
        private CrontabScheduler CreateScheduler(params string[] lines)
        {
            var path = Path.Combine(_directory, Crontab.DEFAULTFILENAME);
            File.WriteAllLines(path, lines);
            return new CrontabScheduler(new Crontab(path, TimeZoneInfo.Utc));
        }

        private static DateTime Utc(int hour, int minute, int second = 0)
            => new DateTime(2026, 1, 5, hour, minute, second, DateTimeKind.Utc);

        #endregion

        #region GetDelay

        [TestMethod]
        public void GetDelay_NoOccurrence_ReturnsMaximum()
        {
            var result = CrontabScheduler.GetDelay(null, Utc(10, 0), TimeSpan.FromSeconds(60));

            Assert.AreEqual(TimeSpan.FromSeconds(60), result);
        }

        [TestMethod]
        public void GetDelay_OccurrenceBeforeMaximum_ReturnsTimeUntilOccurrence()
        {
            var result = CrontabScheduler.GetDelay(Utc(10, 0, 20), Utc(10, 0), TimeSpan.FromSeconds(60));

            Assert.AreEqual(TimeSpan.FromSeconds(20), result);
        }

        [TestMethod]
        public void GetDelay_OccurrenceAfterMaximum_ReturnsMaximum()
        {
            var result = CrontabScheduler.GetDelay(Utc(11, 0), Utc(10, 0), TimeSpan.FromSeconds(60));

            Assert.AreEqual(TimeSpan.FromSeconds(60), result);
        }

        [TestMethod]
        public void GetDelay_OccurrenceInThePast_ReturnsZero()
        {
            var result = CrontabScheduler.GetDelay(Utc(9, 0), Utc(10, 0), TimeSpan.FromSeconds(60));

            Assert.AreEqual(TimeSpan.Zero, result);
        }

        #endregion

        #region Constructor and properties

        [TestMethod]
        public void Constructor_NullCrontab_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CrontabScheduler(null));
        }

        [TestMethod]
        public void Constructor_InvalidMaxWait_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CrontabScheduler(new Crontab(), 0));
        }

        [TestMethod]
        public void Constructor_Defaults_AreApplied()
        {
            var scheduler = new CrontabScheduler(new Crontab());

            Assert.AreEqual(CrontabScheduler.MAXWAITSECONDS, scheduler.MaxWaitSeconds);
            Assert.AreEqual(CrontabScheduler.DEFAULTSTOPTIMEOUTSECONDS, scheduler.StopTimeoutSeconds);
            Assert.IsFalse(scheduler.IsRunning);
        }

        [TestMethod]
        public void StopTimeoutSeconds_Negative_ThrowsArgumentOutOfRangeException()
        {
            var scheduler = new CrontabScheduler(new Crontab());

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => scheduler.StopTimeoutSeconds = -1);
        }

        #endregion

        #region RunCycle

        [TestMethod]
        public void RunCycle_DueAction_RaisesEventAndAwaitsActionHandler()
        {
            var scheduler   = CreateScheduler("* * * * * Import", "0 12 * * * Backup");
            var triggered   = new ConcurrentBag<ScheduledActionTriggeredEventArgs>();
            var handled     = new ConcurrentBag<string>();
            scheduler.ScheduledActionTriggered  += (sender, e) => triggered.Add(e);
            scheduler.ActionHandler             = async e =>
            {
                await Task.Yield();
                handled.Add(e.Action);
            };

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            Assert.IsTrue(scheduler.Stop());

            Assert.AreEqual(1, triggered.Count);
            var args = triggered.Single();
            Assert.AreEqual("Import", args.Action);
            Assert.AreEqual(Utc(10, 1), args.ScheduledTimeUtc);
            Assert.AreEqual(0, args.MissedOccurrences);
            CollectionAssert.AreEqual(new[] { "Import" }, handled.ToArray());
        }

        [TestMethod]
        public void RunCycle_ActionOnMoreLinesAndLateWakeUp_RaisesOneEventWithMissedOccurrences()
        {
            var scheduler   = CreateScheduler("* * * * * Import", "*/2 * * * * IMPORT");
            var triggered   = new ConcurrentBag<ScheduledActionTriggeredEventArgs>();
            scheduler.ScheduledActionTriggered += (sender, e) => triggered.Add(e);

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 5, 30), CancellationToken.None);
            scheduler.Stop();

            var args = triggered.Single();
            Assert.AreEqual(Utc(10, 5), args.ScheduledTimeUtc);
            Assert.AreEqual(4, args.MissedOccurrences);
        }

        [TestMethod]
        public void RunCycle_ActionStillRunning_SkipsAndRaisesScheduledActionSkipped()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var completion  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var executions  = 0;
            var skipped     = new List<ScheduledActionSkippedEventArgs>();
            scheduler.ActionHandler             = e =>
            {
                Interlocked.Increment(ref executions);
                return completion.Task;
            };
            scheduler.ScheduledActionSkipped    += (sender, e) => skipped.Add(e);

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            scheduler.RunCycle(Utc(10, 1, 30), Utc(10, 2, 30), CancellationToken.None);
            completion.SetResult(true);
            scheduler.Stop();

            Assert.AreEqual(1, executions);
            Assert.AreEqual(1, skipped.Count);
            Assert.AreEqual("Import", skipped[0].Action);
            Assert.AreEqual(Utc(10, 2), skipped[0].ScheduledTimeUtc);
            Assert.AreEqual(CrontabScheduler.SKIPREASONSTILLRUNNING, skipped[0].Reason);
        }

        [TestMethod]
        public void RunCycle_CustomComparerActionsWrittenDifferently_RaisesOneEvent()
        {
            var path        = Path.Combine(_directory, Crontab.DEFAULTFILENAME);
            File.WriteAllLines(path, new[] { "* * * * * Import orders", "* * * * * import   orders" });
            var scheduler   = new CrontabScheduler(new Crontab(path, TimeZoneInfo.Utc, new WhitespaceInsensitiveComparer()));
            var triggered   = new ConcurrentBag<ScheduledActionTriggeredEventArgs>();
            scheduler.ScheduledActionTriggered += (sender, e) => triggered.Add(e);

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            scheduler.Stop();

            Assert.AreEqual("Import orders", triggered.Single().Action);
        }

        [TestMethod]
        public void RunCycle_CustomComparerActionWrittenDifferentlyStillRunning_Skips()
        {
            var path        = Path.Combine(_directory, Crontab.DEFAULTFILENAME);
            File.WriteAllLines(path, new[] { "0 * * * * Import orders", "1 * * * * import   orders" });
            var scheduler   = new CrontabScheduler(new Crontab(path, TimeZoneInfo.Utc, new WhitespaceInsensitiveComparer()));
            var completion  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var executions  = 0;
            var skipped     = new List<ScheduledActionSkippedEventArgs>();
            scheduler.ActionHandler             = e =>
            {
                Interlocked.Increment(ref executions);
                return completion.Task;
            };
            scheduler.ScheduledActionSkipped    += (sender, e) => skipped.Add(e);

            scheduler.RunCycle(Utc(9, 59, 30), Utc(10, 0, 30), CancellationToken.None);
            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            completion.SetResult(true);
            scheduler.Stop();

            Assert.AreEqual(1, executions);
            Assert.AreEqual(1, skipped.Count);
            // The action of the second line is reported as written in the first line that schedules it
            Assert.AreEqual("Import orders", skipped[0].Action);
            Assert.AreEqual(Utc(10, 1), skipped[0].ScheduledTimeUtc);
        }

        [TestMethod]
        public void RunCycle_ActionTerminated_StartsAgainAtNextTime()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var executions  = 0;
            scheduler.ScheduledActionTriggered += (sender, e) => Interlocked.Increment(ref executions);

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            scheduler.Stop();
            scheduler.RunCycle(Utc(10, 1, 30), Utc(10, 2, 30), CancellationToken.None);
            scheduler.Stop();

            Assert.AreEqual(2, executions);
        }

        [TestMethod]
        public void RunCycle_EventHandlerThrows_OtherHandlersAndActionHandlerAreExecuted()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var secondCalled = false;
            var handlerCalled = false;
            scheduler.ScheduledActionTriggered  += (sender, e) => throw new InvalidOperationException("boom");
            scheduler.ScheduledActionTriggered  += (sender, e) => secondCalled = true;
            scheduler.ActionHandler             = e =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            };

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);
            scheduler.Stop();

            Assert.IsTrue(secondCalled);
            Assert.IsTrue(handlerCalled);
        }

        [TestMethod]
        public void RunCycle_ActionHandlerThrows_ActionIsTerminated()
        {
            var scheduler = CreateScheduler("* * * * * Import");
            scheduler.ActionHandler = async e =>
            {
                await Task.Yield();
                throw new InvalidOperationException("boom");
            };

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);

            Assert.IsTrue(scheduler.Stop());
        }

        [TestMethod]
        public void RunCycle_FileModified_RaisesCrontabReloadedWithErrors()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var reloaded    = new List<CrontabReloadedEventArgs>();
            scheduler.CrontabReloaded += (sender, e) => reloaded.Add(e);

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 0, 40), CancellationToken.None);
            var path = scheduler.Crontab.FilePath;
            File.WriteAllLines(path, new[] { "* * * * * Import", "bad line" });
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
            scheduler.RunCycle(Utc(10, 0, 40), Utc(10, 0, 50), CancellationToken.None);
            scheduler.RunCycle(Utc(10, 0, 50), Utc(10, 0, 55), CancellationToken.None);

            Assert.AreEqual(2, reloaded.Count);
            Assert.IsTrue(reloaded[0].FileExists);
            Assert.AreEqual(0, reloaded[0].Errors.Count);
            Assert.AreEqual(1, reloaded[1].EntriesCount);
            Assert.AreEqual(1, reloaded[1].Errors.Count);
        }

        [TestMethod]
        public void RunCycle_UpdatesNextOccurrence()
        {
            var scheduler = CreateScheduler("*/5 * * * * Import", "*/5 * * * * Export", "0 12 * * * Backup");

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), CancellationToken.None);

            Assert.AreEqual(Utc(10, 5), scheduler.NextOccurrenceUtc);
            CollectionAssert.AreEquivalent(new[] { "Import", "Export" }, scheduler.NextOccurrenceActions.ToArray());
        }

        #endregion

        #region Start and stop

        [TestMethod]
        public void Start_ReadsFileRaisesCrontabReloadedAndCalculatesNextOccurrence()
        {
            using (var scheduler = CreateScheduler("* * * * * Import"))
            {
                var reloaded = 0;
                scheduler.CrontabReloaded += (sender, e) => reloaded++;

                var started         = scheduler.Start();
                var startedAgain    = scheduler.Start();

                Assert.IsTrue(started);
                Assert.IsFalse(startedAgain);
                Assert.IsTrue(scheduler.IsRunning);
                Assert.AreEqual(1, reloaded);
                Assert.IsNotNull(scheduler.NextOccurrenceUtc);
                CollectionAssert.AreEqual(new[] { "Import" }, scheduler.NextOccurrenceActions.ToArray());

                Assert.IsTrue(scheduler.Stop());
                Assert.IsFalse(scheduler.IsRunning);
                Assert.IsNull(scheduler.NextOccurrenceUtc);
            }
        }

        [TestMethod]
        public void Stop_RunningAction_SignalsCancellationToken()
        {
            var scheduler = CreateScheduler("* * * * * Import");
            var cancelled = false;
            scheduler.ActionHandler = async e =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, e.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
            };
            scheduler.Start();

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), scheduler.StopToken);
            var result = scheduler.Stop();

            Assert.IsTrue(result);
            Assert.IsTrue(cancelled);
        }

        [TestMethod]
        public void Stop_TimeoutExpired_ReturnsFalse()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var completion  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            scheduler.StopTimeoutSeconds    = 0;
            scheduler.ActionHandler         = e => completion.Task;
            scheduler.Start();

            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), scheduler.StopToken);
            var result = scheduler.Stop();
            completion.SetResult(true);

            Assert.IsFalse(result);
            Assert.IsTrue(scheduler.StopAsync().Wait(TimeSpan.FromSeconds(WAITTIMEOUTSECONDS)));
        }

        [TestMethod]
        public void Dispose_AfterStopWithTimeoutExpired_DoesNotWaitAgain()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var completion  = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            scheduler.StopTimeoutSeconds    = 1;
            scheduler.ActionHandler         = e => completion.Task;
            scheduler.Start();
            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 1, 30), scheduler.StopToken);
            scheduler.Stop();

            // The action is still running: a new stop would wait for the timeout again.
            var watch = System.Diagnostics.Stopwatch.StartNew();
            scheduler.Dispose();
            watch.Stop();
            completion.SetResult(true);

            Assert.IsLessThan(TimeSpan.FromMilliseconds(500), watch.Elapsed);
            Assert.IsFalse(scheduler.Start());
        }

        [TestMethod]
        public void Dispose_Running_StopsScheduler()
        {
            var scheduler = CreateScheduler("* * * * * Import");
            scheduler.Start();

            scheduler.Dispose();

            Assert.IsFalse(scheduler.IsRunning);
        }

        [TestMethod]
        public void Dispose_RestartedAfterStop_StopsScheduler()
        {
            var scheduler = CreateScheduler("* * * * * Import");
            scheduler.Start();
            scheduler.Stop();
            scheduler.Start();

            scheduler.Dispose();

            Assert.IsFalse(scheduler.IsRunning);
        }

        [TestMethod]
        public void Start_AfterDispose_ReturnsFalse()
        {
            var scheduler = CreateScheduler("* * * * * Import");
            scheduler.Dispose();

            Assert.IsFalse(scheduler.Start());
        }

        #endregion

        #region Setup

        [TestMethod]
        public void Setup_ValidSettings_AppliesValuesAndChangesFile()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var otherPath   = Path.Combine(_directory, "other");
            File.WriteAllLines(otherPath, new[] { "* * * * * Import", "@daily Cleanup" });
            var settings    = new TestSettings
            {
                CronFilePath                = otherPath,
                CronCheckIntervalSeconds    = 10,
                CronStopTimeoutSeconds      = null,
                CronTimeZone                = "UTC"
            };

            var result = scheduler.Setup(settings);
            scheduler.RunCycle(Utc(10, 0, 30), Utc(10, 0, 40), CancellationToken.None);

            Assert.IsTrue(result);
            Assert.AreEqual(10, scheduler.MaxWaitSeconds);
            Assert.IsNull(scheduler.StopTimeoutSeconds);
            Assert.AreEqual(otherPath, scheduler.Crontab.FilePath);
            Assert.AreEqual(2, scheduler.Crontab.Entries.Count);
        }

        [TestMethod]
        public void Setup_InvalidValue_AppliesNothing()
        {
            var scheduler   = CreateScheduler("* * * * * Import");
            var filePath    = scheduler.Crontab.FilePath;
            var settings    = new TestSettings
            {
                CronFilePath                = Path.Combine(_directory, "other"),
                CronCheckIntervalSeconds    = 10,
                CronStopTimeoutSeconds      = -1,
                CronTimeZone                = string.Empty
            };

            var result = scheduler.Setup(settings);

            Assert.IsFalse(result);
            Assert.AreEqual(CrontabScheduler.MAXWAITSECONDS, scheduler.MaxWaitSeconds);
            Assert.AreEqual(CrontabScheduler.DEFAULTSTOPTIMEOUTSECONDS, scheduler.StopTimeoutSeconds);
            Assert.AreEqual(filePath, scheduler.Crontab.FilePath);
        }

        [TestMethod]
        public void Setup_UnknownTimeZoneOrInvalidPathOrInterval_ReturnsFalse()
        {
            var scheduler = CreateScheduler("* * * * * Import");

            var zone        = scheduler.Setup(new TestSettings { CronCheckIntervalSeconds = 60, CronTimeZone = "Not/AZone" });
            var path        = scheduler.Setup(new TestSettings { CronCheckIntervalSeconds = 60, CronFilePath = "a|b" });
            var interval    = scheduler.Setup(new TestSettings { CronCheckIntervalSeconds = 0 });

            Assert.IsFalse(zone);
            Assert.IsFalse(path);
            Assert.IsFalse(interval);
        }

        [TestMethod]
        public void Setup_NullSettings_ThrowsArgumentNullException()
        {
            var scheduler = CreateScheduler("* * * * * Import");

            Assert.ThrowsExactly<ArgumentNullException>(() => scheduler.Setup(null));
        }

        [TestMethod]
        public void Constructor_WithSettings_AppliesSettings()
        {
            var settings = new TestSettings { CronCheckIntervalSeconds = 15, CronStopTimeoutSeconds = 30 };

            var scheduler = new CrontabScheduler(new Crontab(), settings);

            Assert.AreEqual(15, scheduler.MaxWaitSeconds);
            Assert.AreEqual(30, scheduler.StopTimeoutSeconds);
            Assert.AreEqual(Crontab.DefaultFilePath, scheduler.Crontab.FilePath);
        }

        #endregion
    }
}
