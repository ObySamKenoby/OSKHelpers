using OSKHelpers.Common;
using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OSKHelpers.INIFile
{
    /// <summary>
    /// Helper per l'utilizzo di file ini.
    /// </summary>
    public class IniFileHelper
    {
        #region Costanti

        private const string SHOWKEYVALUE   = "!!SHOWKEYVALUE!!";
        private const string FORCEDEBUG     = "!!DEBUG!!";
        private const string FORCEPROTOCOL  = "!!PROTOCOL!!";

        /// <summary>
        /// Nome del file di default (<b>Settings.ini</b>)
        /// </summary>
        public const string DEFAULTINIFILE  = "Settings.ini";
        /// <summary>
        /// Nome del file template di default (<b>SettingsTemplate.ini</b>)
        /// </summary>
        public const string INIFILETEMPLATE = "SettingsTemplate.ini";
        /// <summary>
        /// Messaggio di errore relativo al tentativo di accedere ad un array.
        /// </summary>
        [Obsolete("Il valore è deprecato, sarà rimosso in una delle prossime versioni.")]
        public const string ARRAYVALUE      = "Questa chiave si riferisce ad un array, utilizzare la proprietà .Arrays per accedere agli elementi.";
        /// <summary>
        /// Percorso completo del file ini di default.
        /// </summary>
        public static readonly string INIFILEFULLPATH = Path.Combine(Paths.AssemblyPath, DEFAULTINIFILE);
        /// <summary>
        /// Percorso completo del file template di default.
        /// </summary>
        public static readonly string INIFILETEMPLATEPATH = Path.Combine(Paths.AssemblyPath, INIFILETEMPLATE);

        #endregion

        #region Membri

        private bool _showKeyValue;
        private readonly Dictionary<string, string> _keys;
        private readonly Dictionary<string, List<string>> _arrays;

        private static List<string> _validBoolValues;

        #endregion

        #region Proprietà

        /// <summary>
        /// Contiene il valore per ogni chiave che non è un array.<br />
        /// In fase di lettura del file tutte le chiavi vengono convertite in maiuscolo.<br />
        /// La collezione è esposta per comodità di utilizzo ma per la lettura delle chiavi si raccomanda di utilizzare i metodi appositi.
        /// </summary>
        public IReadOnlyDictionary<string, string> Keys => _keys;

        /// <summary>
        /// Contiene gli array.<br />
        /// In fase di lettura del file tutte le chiavi vengono convertite in maiuscolo.<br />
        /// La collezione è l'unico metodo per accedere al conenuto degli array.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Arrays => _arrays.ToDictionary(a => a.Key, a => (IReadOnlyList<string>)a.Value);

        /// <summary>
        /// Indica se la lettura del file Ini è andata a buon fine
        /// </summary>
        public bool IniFileRead { get; private set; }

        #endregion

        #region Costruttori

        static IniFileHelper()
        {
            _validBoolValues = new List<string> { "TRUE", "FALSE", "1", "0" };
        }

        /// <summary>
        /// Costruttore standard.
        /// </summary>
        public IniFileHelper()
        {
            _keys           = new Dictionary<string, string>();
            _arrays         = new Dictionary<string, List<string>>();
            _showKeyValue   = false;
            IniFileRead     = false;
        }

        /// <summary>
        /// Costruttore utilizzato per leggere automaticamente il file di default.<br/>
        /// Se <paramref name="defaultIniFile"/> è true viene automaticamente letto il file <see cref="DEFAULTINIFILE"/>,<br/>
        /// altrimenti l'effetto è lo stesso di richiamare il costruttore base.
        /// </summary>
        /// <param name="defaultIniFile">Se true letto il file <see cref="DEFAULTINIFILE"/>.</param>
        public IniFileHelper(bool defaultIniFile) : this()
        {
            if (defaultIniFile)
            {
                Load();
            }
        }
        /// <summary>
        /// Costruttore utilizzato per leggere il file <paramref name="iniFile"/>, se <paramref name="defaultPath"/> è true il file viene<br/>
        /// cercato all'interno della directory in cui risiede l'assembly, in caso contrario è<br/> 
        /// necessario che <paramref name="iniFile"/> contenga il percorso completo del file.
        /// </summary>
        /// <param name="iniFile">Nome del file da leggere.</param>
        /// <param name="defaultPath">Se true <paramref name="iniFile"/> sarà cercato all'interno della directory in cui risiede l'assembly.</param>
        public IniFileHelper(string iniFile, bool defaultPath = true) : this()
        {
            IniFileRead = Load(iniFile, defaultPath);
        }

        #endregion

        #region Metodi

         /// <summary>
        /// Verifica l'esistenza del file ini, nel caso in cui non esista copia il file template
        /// </summary>
        /// <param name="iniFile">Nome del file ini, se non specificato utilizza il default Settings.ini</param>
        /// <param name="templateFile">Nome del fil template, se non specificato utilizza il default SettingsTemplate.ini</param>
        /// <param name="useDefaultPath">Se true cerca i file ini all'interno della cartella del servizio</param>
        /// <returns>Una tupla contenente:<br/>
        /// Exists: indica se il file esiste<br/>
        /// Message: eventuale messaggio d'errore</returns>
        public static (bool Exists, string Message) CheckIniFileExists(string iniFile = DEFAULTINIFILE, string templateFile = INIFILETEMPLATE, bool useDefaultPath = true)
        {
            var exists = false;
            var message = string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(iniFile) && !string.IsNullOrWhiteSpace(templateFile))
                {
                    var file        = Path.Combine(useDefaultPath ? Paths.AssemblyPath : string.Empty, iniFile);
                    var template    = Path.Combine(useDefaultPath ? Paths.AssemblyPath : string.Empty, templateFile);
                    SimpleLog.Write($"IniFile: {iniFile}");
                    SimpleLog.Write($"Template: {template}");
                    if (!File.Exists(iniFile))
                    {
                        File.Copy(template, iniFile);
                        exists  = false;
                        message = "Primo avvio, verificare il contenuto di Settings.ini ed eseguire di nuovo il programma";
                    }
                    else
                    {
                        exists = true;
                    }
                }
                else
                {
                    exists  = false;
                    message = $"{SimpleLog.GetCallerTypeMethodName()}('{iniFile}', '{templateFile}'): uno dei parametri non è valido.";
                    SimpleLog.Write(message);
                }
            }
            catch (Exception ex)
            {
                exists = false;
                message = "Errore durante la lettura del file Settings.ini";
                SimpleLog.Write(message);
                SimpleLog.Write($"Errore in {SimpleLog.GetCallerTypeMethodName()}: {SimpleLog.FormattedException(ex)}");
            }

            return (exists, message);
        }


        /// <summary>
        /// Legge ed elabora il file passato come parametro, restituisce true se l'operazione è andata a buon fine e sono state trovate chiavi valide.
        /// Il valore di IniFileRead viene impostato di conseguenza.
        /// </summary>
        /// <param name="iniFile">File da leggere ed interpretare.</param>
        /// <param name="defaultPath">Indica se il file si trova nella path di default (quella contenente l'eseguibile del programma).
        /// Se il file si trova nella directory di default ne deve essere indicato solamente il nome, altrimenti deve essere passato il percorso completo del file.
        /// Il nome del file deve essere completo di estensione.</param>
        /// <returns>true se la lettura e l'interpretazione sono andate a buon fine.</returns>
        public bool Load(string iniFile = DEFAULTINIFILE, bool defaultPath = true)
        {
            SimpleLog.DebugWrite($"{SimpleLog.GetCallerTypeMethodName()}({iniFile}, {defaultPath})");
            _keys.Clear();
            _arrays.Clear();
            IniFileRead = false;

            try
            {
                // Se è stato selezionato l'utilizzo della cartella di default si modifca il nome del file ini di conseguenza aggiungendo il percorso d'esecuzione
                if (defaultPath)
                {
                    iniFile = Path.Combine(Paths.AssemblyPath, iniFile);
                    SimpleLog.DebugWrite($"ini file full path: {iniFile}");
                }
                if (!string.IsNullOrWhiteSpace(iniFile) && File.Exists(iniFile))
                {
                    SimpleLog.DebugWrite($"Reading file {iniFile}");
                    var iniLines = File.ReadAllLines(iniFile).ToList();
                    Parse(iniLines);
                }
                else
                {
                    SimpleLog.Write($"ini file not found: {iniFile}");
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                IniFileRead = false;
                _keys.Clear();
                _arrays.Clear();
            }

            return IniFileRead;
        }

        /// <summary>
        /// Elabora le righe passate come argomento e ne estra le coppie chiave / valore
        /// valorizza di conseguenza IniFileRead e ne restituisce il valore come risultato.
        /// </summary>
        /// <returns>true se tutte le righe non vuote o di commento contengono coppie chiave/valore valide</returns>
        public bool Parse(List<string> iniLines)
        {
            _keys.Clear();
            _arrays.Clear();
            IniFileRead = false;

            if (iniLines != null && iniLines.Any())
            {
                bool testPragmas = true;

                // Verifichiamo se fossero presenti stringhe di controllo
                while (testPragmas)
                {
                    var pragma = iniLines.First().Trim().ToUpper();
                    if (pragma == SHOWKEYVALUE)
                    {
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: abilitazione log dei valori rilevati all'interno del file ini");
                        _showKeyValue = true;
                    }
                    else if (pragma == FORCEDEBUG)
                    {
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: forzatura livello log a DEBUG");
                        SimpleLog.ForceDebug = true;
                    }
                    else if (pragma == FORCEPROTOCOL)
                    {
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: forzatura livello log a PROTOCOL");
                        SimpleLog.ForceProtocol = true;
                    }
                    else
                        testPragmas = false;

                    // Se testPragmas è true abbiamo trovato una chiave valida, quinddi rimuoviamo la prima riga
                    if (testPragmas)
                        iniLines.RemoveAt(0);
                }

                try
                {
                    var lines = iniLines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l) && l[0] != '#').ToArray();
                    if (lines.Any())
                    {
                        bool valid = true;
                        foreach (var line in lines)
                        {
                            int i = line.IndexOf("=");
                            // Se il carattere non esiste o è in prima od ultima posizione il valore dell'indice non è valido
                            if (i < 1 || i == line.Length - 1)
                            {
                                valid = false;
                                SimpleLog.Write($"Errore di sintassi nella riga {line}. Se è presente il carattere '=' devono essere presenti un parametro ed un valore." );
                            }
                            var key = line.Substring(0, i).Trim().ToUpper();
                            var value = line.Substring(i + 1).Trim();
                            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || _keys.ContainsKey(key))
                            {
                                valid = false;
                                SimpleLog.Write($"Errore di sintassi nella riga {line}. Se è presente il carattere '=' devono essere presenti un parametro ed un valore.");
                            }
                            // Si verifica se il valore di key identifica una chiave singola o un array
                            if (key[0] != '*')
                            {
                                _keys.Add(key, value);
                            }
                            else if (key.Length > 1)
                            {
                                // Se la chiave è corretta si prende solo la parte seguente il carattere "*"
                                key = key.Substring(1);
                                // Si aggiunge l'elemento, eventualmente creando l'array
                                if (_arrays.ContainsKey(key))
                                {
                                    _arrays[key].Add(value);
                                }
                                else
                                {
                                    _arrays.Add(key, new List<string> { value });
                                }
                            }
                            else
                            {
                                valid = false;
                                SimpleLog.Write($"Errore di sintassi nella riga {line}. Se la riga inizia con '*' deve essere presente il nome dell'Array.");
                            }
                        }
                        IniFileRead = valid;
                        LogKeyValues();
                        if (!IniFileRead)
                        {
                            _keys.Clear();
                            _arrays.Clear();
                        }
                    }
                    else
                    {
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: nessuna riga valida trovata.");
                    }
                }
                catch (Exception ex)
                {
                    IniFileRead = false;
                    LogKeyValues();
                    SimpleLog.LogError(ex);
                    _keys.Clear();
                }
            }
            else
            {
                SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: nessuna riga valida trovata.");
                IniFileRead = false;
                _keys.Clear();
            }
            return IniFileRead;
        }

        private void LogKeyValues()
        {
            if (_showKeyValue)
            {
                List<string> lines = new List<string>();
                lines.Add("Valore delle impostazioni (chiave -> valore):");
                var maxKeyNameLength    = _keys.Any() ? _keys.Keys.Max(k => k.Length) : 0;
                // La lunghezza nel nome degli array viene incrementata di 1 in quanto gli array vengono identificati nel log preponendo un asterisco al nome 
                var maxArrayNameLength  = _arrays.Any() ? _arrays.Keys.Max(k => k.Length) + 1 : 0;
                var l = maxKeyNameLength > maxArrayNameLength ? maxKeyNameLength : maxArrayNameLength;
                if (_keys.Any())
                    lines.AddRange(_keys.Select(k => $"    {k.Key.PadRight(l)} -> {k.Value}"));
                if (_arrays.Any())
                    lines.AddRange(_arrays.SelectMany(a => a.Value.Select(v => $"    *{a.Key.PadRight(l)} -> {v}")));
                lines.Add($"file INI valido: {IniFileRead}");
                SimpleLog.LogLines(lines);
            }
        }

        /// <summary>
        /// Restituisce le chiavi trovate
        /// </summary>
        public IEnumerable<string> GetKeys() => _keys.Keys.Select((k, v) => k);

        /// <summary>
        /// Restituisce true se è presente la chiave passata come parametro
        /// </summary>
        public bool HasKey(string key) => _keys.ContainsKey(TrimUpper(key));

        /// <summary>
        /// Restituisce true se tutte le chiavi passate come parametro cono presenti
        /// </summary>
        /// <param name="keys">Chiavi di cui erificare la presenza</param>
        /// <returns>True se tutte le chiavi sono presenti all'interno del file INI</returns>
        public bool HasKeys(IEnumerable<string> keys)
        {
            bool res = true;
            if (_keys != null && keys != null && _keys.Count >= keys.Count())
            {
                foreach (var key in keys)
                {
                    if (!HasKey(TrimUpper(key)))
                    {
                        res = false;
                        SimpleLog.Write($"Il file .ini non contiene una definizione per {key.ToUpper()}.");
                    }
                }
            }
            else
            {
                res = false;
            }

            return res;
        }

        /// <summary>
        /// Aggiunge la coppia chiave/valore alle chiavi, permettendo così di aggiungere nuove chiavi.
        /// Nel caso in cui la chiave sia esistente ne aggiorna il valore
        /// </summary>
        /// <returns>True se l'aggiunta o l'aggiornamento della chiave sono andati a buon fine</returns>
        public bool AddKey(string key, string value)
        {
            bool res = true;

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                res = false;
            }
            else
            {
                key = TrimUpper(key);
                value = value.Trim();
                if (!_keys.ContainsKey(key))
                {
                    _keys.Add(key, value);
                }
                else
                {
                    _keys[key] = value;
                }
            }

            return res;
        }

        /// <summary>
        /// Aggiunge la coppia chiave/valore alle chiavi, permettendo così di aggiungere nuove chiavi.
        /// Nel caso in cui la chiave sia esistente ne aggiorna il valore
        /// </summary>
        /// <returns>True se l'aggiunta o l'aggiornamento della chiave sono andati a buon fine</returns>
        public bool AddKey(string key, int value)
        {
            return AddKey(key, value.ToString());
        }

        /// <summary>
        /// Aggiunge la coppia chiave/valore alle chiavi, permettendo così di aggiungere nuove chiavi.
        /// Nel caso in cui la chiave sia esistente ne aggiorna il valore
        /// </summary>
        /// <returns>True se l'aggiunta o l'aggiornamento della chiave sono andati a buon fine</returns>
        public bool AddKey(string key, bool value)
        {
            return AddKey(key, value.ToString().ToUpper());
        }

        /// <summary>
        /// Restituisce il valore associato a key come stringa, se key non esiste restituisce il valore di default (stringa vuota)
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            string value = HasKey(key) ? _keys[TrimUpper(key)] : defaultValue;
            if (_showKeyValue)
            {
                SimpleLog.DebugWrite($"IniFile GetString({key}) -> {value}");
            }
            return value;
        }

        /// <summary>
        /// Restituisce il valore della chiave key come intero, se key non esiste o se il valore non è un intero restituisce il vaore di default (zero)
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            int value = defaultValue;
            if (HasKey(key))
            {
                var k = TrimUpper(key);
                if (int.TryParse(_keys[k], out int v))
                {
                    value = v;
                }
            }
            if (_showKeyValue)
            {
                SimpleLog.DebugWrite($"IniFile GetInt({key}) -> {value}");
            }
            return value;
        }

        /// <summary>
        /// Restituisce il valore della chiave key come booleano, se key non esiste o se il valore non è valido (true, false, 1, 0) restituisce il valore di default (false).
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = defaultValue;

            var s = GetString(key).ToUpper();

            if (_validBoolValues.Contains(s))
            {
                value = s == "TRUE" || s == "1";
            }

            return value;
        }

        /// <summary>
        /// Restituisce il valore della chiave key come oggetto <see cref="DayOfWeek"/>, se key non esiste o se il valore non è valido restituisce una nuova istanza di<br/>
        /// <see cref="DaysOfWeekParameter"/> inizializzata con costruttore standard.
        /// </summary>
        public DaysOfWeekParameter GetDaysOfWeek(string key)
        {
            DaysOfWeekParameter value = null;
            string val = HasKey(key) ? _keys[TrimUpper(key)] : string.Empty;
            try
            {
                value = new DaysOfWeekParameter(val);
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                value = new DaysOfWeekParameter();
            }
            if (_showKeyValue)
            {
                SimpleLog.DebugWrite($"IniFile GetString({key}) -> {value?.ToString()}");
            }

            return value;
        }

        /// <summary>
        /// Imposta il valore della chiave key.
        /// </summary>
        public void Set(string key, string value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value;
            }
        }

        /// <summary>
        /// Imposta il valore della chiave key.
        /// </summary>
        public void Set(string key, int value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString();
            }
        }

        /// <summary>
        /// Imposta il valore della chiave key.
        /// </summary>
        public void Set(string key, bool value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString().ToUpper();
            }
        }

        /// <summary>
        /// Imposta il valore della chiave key.
        /// </summary>
        public void Set(string key, DaysOfWeekParameter value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString();
            }
        }

        /// <summary>
        /// Restituisce True se l'array con il nnome passato come parametro esiste tra quelli caricati.
        /// </summary>
        /// <param name="arrayName">Nome dell'array</param>
        /// <returns>True se l'array è tra quelli caricati dal file di impostazioni.</returns>
        public bool HasArray(string arrayName)
        {
            return _arrays.ContainsKey(TrimUpper(arrayName));
        }

        /// <summary>
        /// Restituisce l'array con il nome passato come parametro, se esistente, altrimenti un array vuoto.
        /// </summary>
        /// <param name="arrayName">Nome dell'array desiderato.</param>
        /// <returns>Gli elementi dell'array richiesto, se esistenti.</returns>
        public IReadOnlyList<string> Array(string arrayName)
        {
            List<string> array = new List<string>();
            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                var name = TrimUpper(arrayName);
                if (HasArray(name))
                {
                    array = _arrays[TrimUpper(name)];
                }
            }
            return array;
        }

        /// <summary>
        /// Aggiunge un Array con il nome passato come parametro alla collezione, se già esistente non fa alcunché.
        /// </summary>
        public void AddArray(string arrayName)
        {
            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                if (!HasArray(arrayName))
                {
                    _arrays.Add(TrimUpper(arrayName), new List<string>());
                }
            }
        }

        /// <summary>
        /// Aggiunge un Array con il nome passato come parametro alla collezione
        /// Se l'array è già esistente aggiunge i valori passati come parametro se non già presenti. 
        /// Il Tipo T deve implementare correttamente il metodo ToString().
        /// </summary>
        public void AddArray<T>(string arrayName, List<T> elements)
        {
            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                var arrName = TrimUpper(arrayName);
                if (!HasArray(arrName))
                {
                    _arrays.Add(arrName, new List<string>());
                }
                var elems = elements.Select(e => e.ToString().Trim()).ToList();
                _arrays[arrName].AddRange(elems.Where(e => !_arrays[arrName].Contains(e)).ToArray());
            }
        }

        /// <summary>
        /// Aggiunge un elemento all'array arrayName, creandolo se non esistente
        /// </summary>
        /// <param name="arrayName">Nome dell'array</param>
        /// <param name="element">Elemento da aggiungere</param>
        public void AddArrayElement(string arrayName, string element)
        {
            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                if (!HasArray(arrayName))
                {
                    AddArray(arrayName);
                }
                _arrays[TrimUpper(arrayName)].Add(element?.Trim() ?? string.Empty);
            }
        }

        /// <summary>
        /// Cancella tutti gli elementi dell'array arrayName, restituendo True se l'operazione è andata a buon fine.<br/>
        /// Se l'array non esiste viene restituito False.
        /// </summary>
        /// <param name="arrayName">Nome dell'array</param>
        public bool ClearArray(string arrayName)
        {
            bool cleared = false;

            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                if (HasArray(arrayName))
                {
                    _arrays[TrimUpper(arrayName)].Clear();
                    cleared = true;
                }
            }

            return cleared;
        }

        /// <summary>
        /// Rimuove l'array arrayName.
        /// </summary>
        public void RemoveArray(string arrayName)
        {
            if (!string.IsNullOrWhiteSpace(arrayName))
            {
                if (HasArray(arrayName))
                {
                    _arrays.Remove(TrimUpper(arrayName));
                }
            }
        }

        /// <summary>
        /// Salva il file ini riprendendolo dal template
        /// </summary>
        /// <param name="iniFile">Nome el file ini</param>
        /// <param name="defaultPath">Indica se il file è nella posizione di default</param>
        /// <returns>True se tutto è andato a buon fine</returns>
        public bool Save(string iniFile = DEFAULTINIFILE, bool defaultPath = true)
        {
            bool saved = false;

            if (!string.IsNullOrWhiteSpace(iniFile))
            {
                var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var templateFile = Path.Combine(assemblyPath, INIFILETEMPLATE);
                if (File.Exists(templateFile) && !string.IsNullOrWhiteSpace(iniFile))
                {
                    if (defaultPath)
                    {
                        iniFile = Path.Combine(assemblyPath, iniFile);
                        SimpleLog.DebugWrite($"ini file full path: {iniFile}");
                    }
                    SimpleLog.DebugWrite($"Saving file {iniFile}");

                    var lines = File.ReadAllLines(templateFile).Select(l => l.Trim()).ToList();
                    var usedKeys = new List<string>();
                    var usedArrays = new List<string>();

                    var outLines = new List<string>();

                    // Si aggiornano le righe contenenti le chiavi
                    for (var j = 0; j < lines.Count; j++)
                    {
                        if (lines[j].Length == 0 || lines[j][0] == '#')
                        {
                            // Se la riga è vuota od è un commento la si copia
                            outLines.Add(lines[j]);
                        }
                        else if (lines[j][0] == '*')
                        {
                            // Se la riga contiene il riferimento ad un array lo si inserisce
                            var arrayName = TrimUpper(lines[j].Substring(1));
                            if (_arrays.ContainsKey(arrayName))
                            {
                                outLines.AddRange(_arrays[arrayName].Select(a => $"*{arrayName} = {a}"));
                                usedArrays.Add(arrayName);
                            }
                        }
                        else
                        {
                            int i = lines[j].IndexOf("=");
                            // Se il carattere non esiste o è in prima posizione il valore dell'indice non è valido
                            if (i >= 1)
                            {
                                var key = TrimUpper(lines[j].Substring(0, i));
                                if (_keys.ContainsKey(key))
                                {
                                    outLines.Add($"{key} = {_keys[key]}");
                                    usedKeys.Add(key);
                                }
                            }
                        }
                    }
                    // Si aggiungono in fondo al file eventuali chiavi aggiuntive
                    foreach (var keyValue in _keys.Where(k => !usedKeys.Contains(k.Key)))
                    {
                        outLines.Add($"{keyValue.Key} = {keyValue.Value}");
                    }

                    foreach (var array in _arrays.Where(a => !usedArrays.Contains(a.Key)))
                    {
                        outLines.AddRange(_arrays[array.Key].Select(a => $"*{array.Key} = {a}"));
                    }

                    File.WriteAllLines(iniFile, outLines);
                    saved = true;
                }
                else
                {
                    SimpleLog.Write("Impossibile aggiornare il file INI, è necessario che il file SettingsTemplate.ini sia presente all'interno della cartella dell'applicativo.");
                }
            }
            else
            {
                SimpleLog.Write("Impossibile aggiornare il file INI, non è stato specificato il nome del file.");
            }

            return saved;
        }

        private string TrimUpper(string s) => s?.Trim().ToUpper() ?? string.Empty;

        #endregion

    }
}
