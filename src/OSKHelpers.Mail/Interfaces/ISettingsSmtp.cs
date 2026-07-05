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
        /// SMTP Username,
        /// </summary>
        string SmtpUsername { get; set; }
        /// <summary>
        /// SMTP password,
        /// </summary>
        string SmtpPassword { get; set; }
        /// <summary>
        /// Sender of the emails that will be sent by the service,
        /// </summary>
        string SmtpFrom { get; set; }
    }
}
