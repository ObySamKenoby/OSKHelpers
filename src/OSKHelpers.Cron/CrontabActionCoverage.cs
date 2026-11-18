using System;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Coverage of an action by the <c>crontab</c> file, calculated by <see cref="Crontab.GetCoverage"/>: lines that schedule
    /// it, next execution and number of executions in the following periods.
    /// </summary>
    public class CrontabActionCoverage
    {
        #region Properties

        /// <summary>
        /// Name of the action.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Number of lines of the crontab that schedule the action.
        /// </summary>
        public int EntriesCount { get; }

        /// <summary>
        /// Next scheduled execution, in UTC; null if the action is not scheduled or has no future executions.
        /// </summary>
        public DateTime? NextOccurrenceUtc { get; }

        /// <summary>
        /// Number of executions in the next 24 hours (<see cref="Crontab.DAYHOURS"/>), without counting twice the instants
        /// shared by more lines.
        /// </summary>
        public int OccurrencesNextDay { get; }

        /// <summary>
        /// Number of executions in the next 7 days (<see cref="Crontab.WEEKDAYS"/>), without counting twice the instants shared
        /// by more lines: measure of the frequency, comparable also between weekly schedules.
        /// </summary>
        public int OccurrencesNextWeek { get; }

        /// <summary>
        /// True if the action is scheduled by at least one line.
        /// </summary>
        public bool IsScheduled => EntriesCount > 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the coverage of an action.
        /// </summary>
        /// <param name="action">Name of the action.</param>
        /// <param name="entriesCount">Lines of the crontab that schedule it.</param>
        /// <param name="nextOccurrenceUtc">Next execution, in UTC.</param>
        /// <param name="occurrencesNextDay">Executions in the next 24 hours.</param>
        /// <param name="occurrencesNextWeek">Executions in the next 7 days.</param>
        public CrontabActionCoverage(string action, int entriesCount, DateTime? nextOccurrenceUtc, int occurrencesNextDay,
            int occurrencesNextWeek)
        {
            Action              = action;
            EntriesCount        = entriesCount;
            NextOccurrenceUtc   = nextOccurrenceUtc;
            OccurrencesNextDay  = occurrencesNextDay;
            OccurrencesNextWeek = occurrencesNextWeek;
        }

        #endregion
    }
}
