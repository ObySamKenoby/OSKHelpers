namespace OSKHelpers.Logging
{
    /// <summary>
    /// Defines the severity levels used by the logging infrastructure.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>No logging.</summary>
        None     = 0,
        /// <summary>Errors only.</summary>
        Error    = 1,
        /// <summary>Warnings and above.</summary>
        Warning  = 2,
        /// <summary>Informational messages and above.</summary>
        Info     = 3,
        /// <summary>Debug messages and above.</summary>
        Debug    = 4,
        /// <summary>Full protocol-level tracing.</summary>
        Protocol = 5
    }
}
