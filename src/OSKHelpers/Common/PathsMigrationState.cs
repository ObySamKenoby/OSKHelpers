using System.Collections.Generic;

namespace OSKHelpers.Common
{
    /// <summary>
    /// State shared by the steps of <see cref="Paths.MigrateToAppdata"/>: directories, options and lists of the outcome.
    /// </summary>
    internal sealed class PathsMigrationState
    {
        #region Properties

        /// <summary>
        /// Full path of the application directory.
        /// </summary>
        public string SourceRoot { get; }

        /// <summary>
        /// Full path of the data directory.
        /// </summary>
        public string DestinationRoot { get; }

        /// <summary>
        /// True if the files already present in the data directory must be replaced.
        /// </summary>
        public bool Overwrite { get; }

        /// <summary>
        /// Relative paths of the items moved.
        /// </summary>
        public List<string> MovedItems { get; }

        /// <summary>
        /// Relative paths of the items left in the application directory because already present in the data directory.
        /// </summary>
        public List<string> SkippedItems { get; }

        /// <summary>
        /// Problems to report.
        /// </summary>
        public List<string> Problems { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the state of a migration.
        /// </summary>
        /// <param name="sourceRoot">Full path of the application directory.</param>
        /// <param name="destinationRoot">Full path of the data directory.</param>
        /// <param name="overwrite">True if the files already present in the data directory must be replaced.</param>
        public PathsMigrationState(string sourceRoot, string destinationRoot, bool overwrite)
        {
            SourceRoot      = sourceRoot;
            DestinationRoot = destinationRoot;
            Overwrite       = overwrite;
            MovedItems      = new List<string>();
            SkippedItems    = new List<string>();
            Problems        = new List<string>();
        }

        #endregion
    }
}
