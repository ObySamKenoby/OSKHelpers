namespace OSKHelpers.Mail.Interfaces
{
    /// <summary>
    /// Default Setting interface for IMAP usage
    /// </summary>
    public interface ISettingsImap
    {
        /// <summary>
        /// IMAP Server.
        /// </summary>
        string ImapServer { get; set; }
        /// <summary>
        /// IMAP port,
        /// </summary>
        int ImapPort { get; set; }
        /// <summary>
        /// True if SSL must be used with IMAP.
        /// </summary>
        bool ImapSSL { get; set; }
        /// <summary>
        /// IMAP username.
        /// </summary>
        string ImapUsername { get; set; }
        /// <summary>
        /// IMAP password, in plain text or encrypted with <see cref="OSKHelpers.Security.CredentialProtector"/>.<br/>
        /// <see cref="MailWorker.Setup{T}(T, string, OSKHelpers.Security.CredentialProtector, bool, bool)"/> replaces a
        /// plain-text password with the encrypted one.
        /// </summary>
        string ImapPassword { get; set; }

    }
}
