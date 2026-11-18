using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Check of the <c>crontab</c> file (<see cref="CheckCrontab"/>), with the custom logic of the application
    /// (<see cref="CheckCrontabCustomLogic"/>) and the coverage of the actions (<see cref="GetCoverage"/>).
    /// </summary>
    public partial class Crontab
    {
        #region Constants

        /// <summary>
        /// Prefix of the error lines of <see cref="CheckCrontab"/>: an error makes the file invalid.
        /// </summary>
        public const string ERRORPREFIX = "ERROR: ";

        /// <summary>
        /// Prefix of the warning lines of <see cref="CheckCrontab"/>: a warning does not make the file invalid.
        /// </summary>
        public const string WARNINGPREFIX = "WARNING: ";

        /// <summary>
        /// Hours of the first period in which the executions are counted.
        /// </summary>
        public const int DAYHOURS = 24;

        /// <summary>
        /// Days of the second period in which the executions are counted, used to find the least frequent actions.
        /// </summary>
        public const int WEEKDAYS = 7;

        /// <summary>
        /// Format of the date and time of the next execution.
        /// </summary>
        private const string DATETIMEFORMAT = "dd/MM/yyyy HH:mm";

        /// <summary>
        /// Width of the column of the action in the coverage table.
        /// </summary>
        private const int ACTIONCOLUMNWIDTH = 30;

        #endregion

        #region Methods

        /// <summary>
        /// Checks the file, read at the moment of the call (the lines in use by the scheduler are not used nor replaced),
        /// without throwing exceptions and without writing to the console; the result is also written to the log.<br/>
        /// Descriptive lines, in order:
        /// <list type="number">
        /// <item>file checked and number of valid lines;</item>
        /// <item>an error (<see cref="ERRORPREFIX"/>) for each invalid line or line rejected by <see cref="ValidateAction"/>, or for
        /// the missing or unreadable file;</item>
        /// <item>coverage table of the actions (<see cref="AddCoverageLines"/>, by default with the least frequent actions
        /// highlighted);</item>
        /// <item>lines added by <see cref="CheckCrontabCustomLogic"/>;</item>
        /// <item>final result.</item>
        /// </list>
        /// </summary>
        /// <returns>The result of the check: the file is valid if it has no errors and the custom logic accepts it.</returns>
        public CrontabCheckResult CheckCrontab()
        {
            var lines = new List<string>();
            var valid = false;

            try
            {
                string filePath;
                TimeZoneInfo zone;
                lock (_lock)
                {
                    filePath    = _filePath;
                    zone        = _zone;
                }

                var loaded = ReadAndValidate(filePath);
                lines.Add($"Check of the file '{filePath}', valid lines: {loaded.Entries.Count}.");
                lines.AddRange(loaded.Errors.Select(e => ERRORPREFIX + e));
                AddCoverageLines(loaded.Entries, DateTime.UtcNow, zone, lines);

                bool customValid;
                try
                {
                    customValid = CheckCrontabCustomLogic(loaded.Entries, lines);
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex, true);
                    lines.Add($"{ERRORPREFIX}custom check of the crontab file failed: {ex.Message}");
                    customValid = false;
                }

                valid = loaded.Errors.Count == 0 && customValid;
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
                lines.Add($"{ERRORPREFIX}check of the crontab file failed: {ex.Message}");
                valid = false;
            }

            if (valid)
            {
                lines.Add("The crontab file is valid.");
            }
            else
            {
                lines.Add("The crontab file is not valid: the lines with errors are ignored by the scheduler.");
            }
            SimpleLog.LogLines(valid ? LogLevel.Info : LogLevel.Warning, lines);

            return new CrontabCheckResult(valid, lines);
        }

        /// <summary>
        /// Custom logic of the check, to be overridden in derived classes; called by <see cref="CheckCrontab"/> after the default
        /// checks and before the final result.<br/>
        /// Add to <paramref name="lines"/> the errors (<see cref="ERRORPREFIX"/>) and warnings (<see cref="WARNINGPREFIX"/>) of
        /// the application, e.g. configured actions that are not scheduled (see <see cref="GetCoverage"/> with the list of the
        /// actions) or disabled actions. Exceptions are caught and make the file invalid.
        /// </summary>
        /// <param name="entries">Valid lines of the file, accepted by <see cref="ValidateAction"/>.</param>
        /// <param name="lines">Descriptive lines of the check, to which the lines of the custom logic are added.</param>
        /// <returns>False to make the file invalid; warnings alone should not. By default true, without lines.</returns>
        protected virtual bool CheckCrontabCustomLogic(IReadOnlyList<CrontabEntry> entries, List<string> lines) => true;

        /// <summary>
        /// Adds the coverage table of the actions to the lines of the check, called by <see cref="CheckCrontab"/> after the
        /// errors of the file and before <see cref="CheckCrontabCustomLogic"/>.<br/>
        /// By default the table lists the actions of the valid lines (<see cref="GetCoverage"/>), highlighting the least
        /// frequent ones (<see cref="GetLeastCovered"/>). To be overridden in derived classes to show a table of their own, e.g.
        /// all the actions configured in the application with their state, or to omit it. Exceptions are caught and make the
        /// file invalid.
        /// </summary>
        /// <param name="entries">Valid lines of the file, accepted by <see cref="ValidateAction"/>.</param>
        /// <param name="nowUtc">Instant of the check, in UTC, from which the executions are calculated.</param>
        /// <param name="zone">Time zone in which the expressions are interpreted and the next execution is shown.</param>
        /// <param name="lines">Lines of the check, to which the table is added.</param>
        protected virtual void AddCoverageLines(IReadOnlyList<CrontabEntry> entries, DateTime nowUtc, TimeZoneInfo zone,
            List<string> lines)
            => AddCoverageTable(GetCoverage(entries, nowUtc, zone, null, ActionComparer), zone, lines);

        /// <summary>
        /// Adds the default coverage table (<see cref="AddCoverageLines"/>) to the lines of the check, highlighting the actions
        /// not scheduled and the least frequent ones (<see cref="GetLeastCovered"/>).
        /// </summary>
        /// <param name="coverage">Coverage of the actions.</param>
        /// <param name="zone">Time zone in which the next execution is shown.</param>
        /// <param name="lines">Lines of the check.</param>
        private static void AddCoverageTable(List<CrontabActionCoverage> coverage, TimeZoneInfo zone, List<string> lines)
        {
            var leastCovered = GetLeastCovered(coverage);

            lines.Add("Coverage of the actions:");
            lines.Add($"  {"Action",-ACTIONCOLUMNWIDTH} {"Lines",5}  {"Next execution",-16} {$"{DAYHOURS} hours",8} "
                + $"{$"{WEEKDAYS} days",8}");
            foreach (var item in coverage)
            {
                var next = item.NextOccurrenceUtc.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.NextOccurrenceUtc.Value, zone).ToString(DATETIMEFORMAT)
                    : "-";
                string note;
                if (!item.IsScheduled)
                {
                    note = " [NOT SCHEDULED]";
                }
                else if (leastCovered.Contains(item))
                {
                    note = " [LEAST FREQUENT]";
                }
                else
                {
                    note = string.Empty;
                }
                lines.Add($"  {item.Action,-ACTIONCOLUMNWIDTH} {item.EntriesCount,5}  {next,-16} {item.OccurrencesNextDay,8} "
                    + $"{item.OccurrencesNextWeek,8}{note}");
            }
        }

        /// <summary>
        /// Calculates the coverage of the actions by the lines of the crontab.
        /// </summary>
        /// <param name="entries">Valid lines of the crontab.</param>
        /// <param name="nowUtc">Instant from which the executions are calculated, in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="zone">Time zone in which the expressions are interpreted.</param>
        /// <param name="actions">Actions to calculate, e.g. the ones configured in the application, so that the actions not
        /// scheduled are included too; null means the actions of the lines, in the order of their first line.</param>
        /// <param name="actionComparer">Comparer of the actions (usually <see cref="ActionComparer"/>), used to group the lines
        /// by action; null means <see cref="DefaultActionComparer"/>.</param>
        /// <returns>The coverage of the actions.</returns>
        public static List<CrontabActionCoverage> GetCoverage(IEnumerable<CrontabEntry> entries, DateTime nowUtc,
            TimeZoneInfo zone, IEnumerable<string> actions = null, IEqualityComparer<string> actionComparer = null)
        {
            var result      = new List<CrontabActionCoverage>();
            var comparer    = actionComparer ?? DefaultActionComparer;
            var allEntries  = entries.ToList();
            var names       = (actions ?? allEntries.Select(e => e.Action))
                                .Where(a => !string.IsNullOrWhiteSpace(a))
                                .Select(a => a.Trim())
                                .Distinct(comparer)
                                .ToList();

            foreach (var name in names)
            {
                var actionEntries = allEntries
                    .Where(e => comparer.Equals(e.Action, name))
                    .ToList();
                result.Add(new CrontabActionCoverage(
                    name,
                    actionEntries.Count,
                    GetNextOccurrence(actionEntries, nowUtc, zone),
                    CountOccurrences(actionEntries, nowUtc, nowUtc.AddHours(DAYHOURS), zone),
                    CountOccurrences(actionEntries, nowUtc, nowUtc.AddDays(WEEKDAYS), zone)));
            }

            return result;
        }

        /// <summary>
        /// Counts the distinct execution instants of the lines in the given interval, initial bound excluded and final bound
        /// included: two lines that schedule the same instant produce a single execution.
        /// </summary>
        /// <param name="entries">Lines of the crontab of an action.</param>
        /// <param name="fromUtc">Beginning of the interval (excluded), in UTC.</param>
        /// <param name="toUtc">End of the interval (included), in UTC.</param>
        /// <param name="zone">Time zone in which the expressions are interpreted.</param>
        /// <returns>The number of executions.</returns>
        private static int CountOccurrences(IEnumerable<CrontabEntry> entries, DateTime fromUtc, DateTime toUtc, TimeZoneInfo zone)
            => entries
                .SelectMany(e => e.Expression.GetOccurrences(fromUtc, toUtc, zone, fromInclusive: false, toInclusive: true))
                .Distinct()
                .Count();

        /// <summary>
        /// Finds the scheduled actions with the lowest number of executions in the next <see cref="WEEKDAYS"/> days. If all the
        /// scheduled actions have the same frequency (or there is only one) there is no least frequent action and the result
        /// is empty.
        /// </summary>
        /// <param name="coverage">Coverage of the actions.</param>
        /// <returns>The least frequent actions; empty if the frequencies are all equal.</returns>
        public static List<CrontabActionCoverage> GetLeastCovered(IEnumerable<CrontabActionCoverage> coverage)
        {
            var result      = new List<CrontabActionCoverage>();
            var scheduled   = coverage.Where(c => c.IsScheduled).ToList();

            if (scheduled.Select(c => c.OccurrencesNextWeek).Distinct().Count() > 1)
            {
                var minimum = scheduled.Min(c => c.OccurrencesNextWeek);
                result      = scheduled.Where(c => c.OccurrencesNextWeek == minimum).ToList();
            }

            return result;
        }

        #endregion
    }
}
