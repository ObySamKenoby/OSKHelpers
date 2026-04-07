using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.ExtensionMethods
{
    /// <summary>
    /// Common extension methods for all objects.
    /// </summary>
    public static class ObjectExtensionMethods
    {
        /// <inheritdoc cref="ObjectUtils.Dump{T}(T, string, bool, LogLevel)"/>
        public static void OSKDump(this object obj, string objName = "", bool log = false, LogLevel logLevel = LogLevel.Error) 
            => ObjectUtils.Dump(obj, objName, log, logLevel);

        /// <inheritdoc cref="ObjectUtils.IsNumeric(object)"/>
        public static void OSKIsNumeric(this object obj)
            => ObjectUtils.IsNumeric(obj);
    }
}
