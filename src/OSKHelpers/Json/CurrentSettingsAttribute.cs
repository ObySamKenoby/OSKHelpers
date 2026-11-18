using System;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Marks the static property of a class derived from <see cref="JsonSettings{T}"/> that holds the settings currently in
    /// use by the application, assigned by <see cref="JsonSettings{T}.Load(string, bool, System.Text.Json.JsonSerializerOptions)"/>
    /// with the instance read (or created).<br/>
    /// The attribute is needed only if the property has a name other than <c>Current</c>: a static property named
    /// <c>Current</c> is used even without it.
    /// </summary>
    /// <remarks>
    /// The property is declared by the derived class, so that every settings type of the application has its own:
    /// <list type="bullet">
    /// <item>it must be static, of the derived type (or a base type of it) and have a setter, of any visibility (a private
    /// setter is recommended, so that only <c>Load</c> assigns it);</item>
    /// <item>a property marked with the attribute takes precedence over the one named <c>Current</c>; if several properties are
    /// marked, the first one found is used;</item>
    /// <item>it is assigned before <see cref="JsonSettings{T}.Init"/>, so <c>Init</c> can already use it;</item>
    /// <item>it is not assigned by the internal reads of the file (<see cref="JsonSettings{T}.CheckTemplate"/>,
    /// <see cref="JsonSettings{T}.AutoUpdate"/>), and <see cref="JsonSettings{T}.AutoUpdate"/> updates the instance in use
    /// without replacing it, so the property and every reference obtained from it remain valid;</item>
    /// <item>every call of <c>Load</c> assigns it again: to read another file of the same type without changing the current
    /// settings, keep the previous value and assign it back.</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AppSettings : JsonSettings&lt;AppSettings&gt;
    /// {
    ///     // Assigned by Load: no attribute needed with this name
    ///     public static AppSettings Current { get; private set; }
    /// }
    ///
    /// public class DeviceSettings : JsonSettings&lt;DeviceSettings&gt;
    /// {
    ///     [CurrentSettings]
    ///     public static DeviceSettings Instance { get; private set; }
    /// }
    ///
    /// AppSettings.Load();
    /// DeviceSettings.Load("Device.json");
    /// var level = AppSettings.Current.LogLevel;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CurrentSettingsAttribute : Attribute
    {
        #region Constants

        /// <summary>
        /// Name of the static property of the current settings used when no property is marked with the attribute.
        /// </summary>
        public const string DEFAULTPROPERTYNAME = "Current";

        #endregion
    }
}
