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
    /// Helper for working with INI files.
    /// </summary>
    public class IniFileHelper
    {
        #region Constants

        private const string SHOWKEYVALUE   = "!!SHOWKEYVALUE!!";
        private const string FORCEDEBUG     = "!!DEBUG!!";
        private const string FORCEPROTOCOL  = "!!PROTOCOL!!";

        /// <summary>
        /// Default file name (<b>Settings.ini</b>).
        /// </summary>
        public const string DEFAULTINIFILE  = "Settings.ini";
        /// <summary>
        /// Default template file name (<b>SettingsTemplate.ini</b>).
        /// </summary>
        public const string INIFILETEMPLATE = "SettingsTemplate.ini";
        /// <summary>
        /// Error message related to attempting to access an array.
        /// </summary>
        [Obsolete("This value is deprecated and will be removed in a future version.")]
        public const string ARRAYVALUE      = "This key refers to an array; use the .Arrays property to access its elements.";
        /// <summary>
        /// Full path of the default INI file.
        /// </summary>
        public static readonly string INIFILEFULLPATH = Path.Combine(Paths.AssemblyPath, DEFAULTINIFILE);
        /// <summary>
        /// Full path of the default template file.
        /// </summary>
        public static readonly string INIFILETEMPLATEPATH = Path.Combine(Paths.AssemblyPath, INIFILETEMPLATE);

        #endregion

        #region Members

        private bool _showKeyValue;
        private readonly Dictionary<string, string> _keys;
        private readonly Dictionary<string, List<string>> _arrays;

        private static List<string> _validBoolValues;

        #endregion

        #region Properties

        /// <summary>
        /// Contains the value for every key that is not an array.<br />
        /// During file reading all keys are converted to uppercase.<br />
        /// The collection is exposed for convenience, but the dedicated methods are recommended for reading keys.
        /// </summary>
        public IReadOnlyDictionary<string, string> Keys => _keys;

        /// <summary>
        /// Contains the arrays.<br />
        /// During file reading all keys are converted to uppercase.<br />
        /// This collection is the only way to access array contents.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Arrays => _arrays.ToDictionary(a => a.Key, a => (IReadOnlyList<string>)a.Value);

        /// <summary>
        /// Indicates whether the INI file was read successfully.
        /// </summary>
        public bool IniFileRead { get; private set; }

        #endregion

        #region Constructors

        static IniFileHelper()
        {
            _validBoolValues = new List<string> { "TRUE", "FALSE", "1", "0" };
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IniFileHelper()
        {
            _keys           = new Dictionary<string, string>();
            _arrays         = new Dictionary<string, List<string>>();
            _showKeyValue   = false;
            IniFileRead     = false;
        }

        /// <summary>
        /// Constructor used to automatically read the default file.<br/>
        /// If <paramref name="defaultIniFile"/> is true, <see cref="DEFAULTINIFILE"/> is read automatically;<br/>
        /// otherwise the effect is the same as calling the default constructor.
        /// </summary>
        /// <param name="defaultIniFile">If true, reads <see cref="DEFAULTINIFILE"/>.</param>
        public IniFileHelper(bool defaultIniFile) : this()
        {
            if (defaultIniFile)
            {
                Load();
            }
        }
        /// <summary>
        /// Constructor used to read the file <paramref name="iniFile"/>. If <paramref name="defaultPath"/> is true, the file is<br/>
        /// looked up inside the directory containing the assembly; otherwise<br/> 
        /// <paramref name="iniFile"/> must contain the full path of the file.
        /// </summary>
        /// <param name="iniFile">Name of the file to read.</param>
        /// <param name="defaultPath">If true, <paramref name="iniFile"/> is searched inside the assembly directory.</param>
        public IniFileHelper(string iniFile, bool defaultPath = true) : this()
        {
            IniFileRead = Load(iniFile, defaultPath);
        }

        #endregion

        #region Methods

         /// <summary>
        /// Checks whether the INI file exists; if it does not, copies the template file.
        /// </summary>
        /// <param name="iniFile">Name of the INI file; if not specified, defaults to Settings.ini.</param>
        /// <param name="templateFile">Name of the template file; if not specified, defaults to SettingsTemplate.ini.</param>
        /// <param name="useDefaultPath">If true, looks for the files inside the service folder.</param>
        /// <returns>A tuple containing:<br/>
        /// Exists: indicates whether the file exists.<br/>
        /// Message: optional error message.</returns>
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
                    SimpleLog.Write($"IniFile: {file}");
                    SimpleLog.Write($"Template: {template}");
                    if (File.Exists(file))
                    {
                        exists = true;
                    }
                    else
                    {
                        File.Copy(template, file);
                        exists  = false;
                        message = $"First run: check the content of {iniFile} and run the program again.";
                    }
                }
                else
                {
                    exists  = false;
                    message = $"{SimpleLog.GetCallerTypeMethodName()}('{iniFile}', '{templateFile}'): one of the parameters is not valid.";
                    SimpleLog.Write(message);
                }
            }
            catch (Exception ex)
            {
                exists = false;
                message = $"Error while checking the file {iniFile}.";
                SimpleLog.Write(message);
                SimpleLog.Write($"Error in {SimpleLog.GetCallerTypeMethodName()}: {SimpleLog.FormattedException(ex)}");
            }

            return (exists, message);
        }


        /// <summary>
        /// Reads and processes the file passed as parameter; returns true if the operation succeeded and valid keys were found.
        /// The value of IniFileRead is set accordingly.
        /// </summary>
        /// <param name="iniFile">File to read and parse.</param>
        /// <param name="defaultPath">Indicates whether the file is located in the default path (the folder containing the program executable).
        /// If the file is in the default directory, only the name needs to be provided; otherwise the full path must be supplied.
        /// The file name must include the extension.</param>
        /// <returns>True if reading and parsing succeeded.</returns>
        public bool Load(string iniFile = DEFAULTINIFILE, bool defaultPath = true)
        {
            SimpleLog.DebugWrite($"{SimpleLog.GetCallerTypeMethodName()}({iniFile}, {defaultPath})");
            _keys.Clear();
            _arrays.Clear();
            IniFileRead = false;

            try
            {
                // If the default path flag is set, prepend the execution path to the INI file name
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
        /// Processes the lines passed as argument, extracts key/value pairs,
        /// sets IniFileRead accordingly and returns its value.
        /// </summary>
        /// <remarks>
        /// Pragmas are recognised only on the first physical lines of <paramref name="iniLines"/>, before any comment or empty line.<br/>
        /// The list passed as parameter is not modified.
        /// </remarks>
        /// <param name="iniLines">Lines to parse.</param>
        /// <returns>True if all non-empty, non-comment lines contain valid key/value pairs.</returns>
        public bool Parse(List<string> iniLines)
        {
            _keys.Clear();
            _arrays.Clear();
            _showKeyValue   = false;
            IniFileRead     = false;

            if (iniLines != null && iniLines.Any())
            {
                try
                {
                    var firstLine = ParsePragmas(iniLines);
                    var lines = iniLines.Skip(firstLine).Select(l => l?.Trim()).Where(l => !string.IsNullOrWhiteSpace(l) && l[0] != '#').ToArray();
                    if (lines.Any())
                    {
                        bool valid = true;
                        foreach (var line in lines)
                        {
                            int i = line.IndexOf("=");
                            // If the character does not exist or is at the first or last position the index is invalid
                            if (i < 1 || i == line.Length - 1)
                            {
                                valid = false;
                                SimpleLog.Write($"Syntax error in line {line}. When the '=' character is present, both a key and a value are required.");
                            }
                            var key = line.Substring(0, i).Trim().ToUpper();
                            var value = line.Substring(i + 1).Trim();
                            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || _keys.ContainsKey(key))
                            {
                                valid = false;
                                SimpleLog.Write($"Syntax error in line {line}. When the '=' character is present, both a key and a value are required.");
                            }
                            // Check whether the key value identifies a single key or an array
                            if (key[0] != '*')
                            {
                                _keys.Add(key, value);
                            }
                            else if (key.Length > 1)
                            {
                                // If the key is valid, take only the part after '*'
                                key = key.Substring(1);
                                // Add the element, creating the array if needed
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
                                SimpleLog.Write($"Syntax error in line {line}. When the line starts with '*', the array name is required.");
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
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: no valid lines found.");
                    }
                }
                catch (Exception ex)
                {
                    IniFileRead = false;
                    LogKeyValues();
                    SimpleLog.LogError(ex);
                    _keys.Clear();
                    _arrays.Clear();
                }
            }
            else
            {
                SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: no valid lines found.");
            }
            return IniFileRead;
        }

        /// <summary>
        /// Applies the control pragmas found on the first lines of <paramref name="iniLines"/>.
        /// </summary>
        /// <param name="iniLines">Lines of the INI file.</param>
        /// <returns>Index of the first line that is not a pragma.</returns>
        private int ParsePragmas(List<string> iniLines)
        {
            var firstLine   = 0;
            var testPragmas = true;

            while (testPragmas && firstLine < iniLines.Count)
            {
                var pragma = iniLines[firstLine]?.Trim().ToUpper();
                if (pragma == SHOWKEYVALUE)
                {
                    SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: logging of the values read from the ini file enabled");
                    _showKeyValue = true;
                }
                else if (pragma == FORCEDEBUG)
                {
                    SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: log level forced to DEBUG");
                    SimpleLog.ForceDebug = true;
                }
                else if (pragma == FORCEPROTOCOL)
                {
                    SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: log level forced to PROTOCOL");
                    SimpleLog.ForceProtocol = true;
                }
                else
                {
                    testPragmas = false;
                }

                // A valid pragma was found: move on to the next line
                if (testPragmas)
                {
                    firstLine++;
                }
            }

            return firstLine;
        }

        /// <summary>
        /// Logs the keys and arrays read, when enabled by the <see cref="SHOWKEYVALUE"/> pragma.
        /// </summary>
        private void LogKeyValues()
        {
            if (_showKeyValue)
            {
                List<string> lines = new List<string>();
                lines.Add("Settings values (key -> value):");
                var maxKeyNameLength    = _keys.Any() ? _keys.Keys.Max(k => k.Length) : 0;
                // Array name length is incremented by 1 because arrays are identified in the log with a leading asterisk
                var maxArrayNameLength  = _arrays.Any() ? _arrays.Keys.Max(k => k.Length) + 1 : 0;
                var l = maxKeyNameLength > maxArrayNameLength ? maxKeyNameLength : maxArrayNameLength;
                if (_keys.Any())
                {
                    lines.AddRange(_keys.Select(k => $"    {k.Key.PadRight(l)} -> {k.Value}"));
                }

                if (_arrays.Any())
                {
                    lines.AddRange(_arrays.SelectMany(a => a.Value.Select(v => $"    *{a.Key.PadRight(l)} -> {v}")));
                }

                lines.Add($"Valid INI file: {IniFileRead}");
                SimpleLog.LogLines(lines);
            }
        }

        /// <summary>
        /// Returns the keys found.
        /// </summary>
        public IEnumerable<string> GetKeys() => _keys.Keys.Select((k, v) => k);

        /// <summary>
        /// Returns true if the key passed as parameter is present.
        /// </summary>
        public bool HasKey(string key) => _keys.ContainsKey(TrimUpper(key));

        /// <summary>
        /// Returns true if all the keys passed as parameter are present.
        /// </summary>
        /// <param name="keys">Keys whose presence is to be verified.</param>
        /// <returns>True if all keys are present in the INI file.</returns>
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
                        SimpleLog.Write($"The .ini file does not contain a definition for {key.ToUpper()}.");
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
        /// Adds the key/value pair to the keys collection, allowing new keys to be added.
        /// If the key already exists, its value is updated.
        /// </summary>
        /// <returns>True if the key was added or updated successfully.</returns>
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
        /// Adds the key/value pair to the keys collection, allowing new keys to be added.
        /// If the key already exists, its value is updated.
        /// </summary>
        /// <returns>True if the key was added or updated successfully.</returns>
        public bool AddKey(string key, int value)
        {
            return AddKey(key, value.ToString());
        }

        /// <summary>
        /// Adds the key/value pair to the keys collection, allowing new keys to be added.
        /// If the key already exists, its value is updated.
        /// </summary>
        /// <returns>True if the key was added or updated successfully.</returns>
        public bool AddKey(string key, bool value)
        {
            return AddKey(key, value.ToString().ToUpper());
        }

        /// <summary>
        /// Returns the value associated with key as a string; if the key does not exist, returns the default value (empty string).
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
        /// Returns the value of key as an integer; if the key does not exist or the value is not a valid integer, returns the default value (zero).
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
        /// Returns the value of key as a boolean; if the key does not exist or the value is not valid (true, false, 1, 0), returns the default value (false).
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
        /// Returns the value of key as a <see cref="DayOfWeek"/> object; if the key does not exist or the value is not valid, returns a new<br/>
        /// <see cref="DaysOfWeekParameter"/> instance initialised with the default constructor.
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
        /// Sets the value of key.
        /// </summary>
        public void Set(string key, string value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value;
            }
        }

        /// <summary>
        /// Sets the value of key.
        /// </summary>
        public void Set(string key, int value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString();
            }
        }

        /// <summary>
        /// Sets the value of key.
        /// </summary>
        public void Set(string key, bool value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString().ToUpper();
            }
        }

        /// <summary>
        /// Sets the value of key.
        /// </summary>
        public void Set(string key, DaysOfWeekParameter value)
        {
            if (HasKey(key))
            {
                _keys[TrimUpper(key)] = value.ToString();
            }
        }

        /// <summary>
        /// Returns True if the array with the given name exists among those loaded.
        /// </summary>
        /// <param name="arrayName">Name of the array.</param>
        /// <returns>True if the array is among those loaded from the settings file.</returns>
        public bool HasArray(string arrayName)
        {
            return _arrays.ContainsKey(TrimUpper(arrayName));
        }

        /// <summary>
        /// Returns the array with the given name if it exists; otherwise an empty list.
        /// </summary>
        /// <param name="arrayName">Name of the requested array.</param>
        /// <returns>The elements of the requested array, if they exist.</returns>
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
        /// Adds an array with the given name to the collection; if it already exists, does nothing.
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
        /// Adds an array with the given name to the collection.
        /// If the array already exists, the values passed as parameter are added if not already present.
        /// Type T must correctly implement the ToString() method.
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
                // Distinct removes also the duplicates contained in the list passed as parameter
                var elems = elements.Select(e => e.ToString().Trim()).Distinct().ToList();
                _arrays[arrName].AddRange(elems.Where(e => !_arrays[arrName].Contains(e)).ToArray());
            }
        }

        /// <summary>
        /// Adds an element to the array arrayName, creating it if it does not exist.
        /// </summary>
        /// <param name="arrayName">Name of the array.</param>
        /// <param name="element">Element to add.</param>
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
        /// Clears all elements of the array arrayName, returning True if the operation succeeded.<br/>
        /// Returns False if the array does not exist.
        /// </summary>
        /// <param name="arrayName">Name of the array.</param>
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
        /// Removes the array arrayName.
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
        /// Saves the INI file by rebuilding it from the template.
        /// </summary>
        /// <param name="iniFile">Name of the INI file.</param>
        /// <param name="defaultPath">Indicates whether the file is in the default location.</param>
        /// <returns>True if the operation succeeded.</returns>
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

                    // Update lines containing keys
                    for (var j = 0; j < lines.Count; j++)
                    {
                        if (lines[j].Length == 0 || lines[j][0] == '#')
                        {
                            // If the line is empty or a comment, copy it as-is
                            outLines.Add(lines[j]);
                        }
                        else if (lines[j][0] == '*')
                        {
                            // If the line contains an array reference, insert it
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
                            // If the character does not exist or is at the first position the index is invalid
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
                    // Append any additional keys at the end of the file
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
                    SimpleLog.Write("Unable to update the INI file: the file SettingsTemplate.ini must be present in the application folder.");
                }
            }
            else
            {
                SimpleLog.Write("Unable to update the INI file: the file name was not specified.");
            }

            return saved;
        }

        private string TrimUpper(string s) => s?.Trim().ToUpper() ?? string.Empty;

        #endregion

    }
}
