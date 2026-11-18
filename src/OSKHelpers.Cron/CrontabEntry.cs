using Cronos;
using System;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Valid line of a <c>crontab</c> file: schedule of an action, expressed with the standard five-field cron syntax or with
    /// a macro (e.g. <c>@daily</c>), interpreted with <see cref="CronExpression"/> of Cronos.
    /// </summary>
    public class CrontabEntry
    {
        #region Properties

        /// <summary>
        /// Number of the line in the file (from 1), for the messages.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Cron expression as written in the file, e.g. <c>*/15 8-18 * * 1-5</c> or <c>@hourly</c>.
        /// </summary>
        public string ExpressionText { get; }

        /// <summary>
        /// Interpreted cron expression.
        /// </summary>
        public CronExpression Expression { get; }

        /// <summary>
        /// Name of the action to execute, as written in the file (the rest of the line, it may contain spaces).<br/>
        /// Actions are compared without distinction between upper and lower case.
        /// </summary>
        public string Action { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an already interpreted line of the crontab.
        /// </summary>
        /// <param name="lineNumber">Number of the line in the file.</param>
        /// <param name="expressionText">Cron expression as written in the file.</param>
        /// <param name="expression">Interpreted cron expression.</param>
        /// <param name="action">Name of the action.</param>
        public CrontabEntry(int lineNumber, string expressionText, CronExpression expression, string action)
        {
            LineNumber      = lineNumber;
            ExpressionText  = expressionText;
            Expression      = expression;
            Action          = action;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Next execution scheduled by the line, strictly after the given instant.
        /// </summary>
        /// <param name="fromUtc">Starting instant, in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="zone">Time zone in which the expression is interpreted.</param>
        /// <returns>The instant of the next execution in UTC, null if the expression has no more occurrences.</returns>
        public DateTime? GetNextOccurrence(DateTime fromUtc, TimeZoneInfo zone)
            => Expression.GetNextOccurrence(fromUtc, zone);

        /// <inheritdoc/>
        public override string ToString() => $"line {LineNumber}: {ExpressionText} {Action}";

        #endregion
    }
}
