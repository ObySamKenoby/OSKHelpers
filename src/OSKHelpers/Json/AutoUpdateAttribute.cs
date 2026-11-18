using System;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Marks a property of a class derived from <see cref="JsonSettings{T}"/> whose value is applied to the instance in use<br/>
    /// when the configuration file is modified while the application is running (<see cref="JsonSettings{T}.AutoUpdate"/>).<br/>
    /// Properties without this attribute keep the value read at startup until the application is restarted, unless the
    /// derived class copies them in <c>AutoUpdateCustomLogic</c>.
    /// </summary>
    /// <remarks>
    /// The attribute is taken into account only on public instance properties with a public getter and setter that are
    /// persisted in the file: properties marked with <see cref="System.Text.Json.Serialization.JsonIgnoreAttribute"/> are
    /// ignored.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class AppSettings : JsonSettings&lt;AppSettings&gt;
    /// {
    ///     [AutoUpdate]
    ///     public int LogLevel { get; set; }           // applied as soon as the file is modified
    ///
    ///     public string ListenAddress { get; set; }   // applied only after a restart
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AutoUpdateAttribute : Attribute
    {
    }
}
