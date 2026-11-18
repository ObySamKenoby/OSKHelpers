using System.Collections.Generic;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Result of <see cref="Crontab.CheckCrontab"/>: validity of the <c>crontab</c> file and descriptive lines of its state.
    /// </summary>
    public class CrontabCheckResult
    {
        #region Properties

        /// <summary>
        /// True if the file exists, all its lines are valid (also for <see cref="Crontab"/>'s <c>ValidateAction</c>) and the
        /// custom logic (<c>CheckCrontabCustomLogic</c>) did not reject it; warnings do not make the file invalid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Descriptive lines of the state of the file, in order; errors and warnings start with
        /// <see cref="Crontab.ERRORPREFIX"/> and <see cref="Crontab.WARNINGPREFIX"/>.
        /// </summary>
        public List<string> Lines { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the result of a check.
        /// </summary>
        /// <param name="isValid">True if the file is valid.</param>
        /// <param name="lines">Descriptive lines of the state of the file.</param>
        public CrontabCheckResult(bool isValid, List<string> lines)
        {
            IsValid = isValid;
            Lines   = lines ?? new List<string>();
        }

        #endregion
    }
}
