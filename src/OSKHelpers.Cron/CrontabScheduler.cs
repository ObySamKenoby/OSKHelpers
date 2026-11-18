using OSKHelpers.Cron.Interfaces;
using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Scheduler of the actions of a <see cref="Cron.Crontab"/>: at every scheduled time it raises
    /// <see cref="ScheduledActionTriggered"/> and awaits <see cref="ActionHandler"/>, until it is stopped.
    /// <list type="bullet">
    /// <item>It wakes up at the next scheduled time, and anyway at least every <see cref="MaxWaitSeconds"/> seconds to reload
    /// the <c>crontab</c> file if it has been modified (<see cref="CrontabReloaded"/>): the changes are applied without a
    /// restart.</item>
    /// <item>At every wake-up it executes the actions with a scheduled time between the previous wake-up (excluded) and the
    /// current one (included): the scheduled times before the start are not recovered, the times missed by a late wake-up
    /// produce a single execution (<see cref="ScheduledActionTriggeredEventArgs.MissedOccurrences"/>), an action scheduled by
    /// more lines is executed once.</item>
    /// <item>Every action runs in its own task of the thread pool: the actions of the same wake-up start in parallel. If an
    /// action is still running at its next scheduled time, that time is skipped (<see cref="ScheduledActionSkipped"/>).</item>
    /// <item>Two lines schedule the same action when <see cref="Crontab.ActionComparer"/> considers their actions equal (by
    /// default without distinction between upper and lower case): a custom comparer of the crontab extends the protection
    /// against overlapping executions to actions written in different ways.</item>
    /// <item>When stopped (<see cref="Stop"/>), no new execution is started, the
    /// <see cref="ScheduledActionTriggeredEventArgs.CancellationToken"/> is signalled and the running actions are awaited up
    /// to <see cref="StopTimeoutSeconds"/>.</item>
    /// <item>No exception is thrown by the loop: errors are written to the log (<see cref="SimpleLog"/>).</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <b>Asynchronous code</b>: the handlers of <see cref="ScheduledActionTriggered"/> must be synchronous. An <c>async</c>
    /// handler returns at its first <c>await</c>: the scheduler would consider the action terminated while it is still running
    /// (no protection against overlapping executions, no wait when stopped) and an exception thrown after the <c>await</c>
    /// could terminate the process. For asynchronous code use <see cref="ActionHandler"/>, or wait for it in the handler of
    /// the event with <c>.GetAwaiter().GetResult()</c> (the handler already runs in its own task of the thread pool).<br/>
    /// Events and <see cref="ActionHandler"/> must be set before <see cref="Start"/>, which reads the file immediately and
    /// raises <see cref="CrontabReloaded"/>.<br/>
    /// <see cref="Stop"/> must not be called by the handlers of the scheduler: it waits for the loop and for the running
    /// actions, hence also for the handler that calls it.
    /// <code>
    /// var scheduler = new CrontabScheduler(new Crontab(), AppSettings.Current);
    /// scheduler.CrontabReloaded   += (sender, e) =&gt; { /* e.g. send a warning when e.Errors is not empty */ };
    /// scheduler.ActionHandler     = e =&gt; RunActionAsync(e.Action, e.CancellationToken);
    /// scheduler.Start();
    /// ...
    /// scheduler.Stop();
    /// </code>
    /// </remarks>
    public class CrontabScheduler : IDisposable
    {
        #region Constants

        /// <summary>
        /// Default maximum wait, in seconds, between two wake-ups: interval of the check of the changes to the file.
        /// </summary>
        public const int MAXWAITSECONDS = 60;

        /// <summary>
        /// Default maximum wait, in seconds, for the running actions when the scheduler is stopped (5 minutes).
        /// </summary>
        public const int DEFAULTSTOPTIMEOUTSECONDS = 300;

        /// <summary>
        /// Reason of <see cref="ScheduledActionSkippedEventArgs"/> when the action is still running from its previous
        /// scheduled time.
        /// </summary>
        public const string SKIPREASONSTILLRUNNING = "The action is still running from its previous scheduled time.";

        #endregion

        #region Members

        /// <summary>
        /// Protects the state of the scheduler (loop, running actions, parameters, next occurrence).
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Executions started, by action: an action whose task is not terminated is not started again.
        /// </summary>
        private readonly Dictionary<string, Task> _running;

        /// <summary>
        /// Task of the loop; null when the scheduler is stopped.
        /// </summary>
        private Task _loopTask;

        /// <summary>
        /// Source of the token signalled when the scheduler is stopped; null when the scheduler is stopped.
        /// </summary>
        private CancellationTokenSource _cancellation;

        /// <summary>
        /// See <see cref="MaxWaitSeconds"/>.
        /// </summary>
        private int _maxWaitSeconds;

        /// <summary>
        /// See <see cref="StopTimeoutSeconds"/>.
        /// </summary>
        private int? _stopTimeoutSeconds;

        /// <summary>
        /// See <see cref="NextOccurrenceUtc"/>.
        /// </summary>
        private DateTime? _nextOccurrenceUtc;

        /// <summary>
        /// See <see cref="NextOccurrenceActions"/>.
        /// </summary>
        private IReadOnlyList<string> _nextOccurrenceActions;

        /// <summary>
        /// True after <see cref="Dispose()"/>.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// True after <see cref="Stop"/> or <see cref="StopAsync"/>, until the next <see cref="Start"/>: <see cref="Dispose()"/>
        /// does not stop the scheduler again, which would wait once more for the actions still running after the timeout.
        /// </summary>
        private bool _stopRequested;

        #endregion

        #region Events

        /// <summary>
        /// Raised for every action at its scheduled time, in a task of the thread pool. The handlers must be synchronous (see
        /// the remarks of the class) and thread-safe; their exceptions are caught and written to the log, and do not prevent
        /// the other handlers nor <see cref="ActionHandler"/>.
        /// </summary>
        public event EventHandler<ScheduledActionTriggeredEventArgs> ScheduledActionTriggered;

        /// <summary>
        /// Raised when a scheduled execution is skipped because the action is still running from its previous scheduled time.
        /// Raised synchronously by the loop of the scheduler: the handler must be short. Exceptions are caught and written to
        /// the log.
        /// </summary>
        public event EventHandler<ScheduledActionSkippedEventArgs> ScheduledActionSkipped;

        /// <summary>
        /// Raised at every reading of the <c>crontab</c> file (at the start and when the file is modified), with its errors:
        /// e.g. to send a warning. Raised synchronously by <see cref="Start"/> and by the loop of the scheduler. Exceptions are
        /// caught and written to the log.
        /// </summary>
        public event EventHandler<CrontabReloadedEventArgs> CrontabReloaded;

        #endregion

        #region Properties

        /// <summary>
        /// Crontab whose actions are scheduled.
        /// </summary>
        public Crontab Crontab { get; }

        /// <summary>
        /// Asynchronous handler of the actions, awaited by the scheduler after <see cref="ScheduledActionTriggered"/>: the
        /// action is considered running until the returned task is terminated. Exceptions are caught and written to the log.
        /// Null (default) if not used.
        /// </summary>
        public Func<ScheduledActionTriggeredEventArgs, Task> ActionHandler { get; set; }

        /// <summary>
        /// Maximum wait, in seconds, between two wake-ups, i.e. the interval of the check of the changes to the file; at least
        /// 1. A change is applied from the next wake-up.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is lower than 1.</exception>
        public int MaxWaitSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _maxWaitSeconds;
                }
            }
            set
            {
                if (value >= 1)
                {
                    lock (_lock)
                    {
                        _maxWaitSeconds = value;
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The maximum wait must be at least 1 second.");
                }
            }
        }

        /// <summary>
        /// Maximum wait, in seconds, for the running actions when the scheduler is stopped; null means no limit. Default
        /// <see cref="DEFAULTSTOPTIMEOUTSECONDS"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
        public int? StopTimeoutSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _stopTimeoutSeconds;
                }
            }
            set
            {
                if (!value.HasValue || value.Value >= 0)
                {
                    lock (_lock)
                    {
                        _stopTimeoutSeconds = value;
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The stop timeout cannot be negative.");
                }
            }
        }

        /// <summary>
        /// True while the scheduler is running (between <see cref="Start"/> and <see cref="Stop"/>).
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _loopTask != null;
                }
            }
        }

        /// <summary>
        /// Next scheduled time, in UTC, recalculated at every reading of the file and at every wake-up; null if there are no
        /// future executions or the scheduler is stopped.
        /// </summary>
        public DateTime? NextOccurrenceUtc
        {
            get
            {
                lock (_lock)
                {
                    return _nextOccurrenceUtc;
                }
            }
        }

        /// <summary>
        /// Actions scheduled at <see cref="NextOccurrenceUtc"/>; empty if there are no future executions or the scheduler is
        /// stopped.
        /// </summary>
        public IReadOnlyList<string> NextOccurrenceActions
        {
            get
            {
                lock (_lock)
                {
                    return _nextOccurrenceActions;
                }
            }
        }

        /// <summary>
        /// Token passed to the actions while the scheduler is running, signalled by <see cref="Stop"/>;
        /// <see cref="CancellationToken.None"/> when the scheduler is stopped. Used by the tests with <see cref="RunCycle"/>.
        /// </summary>
        internal CancellationToken StopToken
        {
            get
            {
                lock (_lock)
                {
                    return _cancellation?.Token ?? CancellationToken.None;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the scheduler of a crontab, stopped (see <see cref="Start"/>).
        /// </summary>
        /// <param name="crontab">Crontab whose actions are scheduled; it may be a derived class.</param>
        /// <param name="maxWaitSeconds">Initial value of <see cref="MaxWaitSeconds"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="crontab"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxWaitSeconds"/> is lower than 1.</exception>
        public CrontabScheduler(Crontab crontab, int maxWaitSeconds = MAXWAITSECONDS)
        {
            Crontab                 = crontab ?? throw new ArgumentNullException(nameof(crontab));
            _running                = new Dictionary<string, Task>(Crontab.ActionComparer);
            _loopTask               = null;
            _cancellation           = null;
            _stopTimeoutSeconds     = DEFAULTSTOPTIMEOUTSECONDS;
            _nextOccurrenceUtc      = null;
            _nextOccurrenceActions  = new List<string>();
            _disposed               = false;
            _stopRequested          = false;
            MaxWaitSeconds          = maxWaitSeconds;
        }

        /// <summary>
        /// Creates the scheduler of a crontab, stopped, and configures it with <see cref="Setup(ISettingsCron)"/>: with invalid
        /// settings the default values remain (the reasons are written to the log).
        /// </summary>
        /// <param name="crontab">Crontab whose actions are scheduled; it may be a derived class.</param>
        /// <param name="settings">Settings of the scheduler.</param>
        /// <exception cref="ArgumentNullException"><paramref name="crontab"/> or <paramref name="settings"/> is null.</exception>
        public CrontabScheduler(Crontab crontab, ISettingsCron settings) : this(crontab)
        {
            Setup(settings);
        }

        #endregion

        #region Methods

        #region Setup

        /// <summary>
        /// Configures the scheduler from the settings of the application; it can be called before <see cref="Start"/> and while
        /// the scheduler is running (e.g. in the handler of <c>JsonSettings&lt;T&gt;.AutoUpdated</c>).<br/>
        /// All the values are checked before applying any of them: with an invalid value nothing is applied and the reasons are
        /// written to the log. If the file or the time zone change, the file is read at the next wake-up
        /// (<see cref="Crontab.SetFile"/>).
        /// </summary>
        /// <param name="settings">Settings of the scheduler.</param>
        /// <returns>True if the settings are valid and have been applied.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
        public bool Setup(ISettingsCron settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var problems = new List<string>();

            if (!Crontab.TryResolveFilePath(settings.CronFilePath, out var filePath))
            {
                problems.Add($"the path of the crontab file ('{settings.CronFilePath}') contains invalid characters");
            }
            if (settings.CronCheckIntervalSeconds < 1)
            {
                problems.Add($"the check interval ({settings.CronCheckIntervalSeconds}) must be at least 1 second");
            }
            if (settings.CronStopTimeoutSeconds.HasValue && settings.CronStopTimeoutSeconds.Value < 0)
            {
                problems.Add($"the stop timeout ({settings.CronStopTimeoutSeconds.Value}) cannot be negative");
            }
            if (!Crontab.TryGetTimeZone(settings.CronTimeZone, out var zone))
            {
                problems.Add($"the time zone '{settings.CronTimeZone}' does not exist");
            }

            var result = problems.Count == 0;
            if (result)
            {
                MaxWaitSeconds      = settings.CronCheckIntervalSeconds;
                StopTimeoutSeconds  = settings.CronStopTimeoutSeconds;
                if (!string.Equals(filePath, Crontab.FilePath, StringComparison.Ordinal) || zone.Id != Crontab.Zone.Id)
                {
                    Crontab.SetFile(filePath, zone);
                    SimpleLog.Write(LogLevel.Info, $"Crontab scheduler: file '{filePath}', time zone '{zone.Id}'.");
                }
            }
            else
            {
                SimpleLog.Write(LogLevel.Error, $"Crontab scheduler settings not applied: {string.Join("; ", problems)}.");
            }

            return result;
        }

        #endregion

        #region Start and stop

        /// <summary>
        /// Starts the scheduler: reads the file immediately (raising <see cref="CrontabReloaded"/>) and starts the loop in a task
        /// of the thread pool. Events and <see cref="ActionHandler"/> must be set before.
        /// </summary>
        /// <returns>True if the scheduler has been started, false if it was already running or has been disposed.</returns>
        public bool Start()
        {
            var result = false;

            lock (_lock)
            {
                if (_loopTask == null && !_disposed)
                {
                    var startUtc    = DateTime.UtcNow;
                    _cancellation   = new CancellationTokenSource();
                    var token       = _cancellation.Token;

                    SimpleLog.Write(LogLevel.Info, $"Crontab scheduler started, schedule from '{Crontab.FilePath}'.");
                    ReloadCrontab(true);
                    UpdateNextOccurrence(startUtc);
                    _loopTask       = Task.Run(() => RunLoopAsync(startUtc, token));
                    _stopRequested  = false;
                    result          = true;
                }
            }

            return result;
        }

        /// <inheritdoc cref="StopAsync"/>
        public bool Stop()
            => StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Stops the scheduler: no new execution is started, the
        /// <see cref="ScheduledActionTriggeredEventArgs.CancellationToken"/> is signalled and the running actions are awaited up
        /// to <see cref="StopTimeoutSeconds"/>; the actions still running at the timeout are not interrupted.<br/>
        /// It can be called also when the scheduler is stopped: it waits for the actions still running, if any.
        /// </summary>
        /// <returns>True if all the actions are terminated, false if the timeout has expired (the actions still running are
        /// written to the log).</returns>
        public async Task<bool> StopAsync()
        {
            Task loopTask;
            CancellationTokenSource cancellation;
            int? timeoutSeconds;

            lock (_lock)
            {
                loopTask                = _loopTask;
                cancellation            = _cancellation;
                timeoutSeconds          = _stopTimeoutSeconds;
                _loopTask               = null;
                _cancellation           = null;
                _nextOccurrenceUtc      = null;
                _nextOccurrenceActions  = new List<string>();
                _stopRequested          = true;
            }

            if (loopTask != null)
            {
                cancellation.Cancel();
                await loopTask.ConfigureAwait(false);
            }

            var result = await WaitRunningAsync(timeoutSeconds).ConfigureAwait(false);
            if (loopTask != null)
            {
                // The token is still used by the actions not terminated: the source is disposed only when all are terminated.
                if (result)
                {
                    cancellation.Dispose();
                }
                SimpleLog.Write(LogLevel.Info, "Crontab scheduler stopped.");
            }

            return result;
        }

        /// <summary>
        /// Waits for the termination of the running actions, up to the given timeout.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum wait, in seconds; null means no limit.</param>
        /// <returns>True if all the actions are terminated.</returns>
        private async Task<bool> WaitRunningAsync(int? timeoutSeconds)
        {
            var result = true;
            List<KeyValuePair<string, Task>> running;

            lock (_lock)
            {
                running = _running.Where(r => !r.Value.IsCompleted).ToList();
            }

            if (running.Count > 0)
            {
                SimpleLog.Write(LogLevel.Info, $"Crontab scheduler: waiting for the termination of {running.Count} running actions.");
                var all = Task.WhenAll(running.Select(r => r.Value));
                if (timeoutSeconds.HasValue)
                {
                    var completed   = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds.Value)))
                                        .ConfigureAwait(false);
                    result          = completed == all;
                }
                else
                {
                    await all.ConfigureAwait(false);
                }

                if (!result)
                {
                    var stillRunning = running.Where(r => !r.Value.IsCompleted).Select(r => r.Key);
                    SimpleLog.Write(LogLevel.Warning, $"Crontab scheduler: stop timeout ({timeoutSeconds} seconds) expired, actions "
                        + $"still running: {string.Join(", ", stillRunning)}.");
                }
            }

            return result;
        }

        /// <summary>
        /// Stops the scheduler (<see cref="Stop"/>), unless it has already been stopped; afterwards it cannot be started again.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the scheduler when disposed explicitly, unless <see cref="Stop"/> or <see cref="StopAsync"/> has already been
        /// called after the last <see cref="Start"/>: the actions still running after that stop (timeout expired) are not
        /// awaited again.
        /// </summary>
        /// <param name="disposing">True if called by <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                bool stop;
                lock (_lock)
                {
                    _disposed   = true;
                    stop        = !_stopRequested;
                }
                if (stop)
                {
                    Stop();
                }
            }
        }

        #endregion

        #region Loop

        /// <summary>
        /// Loop of the scheduler: wait for the next scheduled time (never longer than <see cref="MaxWaitSeconds"/>), then a
        /// wake-up (<see cref="RunCycle"/>), until the token is signalled.
        /// </summary>
        /// <param name="startUtc">Instant of the start, in UTC: the scheduled times before it are not recovered.</param>
        /// <param name="token">Token signalled when the scheduler is stopped.</param>
        private async Task RunLoopAsync(DateTime startUtc, CancellationToken token)
        {
            var lastUtc = startUtc;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var delay = GetDelay(NextOccurrenceUtc, DateTime.UtcNow, TimeSpan.FromSeconds(MaxWaitSeconds));
                    await Task.Delay(delay, token).ConfigureAwait(false);

                    var nowUtc = DateTime.UtcNow;
                    RunCycle(lastUtc, nowUtc, token);
                    lastUtc = nowUtc;
                }
            }
            catch (OperationCanceledException)
            {
                // Stop requested during the wait.
            }
            catch (Exception ex)
            {
                // Not expected (RunCycle does not throw exceptions): written to the log, the scheduler stops scheduling.
                SimpleLog.LogError(ex, true);
            }
        }

        /// <summary>
        /// Calculates the wait until the next wake-up: until the next scheduled time, but not longer than the given maximum (to
        /// reload the modified file) and never negative.
        /// </summary>
        /// <param name="nextOccurrenceUtc">Next scheduled time, in UTC; null if there are no scheduled times.</param>
        /// <param name="nowUtc">Current instant, in UTC.</param>
        /// <param name="maximum">Maximum wait.</param>
        /// <returns>The wait, between zero and <paramref name="maximum"/>.</returns>
        public static TimeSpan GetDelay(DateTime? nextOccurrenceUtc, DateTime nowUtc, TimeSpan maximum)
        {
            var result = maximum;

            if (nextOccurrenceUtc.HasValue)
            {
                var untilNext = nextOccurrenceUtc.Value - nowUtc;
                if (untilNext < TimeSpan.Zero)
                {
                    result = TimeSpan.Zero;
                }
                else if (untilNext < maximum)
                {
                    result = untilNext;
                }
            }

            return result;
        }

        /// <summary>
        /// A wake-up of the scheduler, without throwing exceptions: reloads the file if modified, starts the actions with a
        /// scheduled time between <paramref name="lastUtc"/> (excluded) and <paramref name="nowUtc"/> (included) and
        /// recalculates the next scheduled time.
        /// </summary>
        /// <param name="lastUtc">Instant of the previous wake-up, in UTC.</param>
        /// <param name="nowUtc">Instant of the current wake-up, in UTC.</param>
        /// <param name="token">Token passed to the actions, signalled when the scheduler is stopped.</param>
        internal void RunCycle(DateTime lastUtc, DateTime nowUtc, CancellationToken token)
        {
            try
            {
                ReloadCrontab(false);
                var due = Crontab.GetDueActions(Crontab.Entries, lastUtc, nowUtc, Crontab.Zone, Crontab.ActionComparer);
                foreach (var item in due)
                {
                    StartAction(item.Action, item.ScheduledTimeUtc, item.Occurrences - 1, token);
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
            }

            UpdateNextOccurrence(nowUtc);
        }

        /// <summary>
        /// Reads the file if modified (or always, if <paramref name="force"/>), writes the result to the log and raises
        /// <see cref="CrontabReloaded"/>; the lines in use remain valid if the reading fails.
        /// </summary>
        /// <param name="force">True to read the file even if not modified (at the start).</param>
        private void ReloadCrontab(bool force)
        {
            try
            {
                if (force)
                {
                    Crontab.Invalidate();
                }
                if (Crontab.ReloadIfChanged())
                {
                    var filePath    = Crontab.FilePath;
                    var fileExists  = Crontab.FileExists;
                    var entries     = Crontab.Entries;
                    var errors      = Crontab.Errors;

                    SimpleLog.Write(LogLevel.Info, $"Crontab file '{filePath}' {(fileExists ? "loaded" : "missing")}, lines in use: "
                        + $"{entries.Count}.");
                    if (errors.Count > 0)
                    {
                        SimpleLog.Write(LogLevel.Warning, $"The crontab file '{filePath}' contains errors, the lines indicated are "
                            + $"ignored. {string.Join(" ", errors)}");
                    }
                    RaiseEvent(CrontabReloaded, new CrontabReloadedEventArgs(filePath, fileExists, entries.Count, errors));
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
            }
        }

        /// <summary>
        /// Recalculates <see cref="NextOccurrenceUtc"/> and <see cref="NextOccurrenceActions"/> after the given instant.
        /// </summary>
        /// <param name="fromUtc">Instant from which the next scheduled time is calculated, in UTC.</param>
        private void UpdateNextOccurrence(DateTime fromUtc)
        {
            DateTime? next  = null;
            var actions     = new List<string>();

            try
            {
                var entries = Crontab.Entries;
                var zone    = Crontab.Zone;
                next        = Crontab.GetNextOccurrence(entries, fromUtc, zone);
                if (next.HasValue)
                {
                    actions = entries
                        .Where(e => e.GetNextOccurrence(fromUtc, zone) == next)
                        .Select(e => e.Action)
                        .Distinct(Crontab.ActionComparer)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
            }

            lock (_lock)
            {
                _nextOccurrenceUtc      = next;
                _nextOccurrenceActions  = actions;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Starts the execution of an action in a new task, unless it is still running from its previous scheduled time (in
        /// that case the scheduled time is skipped, see <see cref="ScheduledActionSkipped"/>).
        /// </summary>
        /// <param name="action">Name of the action.</param>
        /// <param name="scheduledTimeUtc">Scheduled time, in UTC.</param>
        /// <param name="missedOccurrences">Scheduled times missed in the same interval.</param>
        /// <param name="token">Token signalled when the scheduler is stopped.</param>
        private void StartAction(string action, DateTime scheduledTimeUtc, int missedOccurrences, CancellationToken token)
        {
            var skipped = false;

            lock (_lock)
            {
                if (_running.TryGetValue(action, out var previous) && !previous.IsCompleted)
                {
                    skipped = true;
                }
                else
                {
                    var args            = new ScheduledActionTriggeredEventArgs(action, scheduledTimeUtc, missedOccurrences, token);
                    _running[action]    = Task.Run(() => ExecuteActionAsync(args));
                }
            }

            if (skipped)
            {
                SimpleLog.Write(LogLevel.Warning, $"Scheduled execution of the action '{action}' skipped: it is still running "
                    + "from its previous scheduled time.");
                RaiseEvent(ScheduledActionSkipped, new ScheduledActionSkippedEventArgs(action, scheduledTimeUtc, SKIPREASONSTILLRUNNING));
            }
            else
            {
                SimpleLog.Write(LogLevel.Info, $"Scheduled execution of the action '{action}'.");
                if (missedOccurrences > 0)
                {
                    SimpleLog.Write(LogLevel.Warning, $"Action '{action}': {missedOccurrences} scheduled times missed (late wake-up), "
                        + "executed once.");
                }
            }
        }

        /// <summary>
        /// Executes an action: raises <see cref="ScheduledActionTriggered"/> (every handler separately) and awaits
        /// <see cref="ActionHandler"/>, without throwing exceptions.
        /// </summary>
        /// <param name="args">Arguments of the execution.</param>
        private async Task ExecuteActionAsync(ScheduledActionTriggeredEventArgs args)
        {
            RaiseEvent(ScheduledActionTriggered, args);

            var handler = ActionHandler;
            if (handler != null)
            {
                try
                {
                    var task = handler(args);
                    if (task != null)
                    {
                        await task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex, true);
                }
            }
        }

        /// <summary>
        /// Raises an event calling every handler separately: the exception of a handler is written to the log and does not
        /// prevent the others.
        /// </summary>
        /// <typeparam name="TEventArgs">Type of the arguments.</typeparam>
        /// <param name="handlers">Handlers of the event (the delegate of the event); nothing happens if null.</param>
        /// <param name="args">Arguments of the event.</param>
        private void RaiseEvent<TEventArgs>(EventHandler<TEventArgs> handlers, TEventArgs args)
        {
            if (handlers != null)
            {
                foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
                {
                    try
                    {
                        handler(this, args);
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.LogError(ex, true);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
