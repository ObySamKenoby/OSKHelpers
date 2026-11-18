using OSKHelpers.Common;
using OSKHelpers.INIFile;
using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Migration of the configuration of an application from an INI file, read with <see cref="IniFileHelper"/>, to the JSON
    /// file of <see cref="JsonSettings{T}"/> (<see cref="MigrateFromIniFile"/>).
    /// </summary>
    /// <remarks>
    /// Typical usage at startup, before <see cref="Load(bool, JsonSerializerOptions)"/>:
    /// <code>
    /// var migration = AppSettings.MigrateFromIniFile();      // no effect once the JSON file exists
    /// if (migration.Status == JsonSettingsMigrationStatus.Failed)
    /// {
    ///     // e.g. log migration.Problems and stop, so that Load does not create a default file in place of the INI settings
    /// }
    /// AppSettings.Load();
    /// </code>
    /// </remarks>
    public abstract partial class JsonSettings<T>
    {
        #region Methods

        #region INI file migration

        /// <summary>
        /// Creates the JSON file of the settings from an INI file, without throwing exceptions:
        /// <list type="number">
        /// <item>if the JSON file already exists and <paramref name="overwrite"/> is false nothing is done
        /// (<see cref="JsonSettingsMigrationStatus.DestinationExists"/>), so the method can be called at every startup;</item>
        /// <item>the INI file is read with <see cref="IniFileHelper"/>, ignoring its initial pragmas (they are not applied to
        /// <see cref="SimpleLog"/>), and must be valid;</item>
        /// <item>a new instance is created with the default data (<see cref="CreateDefaultData"/>);</item>
        /// <item>every public read/write property not marked with <see cref="System.Text.Json.Serialization.JsonIgnoreAttribute"/>
        /// (except <see cref="Version"/>) gets the value of the key with the same name, not case sensitive, converted according
        /// to its type: <c>string</c>, <c>bool</c> (TRUE, FALSE, 1, 0), integer types, <c>decimal</c>, <c>double</c>,
        /// <c>float</c>, <c>DateTime</c>, <c>TimeSpan</c>, <c>Guid</c> (with the invariant culture), enums (name or numeric
        /// value) and their nullable forms; one-dimensional arrays, <c>List&lt;X&gt;</c> and the generic collection interfaces
        /// of these types get the elements of the array with the same name (<c>*NAME = value</c>), collections of
        /// <see cref="DayOfWeek"/> also the value of a single key in the format of <see cref="DaysOfWeekParameter"/>;</item>
        /// <item>the properties missing in the INI file, of a type not supported, with a value that cannot be converted or
        /// ambiguous (several properties with names that differ only in case) keep their default value;</item>
        /// <item><see cref="MigrateFromIniFileCustomLogic"/> of the derived class handles the special cases or refuses the
        /// migration;</item>
        /// <item>the instance is saved with <see cref="Save(string, JsonSerializerOptions, bool)"/> (an existing JSON file is
        /// first copied to <see cref="Paths.DefaultBackupDirectory"/>).</item>
        /// </list>
        /// The INI file is never modified. The instance is not initialised (<see cref="Init"/>, <see cref="CheckIsValid"/>) and
        /// is not assigned to the static property of the current settings (<see cref="CurrentSettingsAttribute"/>): this is
        /// done by the following <see cref="Load(string, bool, JsonSerializerOptions)"/>. The outcome is written to the log.
        /// </summary>
        /// <param name="iniFilePath">
        /// Full path of the INI file; if null or empty, <see cref="IniFileHelper.INIFILEFULLPATH"/> is used or, if it does not
        /// exist, <see cref="Paths.DefaultSettingsFile"/>.
        /// </param>
        /// <param name="jsonFilePath">
        /// Path of the JSON file, absolute or relative to <see cref="Paths.DefaultConfigsDirectory"/> (default
        /// <see cref="DEFAULTFILENAME"/>; null is handled as the default). For a relative path the default configuration
        /// directory is created if missing; the directory of an absolute path must exist.
        /// </param>
        /// <param name="overwrite">True to migrate the INI file even if the JSON file already exists, replacing it.</param>
        /// <param name="options">
        /// Options of the serialiser used to write the JSON file; if null those of
        /// <see cref="Save(string, JsonSerializerOptions, bool)"/> are used.
        /// </param>
        /// <returns>The outcome of the migration, see <see cref="JsonSettingsMigrationResult{T}"/>.</returns>
        public static JsonSettingsMigrationResult<T> MigrateFromIniFile(string iniFilePath = null, string jsonFilePath = DEFAULTFILENAME,
            bool overwrite = false, JsonSerializerOptions options = null)
        {
            JsonSettingsMigrationStatus status;
            T settings                  = null;
            string resolvedIniFilePath  = null;
            string resolvedJsonFilePath = null;
            var migratedProperties      = new List<string>();
            var notMigratedProperties   = new List<string>();
            var problems                = new List<string>();

            try
            {
                resolvedIniFilePath     = ResolveMigrationIniFilePath(iniFilePath);
                resolvedJsonFilePath    = ResolveMigrationJsonFilePath(jsonFilePath ?? DEFAULTFILENAME);

                if (File.Exists(resolvedJsonFilePath) && !overwrite)
                {
                    status = JsonSettingsMigrationStatus.DestinationExists;
                }
                else if (!File.Exists(resolvedIniFilePath))
                {
                    status = JsonSettingsMigrationStatus.SourceNotFound;
                }
                else
                {
                    settings = MigrateIniFileCore(resolvedIniFilePath, resolvedJsonFilePath, options, migratedProperties,
                        notMigratedProperties, problems);
                    if (settings == null)
                    {
                        status = JsonSettingsMigrationStatus.Failed;
                    }
                    else if (notMigratedProperties.Count == 0 && problems.Count == 0)
                    {
                        status = JsonSettingsMigrationStatus.Migrated;
                    }
                    else
                    {
                        status = JsonSettingsMigrationStatus.MigratedWithWarnings;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                problems.Add($"Migration of the INI file failed: {ex.Message}");
                settings    = null;
                status      = JsonSettingsMigrationStatus.Failed;
            }

            var result = new JsonSettingsMigrationResult<T>(status, settings, resolvedIniFilePath, resolvedJsonFilePath,
                migratedProperties, notMigratedProperties, problems);
            LogMigrationResult(result);

            return result;
        }

        /// <summary>
        /// Custom logic of the derived class, called by <see cref="MigrateFromIniFile"/> on the new instance after the default
        /// conversion of the properties and before the JSON file is written.<br/>
        /// It can:
        /// <list type="bullet">
        /// <item>assign the properties not migrated (<paramref name="notMigratedProperties"/>), e.g. reading from
        /// <paramref name="iniFile"/> a key with a different name or a value of a type not supported, and remove them from
        /// <paramref name="notMigratedProperties"/> (they are then reported as migrated);</item>
        /// <item>correct the values converted;</item>
        /// <item>add messages to <paramref name="problems"/> (the outcome becomes
        /// <see cref="JsonSettingsMigrationStatus.MigratedWithWarnings"/>);</item>
        /// <item>refuse the migration, returning false: the JSON file is not written.</item>
        /// </list>
        /// The default implementation does nothing and returns true. It must never be called directly.
        /// </summary>
        /// <param name="iniFile">INI file read, without its initial pragmas.</param>
        /// <param name="notMigratedProperties">
        /// Names of the properties left at their default value by the default conversion: missing in the INI file, ambiguous,
        /// of a type not supported or with a value that cannot be converted.
        /// </param>
        /// <param name="problems">Messages to report to the caller (<see cref="JsonSettingsMigrationResult{T}.Problems"/>).</param>
        /// <returns>True to write the JSON file, false to cancel the migration.</returns>
        protected virtual bool MigrateFromIniFileCustomLogic(IniFileHelper iniFile, ICollection<string> notMigratedProperties,
            List<string> problems) => true;

        /// <summary>
        /// Reads the INI file, creates the instance, applies the default conversion and the custom logic and writes the JSON
        /// file (steps 2-7 of <see cref="MigrateFromIniFile"/>).
        /// </summary>
        /// <param name="iniFilePath">Full path of the existing INI file.</param>
        /// <param name="jsonFilePath">Full path of the JSON file.</param>
        /// <param name="options">Options of the serialiser.</param>
        /// <param name="migratedProperties">Names of the properties migrated, updated by the method.</param>
        /// <param name="notMigratedProperties">Names of the properties not migrated, updated by the method.</param>
        /// <param name="problems">Problems to report, updated by the method.</param>
        /// <returns>The instance written to the JSON file, or null if the migration failed.</returns>
        private static T MigrateIniFileCore(string iniFilePath, string jsonFilePath, JsonSerializerOptions options,
            List<string> migratedProperties, List<string> notMigratedProperties, List<string> problems)
        {
            T result    = null;
            var iniFile = ReadIniFileWithoutPragmas(iniFilePath);

            if (iniFile.IniFileRead)
            {
                // The instance is created as for a new file, so that the properties not migrated get the default data.
                var settings        = new T();
                settings.IsNew      = true;
                settings.Updated    = true;
                settings.CreateDefaultData();
                settings.ApplyIniFileValues(iniFile, migratedProperties, notMigratedProperties, problems);

                var notMigratedByDefault = notMigratedProperties.ToList();
                if (settings.MigrateFromIniFileCustomLogic(iniFile, notMigratedProperties, problems))
                {
                    // The properties removed from the list by the custom logic have been handled by it.
                    migratedProperties.AddRange(notMigratedByDefault.Where(p => !notMigratedProperties.Contains(p)));
                    settings.Save(jsonFilePath, options);
                    result = settings;
                }
                else
                {
                    problems.Add($"The migration of the file {iniFilePath} has been refused by the custom logic.");
                }
            }
            else
            {
                problems.Add($"The INI file {iniFilePath} is not valid: see the log for the lines with errors.");
            }

            return result;
        }

        /// <summary>
        /// Assigns to this instance the values of the INI file with the default conversion (see <see cref="MigrateFromIniFile"/>).
        /// </summary>
        /// <param name="iniFile">INI file read.</param>
        /// <param name="migratedProperties">Names of the properties migrated, updated by the method.</param>
        /// <param name="notMigratedProperties">Names of the properties not migrated, updated by the method.</param>
        /// <param name="problems">Problems to report, updated by the method.</param>
        private void ApplyIniFileValues(IniFileHelper iniFile, List<string> migratedProperties, List<string> notMigratedProperties,
            List<string> problems)
        {
            var properties      = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => IsSettingsProperty(p) && p.Name != nameof(Version))
                                    .ToList();
            // The keys of the INI file are not case sensitive: properties whose names differ only in case cannot be matched.
            var ambiguousNames  = properties.GroupBy(p => p.Name.ToUpperInvariant())
                                    .Where(g => g.Count() > 1)
                                    .SelectMany(g => g.Select(p => p.Name))
                                    .ToList();

            if (ambiguousNames.Count > 0)
            {
                notMigratedProperties.AddRange(ambiguousNames);
                problems.Add($"Properties whose names differ only in case, not migrated: {string.Join(", ", ambiguousNames)}.");
            }

            foreach (var property in properties.Where(p => !ambiguousNames.Contains(p.Name)))
            {
                if (TryReadIniFileValue(iniFile, property, problems, out object value))
                {
                    property.SetValue(this, value);
                    migratedProperties.Add(property.Name);
                }
                else
                {
                    notMigratedProperties.Add(property.Name);
                }
            }
        }

        /// <summary>
        /// Reads from the INI file the value of a property with the default conversion: a collection from the array with the
        /// same name (or, for days of the week, from the key), any other supported type from the key with the same name.
        /// </summary>
        /// <param name="iniFile">INI file read.</param>
        /// <param name="property">Property to assign.</param>
        /// <param name="problems">Problems to report, updated by the method when a value cannot be converted.</param>
        /// <param name="value">The value converted; null if the result is false.</param>
        /// <returns>
        /// True if the value has been converted; false if the key is missing, the type is not supported or the value cannot be
        /// converted.
        /// </returns>
        private static bool TryReadIniFileValue(IniFileHelper iniFile, PropertyInfo property, List<string> problems, out object value)
        {
            var converted   = false;
            var name        = property.Name;
            var type        = property.PropertyType;
            value           = null;

            if (IniValueConverter.IsSupportedCollection(type, out Type elementType))
            {
                if (iniFile.HasArray(name))
                {
                    converted = IniValueConverter.TryConvertArray(type, elementType, iniFile.Array(name), out value,
                        out string invalidElement);
                    if (!converted)
                    {
                        problems.Add($"Array *{name}: the element '{invalidElement}' is not a valid {GetMigrationTypeName(elementType)}, "
                            + $"the property {name} keeps its default value.");
                    }
                }
                else if (iniFile.HasKey(name) && IniValueConverter.IsDayOfWeek(elementType))
                {
                    var text    = iniFile.GetString(name);
                    converted   = IniValueConverter.TryConvertDaysOfWeek(type, elementType, text, out value);
                    if (!converted)
                    {
                        problems.Add($"Key {name}: the value '{text}' is not a valid list of days of the week, the property {name} "
                            + "keeps its default value.");
                    }
                }
                else if (iniFile.HasKey(name))
                {
                    problems.Add($"Key {name}: the property {name} is a collection and requires an array (*{name} = value), "
                        + "the property keeps its default value.");
                }
            }
            else if (IniValueConverter.IsSupportedScalar(type))
            {
                if (iniFile.HasKey(name))
                {
                    var text    = iniFile.GetString(name);
                    converted   = IniValueConverter.TryConvertValue(type, text, out value);
                    if (!converted)
                    {
                        problems.Add($"Key {name}: the value '{text}' is not a valid {GetMigrationTypeName(type)}, the property {name} "
                            + "keeps its default value.");
                    }
                }
                else if (iniFile.HasArray(name))
                {
                    problems.Add($"Array *{name}: the property {name} requires a single key ({name} = value), the property keeps "
                        + "its default value.");
                }
            }

            return converted;
        }

        /// <summary>
        /// Reads the INI file ignoring its initial pragmas, so that they are not applied to <see cref="SimpleLog"/> during the
        /// migration.
        /// </summary>
        /// <param name="iniFilePath">Full path of the INI file.</param>
        /// <returns>The INI file read; <see cref="IniFileHelper.IniFileRead"/> tells whether it is valid.</returns>
        private static IniFileHelper ReadIniFileWithoutPragmas(string iniFilePath)
        {
            var lines       = File.ReadAllLines(iniFilePath).ToList();
            var firstLine   = 0;
            var iniFile     = new IniFileHelper();

            while (firstLine < lines.Count && IniFileHelper.IsPragma((lines[firstLine] ?? string.Empty).Trim()))
            {
                firstLine++;
            }
            iniFile.Parse(lines.Skip(firstLine).ToList());

            return iniFile;
        }

        /// <summary>
        /// Returns the full path of the INI file to migrate.
        /// </summary>
        /// <param name="iniFilePath">Path passed to <see cref="MigrateFromIniFile"/>.</param>
        /// <returns>
        /// The full path of <paramref name="iniFilePath"/>; if it is null or empty, <see cref="IniFileHelper.INIFILEFULLPATH"/>
        /// or, if it does not exist and <see cref="Paths.DefaultSettingsFile"/> exists, the latter.
        /// </returns>
        private static string ResolveMigrationIniFilePath(string iniFilePath)
        {
            string result;

            if (string.IsNullOrWhiteSpace(iniFilePath))
            {
                result = IniFileHelper.INIFILEFULLPATH;
                if (!File.Exists(result) && !string.IsNullOrWhiteSpace(Paths.DefaultSettingsFile) && File.Exists(Paths.DefaultSettingsFile))
                {
                    result = Paths.DefaultSettingsFile;
                }
            }
            else
            {
                result = Path.GetFullPath(iniFilePath.Trim());
            }

            return result;
        }

        /// <summary>
        /// Returns the full path of the JSON file of the migration; for a relative path the default configuration directory is
        /// created if missing.
        /// </summary>
        /// <param name="jsonFilePath">Path passed to <see cref="MigrateFromIniFile"/>.</param>
        /// <returns>The full path of the JSON file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        private static string ResolveMigrationJsonFilePath(string jsonFilePath)
        {
            if (!string.IsNullOrWhiteSpace(jsonFilePath) && !Path.IsPathRooted(jsonFilePath))
            {
                Paths.CheckDefaultConfigsDirectory();
            }

            return ResolveFilePath(jsonFilePath);
        }

        /// <summary>
        /// Returns the name of a type for the messages of the migration (the underlying type for a nullable).
        /// </summary>
        /// <param name="type">Type to describe.</param>
        /// <returns>The name of the type.</returns>
        private static string GetMigrationTypeName(Type type) => (Nullable.GetUnderlyingType(type) ?? type).Name;

        /// <summary>
        /// Writes to the log the outcome of <see cref="MigrateFromIniFile"/>.
        /// </summary>
        /// <param name="result">Outcome of the migration.</param>
        private static void LogMigrationResult(JsonSettingsMigrationResult<T> result)
        {
            var description = $"Migration of the settings {typeof(T).Name} from {result.IniFilePath} to {result.JsonFilePath}";
            var details     = $" Migrated: {string.Join(", ", result.MigratedProperties)}."
                            + $" Not migrated: {string.Join(", ", result.NotMigratedProperties)}."
                            + (result.Problems.Count > 0 ? $" Problems: {string.Join(" ", result.Problems)}" : string.Empty);

            if (result.Status == JsonSettingsMigrationStatus.Migrated)
            {
                SimpleLog.Write(LogLevel.Info, $"{description}: completed.{details}");
            }
            else if (result.Status == JsonSettingsMigrationStatus.MigratedWithWarnings)
            {
                SimpleLog.Write(LogLevel.Warning, $"{description}: completed with warnings.{details}");
            }
            else if (result.Status == JsonSettingsMigrationStatus.Failed)
            {
                SimpleLog.Write(LogLevel.Error, $"{description}: failed. {string.Join(" ", result.Problems)}");
            }
            else
            {
                // DestinationExists or SourceNotFound: normal condition at every startup after the migration.
                SimpleLog.Write(LogLevel.Debug, $"{description}: not needed ({result.Status}).");
            }
        }

        #endregion

        #endregion
    }
}
