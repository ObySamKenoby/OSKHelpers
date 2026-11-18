using Cronos;
using OSKHelpers.Common;
using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OSKHelpers.Cron
{
    /// <summary>
    /// <c>crontab</c> file: every line schedules an action with the standard cron syntax, interpreted with Cronos.<br/>
    /// Format of a line: <c>&lt;minute&gt; &lt;hour&gt; &lt;day of month&gt; &lt;month&gt; &lt;day of week&gt; &lt;Action&gt;</c>,
    /// or <c>&lt;@macro&gt; &lt;Action&gt;</c> (<c>@hourly</c>, <c>@daily</c>, <c>@weekly</c>, <c>@monthly</c>, <c>@yearly</c>,
    /// ...). The action is the rest of the line and may contain spaces. Empty lines and lines starting with <c>#</c> are
    /// ignored; an action may appear in more lines.<br/>
    /// The times are interpreted in <see cref="Zone"/> (by default the local time zone), including the daylight saving time
    /// changes (handled by Cronos).<br/>
    /// The instance keeps the valid lines in use (<see cref="Entries"/>) and reloads them when the file is modified
    /// (<see cref="ReloadIfChanged"/>); the check of the file is in the partial class <c>Crontab.Check.cs</c>
    /// (<see cref="CheckCrontab"/>).<br/>
    /// A derived class can reject lines whose action is not valid for the application (<see cref="ValidateAction"/>) and add
    /// its own checks (<see cref="CheckCrontabCustomLogic"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// # Every 15 minutes from 8 to 18, Monday to Friday
    /// */15 8-18 * * 1-5   Import orders
    /// # Every day at midnight
    /// @daily              Cleanup
    /// </code>
    /// </example>
    public partial class Crontab
    {
        #region Constants

        /// <summary>
        /// Default name of the file, without extension like the Unix <c>crontab</c>.
        /// </summary>
        public const string DEFAULTFILENAME = "crontab";

        /// <summary>
        /// Initial character of a comment line.
        /// </summary>
        public const char COMMENTPREFIX = '#';

        /// <summary>
        /// Initial character of a macro (e.g. <c>@daily</c>), which replaces the five fields of the expression.
        /// </summary>
        public const char MACROPREFIX = '@';

        /// <summary>
        /// Number of fields of a standard cron expression.
        /// </summary>
        public const int STANDARDFIELDS = 5;

        #endregion

        #region Members

        /// <summary>
        /// Protects the file, the time zone, the lines in use and the state of the last reading, since the scheduler and the
        /// application (<see cref="SetFile"/>, <see cref="CheckCrontab"/>) can use the instance from different threads.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Full path of the file.
        /// </summary>
        private string _filePath;

        /// <summary>
        /// Time zone in which the expressions are interpreted.
        /// </summary>
        private TimeZoneInfo _zone;

        /// <summary>
        /// Valid lines in use, replaced as a whole at every reading.
        /// </summary>
        private IReadOnlyList<CrontabEntry> _entries;

        /// <summary>
        /// Errors of the last reading, replaced as a whole at every reading.
        /// </summary>
        private IReadOnlyList<string> _errors;

        /// <summary>
        /// True if the file existed at the last reading.
        /// </summary>
        private bool _fileExists;

        /// <summary>
        /// Last modification date (UTC) of the file at the last reading, to detect its changes; null if the file did not exist.
        /// </summary>
        private DateTime? _fileWriteTimeUtc;

        /// <summary>
        /// True after a reading of the current file; false forces the reading at the next <see cref="ReloadIfChanged"/>.
        /// </summary>
        private bool _loaded;

        #endregion

        #region Properties

        /// <summary>
        /// Default path of the file: <see cref="DEFAULTFILENAME"/> in <see cref="Paths.DefaultConfigsDirectory"/>.
        /// </summary>
        public static string DefaultFilePath => Path.Combine(Paths.DefaultConfigsDirectory, DEFAULTFILENAME);

        /// <summary>
        /// Default comparer of the actions (<see cref="ActionComparer"/>): without distinction between upper and lower case.
        /// </summary>
        public static IEqualityComparer<string> DefaultActionComparer => StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Comparer that decides when two lines schedule the same action, set by the constructor and never changed: an action
        /// scheduled by more lines is executed once per wake-up, an action still running is not started again by
        /// <see cref="CrontabScheduler"/>, and the coverage groups the lines by action (<see cref="GetCoverage"/>).<br/>
        /// By default <see cref="DefaultActionComparer"/>. A custom comparer can recognize as the same action texts written in
        /// different ways (e.g. with different spaces or quotes), to minimize the overlapping executions; its
        /// <see cref="IEqualityComparer{T}.GetHashCode(T)"/> must be consistent with <see cref="IEqualityComparer{T}.Equals(T, T)"/>
        /// and it must be thread-safe.
        /// </summary>
        public IEqualityComparer<string> ActionComparer { get; }

        /// <summary>
        /// Full path of the file.
        /// </summary>
        public string FilePath
        {
            get
            {
                lock (_lock)
                {
                    return _filePath;
                }
            }
        }

        /// <summary>
        /// Time zone in which the expressions are interpreted.
        /// </summary>
        public TimeZoneInfo Zone
        {
            get
            {
                lock (_lock)
                {
                    return _zone;
                }
            }
        }

        /// <summary>
        /// Valid lines in use, in the order of the file: empty before the first reading (<see cref="Load"/>,
        /// <see cref="ReloadIfChanged"/>). The list is replaced, never modified, at every reading.
        /// </summary>
        public IReadOnlyList<CrontabEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries;
                }
            }
        }

        /// <summary>
        /// Errors of the last reading: a message for each invalid line or line rejected by <see cref="ValidateAction"/>, or
        /// for the missing or unreadable file; empty if the file is valid. The list is replaced, never modified, at every
        /// reading.
        /// </summary>
        public IReadOnlyList<string> Errors
        {
            get
            {
                lock (_lock)
                {
                    return _errors;
                }
            }
        }

        /// <summary>
        /// True if the file existed at the last reading.
        /// </summary>
        public bool FileExists
        {
            get
            {
                lock (_lock)
                {
                    return _fileExists;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the crontab of a file, without reading it (see <see cref="Load"/> and <see cref="ReloadIfChanged"/>).
        /// </summary>
        /// <param name="filePath">Path of the file; a relative path is resolved on <see cref="Paths.DefaultConfigsDirectory"/>,
        /// null or empty means <see cref="DefaultFilePath"/>.</param>
        /// <param name="zone">Time zone in which the expressions are interpreted; null means <see cref="TimeZoneInfo.Local"/>.</param>
        /// <param name="actionComparer">Comparer of the actions (<see cref="ActionComparer"/>); null means
        /// <see cref="DefaultActionComparer"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> contains invalid characters.</exception>
        public Crontab(string filePath = null, TimeZoneInfo zone = null, IEqualityComparer<string> actionComparer = null)
        {
            ActionComparer      = actionComparer ?? DefaultActionComparer;
            _entries            = new List<CrontabEntry>();
            _errors             = new List<string>();
            _fileExists         = false;
            _fileWriteTimeUtc   = null;
            _loaded             = false;
            SetFile(filePath, zone);
        }

        #endregion

        #region Methods

        #region File and time zone

        /// <summary>
        /// Changes the file and the time zone and forces the reading of the file at the next <see cref="ReloadIfChanged"/>;
        /// the lines in use remain valid until then.
        /// </summary>
        /// <param name="filePath">Path of the file (see the constructor).</param>
        /// <param name="zone">Time zone; null means <see cref="TimeZoneInfo.Local"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> contains invalid characters.</exception>
        public void SetFile(string filePath, TimeZoneInfo zone)
        {
            if (TryResolveFilePath(filePath, out var fullPath))
            {
                lock (_lock)
                {
                    _filePath   = fullPath;
                    _zone       = zone ?? TimeZoneInfo.Local;
                    _loaded     = false;
                }
            }
            else
            {
                throw new ArgumentException($"The path of the crontab file ('{filePath}') contains invalid characters.",
                    nameof(filePath));
            }
        }

        /// <summary>
        /// Returns the full path of a crontab file: null or empty means <see cref="DefaultFilePath"/>, a relative path is
        /// resolved on <see cref="Paths.DefaultConfigsDirectory"/>.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        /// <param name="fullPath">Full path of the file; null if <paramref name="filePath"/> is not valid.</param>
        /// <returns>True if the path is valid, false if it contains invalid characters.</returns>
        public static bool TryResolveFilePath(string filePath, out string fullPath)
        {
            var result  = false;
            fullPath    = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                fullPath    = DefaultFilePath;
                result      = true;
            }
            else if (filePath.IndexOfAny(Path.GetInvalidPathChars()) < 0)
            {
                var path    = filePath.Trim();
                fullPath    = Path.IsPathRooted(path) ? path : Path.Combine(Paths.DefaultConfigsDirectory, path);
                result      = true;
            }

            return result;
        }

        /// <summary>
        /// Returns the time zone with the given id, without throwing exceptions.
        /// </summary>
        /// <param name="timeZoneId">Id of the time zone (<see cref="TimeZoneInfo.FindSystemTimeZoneById"/>); null or empty means
        /// <see cref="TimeZoneInfo.Local"/>. The ids differ between Windows and Linux.</param>
        /// <param name="zone">The time zone; null if it does not exist.</param>
        /// <returns>True if the time zone exists.</returns>
        public static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo zone)
        {
            var result  = false;
            zone        = null;

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                zone    = TimeZoneInfo.Local;
                result  = true;
            }
            else
            {
                try
                {
                    zone    = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
                    result  = true;
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
                {
                    zone = null;
                }
            }

            return result;
        }

        #endregion

        #region Reading

        /// <summary>
        /// Reads the file and replaces the lines in use and the errors, without throwing exceptions. The lines rejected by
        /// <see cref="ValidateAction"/> are excluded from <see cref="Entries"/> and their reason is added to
        /// <see cref="Errors"/>.
        /// </summary>
        /// <returns>True if the file is valid (<see cref="Errors"/> empty).</returns>
        public bool Load()
        {
            string filePath;
            lock (_lock)
            {
                filePath = _filePath;
            }

            var writeTimeUtc    = GetWriteTimeUtc(filePath);
            var loaded          = ReadAndValidate(filePath);

            lock (_lock)
            {
                _entries            = loaded.Entries;
                _errors             = loaded.Errors;
                _fileExists         = writeTimeUtc.HasValue;
                _fileWriteTimeUtc   = writeTimeUtc;
                // If the file was changed meanwhile (SetFile), the new one is read at the next check.
                _loaded             = string.Equals(filePath, _filePath, StringComparison.Ordinal);
            }

            return loaded.Errors.Count == 0;
        }

        /// <summary>
        /// Reads the file (<see cref="Load"/>) the first time, after <see cref="SetFile"/> or <see cref="Invalidate"/>, and when
        /// its modification date changes (including its creation and deletion), without throwing exceptions.
        /// </summary>
        /// <returns>True if the file has been read.</returns>
        public bool ReloadIfChanged()
        {
            var result = false;

            try
            {
                string filePath;
                bool loaded;
                DateTime? previousWriteTimeUtc;
                lock (_lock)
                {
                    filePath                = _filePath;
                    loaded                  = _loaded;
                    previousWriteTimeUtc    = _fileWriteTimeUtc;
                }

                if (!loaded || GetWriteTimeUtc(filePath) != previousWriteTimeUtc)
                {
                    Load();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                // The lines in use remain valid; the reading is retried at the next check.
                SimpleLog.LogError(ex, true);
            }

            return result;
        }

        /// <summary>
        /// Forces the reading of the file at the next <see cref="ReloadIfChanged"/>, e.g. when the list of the valid actions of
        /// the application changes (<see cref="ValidateAction"/>).
        /// </summary>
        public void Invalidate()
        {
            lock (_lock)
            {
                _loaded = false;
            }
        }

        /// <summary>
        /// Reads and interprets a file, then applies <see cref="ValidateAction"/> to every valid line, without throwing
        /// exceptions.
        /// </summary>
        /// <param name="filePath">Full path of the file.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Entries</b>: the valid lines accepted by <see cref="ValidateAction"/>, in the order of the file;<br/>
        /// <b>Errors</b>: the errors of the file and the lines rejected.
        /// </returns>
        private (List<CrontabEntry> Entries, List<string> Errors) ReadAndValidate(string filePath)
        {
            var read    = Read(filePath);
            var entries = new List<CrontabEntry>();
            var errors  = read.Errors;

            foreach (var entry in read.Entries)
            {
                string error;
                try
                {
                    error = ValidateAction(entry);
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex, true);
                    error = $"Line {entry.LineNumber}: validation of the action '{entry.Action}' failed: {ex.Message}";
                }

                if (string.IsNullOrEmpty(error))
                {
                    entries.Add(entry);
                }
                else
                {
                    errors.Add(error);
                }
            }

            return (entries, errors);
        }

        /// <summary>
        /// Validation of the action of a valid line, used by <see cref="Load"/> (hence by the scheduler) and by
        /// <see cref="CheckCrontab"/>: a rejected line is excluded from the lines in use and makes the file invalid.<br/>
        /// To be overridden in derived classes, e.g. to reject the actions that the application does not know. It may be called
        /// from the thread of the scheduler: it must be thread-safe. Exceptions are caught and reject the line.
        /// </summary>
        /// <param name="entry">Valid line of the file.</param>
        /// <returns>The reason why the line is rejected, including its number (<see cref="CrontabEntry.LineNumber"/>); empty if
        /// the line is valid. By default every line is valid.</returns>
        protected virtual string ValidateAction(CrontabEntry entry) => string.Empty;

        /// <summary>
        /// Reads and interprets a <c>crontab</c> file, without throwing exceptions and without applying
        /// <see cref="ValidateAction"/>.
        /// </summary>
        /// <param name="filePath">Full path of the file.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Entries</b>: the valid lines, in the order of the file;<br/>
        /// <b>Errors</b>: a message for each invalid line, or for the missing or unreadable file; empty if the file is valid.
        /// </returns>
        public static (List<CrontabEntry> Entries, List<string> Errors) Read(string filePath)
        {
            (List<CrontabEntry> Entries, List<string> Errors) result;

            try
            {
                if (File.Exists(filePath))
                {
                    result = Parse(File.ReadAllLines(filePath));
                }
                else
                {
                    result = (new List<CrontabEntry>(), new List<string> { $"The file '{filePath}' does not exist." });
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
                result = (new List<CrontabEntry>(), new List<string> { $"Unable to read the file '{filePath}': {ex.Message}" });
            }

            return result;
        }

        /// <summary>
        /// Interprets the lines of a <c>crontab</c> file (format described in the class).
        /// </summary>
        /// <param name="lines">Lines of the file.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Entries</b>: the valid lines, in the order of the file;<br/>
        /// <b>Errors</b>: a message for each invalid line, with the line number.
        /// </returns>
        public static (List<CrontabEntry> Entries, List<string> Errors) Parse(IEnumerable<string> lines)
        {
            var entries     = new List<CrontabEntry>();
            var errors      = new List<string>();
            var lineNumber  = 0;

            foreach (var line in lines ?? Enumerable.Empty<string>())
            {
                lineNumber++;
                var text = line?.Trim() ?? string.Empty;
                if (text.Length > 0 && text[0] != COMMENTPREFIX)
                {
                    var parsed = ParseLine(lineNumber, text);
                    if (parsed.Entry != null)
                    {
                        entries.Add(parsed.Entry);
                    }
                    else
                    {
                        errors.Add(parsed.Error);
                    }
                }
            }

            return (entries, errors);
        }

        /// <summary>
        /// Interprets a meaningful line of the file: separates the cron expression (five fields, or a macro) from the action,
        /// which is the rest of the line.
        /// </summary>
        /// <param name="lineNumber">Number of the line, for the messages.</param>
        /// <param name="text">Text of the line, without leading and trailing spaces, not empty and not a comment.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Entry</b>: the interpreted line, null if not valid;<br/>
        /// <b>Error</b>: the reason why the line is not valid, empty if valid.
        /// </returns>
        private static (CrontabEntry Entry, string Error) ParseLine(int lineNumber, string text)
        {
            CrontabEntry entry  = null;
            var error           = string.Empty;
            var fieldsCount     = text[0] == MACROPREFIX ? 1 : STANDARDFIELDS;
            // The fields of the expression, followed by the action (which may contain spaces).
            var match           = Regex.Match(text, $@"^((?:\S+\s+){{{fieldsCount}}})(\S.*)$");

            if (match.Success)
            {
                var expressionText  = Regex.Replace(match.Groups[1].Value.Trim(), @"\s+", " ");
                var action          = match.Groups[2].Value.Trim();
                if (CronExpression.TryParse(expressionText, CronFormat.Standard, out var expression))
                {
                    entry = new CrontabEntry(lineNumber, expressionText, expression, action);
                }
                else
                {
                    error = $"Line {lineNumber}: cron expression '{expressionText}' not valid: '{text}'.";
                }
            }
            else
            {
                var expected    = fieldsCount == 1 ? "a macro" : $"{STANDARDFIELDS} fields";
                error           = $"Line {lineNumber}: expected {expected} followed by the action: '{text}'.";
            }

            return (entry, error);
        }

        /// <summary>
        /// Returns the last modification date (UTC) of a file.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        /// <returns>The modification date, or null if the file does not exist.</returns>
        private static DateTime? GetWriteTimeUtc(string filePath)
        {
            DateTime? result = null;

            if (File.Exists(filePath))
            {
                result = File.GetLastWriteTimeUtc(filePath);
            }

            return result;
        }

        #endregion

        #region Occurrences

        /// <summary>
        /// Returns the lines with at least one execution scheduled in the given interval, initial bound excluded and final bound
        /// included: called at every wake-up with the instant of the previous wake-up, it neither loses nor repeats
        /// executions.
        /// </summary>
        /// <param name="entries">Lines of the crontab.</param>
        /// <param name="fromUtc">Beginning of the interval (excluded), in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="toUtc">End of the interval (included), in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="zone">Time zone in which the expressions are interpreted.</param>
        /// <returns>The lines to execute, in the order of the file.</returns>
        public static List<CrontabEntry> GetDueEntries(IEnumerable<CrontabEntry> entries, DateTime fromUtc, DateTime toUtc,
            TimeZoneInfo zone)
        {
            var result = new List<CrontabEntry>();

            if (toUtc > fromUtc)
            {
                result = entries
                    .Where(e => e.Expression.GetOccurrences(fromUtc, toUtc, zone, fromInclusive: false, toInclusive: true).Any())
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns the actions with at least one execution scheduled in the given interval (bounds as in
        /// <see cref="GetDueEntries"/>), once per action even when more lines or more instants schedule it (actions compared
        /// with <paramref name="actionComparer"/>).
        /// </summary>
        /// <param name="entries">Lines of the crontab.</param>
        /// <param name="fromUtc">Beginning of the interval (excluded), in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="toUtc">End of the interval (included), in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="zone">Time zone in which the expressions are interpreted.</param>
        /// <param name="actionComparer">Comparer of the actions (usually <see cref="ActionComparer"/>); null means
        /// <see cref="DefaultActionComparer"/>.</param>
        /// <returns>
        /// For each action, in the order of its first line:<br/>
        /// <b>Action</b>: the action, as written in its first line;<br/>
        /// <b>ScheduledTimeUtc</b>: its last scheduled instant in the interval;<br/>
        /// <b>Occurrences</b>: the number of its distinct scheduled instants in the interval.
        /// </returns>
        public static List<(string Action, DateTime ScheduledTimeUtc, int Occurrences)> GetDueActions(
            IEnumerable<CrontabEntry> entries, DateTime fromUtc, DateTime toUtc, TimeZoneInfo zone,
            IEqualityComparer<string> actionComparer = null)
        {
            var result = new List<(string Action, DateTime ScheduledTimeUtc, int Occurrences)>();

            if (toUtc > fromUtc)
            {
                foreach (var group in entries.GroupBy(e => e.Action, actionComparer ?? DefaultActionComparer))
                {
                    var occurrences = group
                        .SelectMany(e => e.Expression.GetOccurrences(fromUtc, toUtc, zone, fromInclusive: false, toInclusive: true))
                        .Distinct()
                        .ToList();
                    if (occurrences.Count > 0)
                    {
                        result.Add((group.First().Action, occurrences.Max(), occurrences.Count));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// First execution scheduled by any line after the given instant.
        /// </summary>
        /// <param name="entries">Lines of the crontab.</param>
        /// <param name="fromUtc">Starting instant (excluded), in UTC (<see cref="DateTimeKind.Utc"/>).</param>
        /// <param name="zone">Time zone in which the expressions are interpreted.</param>
        /// <returns>The instant in UTC, null if there are no lines or no future executions.</returns>
        public static DateTime? GetNextOccurrence(IEnumerable<CrontabEntry> entries, DateTime fromUtc, TimeZoneInfo zone)
            => entries
                .Select(e => e.GetNextOccurrence(fromUtc, zone))
                .Where(o => o.HasValue)
                .Min();

        #endregion

        #endregion
    }
}
