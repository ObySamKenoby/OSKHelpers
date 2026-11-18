namespace OSKHelpers.Json
{
    /// <summary>
    /// Outcome of <see cref="JsonSettings{T}.AutoUpdate"/>.
    /// </summary>
    public enum JsonSettingsAutoUpdateStatus
    {
        /// <summary>
        /// The file has not been modified since the last read and the last update had no problems: nothing has been done.
        /// </summary>
        NoChange,

        /// <summary>
        /// The file has been read and is valid: the properties marked with <see cref="AutoUpdateAttribute"/> have been
        /// applied to the instance in use (possibly none, if their values did not change).
        /// </summary>
        Applied,

        /// <summary>
        /// The file is missing, unreadable or not valid, the custom logic refused it, or the resulting configuration is not
        /// valid: the instance in use keeps its values. The read is attempted again at every call until it succeeds.
        /// </summary>
        Rejected,

        /// <summary>
        /// The file could not be accessed (e.g. an editor is still saving it): nothing has changed and the read is attempted
        /// again at the next call.
        /// </summary>
        FileBusy
    }
}
