namespace OSKHelpers.Json
{
    /// <summary>
    /// Outcome of <see cref="JsonSettings{T}.MigrateFromIniFile"/>.
    /// </summary>
    public enum JsonSettingsMigrationStatus
    {
        /// <summary>
        /// The JSON file has been written: every property has been migrated and there are no problems to report.
        /// </summary>
        Migrated,

        /// <summary>
        /// The JSON file has been written, but some properties have not been migrated
        /// (<see cref="JsonSettingsMigrationResult{T}.NotMigratedProperties"/>) or there are problems to report
        /// (<see cref="JsonSettingsMigrationResult{T}.Problems"/>), also after the custom logic of the derived class.
        /// </summary>
        MigratedWithWarnings,

        /// <summary>
        /// The JSON file already exists and overwriting it has not been requested: the migration has already been done and
        /// nothing has been changed.
        /// </summary>
        DestinationExists,

        /// <summary>
        /// The INI file does not exist: there is nothing to migrate.
        /// </summary>
        SourceNotFound,

        /// <summary>
        /// The migration has not been done: the INI file is not valid, a path is not valid, the custom logic refused the
        /// migration or an error occurred. The JSON file has not been written.
        /// </summary>
        Failed
    }
}
