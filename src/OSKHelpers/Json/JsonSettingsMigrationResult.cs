using System.Collections.Generic;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Result of <see cref="JsonSettings{T}.MigrateFromIniFile"/>.
    /// </summary>
    /// <typeparam name="T">Settings class derived from <see cref="JsonSettings{T}"/>.</typeparam>
    public sealed class JsonSettingsMigrationResult<T> where T : JsonSettings<T>, new()
    {
        #region Properties

        /// <summary>
        /// Outcome of the migration.
        /// </summary>
        public JsonSettingsMigrationStatus Status { get; }

        /// <summary>
        /// Instance written to the JSON file, not initialised (<see cref="JsonSettings{T}.Init"/> and
        /// <see cref="JsonSettings{T}.CheckIsValid"/> are called by the following <see cref="JsonSettings{T}.Load(bool, System.Text.Json.JsonSerializerOptions)"/>);
        /// null unless <see cref="Status"/> is <see cref="JsonSettingsMigrationStatus.Migrated"/> or
        /// <see cref="JsonSettingsMigrationStatus.MigratedWithWarnings"/>.
        /// </summary>
        public T Settings { get; }

        /// <summary>
        /// Full path of the INI file used as source; null if it could not be determined.
        /// </summary>
        public string IniFilePath { get; }

        /// <summary>
        /// Full path of the JSON file; null if it could not be determined.
        /// </summary>
        public string JsonFilePath { get; }

        /// <summary>
        /// Names of the properties whose value has been taken from the INI file, by the default conversion or by the custom
        /// logic of the derived class.
        /// </summary>
        public IReadOnlyList<string> MigratedProperties { get; }

        /// <summary>
        /// Names of the properties left at their default value: missing in the INI file, ambiguous, of a type not supported by
        /// the default conversion or with a value that cannot be converted, and not handled by the custom logic.
        /// </summary>
        public IReadOnlyList<string> NotMigratedProperties { get; }

        /// <summary>
        /// Problems to report: reasons of a <see cref="JsonSettingsMigrationStatus.Failed"/> outcome, values that cannot be
        /// converted, ambiguous properties, messages added by the custom logic.
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the result.
        /// </summary>
        /// <param name="status">Outcome of the migration.</param>
        /// <param name="settings">Instance written to the JSON file.</param>
        /// <param name="iniFilePath">Full path of the INI file.</param>
        /// <param name="jsonFilePath">Full path of the JSON file.</param>
        /// <param name="migratedProperties">Names of the properties migrated.</param>
        /// <param name="notMigratedProperties">Names of the properties not migrated.</param>
        /// <param name="problems">Problems to report.</param>
        public JsonSettingsMigrationResult(JsonSettingsMigrationStatus status, T settings, string iniFilePath, string jsonFilePath,
            IReadOnlyList<string> migratedProperties, IReadOnlyList<string> notMigratedProperties, IReadOnlyList<string> problems)
        {
            Status                  = status;
            Settings                = settings;
            IniFilePath             = iniFilePath;
            JsonFilePath            = jsonFilePath;
            MigratedProperties      = migratedProperties ?? new List<string>();
            NotMigratedProperties   = notMigratedProperties ?? new List<string>();
            Problems                = problems ?? new List<string>();
        }

        #endregion
    }
}
