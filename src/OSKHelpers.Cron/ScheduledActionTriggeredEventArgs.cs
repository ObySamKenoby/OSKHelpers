using System;
using System.Threading;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Arguments of <see cref="CrontabScheduler.ScheduledActionTriggered"/> and of <see cref="CrontabScheduler.ActionHandler"/>:
    /// action to execute and its scheduled time.
    /// </summary>
    public class ScheduledActionTriggeredEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Name of the action, as written in the first line of the crontab that schedules it.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Scheduled time of the execution, in UTC: the last scheduled instant of the action between the previous wake-up of the
        /// scheduler (excluded) and the current one (included).
        /// </summary>
        public DateTime ScheduledTimeUtc { get; }

        /// <summary>
        /// Number of scheduled instants of the action in the same interval besides <see cref="ScheduledTimeUtc"/>: greater than
        /// zero after a late wake-up (e.g. after the system was suspended), when the missed instants produce a single
        /// execution.
        /// </summary>
        public int MissedOccurrences { get; }

        /// <summary>
        /// Signalled when the scheduler is stopped (<see cref="CrontabScheduler.Stop"/>): a long action should observe it and
        /// terminate, since the scheduler waits for the running actions only up to
        /// <see cref="CrontabScheduler.StopTimeoutSeconds"/>.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the arguments of a scheduled execution.
        /// </summary>
        /// <param name="action">Name of the action.</param>
        /// <param name="scheduledTimeUtc">Scheduled time, in UTC.</param>
        /// <param name="missedOccurrences">Scheduled instants missed in the same interval.</param>
        /// <param name="cancellationToken">Token signalled when the scheduler is stopped.</param>
        public ScheduledActionTriggeredEventArgs(string action, DateTime scheduledTimeUtc, int missedOccurrences,
            CancellationToken cancellationToken)
        {
            Action              = action;
            ScheduledTimeUtc    = scheduledTimeUtc;
            MissedOccurrences   = missedOccurrences;
            CancellationToken   = cancellationToken;
        }

        #endregion
    }
}
