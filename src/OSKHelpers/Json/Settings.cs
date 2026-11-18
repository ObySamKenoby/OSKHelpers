using OSKHelpers.INIFile;
using OSKHelpers.Json;
using System.Collections.Generic;

namespace OSKHelpers.Templates.Json
{
    /// <summary>
    /// Template class for using <see cref="OSKHelpers.Json.JsonSettings{T}"/>.
    /// </summary>
    /// <remarks>
    /// Typical start of an application that moves its data to <i>AssemblyPath\Appdata</i> and converts Settings.ini:<br/>
    /// 1. <see cref="OSKHelpers.Common.Paths.MigrateToAppdata"/>, first of all;<br/>
    /// 2. <see cref="JsonSettings{T}.MigrateFromIniFile"/>, which finds Settings.ini in the data directory;<br/>
    /// 3. <see cref="JsonSettings{T}.Load(bool, System.Text.Json.JsonSerializerOptions)"/>.
    /// </remarks>
    public class Settings : JsonSettings<Settings>
    {
        #region Constants

        /// <inheritdoc/>
        protected override int LASTVERSION => 1;

        #endregion

        #region Members

        // Private members of the object.

        #endregion

        #region Properties

        /// <summary>
        /// Settings currently in use by the application, assigned by <see cref="JsonSettings{T}.Load(bool, System.Text.Json.JsonSerializerOptions)"/>
        /// with the instance read; null until the file is read.<br/>
        /// A static property named <c>Current</c> is found automatically; with another name, mark it with
        /// <see cref="CurrentSettingsAttribute"/>. Remove it if the application does not need it.
        /// </summary>
        public static Settings Current { get; private set; }

        // Properties of the object.
        // Mark with [AutoUpdate] the properties that must be applied when the file is modified while the application is
        // running (see AutoUpdate() and StartAutoUpdate()); the others are applied only after a restart. Example:
        //
        // [AutoUpdate]
        // public LogLevel LogLevel { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Base constructor; initialises the required properties.
        /// </summary>
        public Settings() : base()
        { }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void Init()
        {
            // Add initialisation logic to be executed after the object properties have been populated.
            // It is executed again on the instance in use by AutoUpdate(), so it must be repeatable.
            // Current is already assigned when Init is called.
        }

        /// <inheritdoc/>
        public override bool CheckIsValid()
        {
            IsValid = true;
            return IsValid;
        }

        /// <inheritdoc/>
        protected override bool AutoUpdateCustomLogic(Settings loaded, List<string> problems)
        {
            // Add the custom logic of the automatic update (e.g. keep the previous value of an invalid item, apply a
            // property not marked with [AutoUpdate]); return false to keep the current values.
            return true;
        }

        /// <inheritdoc/>
        protected override bool MigrateFromIniFileCustomLogic(IniFileHelper iniFile, ICollection<string> notMigratedProperties,
            List<string> problems)
        {
            // Used only by MigrateFromIniFile(), when the settings of an application are converted from Settings.ini: assign
            // the properties not migrated by the default conversion (e.g. a key with a different name) and remove them from
            // notMigratedProperties; return false to cancel the migration. Example:
            //
            // MyProperty = iniFile.GetString("OLDKEYNAME");
            // notMigratedProperties.Remove(nameof(MyProperty));
            //
            // Remove the method if the application has never used an INI file.
            return true;
        }

        #endregion
    }
}
