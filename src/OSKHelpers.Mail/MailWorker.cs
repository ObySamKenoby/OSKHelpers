using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using OSKHelpers.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using OSKHelpers.INIFile;
using OSKHelpers.Common;
using System.Text.Json;

namespace OSKHelpers.Mail
{
    /// <summary>
    /// Helper class for simplifying email sending.
    /// </summary>
    public class MailWorker
    {
        #region File parsing properties

        /// <summary>
        /// File parsing: a '#' in the first position identifies a comment.
        /// </summary>
        public const char COMMENTSYMBOL = '#';
        /// <summary>
        /// File parsing: in the first position identifies the message subject.
        /// </summary>
        public const string SUBJECTTAG = "@Subject:";

        /// <summary>
        /// File parsing: maximum available iterations for tag substitution and condition evaluation before timeout.
        /// </summary>
        public const int MAXITERATIONS = 1000;

        #endregion

        #region Properties

        /// <summary>
        /// SMTP server address.
        /// </summary>
        public static string SmtpServer { get; set; }
        /// <summary>
        /// SMTP server communication port.
        /// </summary>
        public static int SmtpPort { get; set; }
        /// <summary>
        /// The SMTP server requires an SSL connection.
        /// </summary>
        public static bool SmtpSSL { get; set; }
        /// <summary>
        /// Username for SMTP authentication.
        /// </summary>
        public static string SmtpUsername { get; set; }
        /// <summary>
        /// Password for SMTP authentication.
        /// </summary>
        public static string SmtpPassword { get; set; }

        /// <summary>
        /// True if SMTP authentication is enabled.
        /// </summary>
        public static bool SmtpAuthEnabled => !string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword);
        /// <summary>
        /// True if the SMTP authentication configuration contains errors (missing username or password).
        /// </summary>
        public static bool SmtpAuthConfigError => string.IsNullOrWhiteSpace(SmtpUsername) ^ string.IsNullOrWhiteSpace(SmtpPassword);

        /// <summary>
        /// If set, allows sending mail without specifying a sender; in that case it will be used as the default sender.
        /// <see cref="SendAsync(IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        /// 
        /// </summary>
        public static string SmtpFrom { get; set; }

        /// <summary>
        /// IMAP server address.
        /// </summary>
        public static string ImapServer { get; set; }
        /// <summary>
        /// IMAP server communication port.
        /// </summary>
        public static int ImapPort { get; set; }
        /// <summary>
        /// The IMAP server requires an SSL connection.
        /// </summary>
        public static bool ImapSSL { get; set; }
        /// <summary>
        /// Username for IMAP authentication.
        /// </summary>
        public static string ImapUsername { get; set; }
        /// <summary>
        /// Password for IMAP authentication.
        /// </summary>
        public static string ImapPassword { get; set; }

        /// <summary>
        /// True if the IMAP authentication configuration contains errors (missing username or password).
        /// </summary>
        public static bool ImapAuthConfigError => string.IsNullOrWhiteSpace(ImapUsername) || string.IsNullOrWhiteSpace(ImapPassword);

        #endregion

        #region Methods

        /// <summary>
        /// Sets the log level.
        /// </summary>
        /// <param name="logLevel"></param>
        public static void SetLogLevel(LogLevel logLevel)
        {
            SimpleLog.LogLevel = logLevel;
        }

        /// <summary>
        /// Sets the default parameters for the SMTP connection.
        /// </summary>
        public static bool SetupSmtp(IniFileHelper iniFile)
        {
            SimpleLog.SetLogLevel(iniFile);
            SmtpServer      = iniFile.GetString(nameof(SmtpServer));
            SmtpPort        = iniFile.GetInt(nameof(SmtpPort));
            SmtpSSL         = iniFile.GetBool(nameof(SmtpSSL));
            SmtpUsername    = iniFile.GetString(nameof(SmtpUsername));
            SmtpPassword    = iniFile.GetString(nameof(SmtpPassword));
            SmtpFrom        = iniFile.GetString(nameof(SmtpFrom));
            if (SimpleLog.LogLevelDebug)
            {
                DumpStmpParameters();
            }
            return CheckSmtpParameters();
        }

        /// <summary>
        /// Sets the default parameters for the IMAP connection.
        /// </summary>
        public static bool SetupImap(IniFileHelper iniFile)
        {
            SimpleLog.SetLogLevel(iniFile);
            ImapServer = iniFile.GetString(nameof(ImapServer));
            ImapPort        = iniFile.GetInt(nameof(ImapPort));
            ImapSSL         = iniFile.GetBool(nameof(ImapSSL));
            ImapUsername    = iniFile.GetString(nameof(ImapUsername));
            ImapPassword    = iniFile.GetString(nameof(ImapPassword));
            if (SimpleLog.LogLevelDebug)
            {
                DumpImapParameters();
            }
            return CheckImapParameters();
        }

        /// <summary>
        /// Sets the default parameters for the SMTP connection.
        /// </summary>
        /// <param name="server">Server address.</param>
        /// <param name="port">Server port.</param>
        /// <param name="ssl">If true, SSL authentication is enabled (all available types are tried;
        /// works for example with Aruba smtps on port 465).</param>
        /// <param name="username">Username for server authentication. If this and the password are null, authentication is disabled.</param>
        /// <param name="password">Password for server authentication. If this and the username are null, authentication is disabled.</param>
        /// <param name="from">Optional; if not specified, allows sending without specifying a sender.</param>
        public static void SetupSmtp(string server, int port, bool ssl, string username, string password, string from = null)
        {
            SmtpServer      = server;
            SmtpPort        = port;
            SmtpSSL         = ssl;
            SmtpUsername    = username;
            SmtpPassword    = password;
            SmtpFrom        = from;
        }

        /// <summary>
        /// Sets the default parameters for the IMAP connection.
        /// </summary>
        /// <inheritdoc cref="SetupSmtp(string, int, bool, string, string, string)"/>
        public static void SetupImap(string server, int port, bool ssl, string username, string password)
        {
            ImapServer      = server;
            ImapPort        = port;
            ImapSSL         = ssl;
            ImapUsername    = username;
            ImapPassword    = password;
        }

        /// <summary>
        /// Displays or logs the stored SMTP parameters.<br/>
        /// The password, if present, is masked.
        /// </summary>
        /// <param name="console">Displays the settings in the console.</param>
        /// <param name="log">Logs the settings.</param>
        public static void DumpStmpParameters(bool console = true, bool log = false)
        {
            if (console)
            {
                Console.WriteLine($"{nameof(SmtpServer)}    => {SmtpServer}");
                Console.WriteLine($"{nameof(SmtpPort)}      => {SmtpPort}");
                Console.WriteLine($"{nameof(SmtpSSL)}       => {SmtpSSL}");
                Console.WriteLine($"{nameof(SmtpUsername)}  => {SmtpUsername}");
                Console.WriteLine($"{nameof(SmtpPassword)}  => {(string.IsNullOrWhiteSpace(SmtpPassword) ? string.Empty : "****")}");
                Console.WriteLine($"{nameof(SmtpFrom)}      => {SmtpFrom}");
            }
            if (log)
            {
                SimpleLog.Write($"{nameof(SmtpServer)}      => {SmtpServer}");
                SimpleLog.Write($"{nameof(SmtpPort)}        => {SmtpPort}");
                SimpleLog.Write($"{nameof(SmtpSSL)}         => {SmtpSSL}");
                SimpleLog.Write($"{nameof(SmtpUsername)}    => {SmtpUsername}");
                SimpleLog.Write($"{nameof(SmtpPassword)}    => {(string.IsNullOrWhiteSpace(SmtpPassword) ? string.Empty : "****")}");
                SimpleLog.Write($"{nameof(SmtpFrom)}        => {SmtpFrom}");
            }
        }

        /// <summary>
        /// Displays or logs the stored IMAP parameters.<br/>
        /// The password, if present, is masked.
        /// </summary>
        /// <param name="console">Displays the settings in the console.</param>
        /// <param name="log">Logs the settings.</param>
        public static void DumpImapParameters(bool console = true, bool log = false)
        {
            if (console)
            {
                Console.WriteLine($"{nameof(ImapServer)}    => {ImapServer}");
                Console.WriteLine($"{nameof(ImapPort)}      => {ImapPort}");
                Console.WriteLine($"{nameof(ImapSSL)}       => {ImapSSL}");
                Console.WriteLine($"{nameof(ImapUsername)}  => {ImapUsername}");
                Console.WriteLine($"{nameof(ImapPassword)}  => {(string.IsNullOrWhiteSpace(ImapPassword) ? string.Empty : "****")}");
            }
            if (log)
            {
                SimpleLog.Write($"{nameof(ImapServer)}    => {ImapServer}");
                SimpleLog.Write($"{nameof(ImapPort)}      => {ImapPort}");
                SimpleLog.Write($"{nameof(ImapSSL)}       => {ImapSSL}");
                SimpleLog.Write($"{nameof(ImapUsername)}  => {ImapUsername}");
                SimpleLog.Write($"{nameof(ImapPassword)}  => {(string.IsNullOrWhiteSpace(ImapPassword) ? string.Empty : "****")}");
            }
        }
        /// <summary>
        /// Displays or logs the stored SMTP and IMAP parameters.<br/>
        /// The password, if present, is masked.
        /// </summary>
        /// <param name="console">Displays the settings in the console.</param>
        /// <param name="log">Logs the settings.</param>
        public static void DumpParameters(bool console = true, bool log = false)
        {
            DumpStmpParameters(console,log);
            DumpImapParameters(console,log);
        }

        /// <summary>
        /// Verifies that the SMTP server data is present.<br/>
        /// Does not validate the data or check connectivity.<br/>
        /// For the username/password pair to be valid both must be present (authentication enabled) or absent (authentication disabled).<br/>
        /// If logResult is true (default), writes the verification result to the log.
        /// </summary>
        /// <returns></returns>
        public static bool CheckSmtpParameters(bool logResult = true)
        {
            var check =
                !string.IsNullOrWhiteSpace(SmtpServer) &&
                SmtpPort > 0 &&
                !SmtpAuthConfigError;

            if (logResult)
            {
                var msg = check
                    ? $"Dati SMTP corretti. Autenticazione abilitata: {SmtpAuthEnabled }."
                    : $"Verifica dati server SMTP fallita ({nameof(SmtpServer)}: {SmtpServer} - {nameof(SmtpPort)}: {SmtpPort} - " +
                      $"{nameof(SmtpSSL)}: {SmtpSSL} - Auth: {SmtpAuthEnabled} - Auth error: {SmtpAuthConfigError}).";
                SimpleLog.Write(msg);
                if (!MailUtils.ContainsSingleEmail(SmtpFrom))
                {
                    SimpleLog.Write($"Il mittente per le mail inviate ( {SmtpFrom} ) non è corretto, potrebbero verificarsi problemi in fase di invio.");
                }
            }

            return check;
        }

        /// <summary>
        /// Verifies that the IMAP server data is present.<br/>
        /// Does not validate the data or check connectivity.<br/>
        /// For the username/password pair to be valid both must be present (authentication enabled) or absent (authentication disabled).<br/>
        /// If logResult is true (default), writes the verification result to the log.
        /// </summary>
        /// <returns></returns>
        public static bool CheckImapParameters(bool logResult = true)
        {
            var check =
                !string.IsNullOrWhiteSpace(ImapServer) &&
                ImapPort > 0 &&
                !ImapAuthConfigError;

            if (logResult)
            {
                var msg = check
                    ? $"Dati IMAP corretti."
                    : $"Verifica dati server IMAP fallita ({nameof(ImapServer)}: {ImapServer} - {nameof(ImapPort)}: {ImapPort} - {nameof(ImapSSL)}: {ImapSSL} - Auth error: {ImapAuthConfigError}";
                SimpleLog.Write(msg);
            }

            return check;
        }

        #region Synchronous send methods

        /// <inheritdoc cref="SendReplyToAsync(string, string, string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string from, string to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(from, to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string from, string to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(from, to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(string, IEnumerable{string}, string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string from, IEnumerable<string> to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(from, to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(string, IEnumerable{string}, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string from, IEnumerable<string> to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(from, to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(string, string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(string to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(IEnumerable{string}, string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(IEnumerable<string> to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendReplyToAsync(IEnumerable{string}, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool SendReplyTo(IEnumerable<string> to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendReplyToAsync(to, replyTo, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendAsync(string, string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool Send(string from, string to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendAsync(from, to, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendAsync(string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool Send(string from, IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendAsync(from, to, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendAsync(string, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool Send(string to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendAsync(to, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendAsync(IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        public static bool Send(IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => SendAsync(to, subject, body, streamAttachments).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static bool Send(string server, int port, bool ssl, string username, string password, string from, IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null, IEnumerable<string> replyTo = null)
            => SendAsync(server, port, ssl, username, password, from, to, subject, body, streamAttachments, replyTo).ConfigureAwait(false).GetAwaiter().GetResult();

        #endregion

        #region Asynchronous send methods

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string from, string to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from, new List<string>() { to }, subject, body, streamAttachments, new List<string> { replyTo });

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string from, string to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from, new List<string>() { to }, subject, body, streamAttachments, replyTo);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string from, IEnumerable<string> to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from, to, subject, body, streamAttachments, new List<string> { replyTo});

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string from, IEnumerable<string> to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from, to, subject, body, streamAttachments, replyTo);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendReplyToAsync(new List<string>() { to }, new List<string> { replyTo }, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(string to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendReplyToAsync(new List<string>() { to }, replyTo, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(IEnumerable<string> to, string replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendReplyToAsync(to, new List<string>() { replyTo }, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendReplyToAsync(IEnumerable<string> to, IEnumerable<string> replyTo, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
        {
            var sent = false;

            if (!MailUtils.ContainsValidEmails(SmtpFrom))
            {
                SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: {nameof(SmtpFrom)} non è un indirizzo mail valido o non è  stato specificato => [ {SmtpFrom} ]");
            }
            else
            {
                sent = await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, SmtpFrom, to, subject, body, streamAttachments, replyTo);
            }

            return sent;
        }

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendAsync(string from, string to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from,  new List<string>() { to }, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendAsync(string from, IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, from, to, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendAsync(string to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
            => await SendAsync(new List<string>() { to }, subject, body, streamAttachments);

        /// <inheritdoc cref="SendAsync(string, int, bool, string, string, string, IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}}, IEnumerable{string})"/>
        public static async Task<bool> SendAsync(IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null)
        {
            var sent = false;

            if (!MailUtils.ContainsValidEmails(SmtpFrom))
            {
                SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}: {nameof(SmtpFrom)} non è un indirizzo mail valido o non è  stato specificato => [ {SmtpFrom} ]");
            }
            else
            {
                sent = await SendAsync(SmtpServer, SmtpPort, SmtpSSL, SmtpUsername, SmtpPassword, SmtpFrom, to, subject, body, streamAttachments);
            }

            return sent;
        }

        #endregion

        /// <summary>
        /// Sends an email using the server and credentials passed as parameters.
        /// </summary>
        /// <param name="server">SMTP server.</param>
        /// <param name="port">Port to use.</param>
        /// <param name="ssl">If True, uses an SSL connection.</param>
        /// <param name="username">Username for the SMTP server.</param>
        /// <param name="password">Password for the SMTP server.</param>
        /// <param name="from">Sender address.</param>
        /// <param name="to">Recipient addresses.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Content (HTML).</param>
        /// <param name="streamAttachments">List of tuples containing the file name and the stream to send as an attachment.<br />
        /// <param name="replyTo"/>Addresses to which the reply to the email should be sent.</param>
        /// The format is ((Filename, Content) File, Encoding), where:<br />
        /// <b>File.Filename</b> contains the name to assign to the attachment.<br />
        /// <b>File.Content</b> is a readable stream with the file content.<br />
        /// <b>Encoding</b> is the file encoding type (using a value from MediaTypeNames.Text is recommended).
        /// <returns>True if the send succeeded.</returns>
        public static async Task<bool> SendAsync(string server, int port, bool ssl, string username, string password, string from, IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null, IEnumerable<string> replyTo = null)
        {
            // Used to log a truncated version of the mail body
            var debugBody = body.Substring(0, body.Length > 50 ? 50 : body.Length);
            SimpleLog.Write(LogLevel.Debug, $"{SimpleLog.GetCallerTypeMethodName()}(From: '{from}', To: ['{string.Join("', '", to)}']{(replyTo?.Any() ?? false ? $", Reply-To: ['{string.Join("', '", replyTo)}']" : string.Empty)}, Subject: '{subject}', Body: '{debugBody}')");
            
            bool sent = false;

            if (!string.IsNullOrWhiteSpace(from) && to != null && to.Any() && !string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    // create email message
                    var email = new MimeMessage();
                    email.From.Add(MailboxAddress.Parse(from));
                    email.To.AddRange(to.Select(address => MailboxAddress.Parse(address)));
                    if (replyTo?.Any() ?? false)
                    {
                        email.ReplyTo.AddRange(replyTo.Where(address => address != null).Select(address => MailboxAddress.Parse(address)));
                    }
                    email.Subject       = subject;
                    var mailBody        = new BodyBuilder();
                    mailBody.HtmlBody   = body;
                    // Add attachments passed as streams
                    if (streamAttachments?.Any() ?? false)
                    {
                        foreach (var attachment in streamAttachments.Where(a => a.File.Content != null && a.File.Content.Length > 0))
                        {
                            if (attachment.File.Content.CanRead)
                            {
                                attachment.File.Content.Position = 0;
                                if (string.IsNullOrWhiteSpace(attachment.Encoding))
                                    await mailBody.Attachments.AddAsync(attachment.File.FileName, attachment.File.Content);
                                else
                                    await mailBody.Attachments.AddAsync(attachment.File.FileName, attachment.File.Content, ContentType.Parse(attachment.Encoding));
                            }
                        }
                    }
                    email.Body = mailBody.ToMessageBody();

                    // send email
                    using (var smtp = new SmtpClient())
                    {
                        await smtp.ConnectAsync(server, port, ssl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
                        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        {
                            await smtp.AuthenticateAsync(username, password);
                        }
                        await smtp.SendAsync(email);
                        await smtp.DisconnectAsync(true);
                    }
                    sent = true;
                    SimpleLog.Write(LogLevel.Debug, "Mail inviata correttamente");
                }
                catch (Exception ex)
                {
                    SimpleLog.Write(LogLevel.Error, $"Errore in {SimpleLog.GetCallerTypeMethodName()}('{from}','{to}','{subject}','{debugBody}'): {SimpleLog.FormattedException(ex)}");
                    sent = false;
                }
            }
            else
            {
                SimpleLog.Write(LogLevel.Warning, $"Dati non validi passati a {SimpleLog.GetCallerTypeMethodName()}('{from}','{to}','{subject}','{debugBody}')");
                sent = false;
            }

            return sent;
        }

        /// <summary>
        /// Parses the string passed in content, substituting tags and showing (or hiding) sections according to their conditions.<br/>
        /// Note: tags are processed after conditions.
        /// </summary>
        /// <param name="content">String to process.</param>
        /// <param name="tags">
        /// List of tags to find within content and for which Name should be replaced with Value.<br/>
        /// In content, tags must be written as <b>{TAG}</b>; within the list, Name must be without braces, i.e. <b>TAG</b>.<br/>
        /// Tag names are case sensitive.
        /// </param>
        /// <param name="conditions">
        /// List of conditions that allow sections of text to be shown or hidden.<br/>
        /// In content, conditions must be identified as <b>&lt;&lt;COND&gt;&gt;..&lt;&lt;/COND&gt;&gt;</b>; within the list Name must be without symbols, i.e. <b>COND</b>.<br/>
        /// Condition names are case sensitive.
        /// </param>
        /// <param name="tagName">Optional tag to remove (used when the method has been called to retrieve the value of a tag).</param>
        /// <returns>The processed string.</returns>
        public static string ParseString(string content, List<(string Name, string Value)> tags = null, List<(string Name, Func<bool> Check)> conditions = null, string tagName = null)
        {
            var res = string.Empty;

            try
            {
                List<(string Name, bool Check)> conds = null;

                if (conditions != null)
                {
                    conds = conditions
                        .Where(c => !string.IsNullOrWhiteSpace(c.Name) && c.Check != null)
                        .Select(c => (c.Name.Trim(), c.Check()))
                        .ToList();
                }

                res = ParseString(content, tags, conds, tagName);
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                res = string.Empty;
            }

            return res;
        }

        /// <inheritdoc cref="ParseString(string, List{ValueTuple{string, string}}, List{ValueTuple{string, Func{bool}}}, string)"/>
        public static string ParseString(string content, List<(string Name, string Value)> tags = null, List<(string Name, bool Check)> conditions = null, string tagName = null)
        {
            var iterations = 0;
            string output = string.Empty;

            try
            { 
                if (!string.IsNullOrWhiteSpace(content))
                {
                    output = content;
                    if (conditions?.Any() ?? false)
                    {
                        foreach(var condition in conditions.Where(c => !string.IsNullOrWhiteSpace(c.Name)).ToList())
                        {
                            var openTag = $"<<{condition.Name}>>";
                            var closingTag = $"<</{condition.Name}>>";

                            if (iterations < MAXITERATIONS && output.Contains(openTag) && output.Contains(closingTag) && output.IndexOf(closingTag) > output.IndexOf(openTag))
                            {
                                iterations = 0;
                                int textLen = 1;
                                while (textLen > 0 && iterations < MAXITERATIONS)
                                {
                                    var openStart       = output.IndexOf(openTag);
                                    var openEnd         = openStart + openTag.Length;
                                    var closingStart    = output.IndexOf(closingTag);
                                    var closingEnd      = closingStart + closingTag.Length;

                                    textLen = closingStart - openEnd;

                                    if (textLen > 0 && !condition.Check)
                                    {
                                        // If the content length is greater than zero and
                                        // the check fails, remove all text between the tags...
                                        output = output.Substring(0, openStart) + output.Substring(closingEnd);
                                    }
                                    else if (openStart >= 0 && openEnd >= 0 && closingStart > openEnd)
                                    {
                                        // ... otherwise remove only the tags
                                        output = $"{output.Substring(0, openStart)}{output.Substring(openEnd, closingStart - openEnd)}{output.Substring(closingEnd)}";
                                    }
                                    iterations++;
                                }
                            }
                        }
                    }
                    if (tags?.Any() ?? false)
                    {
                        foreach (var tag in tags.Where(t => !string.IsNullOrEmpty(t.Name)).ToList())
                        {
                            output = output.Replace($"{{{tag.Name}}}", tag.Value);
                        }
                    }
                    // Remove the tag if present.
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        if (output.Contains(tagName))
                        {
                            var start   = output.IndexOf(tagName);
                            var end     = start + tagName.Length;
                            output      = $"{output.Substring(0, start)}{output.Substring(end)}";
                        }
                    }

                    if (iterations >= MAXITERATIONS)
                    {
                        SimpleLog.Write($"{SimpleLog.GetCallerTypeMethodName()}('{content}', {JsonSerializer.Serialize(tags)}, {JsonSerializer.Serialize(conditions)}): timeout durante l'elaborazione della stringa.");
                        output = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                output = string.Empty;
            }

            return output?.Trim();
        }

        /// <summary>
        /// Processes the file passed as a series of lines (read via File.ReadAllLines), extracting the email subject and body.<br/>
        /// If the subject or body turn out to be empty, Parsed will be set to False.
        /// </summary>
        /// <param name="lines">Lines to process.</param>
        /// <param name="tags">
        /// List of tags to find within content and for which Name should be replaced with Value.<br/>
        /// In content, tags must be written as <b>{TAG}</b>; within the list Name must be without braces, i.e. <b>TAG</b>.<br/>
        /// Tag names are case sensitive.
        /// </param>
        /// <param name="conditions">
        /// List of conditions that allow sections of text to be shown or hidden.<br/>
        /// In content, conditions must be identified as <b>&lt;&lt;COND&gt;&gt;..&lt;&lt;/COND&gt;&gt;</b>; within the list Name must be without symbols, i.e. <b>COND</b>.<br/>
        /// Condition names are case sensitive.
        /// </param>
        /// <param name="customTags">
        /// Custom tags whose value should be retrieved from inside the template.<br/>
        /// Custom tags follow the same rules as the subject: they must start with '@', end with ':', appear at the beginning of the line, and are case sensitive.<br/>
        /// The leading '@' and trailing ':' are added automatically after trimming the entry; to request parsing of<br/>
        /// tags { '@AltSubject:', '@Value2' } pass the list { 'AltSubject', 'Value2' }; passing '   Value2 ' is equally valid<br/>
        /// and will be searched in the template as '@Value2:' but returned with the key '   Value2 '.<br/>
        /// The content of custom tags may contain any valid tag for the template type.<br/>
        /// Lines containing the tags will be removed.
        /// </param>
        /// <returns>
        /// A tuple with:<br />
        /// <b>Parsed</b>: the lines were processed correctly;<br/>
        /// <b>Subject</b>: email subject;<br/>
        /// <b>Body</b>: email body;<br/>
        /// <b>TagValues</b>: dictionary with <b>Key</b> = requested tag name (as passed in <paramref name="customTags"/>) and <b>Value</b> = detected value.<br/>
        /// If no custom tags are requested, null is returned; on error a dictionary containing empty string values is returned.<br/>
        /// Empty strings passed in <paramref name="customTags"/> are ignored and will not appear as keys; any other string is returned as passed.
        /// </returns>
        public static (bool Parsed, string Subject, string Body, Dictionary<string, string> TagValues) ParseFile(List<string> lines, List<(string Name, string Value)> tags = null, List<(string Name, Func<bool> Check)> conditions = null, List<string> customTags = null)
        {
            (bool Parsed, string Subject, string Body, Dictionary<string, string> TagValues) res = (false, string.Empty, string.Empty, null);

            try
            {
                List<(string Name, bool Check)> conds = null;
                
                if (conditions != null)
                {
                    conds = conditions
                        .Where(c => !string.IsNullOrWhiteSpace(c.Name) && c.Check != null)
                        .Select(c => (c.Name.Trim(), c.Check()))
                        .ToList();
                }
                
                res = ParseFile(lines, tags, conds, customTags);
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                res = (false, string.Empty, string.Empty, null);
            }

            return res;
        }

        /// <inheritdoc cref="ParseFile(List{string}, List{ValueTuple{string, string}}, List{ValueTuple{string, Func{bool}}}, List{string})"/>
        public static (bool Parsed, string Subject, string Body, Dictionary<string, string> TagValues) ParseFile (List<string> lines, List<(string Name, string Value)> tags = null, List<(string Name, bool Check)> conditions = null, List<string> customTags = null)
        {
            var parsed      = false;
            var subject     = string.Empty;
            var body        = string.Empty;
            var tagValues   = customTags != null ? customTags.Where(t => !string.IsNullOrWhiteSpace(t)).ToDictionary(t => t, t => string.Empty) : null;

            try
            {
                if (lines != null && lines.Any())
                {
                    var trimmedLines = lines.Select(l => l?.Trim()).ToList();
                    // Remove comment lines
                    foreach (var line in trimmedLines.Where(l => string.IsNullOrWhiteSpace(l) || l[0] == COMMENTSYMBOL).ToList())
                    {
                        trimmedLines.Remove(line);
                    }

                    // Retrieve the email subject.
                    subject = GetTagValue(SUBJECTTAG, trimmedLines, tags, conditions);

                    // Retrieve the value for the custom tags passed as parameter.
                    if (tagValues != null && tagValues.Count > 0)
                    {
                        foreach (var tag in tagValues)
                        {
                            tagValues[tag.Key] = GetTagValue($"@{tag.Key.Trim()}:", trimmedLines, tags, conditions);
                        }
                    }

                    // Parse the body only if there are remaining lines; otherwise the method fails.
                    if (lines.Any())
                    {
                        // Treat all remaining lines as a single string and parse the body.
                        body = ParseString(string.Join(" ", trimmedLines), tags, conditions);
                    }

                    parsed = !string.IsNullOrWhiteSpace(subject) && !string.IsNullOrWhiteSpace(body);
                    if (!parsed)
                    {
                        subject = string.Empty;
                        body = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                parsed      = false;
                subject     = string.Empty;
                body        = string.Empty;
                tagValues   = null;
            }

            return (parsed, subject, body, tagValues);
        }

        /// <summary>
        /// Searches for a line within <paramref name="lines"/> that starts with <paramref name="tag"/> and returns its value.<br/>
        /// If multiple matching lines are found, the value of the first is returned.<br/>
        /// If no lines are found, an empty string is returned.<br/>
        /// All lines matching <paramref name="tag"/> are removed from the template.
        /// </summary>
        /// <param name="tag">Tag to search for within <paramref name="lines"/>.</param>
        /// <returns>The value of <paramref name="tag"/> found as described above.</returns>
        /// <param name="lines">Lines to process.</param>
        /// <param name="tags">
        /// List of tags to find within content and for which Name should be replaced with Value.<br/>
        /// In content, tags must be written as <b>{TAG}</b>; within the list Name must be without braces, i.e. <b>TAG</b>.<br/>
        /// Tag names are case sensitive.
        /// </param>
        /// <param name="conditions">
        /// List of conditions that allow sections of text to be shown or hidden.<br/>
        /// In content, conditions must be identified as <b>&lt;&lt;COND&gt;&gt;..&lt;&lt;/COND&gt;&gt;</b>; within the list Name must be without symbols, i.e. <b>COND</b>.<br/>
        /// Condition names are case sensitive.
        /// </param>
        private static string GetTagValue(string tag, List<string> lines, List<(string Name, string Value)> tags, List<(string Name, bool Check)> conditions)
        {
            // Value to be returned.
            string value = string.Empty;

            // Line identified as containing the subject.
            string line = null;

            // Search for the line containing the subject and parse it.
            // The loop ensures that if multiple subject lines exist, the first valid one is used.
            do
            {
                line = lines.Where(l => l.Trim().Contains(tag)).FirstOrDefault();
                if (line != null)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = ParseString(line, tags, conditions, tag).Trim();
                    }
                    lines.Remove(line);
                }
            }
            while (!string.IsNullOrWhiteSpace(line));
            
            return value;
        }

        #endregion
    }
}