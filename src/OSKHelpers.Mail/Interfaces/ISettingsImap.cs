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
        /// IMAP password.
        /// </summary>
        string ImapPassword { get; set; }

    }
}
