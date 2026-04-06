using OSKHelpers.Common;
using OSKHelpers.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Classe astratta che semplifica la persistenza di file di configurazione.<br/> 
    /// Supporta l'update automatizzato della versione dell'oggetto, per sfruttarlo è necessario<br/>
    /// attenersi alle seguenti indicazioni:<br/>
    /// - ogni volta che viene aggiunta una nuova proprietà all'oggetto aggiornare il valore di <see cref="LASTVERSION"/>;<br/>
    /// - accertarsi di fornire per le proprietà un valore di default significativo.<br/>
    /// Se una proprietà viene rinominata a fronte di un cambio di versione il valore precedente andrà perduto, non è<br/>
    /// possibile riportarlo automaticamente all'interno della nuova proprietà.<br/>
    /// E' importante fornire una documentazione adeguata riguardo la struttura del file, magari fornendo un esempio che<br/>
    /// includa elementi fittizi all'interno delle collezioni per semplificarne l'ampliamento.
    /// </summary>
    public abstract class JsonSettings<T> where T: JsonSettings<T>, new()
    {
        #region Costanti

        /// <summary>
        /// Nome del file di default cui la configurazione si riferisce.<br/>
        /// Il file sarà cercato all'interno  della cartella d'esecuzione dell'assembly.
        /// </summary>
        public const string DEFAULTFILENAME = "Settings.json";

        /// <summary>
        /// Ultima versione del file.<br/>
        /// Aggiornarla ogni volta che vengono aggiunte o modificate proprietà.<br/>
        /// <b>Nota</b>: quando una proprietà viene rinominata il valore registrato all'itnerno del file di configurazione<br/>
        /// è perso, non esiste un automatismo per recuperarlo dalla configurazione precedente.
        /// </summary>
        protected virtual int LASTVERSION => 1;

        #endregion

        #region Proprietà

        /// <summary>
        /// Opzioni di default (comuni a tutte le classi derivate da <see cref="JsonConfig"/> per la serializzazione e la deserializzazione.<br/>
        /// Possono essere sostituite per adattarle alle proprie preferenze.<br/>
        /// <b>Attenzione</b>: la modifica dei valori per queste opzioni andrà ad influire su tutte le istanze di classi derivate da <see cref="JsonSettings{T}"/>.<br/>
        /// I valori di default sono i seguenti:<br/>
        /// <br/>
        /// <code>
        ///     IgnoreReadOnlyProperties    = true
        ///     PropertyNameCaseInsensitive = true
        ///     PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
        ///     WriteIndented               = true
        /// </code>
        /// </summary>
        protected static JsonSerializerOptions DefaultSerializerOptions { get; set; }

        /// <summary>
        /// Versione del file.<br/>
        /// Il valore è gestito automaticamente e viene conforontato con <see cref="LASTVERSION"/>.<br/>
        /// Un file con un numero di versione obsoleto deve necessariamente venire aggiornato tramite il metodo <see cref="UpdateVersion"/>.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Se True la configurazione è obsoleta
        /// </summary>
        [JsonIgnore]
        public bool Obsolete => Version < LASTVERSION;

        /// <summary>
        /// Se True il file di configurazione è appena stato creato.<br/>
        /// Viene aggiornato dal metodo <see cref="Load(string, bool, JsonSerializerOptions)"/>.
        /// </summary>
        [JsonIgnore]
        public bool IsNew { get; protected set; }
        
        /// <summary>
        /// Se true è stato effettuato un aggiornamento del file, è consigliabile verificare il 
        /// valore delle proprietà aggiunte durante il processo.
        /// </summary>
        [JsonIgnore]
        public bool Updated { get; protected set; }

        /// <summary>
        /// Se True il contenuto del file è valido.
        /// </summary>
        [JsonIgnore]
        public bool IsValid { get; protected set; }

        #endregion

        #region Costruttori

        static JsonSettings()
        {
            DefaultSerializerOptions = GetDefaultSerializerOptions();
        }

        /// <summary>
        /// Costruttore base.<br/>
        /// Di default Version viene impostato a LASTVERSION, in fase di deserializzazine da file<br/>
        /// il valore sarà eventualmente sovrascritto in fase di deserializzazione da una versione precedente.
        /// </summary>
        public JsonSettings() 
        {
            Version = LASTVERSION;
            IsNew   = false;
            Updated = false;
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Inizializzazioni successive al popolamento iniziale delle proprietà.<br/>
        /// Da implementare nelle classi derivate, richiamato da <see cref="Load(string, bool, JsonSerializerOptions)"/> prima di <see cref="CheckIsValid"/>.<br/>
        /// Quando il metodo viene richiamato i valori di <see cref="IsNew"/> e <see cref="Updated"/> sono correttamente impostati.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Da implementare nelle classi derivate, permette l'inizializzazione dell'oggetto per la creazione del file con i valori di default.<br/>
        /// Viene richiamata dopo che <see cref="IsNew"/> e <see cref="Updated"/> sono stati valorizzati correttamente e prima di <see cref="Init"/>.
        /// </summary>
        protected virtual void CreateDefaultData() { }

        /// <summary>
        /// Verifica se il contenuto dell'oggetto è valido, aggiorna il valore di <see cref="IsValid"/> e lo restituisce come risultato.<br/>
        /// Da implemantare nelle classi derivate, richiamato da <see cref="Load(string, bool, JsonSerializerOptions)"/> dopo <see cref="Init"/>.
        /// </summary>
        /// <returns>Il risultato della verifica, ovvero il valore in uscita di <see cref="IsValid"/>.</returns>
        public abstract bool CheckIsValid();

        /// <summary>
        /// Restituisce le impostazioni di default per la serializzazione dell'oggetto.<br/>
        /// I valori di default sono i seguenti:
        /// <code>
        ///     DefaultIgnoreCondition      = JsonIgnoreCondition.Never
        ///     IgnoreReadOnlyProperties    = true
        ///     PropertyNameCaseInsensitive = true
        ///     WriteIndented               = true
        /// </code>
        /// </summary>
        protected static JsonSerializerOptions GetDefaultSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                DefaultIgnoreCondition      = JsonIgnoreCondition.Never,
                IgnoreReadOnlyProperties    = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented               = true
            };
        }

        /// <summary>
        /// Restituisce il template per il tipo di oggetto corrente.<br/>
        /// Il template viene creato richiamando <see cref="CreateDefaultData"/>.
        /// </summary>
        /// <returns>Il template per il tipo di oggetto corrente.</returns>
        public static T GetTemplate()
        {
            var settings = new T();
            settings.CreateDefaultData();
            return settings;
        }

        /// <summary>
        /// Verifica che il file JsonTemplate.json esista e sia aggiornato all'interno di <see cref="Paths.DefaultConfigsDirectory"/>.<br/>
        /// Se il file non esistesse o non fosse aggiornato provvederà al suo aggiornamento.<br/>
        /// La verifica dell'aggiornamento si basa su <see cref="Version"/> e <see cref="LASTVERSION"/>.
        /// </summary>
        public void CheckTemplate()
        {
            try
            {
                var fileName = Path.Combine(Paths.DefaultConfigsDirectory, "SettingsTemplate.json");
                var template = Load(fileName, true, GetDefaultSerializerOptions());
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
            }
        }

        /// <summary>
        /// Aggiorna la configurazione in modo che sia coerente con l'ultima versione.<br/>
        /// E' possibile implementare l'eventuale logica di aggiornamento attraverso le versioni nelle classi derivate<br/> 
        /// effettuando l'override di <see cref="UpdateVersionCustomCode"/>, che non dovrà mai essere richiamato direttamente.
        /// Un risultato restituito True non significa necessariamente che la versione del file sia stata aggiornata, ma solo che il<br/>
        /// metodo è terminato correttamente. Leventuale aggiornamento della versione è rilevabile attraverso <see cref="Updated"/>.
        /// <b>Attenzione</b>: la logica da implementare all'interno dei metodi derivati riguarda esclusivamente l'aggiornamento dei dati<br/>
        /// da una versione precedente, un oggetto creato ex novo non 
        /// </summary>
        /// <returns>True se il metodo è terminato correttamente.</returns>
        public bool UpdateVersion()
        {
            var updated     = UpdateVersionCustomCode();

            if (Obsolete)
            {
                Version = LASTVERSION;
                Updated = true;
                updated = true;
            }

            return updated;
        }

        /// <summary>
        /// Logica custom da implementare nelle classi derivate per gestire l'aggiornamento dell'oggetto da una versione precedente.<br/>
        /// Per maggiori dettagli vedere  <seealso cref="UpdateVersion"/>.<br/>
        /// </summary>
        /// <returns>True se il metodo è terminato correttamente.</returns>
        protected virtual bool UpdateVersionCustomCode() => false;

        /// <inheritdoc cref="Load(string, bool, JsonSerializerOptions)"/>
        /// <remarks>
        /// <b>Questo metodo andrà a leggere il fle di default.</b>
        /// </remarks>
        public static (T Object, bool IsNew) Load(bool createIfNotExists = true, JsonSerializerOptions options = null) => Load(DEFAULTFILENAME, createIfNotExists, options);

        /// <summary>
        /// Legge il file passato come parametro e restituisce una istanza dell'oggetto.<br/>
        /// Se il file non esiste e <paramref name="createIfNotExists"/> è true sarà tentata la creazione del file, non sarà in alcun caso tentata la creazione della directory atta a contenerlo.<br/>
        /// Se necessario provvederà all'aggiornamento della versione del file, in questo caso il file d'origine sarà sovrascritto dalla nuova versione ed il valore di <br/>
        /// <see cref="Updated"/> dell'oggetto sarà posto a True.<br/>
        /// <br/>
        /// <b>Nota</b>: il metodo può restituire eccezioni relative al tentativo di lettura o scrittura del file, è un comportamento desiderato e le stesse non vengono<br/>
        /// intercettate per permettere di avere il massimo feedbck in caso di problemi.
        /// </summary>
        /// <param name="filename">
        /// Percorso del file da leggere, può essere sia un percorso assoluto che relativo,<br/>
        /// in questo caso sarà utilizzata come directory di partenza quella di default contenente le configurazioni (<see cref="Paths.DefaultConfigsDirectory"/>) .<br/>
        /// Se viene fornito un percorso completo e la directory non esiste sarà elevata una <see cref="DirectoryNotFoundException"/>.
        /// </param>
        /// <param name="createIfNotExists">
        /// Se true il file sarà creato con i valori di default nel caso in cui non esista già.<br/>
        /// Se false nel caso in cui il file non esista sarà restituita una eccezione <see cref="FileNotFoundException"/>.
        /// </param>
        /// <param name="options">Opzioni da utilizzare per serializzare l'oggetto, se null saranno utilizzate le impostaizoni contenute in <see cref="DefaultSerializerOptions"/>.</param>
        /// <returns>
        /// una tupla così composta:<br/>
        /// <b>Object</b>: l'oggetto recuperato dal file o, nel caso in cui non fosse già esistente, l'istanza relativa al nuovo file creato.<br/>
        /// <b>IsNew</b>: se True l'istanza restituita è stata creata ex novo.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="FileNotFoundException"/>
        public static (T Object, bool IsNew) Load(string filename, bool createIfNotExists = true, JsonSerializerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(filename)) 
                throw new ArgumentNullException(nameof(filename));

            // Verifica che il nome del file non contenga caratteri non validi.
            var invalidPathChars = Path.GetInvalidPathChars();
            if (filename.Any(c => invalidPathChars.Contains(c)))
            {
                throw new FormatException($"il parametro {nameof(filename)} ('{filename}') contiene  caratter non validi.");
            }
            // Verifica se il percorso del file è assoluto, in caso contrario si provvede a renderlo relativo
            // alla directory di default contenente le configurazioni.
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(Paths.DefaultConfigsDirectory, filename.Trim());
            }
            // Verifica che la directory esista
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(filename));
            }

            if (options == null)
            {
                options = GetDefaultSerializerOptions();
            }

            // Se tutte le verifiche sono andate a buon fine provvede al caricamento del file e alla sua deserializzazione.
            T obj = null;
            bool isNew = false;

            if (File.Exists(filename))
            {
                obj = JsonSerializer.Deserialize<T>(File.ReadAllText(filename), options);
                if (obj.UpdateVersion())
                {
                    obj.Save(filename);
                }
            }
            else
            {
                if (createIfNotExists)
                {
                    isNew       = true;
                    obj         = new T();
                    obj.IsNew   = true;
                    obj.Updated = true;
                    obj.CreateDefaultData();
                    obj.Save(filename, options);
                }
                else
                {
                    throw new FileNotFoundException(filename);
                }
            }

            // Parte finale dell'inizializzazione e verifica della validità della configurazione.
            obj.Init();
            obj.CheckIsValid();

            return (obj, isNew);
        }

        /// <inheritdoc cref="Save(string, JsonSerializerOptions)"/>
        /// <remarks>
        /// <b>Questo metodo andrà a scrivere il fle di default.</b>
        /// </remarks>
        public void Save(JsonSerializerOptions options = null) => Save(DEFAULTFILENAME, options);

        /// <summary>
        /// Salva la serializzazione dell'oggetto su disco.
        /// </summary>
        /// <remarks>
        /// <b>Nota</b>: il metodo può restituire eccezioni relative al tentativo di lettura o scrittura del file,<br/>
        /// è un comportamento desiderato e le stesse non vengono intercettate per permettere di<br/>
        /// avere il massimo feedbck in caso di problemi.
        /// </remarks>
        /// <param name="filename">
        /// Percorso del file su cui salvare l'oggetto serializzto, può essere sia un percorso assoluto che relativo,<br/>
        /// in questo caso sarà utilizzata come directory di partenza quella di default contenente le configurazioni (<see cref="Paths.DefaultConfigsDirectory"/>) .<br/>
        /// Se viene fornito un percorso completo e la directory non esiste sarà elevata una <see cref="DirectoryNotFoundException"/>.
        /// </param>
        /// <param name="options">
        /// Opzioni da utilizzare per il serializzatore, se null saranno utilizzate quelle presenti in <see cref="DefaultSerializerOptions"/> o,<br/>
        /// nel caso in cui anche questo sia nullo,  quelle di default generate da <see cref="GetDefaultSerializerOptions"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        public void Save(string filename, JsonSerializerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));

            // Verifica che il nome del file non contenga caratteri non validi.
            var invalidPathChars = Path.GetInvalidPathChars();
            if (filename.Any(c => invalidPathChars.Contains(c)))
            {
                throw new FormatException($"il parametro {nameof(filename)} ('{filename}') contiene  caratteri non validi.");
            }
            // Verifica se il percorso del file è assoluto, in caso contrario si provvede a renderlo relativo
            // alla directory di default contenente le configurazioni.
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(Paths.DefaultConfigsDirectory, filename.Trim());
            }
            // Verifica che la directory esista
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(filename));
            }

            if (options == null)
            {
                options = DefaultSerializerOptions ?? GetDefaultSerializerOptions();
            }
            if (File.Exists(filename))
            {
                if (!Paths.CheckDefaultBackupDirectory())
                {
                    throw new DirectoryNotFoundException($"Impossibile accedere alla directory {Paths.DefaultBackupDirectory}.");
                }
                var bkpFileName = Path.Combine(Paths.DefaultBackupDirectory, $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.Now:yyyyMMddHHmmss}.{Path.GetExtension(filename)}");

                // Se il file di backup è esistente si aggiunge un suffisso per renderlo univoco
                var ver = 0;
                while (File.Exists(bkpFileName))
                {
                    ver++;
                    bkpFileName = Path.Combine(Paths.DefaultBackupDirectory, $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.Now:yyyyMMddHHmmss}_{ver}.{Path.GetExtension(filename)}");
                }
                File.Copy(filename, bkpFileName, false);
            }
            File.WriteAllText(filename, JsonUtils.Serialize(this, options));
        }

        #endregion
    }
}
