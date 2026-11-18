using System;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Arguments of <see cref="CrontabScheduler.ScheduledActionSkipped"/>: scheduled execution not started.
    /// </summary>
    public class ScheduledActionSkippedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Name of the action.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Scheduled time of the skipped execution, in UTC.
        /// </summary>
        public DateTime ScheduledTimeUtc { get; }

        /// <summary>
        /// Reason why the execution was skipped (e.g. <see cref="CrontabScheduler.SKIPREASONSTILLRUNNING"/>).
        /// </summary>
        public string Reason { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the arguments of a skipped execution.
        /// </summary>
        /// <param name="action">Name of the action.</param>
        /// <param name="scheduledTimeUtc">Scheduled time, in UTC.</param>
        /// <param name="reason">Reason why the execution was skipped.</param>
        public ScheduledActionSkippedEventArgs(string action, DateTime scheduledTimeUtc, string reason)
        {
            Action              = action;
            ScheduledTimeUtc    = scheduledTimeUtc;
            Reason              = reason;
        }

        #endregion
    }
}
