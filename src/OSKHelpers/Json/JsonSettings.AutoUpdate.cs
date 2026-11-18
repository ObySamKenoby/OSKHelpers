using OSKHelpers.Common;
using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Automatic update of the settings in use when the configuration file is modified while the application is running
    /// (<see cref="AutoUpdate"/>, <see cref="StartAutoUpdate"/>, <see cref="StopAutoUpdate"/>) and assignment of the static
    /// property of the current settings declared by the derived class (<see cref="CurrentSettingsAttribute"/>).
    /// </summary>
    /// <remarks>
    /// Typical usage in a service:
    /// <code>
    /// public class AppSettings : JsonSettings&lt;AppSettings&gt;
    /// {
    ///     public static AppSettings Current { get; private set; }     // assigned by Load
    ///
    ///     [AutoUpdate]
    ///     public LogLevel LogLevel { get; set; }                      // applied as soon as the file is modified
    ///
    ///     public string ListenAddress { get; set; }                   // applied only after a restart
    ///
    ///     public override void Init() => SimpleLog.LogLevel = LogLevel;
    ///     ...
    /// }
    ///
    /// // At startup
    /// AppSettings.Load();
    /// AppSettings.Current.AutoUpdated += (sender, result) =&gt; { /* e.g. apply the changes, send a warning */ };
    /// AppSettings.Current.StartAutoUpdate();                         // checks the file every second
    ///
    /// // Anywhere in the application
    /// var level = AppSettings.Current.LogLevel;
    ///
    /// // At shutdown
    /// AppSettings.Current.StopAutoUpdate();
    /// </code>
    /// Without the timer, <see cref="AutoUpdate"/> can be called directly, e.g. at every wake-up of the main loop.
    /// </remarks>
    public abstract partial class JsonSettings<T>
    {
        #region Constants

        /// <summary>
        /// Default interval, in seconds, between two checks of the file by <see cref="StartAutoUpdate"/>.
        /// </summary>
        public const int DEFAULTAUTOUPDATEINTERVALSECONDS = 1;

        #endregion

        #region Members

        /// <summary>
        /// Properties of <typeparamref name="T"/> marked with <see cref="AutoUpdateAttribute"/>, read once in the static
        /// constructor (<see cref="FindAutoUpdateProperties"/>).
        /// </summary>
        private static readonly PropertyInfo[] _autoUpdateProperties;

        /// <summary>
        /// Static property of <typeparamref name="T"/> for the current settings (<see cref="CurrentSettingsAttribute"/>), read
        /// once in the static constructor (<see cref="FindCurrentSettingsProperty"/>); null if the derived class does not
        /// declare it.
        /// </summary>
        private static readonly PropertyInfo _currentSettingsProperty;

        /// <summary>
        /// Prevents two concurrent executions of <see cref="AutoUpdate"/> on the same instance.
        /// </summary>
        private readonly object _autoUpdateLock = new object();

        /// <summary>
        /// Protects <see cref="_autoUpdateTimer"/> and <see cref="AutoUpdateIntervalSeconds"/>.
        /// </summary>
        private readonly object _autoUpdateTimerLock = new object();

        /// <summary>
        /// Timer of <see cref="StartAutoUpdate"/>; null when the automatic update is stopped.<br/>
        /// It runs once and is rescheduled at the end of every check, so two checks never overlap.
        /// </summary>
        private Timer _autoUpdateTimer;

        /// <summary>
        /// Serialisation options used by <see cref="Load(string, bool, JsonSerializerOptions)"/> for
        /// <see cref="SourceFilePath"/>, reused by <see cref="AutoUpdate"/> to read the file again.
        /// </summary>
        private JsonSerializerOptions _sourceSerializerOptions;

        /// <summary>
        /// True if the last <see cref="AutoUpdate"/> was rejected or the file was busy: the file is read again at the next call
        /// even if it has not been modified (e.g. a network directory that is available again).
        /// </summary>
        private bool _autoUpdatePending;

        #endregion

        #region Events

        /// <summary>
        /// Raised by the timer of <see cref="StartAutoUpdate"/> after every check with an outcome other than
        /// <see cref="JsonSettingsAutoUpdateStatus.NoChange"/>, e.g. to apply the changes or report the problems
        /// (<see cref="JsonSettingsAutoUpdateResult.FileChanged"/> tells whether they are new).<br/>
        /// It is raised on a thread of the thread pool; exceptions of the handlers are logged and ignored. It is not raised by
        /// direct calls of <see cref="AutoUpdate"/>, which return the outcome.
        /// </summary>
        public event EventHandler<JsonSettingsAutoUpdateResult> AutoUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// Full path of the file the instance has been read from by <see cref="Load(string, bool, JsonSerializerOptions)"/>,
        /// used by <see cref="AutoUpdate"/>; null for an instance created in another way.
        /// </summary>
        [JsonIgnore]
        public string SourceFilePath { get; private set; }

        /// <summary>
        /// Modification date (UTC) of <see cref="SourceFilePath"/> at the last read or save, compared by
        /// <see cref="AutoUpdate"/> to detect a modification; null if the file did not exist.
        /// </summary>
        [JsonIgnore]
        public DateTime? SourceFileWriteTimeUtc { get; private set; }

        /// <summary>
        /// True while the timer of <see cref="StartAutoUpdate"/> is active.
        /// </summary>
        [JsonIgnore]
        public bool IsAutoUpdateRunning
        {
            get
            {
                bool result;

                lock (_autoUpdateTimerLock)
                {
                    result = _autoUpdateTimer != null;
                }

                return result;
            }
        }

        /// <summary>
        /// Interval, in seconds, between two checks of the file by the timer, set by <see cref="StartAutoUpdate"/>
        /// (default <see cref="DEFAULTAUTOUPDATEINTERVALSECONDS"/>).
        /// </summary>
        [JsonIgnore]
        public int AutoUpdateIntervalSeconds { get; private set; }

        #endregion

        #region Methods

        #region Current settings

        /// <summary>
        /// Name of the static property of <typeparamref name="T"/> assigned by
        /// <see cref="Load(string, bool, JsonSerializerOptions)"/> with the instance read (see
        /// <see cref="CurrentSettingsAttribute"/>).
        /// </summary>
        /// <returns>The name of the property, or null if the derived class does not declare it.</returns>
        public static string GetCurrentSettingsPropertyName() => _currentSettingsProperty?.Name;

        /// <summary>
        /// Assigns an instance to the static property of the current settings, if the derived class declares it.
        /// </summary>
        /// <param name="settings">Instance to assign.</param>
        private static void SetCurrentSettings(object settings) => _currentSettingsProperty?.SetValue(null, settings);

        /// <summary>
        /// Returns the value of the static property of the current settings.
        /// </summary>
        /// <returns>The current settings, or null if the derived class does not declare the property.</returns>
        private static object GetCurrentSettings() => _currentSettingsProperty?.GetValue(null);

        /// <summary>
        /// Finds the static property of <typeparamref name="T"/> for the current settings: static, not indexed, with a setter
        /// of any visibility and of a type between <see cref="JsonSettings{T}"/> and <typeparamref name="T"/>. A property marked
        /// with <see cref="CurrentSettingsAttribute"/> takes precedence over the one named
        /// <see cref="CurrentSettingsAttribute.DEFAULTPROPERTYNAME"/>.
        /// </summary>
        /// <returns>The property found, or null.</returns>
        private static PropertyInfo FindCurrentSettingsProperty()
        {
            var flags       = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var candidates  = typeof(T).GetProperties(flags)
                                .Where(p => p.GetIndexParameters().Length == 0
                                         && p.GetSetMethod(true) != null
                                         && p.PropertyType.IsAssignableFrom(typeof(T))
                                         && typeof(JsonSettings<T>).IsAssignableFrom(p.PropertyType))
                                .ToList();

            return candidates.FirstOrDefault(p => p.GetCustomAttribute<CurrentSettingsAttribute>(true) != null)
                ?? candidates.FirstOrDefault(p => p.Name == CurrentSettingsAttribute.DEFAULTPROPERTYNAME);
        }

        /// <summary>
        /// Updates the settings in use (this instance, the value of the property of the current settings) with an instance read
        /// by <see cref="Load(string, bool, JsonSerializerOptions)"/>, which never replaces them once assigned. The rules are
        /// those of <see cref="AutoUpdate"/>:
        /// <list type="number">
        /// <item><see cref="Init"/> and <see cref="CheckIsValid"/> are called on <paramref name="loaded"/>; the property of the
        /// current settings is restored afterwards, also if <see cref="Init"/> of the derived class assigns it;</item>
        /// <item>if <paramref name="loaded"/> is valid, <see cref="AutoUpdateCustomLogic"/> is executed and the properties marked
        /// with <see cref="AutoUpdateAttribute"/> whose value changed are copied to this instance, then <see cref="Init"/> and
        /// <see cref="CheckIsValid"/> are called on it again;</item>
        /// <item>if the file is not valid, or the update is refused or produces settings that are not valid, the previous values
        /// are restored and the problems are written to the log as a warning.</item>
        /// </list>
        /// If <paramref name="loaded"/> has been read from <see cref="SourceFilePath"/>, its modification date is recorded, so
        /// that <see cref="AutoUpdate"/> does not read the file again; otherwise <see cref="SourceFilePath"/> does not change.
        /// </summary>
        /// <remarks>
        /// Exceptions of <see cref="Init"/>, <see cref="CheckIsValid"/> or <see cref="AutoUpdateCustomLogic"/> are not caught, as
        /// for the other errors of <see cref="Load(string, bool, JsonSerializerOptions)"/>, after the previous values of this
        /// instance have been restored.
        /// </remarks>
        /// <param name="loaded">Instance read from the file, with its source file already recorded.</param>
        private void UpdateFromLoadedSettings(T loaded)
        {
            var status              = JsonSettingsAutoUpdateStatus.Rejected;
            var problems            = new List<string>();
            var changedProperties   = new List<string>();
            var previousCurrent     = GetCurrentSettings();
            Dictionary<PropertyInfo, object> previousValues = null;

            lock (_autoUpdateLock)
            {
                try
                {
                    loaded.Init();
                    loaded.CheckIsValid();
                    // Init of the derived class may have assigned the instance read to the property of the current settings.
                    SetCurrentSettings(previousCurrent);

                    if (loaded.IsValid)
                    {
                        previousValues  = _autoUpdateProperties.ToDictionary(p => p, p => p.GetValue(this));
                        status          = ApplyLoadedSettings(loaded, problems, changedProperties);
                    }
                    else
                    {
                        AutoUpdateInvalidFileProblems(loaded, problems);
                    }
                }
                catch
                {
                    SetCurrentSettings(previousCurrent);
                    RestoreAfterRejectedUpdate(previousValues);
                    throw;
                }

                if (status == JsonSettingsAutoUpdateStatus.Rejected)
                {
                    problems.Add("The current settings are kept.");
                    changedProperties.Clear();
                    RestoreAfterRejectedUpdate(previousValues);
                }
                if (IsSourceFile(loaded.SourceFilePath))
                {
                    SourceFileWriteTimeUtc  = loaded.SourceFileWriteTimeUtc;
                    _autoUpdatePending      = status == JsonSettingsAutoUpdateStatus.Rejected;
                }
            }

            LogLoadUpdateResult(loaded.SourceFilePath, status, changedProperties, problems);
        }

        /// <summary>
        /// Writes to the log the outcome of <see cref="UpdateFromLoadedSettings"/>: the properties applied and the problems
        /// (<see cref="LogLevel.Warning"/> if the update has been rejected).
        /// </summary>
        /// <param name="filename">Full path of the file read.</param>
        /// <param name="status">Outcome of the update.</param>
        /// <param name="changedProperties">Names of the properties applied.</param>
        /// <param name="problems">Problems of the update.</param>
        private static void LogLoadUpdateResult(string filename, JsonSettingsAutoUpdateStatus status,
            List<string> changedProperties, List<string> problems)
        {
            if (changedProperties.Count > 0)
            {
                SimpleLog.Write(LogLevel.Info, $"File {filename} loaded, current settings {typeof(T).Name} updated: "
                    + string.Join(", ", changedProperties) + ".");
            }
            else if (status == JsonSettingsAutoUpdateStatus.Applied)
            {
                SimpleLog.Write(LogLevel.Debug, $"File {filename} loaded, current settings {typeof(T).Name} unchanged.");
            }
            if (problems.Count > 0)
            {
                var logLevel = status == JsonSettingsAutoUpdateStatus.Rejected ? LogLevel.Warning : LogLevel.Info;
                SimpleLog.Write(logLevel, $"Update of the current settings {typeof(T).Name} from the file {filename}: "
                    + string.Join(" ", problems));
            }
        }

        #endregion

        #region Timer

        /// <summary>
        /// Starts (or restarts with a new interval) a timer that calls <see cref="AutoUpdate"/> every
        /// <paramref name="intervalSeconds"/> seconds: the file is read again only when its modification date changes, or
        /// while the last update had problems.<br/>
        /// After every check with an outcome other than <see cref="JsonSettingsAutoUpdateStatus.NoChange"/> the timer:
        /// <list type="bullet">
        /// <item>writes to the log the properties applied and, at the first read of every version of the file, the problems
        /// (<see cref="LogLevel.Warning"/> if the update has been rejected);</item>
        /// <item>raises <see cref="AutoUpdated"/>.</item>
        /// </list>
        /// The timer keeps the instance alive until <see cref="StopAutoUpdate"/> is called.
        /// </summary>
        /// <param name="intervalSeconds">
        /// Interval between two checks, in seconds, at least 1 (default <see cref="DEFAULTAUTOUPDATEINTERVALSECONDS"/>); it
        /// runs from the end of a check, so two checks never overlap.
        /// </param>
        /// <returns>
        /// True if the timer is active; false (with an error in the log) if the interval is less than 1 second or the instance
        /// has not been read from a file.
        /// </returns>
        public bool StartAutoUpdate(int intervalSeconds = DEFAULTAUTOUPDATEINTERVALSECONDS)
        {
            bool result;

            lock (_autoUpdateTimerLock)
            {
                if (intervalSeconds < 1)
                {
                    SimpleLog.Write(LogLevel.Error, $"Automatic update of {typeof(T).Name} not started: invalid interval "
                        + $"({intervalSeconds} seconds).");
                    result = false;
                }
                else if (string.IsNullOrWhiteSpace(SourceFilePath))
                {
                    SimpleLog.Write(LogLevel.Error, $"Automatic update of {typeof(T).Name} not started: the settings have not "
                        + "been read from a file.");
                    result = false;
                }
                else
                {
                    AutoUpdateIntervalSeconds = intervalSeconds;
                    if (_autoUpdateTimer == null)
                    {
                        _autoUpdateTimer = new Timer(OnAutoUpdateTimer, null, Timeout.Infinite, Timeout.Infinite);
                    }
                    _autoUpdateTimer.Change(TimeSpan.FromSeconds(intervalSeconds), Timeout.InfiniteTimeSpan);
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Stops the timer of <see cref="StartAutoUpdate"/>, waiting for the end of the update in progress, if any (the
        /// <see cref="AutoUpdated"/> event of that check can still be raised). Has no effect if the timer is not active.
        /// </summary>
        public void StopAutoUpdate()
        {
            lock (_autoUpdateTimerLock)
            {
                if (_autoUpdateTimer != null)
                {
                    _autoUpdateTimer.Dispose();
                    _autoUpdateTimer = null;
                }
            }
            // Waits for the end of an update in progress, so that the instance is no longer modified after the return.
            lock (_autoUpdateLock)
            {
                SimpleLog.Write(LogLevel.Debug, $"Automatic update of {typeof(T).Name} stopped.");
            }
        }

        /// <summary>
        /// Callback of the timer: executes <see cref="AutoUpdate"/>, logs the outcome, raises <see cref="AutoUpdated"/> and
        /// schedules the next check, if the timer is still active.
        /// </summary>
        /// <param name="state">Not used.</param>
        private void OnAutoUpdateTimer(object state)
        {
            try
            {
                var result = AutoUpdate();
                if (result.Status != JsonSettingsAutoUpdateStatus.NoChange)
                {
                    LogAutoUpdateResult(result);
                    AutoUpdated?.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                // Exceptions of the event handlers: the timer must keep working.
                SimpleLog.LogError(ex, true);
            }
            finally
            {
                lock (_autoUpdateTimerLock)
                {
                    _autoUpdateTimer?.Change(TimeSpan.FromSeconds(AutoUpdateIntervalSeconds), Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Writes to the log the outcome of an update executed by the timer: the properties applied and, at the first read of
        /// every version of the file, the problems.
        /// </summary>
        /// <param name="result">Outcome of <see cref="AutoUpdate"/>.</param>
        private void LogAutoUpdateResult(JsonSettingsAutoUpdateResult result)
        {
            if (result.ChangedProperties.Count > 0)
            {
                SimpleLog.Write(LogLevel.Info, $"File {SourceFilePath} modified, settings applied: "
                    + string.Join(", ", result.ChangedProperties) + ".");
            }
            if (result.FileChanged && result.Problems.Count > 0)
            {
                var logLevel = result.Status == JsonSettingsAutoUpdateStatus.Rejected ? LogLevel.Warning : LogLevel.Info;
                SimpleLog.Write(logLevel, $"Automatic update of the file {SourceFilePath}: " + string.Join(" ", result.Problems));
            }
        }

        #endregion

        #region Automatic update

        /// <summary>
        /// Names of the properties of <typeparamref name="T"/> applied by <see cref="AutoUpdate"/>, i.e. the public read/write
        /// properties marked with <see cref="AutoUpdateAttribute"/> and not marked with <see cref="JsonIgnoreAttribute"/>.
        /// </summary>
        /// <returns>The names of the properties, in declaration order.</returns>
        public static IReadOnlyList<string> GetAutoUpdatePropertyNames()
            => _autoUpdateProperties.Select(p => p.Name).ToList();

        /// <summary>
        /// Reads <see cref="SourceFilePath"/> again if it has been modified since the last read (or if the last update had
        /// problems) and applies to this instance the properties marked with <see cref="AutoUpdateAttribute"/>, without
        /// throwing exceptions:
        /// <list type="number">
        /// <item>the file is read with <see cref="Load(string, bool, JsonSerializerOptions)"/> (including the possible version
        /// update, which rewrites the file) and must be valid (<see cref="CheckIsValid"/>); the
        /// reasons of an invalid file are added to the problems by <see cref="AutoUpdateInvalidFileProblems"/>;</item>
        /// <item><see cref="AutoUpdateCustomLogic"/> of the derived class can adjust the instance read, handle special cases
        /// or refuse the update;</item>
        /// <item>the marked properties whose value changed (compared through their serialisation) are copied to this
        /// instance, then <see cref="Init"/> and <see cref="CheckIsValid"/> are called on it again;</item>
        /// <item>if the resulting configuration is not valid, the previous values of the marked properties are restored.</item>
        /// </list>
        /// Whenever the update is not applied, <see cref="Init"/> is called again on this instance, to restore the side effects
        /// of the initialisation of the instance read (e.g. the log level).<br/>
        /// The file is read without assigning the static property of the current settings (<see cref="CurrentSettingsAttribute"/>),
        /// which keeps referring to the instance in use, updated in place; its value is restored also if <see cref="Init"/>
        /// of the derived class assigns it.<br/>
        /// The method does not check the file by itself: it must be called periodically, by the timer of
        /// <see cref="StartAutoUpdate"/> or directly by the application (e.g. by the main loop of a service).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><see cref="Init"/> of the derived class is called again on the instance in use after every applied or
        /// rejected update, so it must be repeatable.</item>
        /// <item>The marked properties are assigned one at a time: a thread reading the instance during the update can see
        /// some properties already updated and others not yet.</item>
        /// <item>The modifications made by <see cref="AutoUpdateCustomLogic"/> to this instance, outside the marked
        /// properties, are not undone if the update is rejected.</item>
        /// <item>Saving this instance on <see cref="SourceFilePath"/> with <see cref="Save(string, JsonSerializerOptions, bool)"/>
        /// updates <see cref="SourceFileWriteTimeUtc"/>, so the application's own saves are not detected as modifications.</item>
        /// </list>
        /// </remarks>
        /// <returns>The outcome of the update, see <see cref="JsonSettingsAutoUpdateResult"/>.</returns>
        public JsonSettingsAutoUpdateResult AutoUpdate()
        {
            JsonSettingsAutoUpdateResult result;

            lock (_autoUpdateLock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(SourceFilePath))
                    {
                        result = new JsonSettingsAutoUpdateResult(JsonSettingsAutoUpdateStatus.Rejected, false, false, null,
                            new List<string> { "The settings have not been read from a file: automatic update is not available." });
                    }
                    else
                    {
                        var writeTimeUtc    = GetFileWriteTimeUtc(SourceFilePath);
                        var fileChanged     = writeTimeUtc != SourceFileWriteTimeUtc;
                        if (fileChanged || _autoUpdatePending)
                        {
                            result = ReadAndApplySourceFile(writeTimeUtc, fileChanged);
                        }
                        else
                        {
                            result = new JsonSettingsAutoUpdateResult(JsonSettingsAutoUpdateStatus.NoChange, false, false, null, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // E.g. the modification date of the file cannot be read: the instance keeps its values.
                    SimpleLog.LogError(ex);
                    result = new JsonSettingsAutoUpdateResult(JsonSettingsAutoUpdateStatus.Rejected, false, false, null,
                        new List<string> { $"Unable to check the file {SourceFilePath}: {ex.Message}" });
                }

                _autoUpdatePending = result.Status == JsonSettingsAutoUpdateStatus.Rejected
                                  || result.Status == JsonSettingsAutoUpdateStatus.FileBusy;
            }

            return result;
        }

        /// <summary>
        /// Custom logic of the derived class, called by <see cref="AutoUpdate"/> after the file has been read and found
        /// valid, before the properties marked with <see cref="AutoUpdateAttribute"/> are copied to this instance.<br/>
        /// It can:
        /// <list type="bullet">
        /// <item>modify <paramref name="loaded"/>, e.g. to keep the previous value of an item of a collection that is not
        /// valid in the new file;</item>
        /// <item>copy to this instance properties that are not marked, or apply their changes in a particular way;</item>
        /// <item>refuse the whole update, returning false.</item>
        /// </list>
        /// The default implementation does nothing and returns true. It must never be called directly.
        /// </summary>
        /// <param name="loaded">
        /// Instance read from the file, already initialised (<see cref="Init"/>) and valid (<see cref="CheckIsValid"/>).
        /// </param>
        /// <param name="problems">
        /// Messages to report to the caller (<see cref="JsonSettingsAutoUpdateResult.Problems"/>), also when the update is
        /// applied.
        /// </param>
        /// <returns>True to apply the update, false to keep the current values.</returns>
        protected virtual bool AutoUpdateCustomLogic(T loaded, List<string> problems) => true;

        /// <summary>
        /// Adds to the problems of <see cref="AutoUpdate"/> the reasons why the file read is not valid
        /// (<see cref="CheckIsValid"/> returned false); the update is then rejected and the current values are kept.<br/>
        /// To be overridden in derived classes that collect the validation errors, so that they are reported to the caller
        /// (<see cref="JsonSettingsAutoUpdateResult.Problems"/>) instead of the generic message. It must never be called
        /// directly; exceptions are caught by <see cref="AutoUpdate"/>, which rejects the update as if the file could not be
        /// read.
        /// </summary>
        /// <param name="loaded">
        /// Instance read from the file, already initialised (<see cref="Init"/>) and checked (<see cref="CheckIsValid"/>), not
        /// valid.
        /// </param>
        /// <param name="problems">Problems of the update, to which the reasons are added.</param>
        protected virtual void AutoUpdateInvalidFileProblems(T loaded, List<string> problems)
            => problems.Add($"The file {loaded.SourceFilePath} is not valid.");

        /// <summary>
        /// Reads <see cref="SourceFilePath"/> and applies it to this instance (see <see cref="AutoUpdate"/>), handling the
        /// errors of the read.
        /// </summary>
        /// <param name="writeTimeUtc">Modification date of the file read before the read.</param>
        /// <param name="fileChanged">True if the file has been modified since the last read.</param>
        /// <returns>The outcome of the update.</returns>
        private JsonSettingsAutoUpdateResult ReadAndApplySourceFile(DateTime? writeTimeUtc, bool fileChanged)
        {
            JsonSettingsAutoUpdateStatus status;
            var problems            = new List<string>();
            var changedProperties   = new List<string>();
            var fileSaved           = false;
            var previousCurrent     = GetCurrentSettings();
            Dictionary<PropertyInfo, object> previousValues = null;

            try
            {
                var loaded = LoadCore(SourceFilePath, false, _sourceSerializerOptions, false).Object;
                // If the version has been updated the file has been rewritten: its new date is taken, so that it is not
                // detected again as modified by the operator.
                fileSaved               = loaded.Updated;
                SourceFileWriteTimeUtc  = fileSaved ? GetFileWriteTimeUtc(SourceFilePath) : writeTimeUtc;

                if (loaded.IsValid)
                {
                    previousValues  = _autoUpdateProperties.ToDictionary(p => p, p => p.GetValue(this));
                    status          = ApplyLoadedSettings(loaded, problems, changedProperties);
                }
                else
                {
                    AutoUpdateInvalidFileProblems(loaded, problems);
                    status = JsonSettingsAutoUpdateStatus.Rejected;
                }
            }
            catch (Exception ex) when (ex is IOException && !(ex is FileNotFoundException) && !(ex is DirectoryNotFoundException))
            {
                // Typically the file is still open in the editor that is saving it: it is read again at the next call.
                SimpleLog.Write(LogLevel.Debug, $"File {SourceFilePath} not accessible, read postponed: {ex.Message}");
                status = JsonSettingsAutoUpdateStatus.FileBusy;
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                problems.Add($"The file {SourceFilePath} cannot be read: {ex.Message}");
                SourceFileWriteTimeUtc  = writeTimeUtc;
                status                  = JsonSettingsAutoUpdateStatus.Rejected;
            }
            finally
            {
                // Init of the derived class may have assigned the instance read to the property of the current settings.
                SetCurrentSettings(previousCurrent);
            }

            if (status == JsonSettingsAutoUpdateStatus.Rejected)
            {
                problems.Add("The current settings are kept.");
                changedProperties.Clear();
                RestoreAfterRejectedUpdate(previousValues);
            }

            return new JsonSettingsAutoUpdateResult(status, fileChanged, fileSaved, changedProperties, problems);
        }

        /// <summary>
        /// Executes <see cref="AutoUpdateCustomLogic"/>, copies to this instance the marked properties whose value changed
        /// and checks the resulting configuration.
        /// </summary>
        /// <param name="loaded">Valid instance read from the file.</param>
        /// <param name="problems">Problems to report, updated by the method.</param>
        /// <param name="changedProperties">Names of the properties changed, updated by the method.</param>
        /// <returns>
        /// <see cref="JsonSettingsAutoUpdateStatus.Applied"/>, or <see cref="JsonSettingsAutoUpdateStatus.Rejected"/> if the
        /// custom logic refused the update or the resulting configuration is not valid (the caller restores the values).
        /// </returns>
        private JsonSettingsAutoUpdateStatus ApplyLoadedSettings(T loaded, List<string> problems, List<string> changedProperties)
        {
            JsonSettingsAutoUpdateStatus status;

            if (AutoUpdateCustomLogic(loaded, problems))
            {
                foreach (var property in _autoUpdateProperties)
                {
                    var newValue = property.GetValue(loaded);
                    if (!AreEqualValues(property.PropertyType, property.GetValue(this), newValue))
                    {
                        property.SetValue(this, newValue);
                        changedProperties.Add(property.Name);
                    }
                }

                Init();
                if (CheckIsValid())
                {
                    status = JsonSettingsAutoUpdateStatus.Applied;
                }
                else
                {
                    problems.Add($"The settings obtained by applying the file {loaded.SourceFilePath} are not valid.");
                    status = JsonSettingsAutoUpdateStatus.Rejected;
                }
            }
            else
            {
                problems.Add($"The update from the file {loaded.SourceFilePath} has been refused by the custom logic.");
                status = JsonSettingsAutoUpdateStatus.Rejected;
            }

            return status;
        }

        /// <summary>
        /// Restores this instance after a rejected update: previous values of the marked properties (if they had been read)
        /// and initialisation (<see cref="Init"/>, <see cref="CheckIsValid"/>), changed by the read of the file.
        /// </summary>
        /// <param name="previousValues">Values of the marked properties before the update; null if they were not read.</param>
        private void RestoreAfterRejectedUpdate(Dictionary<PropertyInfo, object> previousValues)
        {
            try
            {
                if (previousValues != null)
                {
                    foreach (var previousValue in previousValues)
                    {
                        previousValue.Key.SetValue(this, previousValue.Value);
                    }
                }
                Init();
                CheckIsValid();
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex, true);
            }
        }

        /// <summary>
        /// Compares two values of a property through their serialisation, so that collections and objects with the same
        /// content are considered equal.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="current">Value of the instance in use.</param>
        /// <param name="loaded">Value read from the file.</param>
        /// <returns>True if the two values have the same serialisation.</returns>
        private bool AreEqualValues(Type type, object current, object loaded)
        {
            var options = _sourceSerializerOptions ?? DefaultSerializerOptions ?? GetDefaultSerializerOptions();

            return string.Equals(JsonSerializer.Serialize(current, type, options), JsonSerializer.Serialize(loaded, type, options),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Records the file the instance has been read from, with its options and modification date, for
        /// <see cref="AutoUpdate"/>.
        /// </summary>
        /// <param name="filename">Full path of the file.</param>
        /// <param name="options">Serialisation options used to read it.</param>
        private void SetSourceFile(string filename, JsonSerializerOptions options)
        {
            SourceFilePath              = filename;
            SourceFileWriteTimeUtc      = GetFileWriteTimeUtc(filename);
            _sourceSerializerOptions    = options;
        }

        /// <summary>
        /// Updates <see cref="SourceFileWriteTimeUtc"/> after a save, if the file saved is <see cref="SourceFilePath"/>.
        /// </summary>
        /// <param name="filename">Full path of the file saved.</param>
        private void UpdateSourceFileWriteTime(string filename)
        {
            if (IsSourceFile(filename))
            {
                SourceFileWriteTimeUtc = GetFileWriteTimeUtc(filename);
            }
        }

        /// <summary>
        /// Returns true if <paramref name="filename"/> is <see cref="SourceFilePath"/> (case insensitive on Windows).
        /// </summary>
        /// <param name="filename">Path of the file to compare.</param>
        /// <returns>True if the two paths refer to the same file; false if one of them is null.</returns>
        private bool IsSourceFile(string filename)
        {
            var comparison = OSKEnvironment.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return SourceFilePath != null && filename != null
                && string.Equals(Path.GetFullPath(filename), Path.GetFullPath(SourceFilePath), comparison);
        }

        /// <summary>
        /// Returns the modification date (UTC) of a file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>The modification date, or null if the file does not exist.</returns>
        private static DateTime? GetFileWriteTimeUtc(string path)
        {
            DateTime? result = null;

            if (File.Exists(path))
            {
                result = File.GetLastWriteTimeUtc(path);
            }

            return result;
        }

        /// <summary>
        /// Finds the properties of <typeparamref name="T"/> applied by <see cref="AutoUpdate"/>: public instance properties,
        /// not indexed, with a public getter and setter, marked with <see cref="AutoUpdateAttribute"/> and not marked with
        /// <see cref="JsonIgnoreAttribute"/>.
        /// </summary>
        /// <returns>The properties found.</returns>
        private static PropertyInfo[] FindAutoUpdateProperties()
            => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsSettingsProperty(p) && p.GetCustomAttribute<AutoUpdateAttribute>(true) != null)
                .ToArray();

        /// <summary>
        /// Returns true if <paramref name="property"/> is a persisted setting that can be assigned: public instance property
        /// (filter of the caller), not indexed, with a public getter and setter and not marked with
        /// <see cref="JsonIgnoreAttribute"/>.<br/>
        /// Common filter of <see cref="FindAutoUpdateProperties"/> and of <see cref="MigrateFromIniFile"/>.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is a persisted, assignable setting.</returns>
        private static bool IsSettingsProperty(PropertyInfo property)
            => property.GetIndexParameters().Length == 0
            && property.GetGetMethod() != null
            && property.GetSetMethod() != null
            && property.GetCustomAttribute<JsonIgnoreAttribute>(true) == null;

        #endregion

        #endregion
    }
}
