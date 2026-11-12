using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSKHelpers.Logging
{
    /// <summary>
    /// Archiving of the old log files of <see cref="SimpleLogger"/>.<br/>
    /// The daily log files of the months older than the current one and the previous <see cref="ArchiveRetentionMonths"/>
    /// months are moved into a monthly zip file named PREFIXLog_YYYY_MM.zip, in the same folder; no log file and no zip file
    /// is ever deleted.
    /// </summary>
    public partial class SimpleLogger
    {
        #region Constants

        /// <summary>
        /// Default number of complete months, before the current one, whose log files are kept uncompressed.
        /// </summary>
        public const int DEFAULTARCHIVERETENTIONMONTHS = 1;

        /// <summary>
        /// Minimum number of complete months, before the current one, whose log files are kept uncompressed.
        /// </summary>
        public const int MINARCHIVERETENTIONMONTHS = 1;

        /// <summary>
        /// Format of the date in the name of the daily log files.
        /// </summary>
        private const string LOGFILEDATEFORMAT = "yyyyMMdd";

        /// <summary>
        /// Format of the month in the name of the zip files (PREFIXLog_YYYY_MM.zip).
        /// </summary>
        private const string ARCHIVEFILEMONTHFORMAT = "yyyy_MM";

        /// <summary>
        /// Extension of the lock file that prevents two processes from updating the same zip file at the same time.
        /// </summary>
        private const string ARCHIVELOCKEXTENSION = ".lock";

        /// <summary>
        /// Extension of the working copy of the zip file, which replaces the zip file only when complete.
        /// </summary>
        private const string ARCHIVETEMPEXTENSION = ".tmp";

        #endregion

        #region Members

        /// <summary>
        /// Serialises the archiving of all the instances of the process (more loggers can share folder and prefix).
        /// </summary>
        private static readonly object _archiveLock = new object();

        private bool _enableArchiving;
        private int _archiveRetentionMonths;

        #endregion

        #region Properties

        /// <summary>
        /// Enables the automatic archiving of the old log files.<br/>
        /// When enabled, the check is started, on a thread pool thread, whenever the name of the log file (<see cref="LogFile"/>)
        /// is regenerated: on the first write, at the change of date and at the change of folder or prefix.<br/>
        /// Enabling the archiving forces the regeneration of the name, so a check is made at the next write.<br/>
        /// Default value: false.
        /// </summary>
        public bool EnableArchiving
        {
            get => _enableArchiving;
            set
            {
                _enableArchiving = value;
                if (_enableArchiving)
                {
                    // _lastDate is set to null to force file name regeneration, which starts the archiving check
                    _lastDate = null;
                }
            }
        }

        /// <summary>
        /// Number of complete months, before the current one, whose log files are kept uncompressed.<br/>
        /// The files of a month are archived, all together, once that month is older than the current month and the previous
        /// <see cref="ArchiveRetentionMonths"/> months: with 1, during 09/2026 the files up to 07/2026 are archived, so the log
        /// folder holds the files of the last 30 - 60 days.<br/>
        /// Values lower than <see cref="MINARCHIVERETENTIONMONTHS"/> are replaced by <see cref="MINARCHIVERETENTIONMONTHS"/>.<br/>
        /// Default value: <see cref="DEFAULTARCHIVERETENTIONMONTHS"/>.
        /// </summary>
        public int ArchiveRetentionMonths
        {
            get => _archiveRetentionMonths;
            set
            {
                _archiveRetentionMonths = Math.Max(value, MINARCHIVERETENTIONMONTHS);
                // _lastDate is set to null to force file name regeneration, which starts the archiving check
                _lastDate = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Archives at once, on the calling thread, the log files of the months older than the current one and the previous
        /// <see cref="ArchiveRetentionMonths"/> months, regardless of <see cref="EnableArchiving"/>.<br/>
        /// The files are grouped by month and added to PREFIXLog_YYYY_MM.zip in <see cref="LogPath"/> (created if missing); an
        /// existing zip file is updated, keeping its entries. An entry with the same name of an existing one is added with a
        /// numeric suffix. A log file is deleted only after the zip file containing it has been saved.<br/>
        /// Errors are written to the log.
        /// </summary>
        /// <returns>True if all the files to archive have been archived; false in case of errors, or when the zip file of a
        /// month is being updated by another process (the files of that month are archived at the next check).</returns>
        public bool ArchiveLogs()
        {
            return ArchiveOldLogFiles(DateTime.Now.Date);
        }

        /// <summary>
        /// Initialises the archiving members; called by the constructor.<br/>
        /// The archiving is disabled by default.
        /// </summary>
        private void InitializeArchiving()
        {
            _enableArchiving        = false;
            _archiveRetentionMonths = DEFAULTARCHIVERETENTIONMONTHS;
        }

        /// <summary>
        /// Called by <see cref="LogFile"/> when the name of the log file is regenerated: when archiving is enabled, starts the
        /// archiving check on a thread pool thread, so that the write that caused the regeneration is not slowed down.
        /// </summary>
        private void OnLogFileChanged()
        {
            if (_enableArchiving)
            {
                Task.Run(() => CheckArchiving());
            }
        }

        /// <summary>
        /// Archives the old log files; runs on a thread pool thread.<br/>
        /// The files that could not be archived (errors, zip file locked by another process) are retried at the next change of
        /// <see cref="LogFile"/>.
        /// </summary>
        private void CheckArchiving()
        {
            try
            {
                ArchiveOldLogFiles(DateTime.Now.Date);
            }
            catch (Exception ex)
            {
                // The method runs on a thread pool thread: an unhandled exception would terminate the process.
                LogError(ex, nameof(SimpleLogger), nameof(CheckArchiving));
            }
        }

        /// <summary>
        /// Archives the log files of this logger dated before the first day of the month that is
        /// <see cref="ArchiveRetentionMonths"/> months before the month of <paramref name="today"/>, grouped by month.
        /// </summary>
        /// <param name="today">Current date.</param>
        /// <returns>True if all the files have been archived.</returns>
        private bool ArchiveOldLogFiles(DateTime today)
        {
            var result = true;

            lock (_archiveLock)
            {
                try
                {
                    var logPath = LogPath;
                    var prefix  = _prefix ?? string.Empty;
                    if (Directory.Exists(logPath))
                    {
                        var limitDate       = new DateTime(today.Year, today.Month, 1).AddMonths(-_archiveRetentionMonths);
                        var filesByMonth    = GetLogFilesToArchive(logPath, prefix, limitDate)
                                                .GroupBy(file => new DateTime(file.Date.Year, file.Date.Month, 1));
                        foreach (var monthFiles in filesByMonth)
                        {
                            var month   = monthFiles.Key.ToString(ARCHIVEFILEMONTHFORMAT, CultureInfo.InvariantCulture);
                            var zipFile = Path.Combine(logPath, $"{prefix}Log_{month}.zip");
                            result &= ArchiveMonthLogFiles(zipFile, monthFiles.Select(file => file.Path).ToList());
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    LogError(ex, nameof(SimpleLogger), nameof(ArchiveLogs));
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the daily log files of this logger (name PREFIXLogYYYYMMDD.txt) dated before <paramref name="limitDate"/>,
        /// ordered by date.<br/>
        /// Files of other prefixes, the file of <see cref="UsePrefixAsLogFile"/> and names with an invalid date are ignored.
        /// </summary>
        /// <param name="logPath">Log folder.</param>
        /// <param name="prefix">Log file prefix.</param>
        /// <param name="limitDate">First date not to archive.</param>
        private static List<(string Path, DateTime Date)> GetLogFilesToArchive(string logPath, string prefix, DateTime limitDate)
        {
            var result          = new List<(string Path, DateTime Date)>();
            // The search pattern selects the candidates; the regular expression excludes the prefixes that start with this one
            var fileNameRegex   = new Regex($"^{Regex.Escape(prefix)}Log(\\d{{8}})\\.txt$", RegexOptions.IgnoreCase);

            foreach (var file in Directory.EnumerateFiles(logPath, $"{prefix}Log*.txt"))
            {
                var match = fileNameRegex.Match(Path.GetFileName(file));
                if (match.Success
                    && DateTime.TryParseExact(match.Groups[1].Value, LOGFILEDATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate)
                    && fileDate < limitDate)
                {
                    result.Add((file, fileDate));
                }
            }

            return result.OrderBy(file => file.Date).ToList();
        }

        /// <summary>
        /// Adds the given log files to the zip file and, once the zip file has been saved, deletes them.<br/>
        /// The zip file is locked against other processes with a lock file; it is updated on a working copy that replaces it
        /// only when complete, so an interruption never damages the entries already archived.
        /// </summary>
        /// <param name="zipFile">Zip file of the month.</param>
        /// <param name="files">Log files to archive.</param>
        /// <returns>True if all the files have been archived; false also when the zip file is being updated by another process
        /// (no file is archived).</returns>
        private bool ArchiveMonthLogFiles(string zipFile, List<string> files)
        {
            var result              = false;
            FileStream lockStream   = null;

            try
            {
                // Exclusive lock (FileShare.None is honoured also by .NET on Unix); the file is deleted when closed
                lockStream = new FileStream(zipFile + ARCHIVELOCKEXTENSION, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                DebugWrite($"{nameof(SimpleLogger)}.{nameof(ArchiveLogs)}: {zipFile} in use by another process, retry at next check.");
            }

            if (lockStream != null)
            {
                using (lockStream)
                {
                    result = UpdateArchive(zipFile, files);
                }
            }

            return result;
        }

        /// <summary>
        /// Adds the log files to a working copy of the zip file, replaces the zip file with it and deletes the archived files.<br/>
        /// Must be called while holding the lock file of the zip file.
        /// </summary>
        /// <param name="zipFile">Zip file of the month.</param>
        /// <param name="files">Log files to archive.</param>
        /// <returns>True if all the files have been archived.</returns>
        private bool UpdateArchive(string zipFile, List<string> files)
        {
            var result      = true;
            var tempFile    = zipFile + ARCHIVETEMPEXTENSION;
            var archived    = new List<string>();

            // A working copy left by an interrupted update is overwritten: the zip file is still the last complete version
            if (File.Exists(zipFile))
            {
                File.Copy(zipFile, tempFile, true);
            }
            else if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            using (var archive = ZipFile.Open(tempFile, ZipArchiveMode.Update))
            {
                var entryNames = new HashSet<string>(archive.Entries.Select(entry => entry.FullName), StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    try
                    {
                        var entryName = GetUniqueEntryName(entryNames, Path.GetFileName(file));
                        archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                        entryNames.Add(entryName);
                        archived.Add(file);
                    }
                    catch (Exception ex)
                    {
                        // The file stays in the log folder and is retried at the next archiving
                        result = false;
                        Write(LogLevel.Error, $"Error in {nameof(SimpleLogger)}.{nameof(ArchiveLogs)}: archiving {file} into {zipFile}: {FormattedException(ex)}");
                    }
                }
            }

            if (archived.Count > 0)
            {
                if (File.Exists(zipFile))
                {
                    File.Replace(tempFile, zipFile, null);
                }
                else
                {
                    File.Move(tempFile, zipFile);
                }

                result &= DeleteArchivedLogFiles(archived);
                DebugWrite($"{nameof(SimpleLogger)}.{nameof(ArchiveLogs)}: {archived.Count} log files archived into {zipFile}.");
            }
            else
            {
                File.Delete(tempFile);
            }

            return result;
        }

        /// <summary>
        /// Deletes the log files already saved in the zip file.<br/>
        /// A file that cannot be deleted is archived again at the next archiving, as a new entry with a numeric suffix.
        /// </summary>
        /// <param name="files">Archived log files.</param>
        /// <returns>True if all the files have been deleted.</returns>
        private bool DeleteArchivedLogFiles(List<string> files)
        {
            var result = true;

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    result = false;
                    Write(LogLevel.Error, $"Error in {nameof(SimpleLogger)}.{nameof(ArchiveLogs)}: deleting archived file {file}: {FormattedException(ex)}");
                }
            }

            return result;
        }

        /// <summary>
        /// Returns <paramref name="fileName"/> if no entry has that name, otherwise the first free name of the form
        /// NAME_N.EXT, so that an existing entry is never overwritten.
        /// </summary>
        /// <param name="entryNames">Names of the entries already in the zip file.</param>
        /// <param name="fileName">Name of the file to add.</param>
        private static string GetUniqueEntryName(HashSet<string> entryNames, string fileName)
        {
            var result      = fileName;
            var name        = Path.GetFileNameWithoutExtension(fileName);
            var extension   = Path.GetExtension(fileName);
            var counter     = 1;

            while (entryNames.Contains(result))
            {
                result = $"{name}_{counter}{extension}";
                counter++;
            }

            return result;
        }

        #endregion
    }
}
