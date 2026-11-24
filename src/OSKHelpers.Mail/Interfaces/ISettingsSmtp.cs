namespace OSKHelpers.Mail.Interfaces
{
    /// <summary>
    /// Default Setting interface for SMTP usage
    /// </summary>
    public interface ISettingsSmtp
    {
        /// <summary>
        /// SMTP server address.
        /// </summary>
        string SmtpServer { get; set;  }
        /// <summary>
        /// SMTP server port,
        /// </summary>
        int SmtpPort { get; set; }
        /// <summary>
        /// True if SSL must be used with SMTP.
        /// </summary>
        bool SmtpSSL { get; set; }
        /// <summary>
        /// True if the SMTP connection must be upgraded to TLS with STARTTLS (e.g. port 587); used only when
        /// <see cref="SmtpSSL"/> is false. Sets <see cref="MailWorker.SmtpStartTls"/>.
        /// </summary>
        bool SmtpStartTls { get; set; }
        /// <summary>
        /// SMTP Username,
        /// </summary>
        string SmtpUsername { get; set; }
        /// <summary>
        /// SMTP password, in plain text or encrypted with <see cref="OSKHelpers.Security.CredentialProtector"/>.<br/>
        /// <see cref="MailWorker.Setup{T}(T, string, OSKHelpers.Security.CredentialProtector, bool)"/> replaces a
        /// plain-text password with the encrypted one.
        /// </summary>
        string SmtpPassword { get; set; }
        /// <summary>
        /// Sender of the emails that will be sent by the service,
        /// </summary>
        string SmtpFrom { get; set; }
    }
}
