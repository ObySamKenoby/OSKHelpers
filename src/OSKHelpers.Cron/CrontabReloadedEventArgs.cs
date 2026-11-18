using System;
using System.Collections.Generic;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// Arguments of <see cref="CrontabScheduler.CrontabReloaded"/>: result of a reading of the <c>crontab</c> file.
    /// </summary>
    public class CrontabReloadedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Full path of the file read.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// True if the file exists.
        /// </summary>
        public bool FileExists { get; }

        /// <summary>
        /// Number of valid lines now in use by the scheduler.
        /// </summary>
        public int EntriesCount { get; }

        /// <summary>
        /// A message for each invalid or rejected line, or for the missing or unreadable file; empty if the file is valid.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the arguments of a reading of the file.
        /// </summary>
        /// <param name="filePath">Full path of the file.</param>
        /// <param name="fileExists">True if the file exists.</param>
        /// <param name="entriesCount">Number of valid lines in use.</param>
        /// <param name="errors">Errors of the file.</param>
        public CrontabReloadedEventArgs(string filePath, bool fileExists, int entriesCount, IReadOnlyList<string> errors)
        {
            FilePath        = filePath;
            FileExists      = fileExists;
            EntriesCount    = entriesCount;
            Errors          = errors ?? new List<string>();
        }

        #endregion
    }
}
