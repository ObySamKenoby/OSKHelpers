namespace OSKHelpers.Common
{
    /// <summary>
    /// Outcome of <see cref="Paths.MigrateToAppdata"/>.
    /// </summary>
    public enum PathsMigrationStatus
    {
        /// <summary>
        /// Every item found in the application directory has been moved to the data directory and there are no problems to
        /// report; the data directory is now the base of the default directories.
        /// </summary>
        Migrated,

        /// <summary>
        /// The items have been moved, but some of them have been left in the application directory because they already exist
        /// in the data directory (<see cref="PathsMigrationResult.SkippedItems"/>), or there are problems to report
        /// (<see cref="PathsMigrationResult.Problems"/>), e.g. files in use; the data directory is now the base of the default
        /// directories.
        /// </summary>
        MigratedWithWarnings,

        /// <summary>
        /// No item to move has been found in the application directory: the migration has already been done or the
        /// installation is new. The data directory is now the base of the default directories and they have been created.<br/>
        /// <see cref="PathsMigrationResult.Problems"/> can still report additional items with a name that is not valid.
        /// </summary>
        NothingToMigrate,

        /// <summary>
        /// The application is running in a container: the base of the default directories is already /app/appdata, nothing
        /// has been moved and the default directories have been created.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// The data directory cannot be created or an unexpected error occurred: the base of the default directories has not
        /// been changed.
        /// </summary>
        Failed
    }
}
