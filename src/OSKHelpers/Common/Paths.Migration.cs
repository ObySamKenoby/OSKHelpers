using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OSKHelpers.Common
{
    public partial class Paths
    {
        #region Methods

        #region Migration to Appdata

        // The parameter sourceDirectory exists only in the Debug build, but its documentation cannot be excluded with #if
        // without splitting the XML comment: the warning for its tag is disabled for the Release build
#pragma warning disable CS1572
        /// <summary>
        /// Moves the data of the application from the old structure, directly inside <see cref="AssemblyPath"/>, to the
        /// <i>AssemblyPath\Appdata</i> directory, then uses it as the base of all default directories and creates them,
        /// as <see cref="UseLocalAppdataDirectory"/> does.
        /// </summary>
        /// <remarks>
        /// Items moved: the directories Backups, Configs, Database, Logs, Output, Scripts and Temp, the file Settings.ini and
        /// <paramref name="additionalItems"/>. The files distributed with the application (e.g. SettingsTemplate.ini) are not
        /// moved.<br/>
        /// A directory not present in the data directory is moved as a whole; otherwise, or if it cannot be moved as a whole,
        /// its content is moved file by file. A file already present in the data directory is not overwritten, unless
        /// <paramref name="overwrite"/> is true: it is left in the application directory and listed in
        /// <see cref="PathsMigrationResult.SkippedItems"/>. A file that cannot be moved (e.g. in use) is left in the
        /// application directory and reported in <see cref="PathsMigrationResult.Problems"/>; the other items are moved
        /// anyway.<br/>
        /// The method never throws and can be called at every start: once the migration is done it only activates the data
        /// directory. It must be called first, before any use of SimpleLog, IniFileHelper, JsonSettings, CredentialProtector
        /// or MailWorker, which compute or use the default paths. Nothing is logged while the items are moved, so that the
        /// old Logs directory is not created again: the outcome is logged once the data directory has been activated.<br/>
        /// In a container nothing is moved and the default directories under /app/appdata are created.<br/>
        /// The base of the default directories is not changed only if the outcome is <see cref="PathsMigrationStatus.Failed"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var migration = Paths.MigrateToAppdata(new[] { "Templates" });
        /// if (migration.Status == PathsMigrationStatus.Failed)
        /// {
        ///     // The data is still in AssemblyPath
        /// }
        /// </code>
        /// </example>
        /// <param name="additionalItems">Names of files or directories of the application to move as well, relative to the
        /// application directory (e.g. <c>Templates</c>, <c>Data/Archive.db</c>). Absolute paths, names containing <c>..</c>
        /// and names starting with <c>Appdata</c> are ignored and reported in <see cref="PathsMigrationResult.Problems"/>.</param>
        /// <param name="overwrite">If true, a file already present in the data directory is replaced by the one of the
        /// application directory.</param>
        /// <param name="sourceDirectory">For testing only, available in the Debug build: directory to use instead of
        /// <see cref="AssemblyPath"/> as the application directory; the data directory becomes
        /// <i>sourceDirectory\Appdata</i>.</param>
        /// <returns>The outcome of the migration.</returns>
#if DEBUG
        public static PathsMigrationResult MigrateToAppdata(IEnumerable<string> additionalItems = null, bool overwrite = false,
            string sourceDirectory = null)
#else
        public static PathsMigrationResult MigrateToAppdata(IEnumerable<string> additionalItems = null, bool overwrite = false)
#endif
#pragma warning restore CS1572
        {
#if !DEBUG
            // Only the Debug build lets the tests replace the application directory
            string sourceDirectory = null;
#endif
            PathsMigrationStatus status;
            var source      = string.IsNullOrWhiteSpace(sourceDirectory) ? AssemblyPath : sourceDirectory;
            var destination = _isDockerized ? AppdataDirectory : Path.Combine(source, LOCALAPPDATADIRECTORYNAME);
            var state       = new PathsMigrationState(source, destination, overwrite);

            try
            {
                if (_isDockerized)
                {
                    ActivateAppdataDirectory(destination);
                    status = PathsMigrationStatus.NotApplicable;
                }
                else if (TryCreateMigrationDirectory(state))
                {
                    var itemsFound = MoveMigrationItems(GetMigrationItems(additionalItems, state), state);
                    ActivateAppdataDirectory(destination);
                    status = GetMigrationStatus(itemsFound, state);
                }
                else
                {
                    status = PathsMigrationStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                status = PathsMigrationStatus.Failed;
                state.Problems.Add($"Unexpected error: {ex.Message}");
            }

            var result = new PathsMigrationResult(status, source, destination, state.MovedItems, state.SkippedItems, state.Problems);
            LogMigrationResult(result);

            return result;
        }

        /// <summary>
        /// Creates the data directory, if missing.
        /// </summary>
        /// <param name="state">State of the migration.</param>
        /// <returns>True if the data directory exists after the call.</returns>
        private static bool TryCreateMigrationDirectory(PathsMigrationState state)
        {
            var created = false;

            try
            {
                Directory.CreateDirectory(state.DestinationRoot);
                created = true;
            }
            catch (Exception ex)
            {
                state.Problems.Add($"Unable to create the data directory {state.DestinationRoot}: {ex.Message}");
            }

            return created;
        }

        /// <summary>
        /// Returns the names of the items to move: the default ones followed by the valid additional items.
        /// </summary>
        /// <param name="additionalItems">Additional items requested by the application; may be null.</param>
        /// <param name="state">State of the migration, which receives the additional items that are not valid.</param>
        /// <returns>The names of the items, relative to the application directory, without duplicates.</returns>
        private static List<string> GetMigrationItems(IEnumerable<string> additionalItems, PathsMigrationState state)
        {
            var items = new List<string>
            {
                BACKUPSDIRECTORYNAME,
                CONFIGSDIRECTORYNAME,
                DATABASEDIRECTORYNAME,
                LOGSDIRECTORYNAME,
                OUTPUTDIRECTORYNAME,
                SCRIPTSDIRECTORYNAME,
                TEMPDIRECTORYNAME,
                SETTINGSFILENAME
            };

            if (additionalItems != null)
            {
                foreach (var item in additionalItems)
                {
                    var name = NormalizeMigrationItem(item);
                    if (name == null)
                    {
                        state.Problems.Add($"'{item}' ignored: an additional item must be a path relative to the application "
                            + $"directory, without '..' and not inside {LOCALAPPDATADIRECTORYNAME}.");
                    }
                    else if (!items.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        items.Add(name);
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Validates the name of an additional item and returns it with the separators of the current system.
        /// </summary>
        /// <param name="item">Name of the item.</param>
        /// <returns>The normalised name, or null if the name is empty, absolute, contains invalid characters, <c>.</c> or
        /// <c>..</c> segments, or starts with the data directory.</returns>
        private static string NormalizeMigrationItem(string item)
        {
            string result = null;

            if (!string.IsNullOrWhiteSpace(item))
            {
                var name = item.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (name.Length > 0 && name.IndexOfAny(Path.GetInvalidPathChars()) < 0 && !Path.IsPathRooted(name))
                {
                    var segments        = name.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                    var invalidChars    = Path.GetInvalidFileNameChars();
                    var segmentsValid   = segments.All(s => s.Length > 0 && s != "." && s != ".." && s.IndexOfAny(invalidChars) < 0);
                    if (segmentsValid && !string.Equals(segments[0], LOCALAPPDATADIRECTORYNAME, StringComparison.OrdinalIgnoreCase))
                    {
                        result = Path.Combine(segments);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Moves the items present in the application directory to the data directory.
        /// </summary>
        /// <param name="items">Names of the items, relative to the application directory.</param>
        /// <param name="state">State of the migration.</param>
        /// <returns>True if at least one item has been found in the application directory.</returns>
        private static bool MoveMigrationItems(IEnumerable<string> items, PathsMigrationState state)
        {
            var itemsFound = false;

            foreach (var item in items)
            {
                var sourcePath = Path.Combine(state.SourceRoot, item);
                if (Directory.Exists(sourcePath))
                {
                    itemsFound = true;
                    MoveMigrationDirectory(item, state);
                }
                else if (File.Exists(sourcePath))
                {
                    itemsFound = true;
                    MoveMigrationFile(item, state);
                }
            }

            return itemsFound;
        }

        /// <summary>
        /// Moves a directory to the data directory: as a whole if it is not present there, otherwise merging its content.
        /// </summary>
        /// <param name="relativePath">Path of the directory, relative to the application directory.</param>
        /// <param name="state">State of the migration.</param>
        private static void MoveMigrationDirectory(string relativePath, PathsMigrationState state)
        {
            var destinationPath = Path.Combine(state.DestinationRoot, relativePath);

            try
            {
                if (File.Exists(destinationPath))
                {
                    // A file with the same name occupies the destination: the directory cannot be moved
                    state.SkippedItems.Add(relativePath);
                }
                else if (Directory.Exists(destinationPath))
                {
                    MergeMigrationDirectory(relativePath, state);
                }
                else if (TryMoveWholeDirectory(Path.Combine(state.SourceRoot, relativePath), destinationPath))
                {
                    state.MovedItems.Add(relativePath);
                }
                else
                {
                    // The directory cannot be moved as a whole (e.g. a file in use on Windows): its content is moved file by
                    // file, so that only the files that cannot be moved are left in the application directory
                    MergeMigrationDirectory(relativePath, state);
                }
            }
            catch (Exception ex)
            {
                state.Problems.Add($"{relativePath}: unable to move the directory ({ex.Message}).");
            }
        }

        /// <summary>
        /// Moves a directory with a single operation.
        /// </summary>
        /// <param name="sourcePath">Full path of the directory to move.</param>
        /// <param name="destinationPath">Full path of the destination, which must not exist.</param>
        /// <returns>True if the directory has been moved; false if the operation failed and the directory is unchanged.</returns>
        private static bool TryMoveWholeDirectory(string sourcePath, string destinationPath)
        {
            var moved = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                Directory.Move(sourcePath, destinationPath);
                moved = true;
            }
            catch (IOException)
            {
                moved = false;
            }
            catch (UnauthorizedAccessException)
            {
                moved = false;
            }

            return moved;
        }

        /// <summary>
        /// Moves the content of a directory into the directory with the same name in the data directory, then removes the
        /// directory from the application directory if it is left empty.
        /// </summary>
        /// <param name="relativePath">Path of the directory, relative to the application directory.</param>
        /// <param name="state">State of the migration.</param>
        private static void MergeMigrationDirectory(string relativePath, PathsMigrationState state)
        {
            var sourcePath = Path.Combine(state.SourceRoot, relativePath);

            Directory.CreateDirectory(Path.Combine(state.DestinationRoot, relativePath));
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                MoveMigrationFile(Path.Combine(relativePath, Path.GetFileName(file)), state);
            }
            foreach (var directory in Directory.GetDirectories(sourcePath))
            {
                MoveMigrationDirectory(Path.Combine(relativePath, Path.GetFileName(directory)), state);
            }
            DeleteMigrationDirectoryIfEmpty(relativePath, state);
        }

        /// <summary>
        /// Moves a file to the data directory; an existing file is replaced only if requested.
        /// </summary>
        /// <param name="relativePath">Path of the file, relative to the application directory.</param>
        /// <param name="state">State of the migration.</param>
        private static void MoveMigrationFile(string relativePath, PathsMigrationState state)
        {
            var sourcePath      = Path.Combine(state.SourceRoot, relativePath);
            var destinationPath = Path.Combine(state.DestinationRoot, relativePath);

            try
            {
                if (Directory.Exists(destinationPath) || (File.Exists(destinationPath) && !state.Overwrite))
                {
                    state.SkippedItems.Add(relativePath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    if (File.Exists(destinationPath))
                    {
                        // Copy and delete instead of delete and move: if the source cannot be removed, the destination is
                        // not lost (File.Move with overwrite is not available in .NET Standard 2.0)
                        File.Copy(sourcePath, destinationPath, true);
                        File.Delete(sourcePath);
                    }
                    else
                    {
                        File.Move(sourcePath, destinationPath);
                    }
                    state.MovedItems.Add(relativePath);
                }
            }
            catch (Exception ex)
            {
                state.Problems.Add($"{relativePath}: unable to move the file ({ex.Message}).");
            }
        }

        /// <summary>
        /// Removes a directory of the application directory if it is empty.
        /// </summary>
        /// <param name="relativePath">Path of the directory, relative to the application directory.</param>
        /// <param name="state">State of the migration.</param>
        private static void DeleteMigrationDirectoryIfEmpty(string relativePath, PathsMigrationState state)
        {
            var path = Path.Combine(state.SourceRoot, relativePath);

            try
            {
                if (!Directory.EnumerateFileSystemEntries(path).Any())
                {
                    Directory.Delete(path);
                }
            }
            catch (Exception ex)
            {
                state.Problems.Add($"{relativePath}: unable to remove the empty directory ({ex.Message}).");
            }
        }

        /// <summary>
        /// Returns the outcome of a migration whose data directory has been activated.
        /// </summary>
        /// <param name="itemsFound">True if at least one item has been found in the application directory.</param>
        /// <param name="state">State of the migration.</param>
        /// <returns>The outcome of the migration.</returns>
        private static PathsMigrationStatus GetMigrationStatus(bool itemsFound, PathsMigrationState state)
        {
            PathsMigrationStatus status;

            if (itemsFound)
            {
                status = state.SkippedItems.Count == 0 && state.Problems.Count == 0
                    ? PathsMigrationStatus.Migrated
                    : PathsMigrationStatus.MigratedWithWarnings;
            }
            else
            {
                status = PathsMigrationStatus.NothingToMigrate;
            }

            return status;
        }

        /// <summary>
        /// Writes to the log the outcome of <see cref="MigrateToAppdata"/>.
        /// </summary>
        /// <param name="result">Outcome of the migration.</param>
        private static void LogMigrationResult(PathsMigrationResult result)
        {
            var description = $"Migration of the data from {result.SourceDirectory} to {result.DestinationDirectory}";
            var problems    = result.Problems.Count > 0 ? $" Problems: {string.Join(" ", result.Problems)}" : string.Empty;
            var details     = $" Moved: {string.Join(", ", result.MovedItems)}."
                            + $" Skipped (already present in the data directory): {string.Join(", ", result.SkippedItems)}."
                            + problems;

            if (result.Status == PathsMigrationStatus.Migrated)
            {
                SimpleLog.Write(LogLevel.Info, $"{description}: completed.{details}");
            }
            else if (result.Status == PathsMigrationStatus.MigratedWithWarnings)
            {
                SimpleLog.Write(LogLevel.Warning, $"{description}: completed with warnings.{details}");
            }
            else if (result.Status == PathsMigrationStatus.Failed)
            {
                SimpleLog.Write(LogLevel.Error, $"{description}: failed.{problems}");
            }
            else if (result.Problems.Count > 0)
            {
                // NothingToMigrate with additional items that are not valid
                SimpleLog.Write(LogLevel.Warning, $"{description}: not needed ({result.Status}).{problems}");
            }
            else
            {
                // NothingToMigrate or NotApplicable: normal condition at every start after the migration
                SimpleLog.Write(LogLevel.Debug, $"{description}: not needed ({result.Status}).");
            }
        }

        #endregion

        #endregion
    }
}
