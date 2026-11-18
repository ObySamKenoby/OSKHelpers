using System.Collections.Generic;

namespace OSKHelpers.Json
{
    /// <summary>
    /// Result of <see cref="JsonSettings{T}.AutoUpdate"/>.
    /// </summary>
    public sealed class JsonSettingsAutoUpdateResult
    {
        #region Properties

        /// <summary>
        /// Outcome of the update.
        /// </summary>
        public JsonSettingsAutoUpdateStatus Status { get; }

        /// <summary>
        /// True if the modification date of the file changed since the last read (including its creation or deletion);<br/>
        /// false if nothing changed or the call is only a new attempt after a previous problem. Useful to report the
        /// problems only once for each version of the file.
        /// </summary>
        public bool FileChanged { get; }

        /// <summary>
        /// True if the file has been rewritten during the read (version update, see <see cref="JsonSettings{T}.UpdateVersion"/>).
        /// </summary>
        public bool FileSaved { get; }

        /// <summary>
        /// Names of the properties marked with <see cref="AutoUpdateAttribute"/> whose value has been changed on the instance
        /// in use; empty unless <see cref="Status"/> is <see cref="JsonSettingsAutoUpdateStatus.Applied"/>.
        /// </summary>
        public IReadOnlyList<string> ChangedProperties { get; }

        /// <summary>
        /// Problems to report: reasons of a <see cref="JsonSettingsAutoUpdateStatus.Rejected"/> outcome or messages added by the
        /// custom logic of the derived class (also with <see cref="JsonSettingsAutoUpdateStatus.Applied"/>).
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the result.
        /// </summary>
        /// <param name="status">Outcome of the update.</param>
        /// <param name="fileChanged">True if the modification date of the file changed since the last read.</param>
        /// <param name="fileSaved">True if the file has been rewritten during the read.</param>
        /// <param name="changedProperties">Names of the properties changed on the instance in use.</param>
        /// <param name="problems">Problems to report.</param>
        public JsonSettingsAutoUpdateResult(JsonSettingsAutoUpdateStatus status, bool fileChanged, bool fileSaved,
            IReadOnlyList<string> changedProperties, IReadOnlyList<string> problems)
        {
            Status              = status;
            FileChanged         = fileChanged;
            FileSaved           = fileSaved;
            ChangedProperties   = changedProperties ?? new List<string>();
            Problems            = problems ?? new List<string>();
        }

        #endregion
    }
}
