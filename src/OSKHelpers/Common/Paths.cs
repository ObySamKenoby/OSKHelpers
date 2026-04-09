using OSKHelpers.Docker;
using OSKHelpers.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Provides standard filesystem paths and related utilities for use within applications.
    /// </summary>
    public class Paths
    {
        #region Constants

        private const string LOWERCASELETTERS   = "abcdefghijklmnopqrstuvwxyz";
        private const string UPPERCASELETTERS   = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NUMBERS            = "1234567890";
        private const string LETTERS            = UPPERCASELETTERS + LOWERCASELETTERS;
        private const string LOWERCASENUMBERS   = LOWERCASELETTERS + NUMBERS;
        private const string UPPERCASENUMBERS   = UPPERCASELETTERS + NUMBERS;
        private const string ALPHANUMERIC       = LETTERS + NUMBERS;

        #endregion

        #region Members

        /// <summary>
        /// True if the application is running inside a Docker container (checks for the ./dockerenv file).
        /// </summary>
        private static bool _isDockerized;

        /// <summary>
        /// Used to start the automatic cleanup routine for the temporary files directory.
        /// </summary>
        private static Timer _tmpFilesTimer;
        private static string _appdataDirectory;
        private static readonly object _fileLock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Assembly path.<br/><br/>
        /// <code>
        /// Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        /// </code>
        /// </summary>
        public static string AssemblyPath { get; private set; }
        /// <summary>
        /// Directory base AppDomain.<br/><br/>
        /// <code>
        /// AppDomain.CurrentDomain.BaseDirectory;
        /// </code>
        /// </summary>
        public static string AppDomainBaseDirectory { get; private set; }
        /// <summary>
        /// Directory base AppContext.<br/><br/>
        /// <code>
        /// AppContext.BaseDirectory;
        /// </code>
        /// </summary>
        public static string AppContextBaseDirectory { get; private set; }
        /// <summary>
        /// Base data directory for the application.<br/>
        /// Used mainly for containerised deployments to keep all data under /app/appdata.<br/>
        /// Changing this value changes all default directories as well.<br/>
        /// To create the new default directories call <see cref="InitializeDefaultDirectories(bool)"/>.
        /// </summary>
        /// <remarks>
        /// <b>In a containerised environment this assignment will not fail but will have no effect,<br/>
        /// as path overrides are handled at the container configuration level.</b></remarks>
        public static string AppdataDirectory 
        { 
            get => _appdataDirectory;
            set
            {
                if (!_isDockerized)
                {
                    _appdataDirectory = value;
                    InitializeDefaultDirectories(false);
                }
            }
        }
        /// <summary>
        /// Directory containing temporary files.<br/>
        /// Can be used and customised via:<br/>
        /// <see cref="StaleFilesInterval"/><br/>
        /// <see cref="CheckTempDirectory"/><br/>
        /// <see cref="StartTempDirectoryCleanTimer"/><br/>
        /// <see cref="StopTempDirectoryCleanTimer"/><br/>
        /// <see cref="GetNewTempFilename(bool, string)"/>
        /// </summary>
        public static string TempDirectory { get; private set; }

        /// <summary>
        /// Minutes after which a temporary file is considered stale and eligible for deletion.<br/>
        /// Temporary-file cleanup requires an initial call to CheckTempDirectory<br/>
        /// followed by StartTempDirectoryCleanTimer().<br/>
        /// If StopTempDirectoryCleanTimer() is later called, the timer is stopped and cleanup halts.<br/>
        /// A value of zero or less disables temporary-file cleanup.<br/>
        /// Default value: 60 (one hour).
        /// </summary>
        public static int StaleFilesInterval { get; set; }

        /// <summary>
        /// Path of the default Settings.ini file.
        /// </summary>
        public static string DefaultSettingsFile { get; private set; }
        /// <summary>
        /// Default directory for backups.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultBackupDirectory"/>.
        /// </summary>
        public static string DefaultBackupDirectory { get; private set; }
        /// <summary>
        /// Default directory for configuration files.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultConfigsDirectory"/>.
        /// </summary>
        public static string DefaultConfigsDirectory { get; private set; }
        /// <summary>
        /// Default directory for SQLite databases.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultDatabaseDirectory"/>.
        /// </summary>
        public static string DefaultDatabaseDirectory { get; private set; }
        /// <summary>
        /// Default directory for log files.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultLogsDirectory"/>.
        /// </summary>
        public static string DefaultLogsDirectory { get; private set; }
        /// <summary>
        /// Default directory for output files.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultOutputDirectory"/>.
        /// </summary>
        public static string DefaultOutputDirectory { get; private set; }
        /// <summary>
        /// Default directory for scripts.<br/>
        /// Its existence can be verified with <see cref="CheckDefaultScriptsDirectory"/>.
        /// </summary>
        public static string DefaultScriptsDirectory { get; private set; }

        #endregion

        #region Constructor

        static Paths()
        {
            _isDockerized               = DockerUtils.IsDockerized;
            AssemblyPath                = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppDomainBaseDirectory      = AppDomain.CurrentDomain.BaseDirectory;
            AppContextBaseDirectory     = AppContext.BaseDirectory;
            StaleFilesInterval          = 60;
            _appdataDirectory           = _isDockerized ? "/app/appdata" : AssemblyPath;

            InitializeDefaultDirectories(false);

            // Timer setup
            _tmpFilesTimer              = new Timer();
            _tmpFilesTimer.Interval     = TimeSpan.FromMinutes(1).TotalMilliseconds;
            _tmpFilesTimer.AutoReset    = true;
            _tmpFilesTimer.Elapsed      += OnCleanTempFilesTimerElapsed;
        }

        #endregion

        #region Methods

#if DEBUG

        /// <summary>
        /// For testing only: allows overriding the value of IsDockerized.
        /// </summary>
        public static void OverrideIsDockerized(bool value)
        {
            _isDockerized = value;
            InitializeDefaultDirectories(false);
        }

        /// <summary>
        /// For testing only: allows overriding the value of _appDataDirectory.
        /// </summary>
        public static void OverrideAppdataDirectory(string value)
        {
            _appdataDirectory = value;
            InitializeDefaultDirectories(false);
        }


#endif

        /// <summary>
        /// Calling this method requests use of the <i>AssemblyPath\Appdata</i> directory<br/>
        /// as the base for all default directories.<br/>
        /// Any non-existent default directories will be created.<br/>
        /// Has no effect when the application is running in a containerised environment.
        /// </summary>
        public static void UseLocalAppdataDirectory()
        {
            if (!_isDockerized)
            {
                AppdataDirectory = Path.Combine(AssemblyPath, "Appdata");
                InitializeDefaultDirectories(true);
            }
        }

        /// <summary>
        /// Initialises the paths and creates the default directories.
        /// </summary>
        /// <param name="createDirectories">When true, creates the default directories.</param>
        public static void InitializeDefaultDirectories(bool createDirectories = true)
        {
            DefaultSettingsFile         = GetDataDirectoryPath("Settings.ini");
            DefaultBackupDirectory      = GetDataDirectoryPath("Backups");
            DefaultConfigsDirectory     = GetDataDirectoryPath("Configs");
            DefaultDatabaseDirectory    = GetDataDirectoryPath("Database");
            DefaultLogsDirectory        = GetDataDirectoryPath("Logs");
            DefaultOutputDirectory      = GetDataDirectoryPath("Output");
            DefaultScriptsDirectory     = GetDataDirectoryPath("Scripts");
            TempDirectory               = GetDataDirectoryPath("Temp");
            if (createDirectories)
            {
                CheckDefaultDirectories();
            }
        }

        /// <summary>
        /// Returns the full path of the subdirectory with the given name inside the application root directory.<br/>
        /// Used to generate the paths of default directories (DefaultBackupDirectory, DefaultConfigsDirectory, …).<br/>
        /// To check for and optionally create the directory use <see cref="CheckDirectory(string)"/>.
        /// </summary>
        /// <param name="directory">Name of the subdirectory whose full path is requested.</param>
        /// <returns>The full path of the requested directory.</returns>
        //public static string GetDomainDirectoryPath(string directory) => Path.Combine(AssemblyPath, AppDomainBaseDirectory, directory);
        public static string GetDomainDirectoryPath(string directory)
        {
            string basePath = _isDockerized ? "/app" : AssemblyPath;
            return Path.Combine(basePath, directory);
        }

        /// <summary>
        /// Returns the full path of the subdirectory with the given name inside the application data directory.<br/>
        /// Used to generate the paths of default directories (DefaultBackupDirectory, DefaultConfigsDirectory, …).<br/>
        /// To check for and optionally create the directory use <see cref="CheckDirectory(string)"/>.
        /// </summary>
        /// <param name="directory">Name of the subdirectory whose full path is requested.</param>
        /// <returns>The full path of the requested directory.</returns>
        //public static string GetDomainDirectoryPath(string directory) => Path.Combine(AssemblyPath, AppDomainBaseDirectory, directory);
        public static string GetDataDirectoryPath(string directory)
        {
            return Path.Combine(AppdataDirectory, directory);
        }

        /// <summary>
        /// Checks whether the specified directory exists, and attempts to create it if it does not.
        /// </summary>
        /// <returns>True if the directory exists after the call.</returns>
        public static bool CheckDirectory(string directory)
        {
            var exists = true;

            try
            {
                // Create the directory if it does not exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                exists = false;
            }

            return exists;
        }

        /// <summary>
        /// Checks the existence of all default directories.
        /// </summary>
        private static void CheckDefaultDirectories()
        {
            CheckDirectory(DefaultBackupDirectory);
            CheckDirectory(DefaultConfigsDirectory);
            CheckDirectory(DefaultDatabaseDirectory);
            CheckDirectory(DefaultLogsDirectory);
            CheckDirectory(DefaultOutputDirectory);
            CheckDirectory(DefaultScriptsDirectory);
            CheckTempDirectory();
        }

        /// <summary>
        /// Checks the existence of the default backup directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultBackupDirectory() => CheckDirectory(DefaultBackupDirectory);

        /// <summary>
        /// Checks the existence of the default configuration directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultConfigsDirectory() => CheckDirectory(DefaultConfigsDirectory);

        /// <summary>
        /// Checks the existence of the default database directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultDatabaseDirectory() => CheckDirectory(DefaultDatabaseDirectory);
        /// <summary>
        /// Checks the existence of the default logs directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultLogsDirectory() => CheckDirectory(DefaultDatabaseDirectory);

        /// <summary>
        /// Checks the existence of the default output directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultOutputDirectory() => CheckDirectory(DefaultOutputDirectory);
        /// <summary>
        /// Checks the existence of the default scripts directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultScriptsDirectory() => CheckDirectory(DefaultScriptsDirectory);

        /// <summary>
        /// Checks the existence of the temporary files directory.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckTempDirectory()
        {
            var checkOk = CheckDirectory(TempDirectory);
            if (checkOk)
            {
                CleanTempFiles();
            }
            return checkOk;
        }

        /// <summary>
        /// Starts the timer for automatic cleanup of the temporary files directory.<br/>
        /// <see cref="CheckTempDirectory"/> must be called before this method to verify<br/>
        /// the directory exists and initialise the timer.
        /// </summary>
        /// <returns>
        /// True if the timer was started successfully; false otherwise.<br/>
        /// A negative result is normally caused by not having called <see cref="CheckTempDirectory"/>.
        /// </returns>
        public static bool StartTempDirectoryCleanTimer()
        {
            var started = _tmpFilesTimer != null;

            try
            {
                if (started)
                {
                    _tmpFilesTimer.Start();
                }
            }
            catch (Exception ex)
            {
                started = false;
                SimpleLog.LogError(ex);
            }

            return started;
        }

        /// <summary>
        /// Stops the timer for automatic cleanup of the temporary files directory.<br/>
        /// <see cref="CheckTempDirectory"/> must have been called before this method.
        /// </summary>
        /// <returns>
        /// True if the timer was stopped successfully; false otherwise.<br/>
        /// A negative result is normally caused by not having called <see cref="CheckTempDirectory"/>.
        /// </returns>
        public static bool StopTempDirectoryCleanTimer()
        {
            bool stopped = _tmpFilesTimer != null; ;

            try
            {
                if (stopped)
                {
                    _tmpFilesTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                stopped = false;
            }

            return stopped;
        }

        private static void OnCleanTempFilesTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CleanTempFiles();
        }

        /// <summary>
        /// Removes temporary files older than StaleFilesInterval minutes.
        /// </summary>
        /// <returns></returns>
        public static bool CleanTempFiles()
        {
            var cleaned = true;

            try
            {
                if (Directory.Exists(TempDirectory) && StaleFilesInterval > 0)
                {
                    var staleFilesEnd = DateTime.Now.AddMinutes(-StaleFilesInterval);
                    var files = Directory.GetFiles(TempDirectory);
                    if (files.Any())
                    {
                        foreach (var file in files)
                        {
                            if (Directory.GetLastWriteTime(file) <= staleFilesEnd)
                            {
                                File.Delete(file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                cleaned = false;
            }

            return cleaned;
        }

        /// <summary>
        /// Returns a new temporary file name.<br/>
        /// Note: to guarantee uniqueness when createFile is true, an empty file with the returned name<br/>
        /// is created as a placeholder to be overwritten by the caller.<br/>
        /// Since file names are GUIDs, collisions are extremely unlikely in any case.
        /// </summary>
        /// <param name="createFile">When true, creates an empty placeholder file.</param>
        /// <param name="extension">
        /// Extension to assign to the file; may or may not include the leading dot (e.g. 'csv' or '.csv').<br/>
        /// The extension is always converted to lowercase and may contain only letters and digits;<br/>
        /// if it contains invalid characters it is ignored.
        /// </param>
        /// <returns></returns>
        public static string GetNewTempFilename(bool createFile = false, string extension = null)
        {
            string filename;

            lock (_fileLock)
            {
                while (true)
                {
                    if (!string.IsNullOrWhiteSpace(extension))
                    {
                        extension = extension.Trim().ToLower();
                        // Remove the optional leading dot
                        if (extension[0] == '.')
                        {
                            extension = extension.Substring(1);
                        }
                        if (extension.Any(l => !LOWERCASENUMBERS.Contains(l)))
                        {
                            extension = string.Empty;
                        }
                        else
                        {
                            extension = $".{extension}";
                        }
                    }
                    else
                    {
                        extension = string.Empty;
                    }
                    filename = Path.Combine(TempDirectory, $"{Guid.NewGuid()}{extension}");
                    if (!File.Exists(filename))
                    {
                        if (createFile)
                        {
                            File.WriteAllText(filename, string.Empty);
                        }
                        break;
                    }
                }

            }

            return filename;
        }

#endregion
    }
}
