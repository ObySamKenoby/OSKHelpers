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
    /// Abstract class that simplifies the persistence of configuration files.<br/> 
    /// Supports automatic version updating of the object; to use this feature:<br/>
    /// - every time a new property is added to the object, update the value of <see cref="LASTVERSION"/>;<br/>
    /// - make sure to provide a meaningful default value for each property.<br/>
    /// If a property is renamed following a version change, the previous value will be lost and cannot be<br/>
    /// automatically carried over to the new property.<br/>
    /// It is important to provide adequate documentation about the file structure, possibly including a sample<br/>
    /// with placeholder collection entries to simplify future extension.
    /// </summary>
    public abstract class JsonSettings<T> where T: JsonSettings<T>, new()
    {
        #region Constants

        /// <summary>
        /// Default file name the configuration refers to.<br/>
        /// The file will be looked up in the assembly execution folder.
        /// </summary>
        public const string DEFAULTFILENAME = "Settings.json";

        /// <summary>
        /// Latest version of the file.<br/>
        /// Update it whenever properties are added or modified.<br/>
        /// <b>Note</b>: when a property is renamed, the value stored in the configuration file<br/>
        /// is lost; there is no automatic way to recover it from the previous configuration.
        /// </summary>
        protected virtual int LASTVERSION => 1;

        #endregion

        #region Properties

        /// <summary>
        /// Default options (shared by all classes deriving from <see cref="JsonSettings{T}"/>) for serialisation and deserialisation.<br/>
        /// Can be replaced to suit personal preferences.<br/>
        /// <b>Warning</b>: changing these options will affect all instances of classes derived from <see cref="JsonSettings{T}"/>.<br/>
        /// Default values are:<br/>
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
        /// File version.<br/>
        /// Managed automatically and compared against <see cref="LASTVERSION"/>.<br/>
        /// A file with an obsolete version number must be updated via the <see cref="UpdateVersion"/> method.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// True if the configuration is obsolete.
        /// </summary>
        [JsonIgnore]
        public bool Obsolete => Version < LASTVERSION;

        /// <summary>
        /// True if the configuration file has just been created.<br/>
        /// Updated by the <see cref="Load(string, bool, JsonSerializerOptions)"/> method.
        /// </summary>
        [JsonIgnore]
        public bool IsNew { get; protected set; }

        /// <summary>
        /// True if the file has been updated; it is advisable to check the values<br/>
        /// of any properties added during the update process.
        /// </summary>
        [JsonIgnore]
        public bool Updated { get; protected set; }

        /// <summary>
        /// True if the file content is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid { get; protected set; }

        #endregion

        #region Constructors

        static JsonSettings()
        {
            DefaultSerializerOptions = GetDefaultSerializerOptions();
        }

        /// <summary>
        /// Base constructor.<br/>
        /// By default Version is set to LASTVERSION; during deserialisation from file<br/>
        /// the value may be overwritten when loading from an older version.
        /// </summary>
        public JsonSettings() 
        {
            Version = LASTVERSION;
            IsNew   = false;
            Updated = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Post-initialisation steps after the initial property population.<br/>
        /// Must be implemented in derived classes; called by <see cref="Load(string, bool, JsonSerializerOptions)"/> before <see cref="CheckIsValid"/>.<br/>
        /// When this method is invoked, the values of <see cref="IsNew"/> and <see cref="Updated"/> are already set correctly.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// To be implemented in derived classes; allows object initialisation for file creation with default values.<br/>
        /// Called after <see cref="IsNew"/> and <see cref="Updated"/> have been set and before <see cref="Init"/>.
        /// </summary>
        protected virtual void CreateDefaultData() { }

        /// <summary>
        /// Verifies whether the object content is valid, updates <see cref="IsValid"/> and returns it as the result.<br/>
        /// Must be implemented in derived classes; called by <see cref="Load(string, bool, JsonSerializerOptions)"/> after <see cref="Init"/>.
        /// </summary>
        /// <returns>The result of the check, i.e. the output value of <see cref="IsValid"/>.</returns>
        public abstract bool CheckIsValid();

        /// <summary>
        /// Returns the default serialisation options for the object.<br/>
        /// Default values are:
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
                WriteIndented               = true,
                ReadCommentHandling         = JsonCommentHandling.Skip
            };
        }

        /// <summary>
        /// Returns the template for the current object type.<br/>
        /// The template is created by calling <see cref="CreateDefaultData"/>.
        /// </summary>
        /// <returns>The template for the current object type.</returns>
        public static T GetTemplate()
        {
            var settings = new T();
            settings.CreateDefaultData();
            return settings;
        }

        /// <summary>
        /// Checks that the JsonTemplate.json file exists and is up to date inside <see cref="Paths.DefaultConfigsDirectory"/>.<br/>
        /// If the file does not exist or is out of date, it will be updated.<br/>
        /// The version check is based on <see cref="Version"/> and <see cref="LASTVERSION"/>.
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
        /// Updates the configuration to be consistent with the latest version.<br/>
        /// Custom update logic across versions can be implemented in derived classes<br/> 
        /// by overriding <see cref="UpdateVersionCustomCode"/>, which must never be called directly.<br/>
        /// A True result does not necessarily mean the file version was updated, only that the<br/>
        /// method completed successfully. Any actual update is detectable via <see cref="Updated"/>.<br/>
        /// <b>Warning</b>: the logic to implement in derived methods concerns exclusively the update of data<br/>
        /// from a previous version; a newly created object does not require this.
        /// </summary>
        /// <returns>True if the method completed successfully.</returns>
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
        /// Custom logic to implement in derived classes to handle object updates from a previous version.<br/>
        /// For more details see <seealso cref="UpdateVersion"/>.<br/>
        /// </summary>
        /// <returns>True if the method completed successfully.</returns>
        protected virtual bool UpdateVersionCustomCode() => false;

        /// <inheritdoc cref="Load(string, bool, JsonSerializerOptions)"/>
        /// <remarks>
        /// <b>This method will read the default file.</b>
        /// </remarks>
        public static (T Object, bool IsNew) Load(bool createIfNotExists = true, JsonSerializerOptions options = null) => Load(DEFAULTFILENAME, createIfNotExists, options);

        /// <summary>
        /// Reads the file passed as parameter and returns an instance of the object.<br/>
        /// If the file does not exist and <paramref name="createIfNotExists"/> is true, file creation will be attempted; the directory will never be created automatically.<br/>
        /// If needed, it will update the file version; in that case the original file will be overwritten by the new version and<br/>
        /// <see cref="Updated"/> of the object will be set to True.<br/>
        /// <br/>
        /// <b>Note</b>: the method may throw exceptions related to file read/write attempts; this is intentional and they are not<br/>
        /// caught so as to provide maximum feedback in case of problems.
        /// </summary>
        /// <param name="filename">
        /// Path of the file to read; can be either absolute or relative.<br/>
        /// If relative, the default configuration directory (<see cref="Paths.DefaultConfigsDirectory"/>) will be used as the base.<br/>
        /// If a full path is supplied and the directory does not exist, a <see cref="DirectoryNotFoundException"/> will be thrown.
        /// </param>
        /// <param name="createIfNotExists">
        /// If true the file will be created with default values if it does not already exist.<br/>
        /// If false and the file does not exist, a <see cref="FileNotFoundException"/> will be thrown.
        /// </param>
        /// <param name="options">Options to use for serialising the object; if null the settings in <see cref="DefaultSerializerOptions"/> will be used.</param>
        /// <returns>
        /// A tuple composed as follows:<br/>
        /// <b>Object</b>: the object retrieved from the file or, if the file did not exist, the instance for the newly created file.<br/>
        /// <b>IsNew</b>: True if the returned instance was newly created.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="FileNotFoundException"/>
        public static (T Object, bool IsNew) Load(string filename, bool createIfNotExists = true, JsonSerializerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(filename)) 
                throw new ArgumentNullException(nameof(filename));

            // Verify that the file name does not contain invalid characters.
            var invalidPathChars = Path.GetInvalidPathChars();
            if (filename.Any(c => invalidPathChars.Contains(c)))
            {
                throw new FormatException($"the parameter {nameof(filename)} ('{filename}') contains invalid characters.");
            }
            // Check whether the file path is absolute; if not, make it relative
            // to the default configuration directory.
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(Paths.DefaultConfigsDirectory, filename.Trim());
            }
            // Verify that the directory exists
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(filename));
            }

            if (options == null)
            {
                options = GetDefaultSerializerOptions();
            }

            // If all checks passed, load and deserialise the file.
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

            // Final initialisation and validity check.
            obj.Init();
            obj.CheckIsValid();

            return (obj, isNew);
        }

        /// <inheritdoc cref="Save(string, JsonSerializerOptions)"/>
        /// <remarks>
        /// <b>This method will write the default file.</b>
        /// </remarks>
        public void Save(JsonSerializerOptions options = null) => Save(DEFAULTFILENAME, options);

        /// <summary>
        /// Saves the serialised object to disk.
        /// </summary>
        /// <remarks>
        /// <b>Note</b>: the method may throw exceptions related to file read/write attempts;<br/>
        /// this is intentional and they are not caught so as to provide<br/>
        /// maximum feedback in case of problems.
        /// </remarks>
        /// <param name="filename">
        /// Path of the file to save the serialised object to; can be either absolute or relative.<br/>
        /// If relative, the default configuration directory (<see cref="Paths.DefaultConfigsDirectory"/>) will be used as the base.<br/>
        /// If a full path is supplied and the directory does not exist, a <see cref="DirectoryNotFoundException"/> will be thrown.
        /// </param>
        /// <param name="options">
        /// Options to use for the serialiser; if null the settings in <see cref="DefaultSerializerOptions"/> or,<br/>
        /// if that is also null, the defaults generated by <see cref="GetDefaultSerializerOptions"/> will be used.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        public void Save(string filename, JsonSerializerOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));

            // Verify that the file name does not contain invalid characters.
            var invalidPathChars = Path.GetInvalidPathChars();
            if (filename.Any(c => invalidPathChars.Contains(c)))
            {
                throw new FormatException($"the parameter {nameof(filename)} ('{filename}') contains invalid characters.");
            }
            // Check whether the file path is absolute; if not, make it relative
            // to the default configuration directory.
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.Combine(Paths.DefaultConfigsDirectory, filename.Trim());
            }
            // Verify that the directory exists
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
                    throw new DirectoryNotFoundException($"Unable to access the directory {Paths.DefaultBackupDirectory}.");
                }
                var bkpFileName = Path.Combine(Paths.DefaultBackupDirectory, $"{Path.GetFileNameWithoutExtension(filename)}_{DateTime.Now:yyyyMMddHHmmss}.{Path.GetExtension(filename)}");

                // If the backup file already exists, append a suffix to make it unique
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
