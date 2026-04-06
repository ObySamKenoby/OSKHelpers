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
    /// Utilità varie relative ai percorsi standard da utilizzare all'interno degli applicativi.
    /// </summary>
    public class Paths
    {
        #region Costanti

        private const string LOWERCASELETTERS   = "abcdefghijklmnopqrstuvwxyz";
        private const string UPPERCASELETTERS   = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NUMBERS            = "1234567890";
        private const string LETTERS            = UPPERCASELETTERS + LOWERCASELETTERS;
        private const string LOWERCASENUMBERS   = LOWERCASELETTERS + NUMBERS;
        private const string UPPERCASENUMBERS   = UPPERCASELETTERS + NUMBERS;
        private const string ALPHANUMERIC       = LETTERS + NUMBERS;

        #endregion

        #region Membri

        /// <summary>
        /// True se l'applicazione è dockerizzata (verifica la presenza del file ./dockerenv).
        /// </summary>
        private static bool _isDockerized;

        /// <summary>
        /// Utilizzato per avviare la routine di pulizia automatica della directory dei file temporanei
        /// </summary>
        private static Timer _tmpFilesTimer;
        private static string _appdataDirectory;
        private static readonly object _fileLock = new object();

        #endregion

        #region Proprietà

        /// <summary>
        /// Path dell'Assembly.<br/><br/>
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
        /// Directory base per le directory utilizzate dall'applicativo.<br/>
        /// Utilizzato principalmente per la dockerizzazione, in modo da avere tutti i dati dotto /app/appdata.<br/>
        /// La modifica del valore modificherà quello di tutte le directory di default.<br/>
        /// Per creare le nuove direcotry di default sarà necessario richiamare <see cref="InitializeDefaultDirectories(bool)"/>.
        /// </summary>
        /// <remarks>
        /// <b>Se l'esecuzione avviene in ambiente containerizzato l'assegnazione non fallirà ma non avrà alcun effetto<br/>
        /// in quanto l'overrided dei percorsi avverrà a livello di configurazione del container.</b></remarks>
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
        /// Directory contenente i file temporanei.<br/>
        /// E' possibile utilizzarla e personalizzarne il comportamento utilizzando<br/>
        /// <see cref="StaleFilesInterval"/><br/>
        /// <see cref="CheckTempDirectory"/><br/>
        /// <see cref="StartTempDirectoryCleanTimer"/><br/>
        /// <see cref="StopTempDirectoryCleanTimer"/><br/>
        /// <see cref="GetNewTempFilename(bool, string)"/>
        /// </summary>
        public static string TempDirectory { get; private set; }

        /// <summary>
        /// Minuti dopo i quali un file temporaneo è considerato obsoleto e quindi eliminabile.<br/>
        /// Perché i file temporanei vengano verificati è necessario che venga inizialmente richiamato CheckTempDirectory<br/>
        /// e successivamente StartTempDirectoryCleanTimer().<br/>
        /// Se successivamente venisse richiamato StopTempDirectoryCleanTimer() il timer sarà arrestato e la<br/>
        /// cancellazione dei file temporanei interrotta.<br/>
        /// Un valore minore o uguale a zero impedirà la cancellazione dei file temporanei.<br/>
        /// Valore di default 60 (un'ora).
        /// </summary>
        public static int StaleFilesInterval { get; set; }

        /// <summary>
        /// Percorso del file Settings.ini di default.
        /// </summary>
        public static string DefaultSettingsFile { get; private set; }
        /// <summary>
        /// Cartella di default per i backup.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultBackupDirectory"/>
        /// </summary>
        public static string DefaultBackupDirectory { get; private set; }
        /// <summary>
        /// Cartella di default per i file di configurazione.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultConfigsDirectory"/>
        /// </summary>
        public static string DefaultConfigsDirectory { get; private set; }
        /// <summary>
        /// Cartella di default per i database SQlite.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultDatabaseDirectory"/>
        /// </summary>
        public static string DefaultDatabaseDirectory { get; private set; }
        /// <summary>
        /// Cartella di default per i Logs.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultLogsDirectory"/>
        /// </summary>
        public static string DefaultLogsDirectory { get; private set; }
        /// <summary>
        /// Cartella di default per i file di output.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultOutputDirectory"/>
        /// </summary>
        public static string DefaultOutputDirectory { get; private set; }
        /// <summary>
        /// Cartella di default per gli script.<br/>
        /// E' possibile verificarne l'esistenza con <see cref="CheckDefaultScriptsDirectory"/>
        /// </summary>
        public static string DefaultScriptsDirectory { get; private set; }

        #endregion

        #region Costruttore

        static Paths()
        {
            _isDockerized               = DockerUtils.IsDockerized;
            AssemblyPath                = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppDomainBaseDirectory      = AppDomain.CurrentDomain.BaseDirectory;
            AppContextBaseDirectory     = AppContext.BaseDirectory;
            StaleFilesInterval          = 60;
            _appdataDirectory           = _isDockerized ? "/app/appdata" : AssemblyPath;

            InitializeDefaultDirectories(false);

            // Perparazione del timer
            _tmpFilesTimer              = new Timer();
            _tmpFilesTimer.Interval     = TimeSpan.FromMinutes(1).TotalMilliseconds;
            _tmpFilesTimer.AutoReset    = true;
            _tmpFilesTimer.Elapsed      += OnCleanTempFilesTimerElapsed;
        }

        #endregion

        #region Metodi

#if DEBUG

        /// <summary>
        /// Solo per effettuare i test, permette di eseguire l'override del valore di IsDockerized.
        /// </summary>
        public static void OverrideIsDockerized(bool value)
        {
            _isDockerized = value;
            InitializeDefaultDirectories(false);
        }

        /// <summary>
        /// Solo per effettuare i test, permette di eseguire l'override del valore di _appDataDirectory.
        /// </summary>
        public static void OverrideAppdataDirectory(string value)
        {
            _appdataDirectory = value;
            InitializeDefaultDirectories(false);
        }


#endif

        /// <summary>
        /// Richiamaando questo meotdo sarà richiesto l'utilizzo della directory <i>AssemblyPath\Appdata</i><br/>
        /// come base per tutte le directory di default.<br/>
        /// Se non esistenti le varie directory di default saranno create.<br/>
        /// Nel caso in cui l'applicativo sia eseguito in modo containerizzato non avrà alcun effetto.
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
        /// Inizializza i percorsi e crea le direcotry di default.
        /// </summary>
        /// <param name="createDirectories">Se True provvede alla creazione delle directory di default.</param>
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
        /// Restituisce il percorso completo della directory con nome passato come parametro all'interno della directory dell'applicativo.<br/>
        /// E' utilizzato per generare i percorsi delle directory di default (DefaultBackupDirectory, DefaultConfigsDirectory...).<br/>
        /// Per verificare l'effettiva esistenza della directory ed eventualmente crearla utilizzare <see cref="CheckDirectory(string)"/>.
        /// </summary>
        /// <param name="directory">Nome della sottodirectory per cui ottenere il percorso completo.</param>
        /// <returns>Il percorso completo della directory richiesta.</returns>
        //public static string GetDomainDirectoryPath(string directory) => Path.Combine(AssemblyPath, AppDomainBaseDirectory, directory);
        public static string GetDomainDirectoryPath(string directory)
        {
            string basePath = _isDockerized ? "/app" : AssemblyPath;
            return Path.Combine(basePath, directory);
        }

        /// <summary>
        /// Restituisce il percorso completo della directory con nome passato come parametro all'interno della directory dei dati dell'applicativo.<br/>
        /// E' utilizzato per generare i percorsi delle directory di default (DefaultBackupDirectory, DefaultConfigsDirectory...).<br/>
        /// Per verificare l'effettiva esistenza della directory ed eventualmente crearla utilizzare <see cref="CheckDirectory(string)"/>.
        /// </summary>
        /// <param name="directory">Nome della sottodirectory per cui ottenere il percorso completo.</param>
        /// <returns>Il percorso completo della directory richiesta.</returns>
        //public static string GetDomainDirectoryPath(string directory) => Path.Combine(AssemblyPath, AppDomainBaseDirectory, directory);
        public static string GetDataDirectoryPath(string directory)
        {
            return Path.Combine(AppdataDirectory, directory);
        }

        /// <summary>
        /// Verifica l'esistenza della cartella passata come parametro e nel caso non esista tenta di crearla.
        /// </summary>
        /// <returns>True se la cartella è esistente.</returns>
        public static bool CheckDirectory(string directory)
        {
            var exists = true;

            try
            {
                // Creazione della directory se non esiste
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
        /// Verifica l'esistenza di tutte le directory di default.
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
        /// Verifica l'esistenza della cartella di default per i backup.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultBackupDirectory() => CheckDirectory(DefaultBackupDirectory);

        /// <summary>
        /// Verifica l'esistenza della cartella di default delle configurazioni.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultConfigsDirectory() => CheckDirectory(DefaultConfigsDirectory);

        /// <summary>
        /// Verifica l'esistenza della cartella di default del Database.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultDatabaseDirectory() => CheckDirectory(DefaultDatabaseDirectory);
        /// <summary>
        /// Verifica l'esistenza della cartella di default dei Logs.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultLogsDirectory() => CheckDirectory(DefaultDatabaseDirectory);

        /// <summary>
        /// Verifica l'esistenza della cartella di default di output.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultOutputDirectory() => CheckDirectory(DefaultOutputDirectory);
        /// <summary>
        /// Verifica l'esistenza della cartella di default per gli script.
        /// </summary>
        /// <inheritdoc cref="CheckDirectory(string)"/>
        public static bool CheckDefaultScriptsDirectory() => CheckDirectory(DefaultScriptsDirectory);

        /// <summary>
        /// Verifica l'esistenza della cartella per i file temporanei.
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
        /// Avvia il timer per la pulizia automatica della cartella dei file temporanei.<br/>
        /// Prima di richiamare questo metodo è necessario richiamare CheckTempDirectory per <br/>
        /// verificare l'esistenza della cartella ed inizializzare il timer.
        /// </summary>
        /// <returns>
        /// True se l'avvio è andato a buon fine, false in caso contrario.<br/>
        /// Un risultato negativo è normalmente dovuto al mancato richiamo di CheckTempDirectory.
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
        /// Arresta il timer per la pulizia automatica della cartella dei file temporanei.<br/>
        /// Prima di richiamare questo metodo è necessario richiamare CheckTempDirectory per <br/>
        /// verificare l'esistenza della cartella ed inizializzare il timer.
        /// </summary>
        /// <returns>
        /// True se l'avvio è andato a buon fine, false in caso contrario.<br/>
        /// Un risultato negativo è normalmente dovuto al mancato richiamo di CheckTempDirectory.
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
        /// Rimuove i file temporanei più vecchi di StaleFilesInterval minuti.
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
        /// Restituisce un nuovo nome di file temporaneo.<br/>
        /// Attenzione: per assicurarne l'unicità quando createFile è True viene creato un file vuoto<br/>
        /// con il nome restituito a mo' di segnalibro che dovrà essere sovrascritto dal metodo richiamante.<br/>
        /// Essendo i nomi dei file delle UUID non covrebbero sussitere problemi in ogni caso. 
        /// </summary>
        /// <param name="createFile">Se True crea un file vuoto.</param>
        /// <param name="extension">
        /// Estensione da attribuire al file, può o meno includere il punto iniziale (ad esempio 'csv' o '.csv').<br/>
        /// L'estensione sarà sempre convertita in lowercase e può contenere solamente lettere e numeri, se dovesse<br/>
        /// contenere caratteri non validi sarà ignorata.
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
                        // Rimuove l'eventuale punto iniziale
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
