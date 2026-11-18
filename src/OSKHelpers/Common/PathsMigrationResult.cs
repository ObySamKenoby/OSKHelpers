using System.Collections.Generic;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Result of <see cref="Paths.MigrateToAppdata"/>.
    /// </summary>
    public sealed class PathsMigrationResult
    {
        #region Properties

        /// <summary>
        /// Outcome of the migration.
        /// </summary>
        public PathsMigrationStatus Status { get; }

        /// <summary>
        /// Full path of the application directory the items have been moved from.
        /// </summary>
        public string SourceDirectory { get; }

        /// <summary>
        /// Full path of the data directory the items have been moved to; null if it could not be determined.
        /// </summary>
        public string DestinationDirectory { get; }

        /// <summary>
        /// Paths, relative to <see cref="SourceDirectory"/>, of the directories and files moved.<br/>
        /// A directory moved as a whole is listed once; the files of a directory merged into an existing one are listed
        /// one by one.
        /// </summary>
        public IReadOnlyList<string> MovedItems { get; }

        /// <summary>
        /// Paths, relative to <see cref="SourceDirectory"/>, of the files and directories left in the application directory
        /// because an item with the same name already exists in the data directory.
        /// </summary>
        public IReadOnlyList<string> SkippedItems { get; }

        /// <summary>
        /// Problems to report: reasons of a <see cref="PathsMigrationStatus.Failed"/> outcome, items that cannot be moved
        /// (e.g. files in use), additional items with a name that is not valid.
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the result.
        /// </summary>
        /// <param name="status">Outcome of the migration.</param>
        /// <param name="sourceDirectory">Full path of the application directory.</param>
        /// <param name="destinationDirectory">Full path of the data directory.</param>
        /// <param name="movedItems">Relative paths of the items moved.</param>
        /// <param name="skippedItems">Relative paths of the items left in the application directory.</param>
        /// <param name="problems">Problems to report.</param>
        public PathsMigrationResult(PathsMigrationStatus status, string sourceDirectory, string destinationDirectory,
            IReadOnlyList<string> movedItems, IReadOnlyList<string> skippedItems, IReadOnlyList<string> problems)
        {
            Status                  = status;
            SourceDirectory         = sourceDirectory;
            DestinationDirectory    = destinationDirectory;
            MovedItems              = movedItems ?? new List<string>();
            SkippedItems            = skippedItems ?? new List<string>();
            Problems                = problems ?? new List<string>();
        }

        #endregion
    }
}
