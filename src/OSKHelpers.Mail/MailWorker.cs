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
    /// Classe di supporto per semplificare l'invio di mail.
    /// </summary>
    public class MailWorker
    {
        #region Proprietà relative al parsing dei file

        /// <summary>
        /// Parsing del file: in prima posizione il simbolo # identifica un commento.
        /// </summary>
        public const char COMMENTSYMBOL = '#';
        /// <summary>
        /// Parsing del file: in prima posizione identifica l'oggetto del messaggio.
        /// </summary>
        public const string SUBJECTTAG = "@Subject:";

        /// <summary>
        /// PArsing del file: massime iterazioni disponibili per la sostituzione di Tag e verifica delle condizioni prima del timeout
        /// </summary>
        public const int MAXITERATIONS = 1000;

        #endregion

        #region Proprietà

        /// <summary>
        /// Indirizzo del server SMTP.
        /// </summary>
        public static string SmtpServer { get; set; }
        /// <summary>
        /// Porta di comunicazione server SMTP.
        /// </summary>
        public static int SmtpPort { get; set; }
        /// <summary>
        /// Il server SMTP richiede connessione SSL.
        /// </summary>
        public static bool SmtpSSL { get; set; }
        /// <summary>
        /// Nome utente per autenticazione SMTP.
        /// </summary>
        public static string SmtpUsername { get; set; }
        /// <summary>
        /// Password per autenticazione SMTP.
        /// </summary>
        public static string SmtpPassword { get; set; }

        /// <summary>
        /// Se True l'autenticazione SMTP è abilitata.
        /// </summary>
        public static bool SmtpAuthEnabled => !string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword);
        /// <summary>
        /// Se True l'autenticazione SMTP contiene errori di configurazione (username o password assenti)
        /// </summary>
        public static bool SmtpAuthConfigError => string.IsNullOrWhiteSpace(SmtpUsername) ^ string.IsNullOrWhiteSpace(SmtpPassword);

        /// <summary>
        /// Se presente permette l'invio di mail senza specificare il mittente, in tal caso sarà utilizzato come mittente di default.
        /// <see cref="SendAsync(IEnumerable{string}, string, string, IEnumerable{ValueTuple{ValueTuple{string, Stream}, string}})"/>
        /// 
        /// </summary>
        public static string SmtpFrom { get; set; }

        /// <summary>
        /// Indirizzo server IMAP.
        /// </summary>
        public static string ImapServer { get; set; }
        /// <summary>
        /// Porta di comunicazione server IMAP.
        /// </summary>
        public static int ImapPort { get; set; }
        /// <summary>
        /// Il server IMAP richiede connessione SSL.
        /// </summary>
        public static bool ImapSSL { get; set; }
        /// <summary>
        /// Nome utente per l'autenticazione IMAP.
        /// </summary>
        public static string ImapUsername { get; set; }
        /// <summary>
        /// Password per l'autenticazione IMAP.
        /// </summary>
        public static string ImapPassword { get; set; }

        /// <summary>
        /// Se True l'autenticazione IMAP contiene errori di configurazione (username o password assenti)
        /// </summary>
        public static bool ImapAuthConfigError => string.IsNullOrWhiteSpace(ImapUsername) || string.IsNullOrWhiteSpace(ImapPassword);

        #endregion

        #region Metodi

        /// <summary>
        /// Imposta il livello di log.
        /// </summary>
        /// <param name="logLevel"></param>
        public static void SetLogLevel(LogLevel logLevel)
        {
            SimpleLog.LogLevel = logLevel;
        }

        /// <summary>
        /// Imposta i parametri di default per la connessione SMTP
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
        /// Imposta i parametri di default per la connessione IMAP
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
        /// Imposta i parametri di default per la connessione SMTP
        /// </summary>
        /// <param name="server">Indirizzo del server.</param>
        /// <param name="port">Porta del server.</param>
        /// <param name="ssl">Se true viene abilitata l'autenticazione SSL (vengono tentate le varie tipologie, 
        /// funziona ad esempio con l'smtps aruba su porta 465).</param>
        /// <param name="username">Nome utente per l'autenticazione al server. Se questo e la password sono nulli l'autenticazione viene disattivata.</param>
        /// <param name="password">Password per l'autenticazione al server. Se questo es il nome utente sono nulli l'autenticazione viene disattivata.</param>
        /// <param name="from">Opzionale, se non specificato abilita l'invio senza specificare il mittente.</param>
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
        /// Imposta i parametri di default per la connessione IMAP
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
        /// Visualizza o effettua il log dei parametri SMTP memorizzati.<br/>
        /// La password, se presente, è oscurata.
        /// </summary>
        /// <param name="console">Visualizza le impostazioni nella console.</param>
        /// <param name="log">Effettua il log delle impostazioni.</param>
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
        /// Visualizza o effettua il log dei parametri IMAP memorizzati.<br/>
        /// La password, se presente, è oscurata.
        /// </summary>
        /// <param name="console">Visualizza le impostazioni nella console.</param>
        /// <param name="log">Effettua il log delle impostazioni.</param>
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
        /// Visualizza o effettua il log dei parametri SMTP e IMAP memorizzati.<br/>
        /// La password, se presente, è oscurata.
        /// </summary>
        /// <param name="console">Visualizza le impostazioni nella console.</param>
        /// <param name="log">Effettua il log delle impostazioni.</param>
        public static void DumpParameters(bool console = true, bool log = false)
        {
            DumpStmpParameters(console,log);
            DumpImapParameters(console,log);
        }

        /// <summary>
        /// Verifica che i dati del server SMTP siano presenti.<br/>
        /// Non esegue una verifica di correttezza dei dati o un controllo di connettività.<br/>
        /// Perché la coppia username / password sia valida devono essere entrambi presenti (autenticazione abilitata) o assenti (autenticazione non abilitata).<br/>
        /// Se logResult è true (default) scrive nel log il risultato della verifica.
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
        /// Verifica che i dati del server IMAP siano presenti.<br/>
        /// Non esegue una verifica di correttezza dei dati o un controllo di connettività.<br/>
        /// Perché la coppia username / password sia valida devono essere entrambi presenti (autenticazione abilitata) o assenti (autenticazione non abilitata).<br/>
        /// Se logResult è true (default) scrive nel log il risultato della verifica.
        /// </summary>
        /// <returns></returns>
        public static bool CheckImapParameters(bool logResult = true)
        {
            var check =
                !string.IsNullOrWhiteSpace(ImapServer) &&
                SmtpPort > 0 &&
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

        #region Metodi per invio sincorono

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

        #region Metodi per invio asincrono

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
        /// Invia una mail utilizzando il server e le credenziali passate come parametro
        /// </summary>
        /// <param name="server">Server SMTP</param>
        /// <param name="port">Porta da utilizzare</param>
        /// <param name="ssl">Se True utilizza una connessione SSL</param>
        /// <param name="username">Nome utente per il server SMTP</param>
        /// <param name="password">Password per il server SMTP</param>
        /// <param name="from">Indirizzo mittente</param>
        /// <param name="to">Indirizzi dei destinatari</param>
        /// <param name="subject">Oggetto della mail</param>
        /// <param name="body">Contenuto (HTML)</param>
        /// <param name="streamAttachments">Elenco di tuple contenenti il nome del file e lo stream da inviare come allegato.<br />
        /// <param name="replyTo"/>Indirizzi cui la risposta alla mail deve pervenire.</param>
        /// Il formato è ((Filename, Content) File, Encoding), dove:<br />
        /// <b>File.Filename</b> contiene il nome del file da assegnare all'allegato<br />
        /// <b>File.Content</b> è uno stream abilitato alla lettura con il contenuto del file<br />
        /// <b>Encoding</b> è il tipo di encoding del file (è conveniente usare uno dei valori di MediaTypeNames.Text)
        /// <returns>True se l'invio è andato a buon fine.</returns>
        public static async Task<bool> SendAsync(string server, int port, bool ssl, string username, string password, string from, IEnumerable<string> to, string subject, string body, IEnumerable<((string FileName, Stream Content) File, string Encoding)> streamAttachments = null, IEnumerable<string> replyTo = null)
        {
            // Utilizzato per effettuare un log del corpo della mail di dimensioni ridotte
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
                    // Aggiunge gli attachment passati come stream
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
        /// Effettua il parsing della stringa passata in content sostituendo i Tags e mostrando (o nascondendo) le sezioni a seconda delle relative condizioni.<br/>
        /// Attenzione: i tag vengono elaborati dopo le condizioni.
        /// </summary>
        /// <param name="content">stringa da elaborare.</param>
        /// <param name="tags">
        /// Lista dei tag da cercare all'interno di content e per cui sostituire Name con Value.<br/>
        /// In content i tag devono essere scritti come <b>{TAG}</b>, all'interno della lista Name deve essere privo delle parentesi, quindi <b>TAG</b>.<br/>
        /// I nomi dei tag sono case sensitive.
        /// </param>
        /// <param name="conditions">
        /// Lista di condizioni che permettono di mostrare o nascondere parti del testo.<br/>
        /// In content le condizioni devono essere identificate come <b>&lt;&lt;COND&gt;&gt;..&lt;&lt;/COND&gt;&gt;</b> all'interno della lista Name deve essere privo dei simboli, quindi <b>COND</b>.<br/>
        /// I nomi delle condizioni sono case sensitive.
        /// </param>
        /// <param name="tagName">Eventuale tag da rimuovere (utilizzato nel caso in cui il metodi sia stato richiamato per recuperare il valore di un tag).</param>
        /// <returns>La stringa elaborata.</returns>
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
                                        // Nel caso in cui la lunghezza del contenuto sia maggiore di zero ed
                                        // il check fallisca si rimuove tutto il testo tra i tag...
                                        output = output.Substring(0, openStart) + output.Substring(closingEnd);
                                    }
                                    else if (openStart >= 0 && openEnd >= 0 && closingStart > openEnd)
                                    {
                                        // ... altrimenti si rimuovono i soli tag
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
                    // Rimuove, se presente, il tag.
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
        /// Elabora il file passato come serie di righe (lette attraverso File.ReadAllLines) estraendone oggettoe corpo della mail.<br/>
        /// Se l'oggetto od il corpo della mail risultano vuoti Parsed sarà valorizzato a False.
        /// </summary>
        /// <param name="lines">Righe da elaborare.</param>
        /// <param name="tags">
        /// Lista dei tag da cercare all'interno di content e per cui sostituire Name con Value.<br/>
        /// In content i tag devono essere scritti come <b>{TAG}</b>, all'interno della lista Name deve essere privo delle parentesi, quindi <b>TAG</b>.<br/>
        /// I nomi dei tag sono case sensitive.
        /// </param>
        /// <param name="conditions">
        /// Lista di condizioni che permettono di mostrare o nascondere parti del testo.<br/>
        /// In content le condizioni devono essere identificate come <b>&lt;&lt;COND&gt;&gt;..&lt;&lt;/COND&gt;&gt;</b> all'interno della lista Name deve essere privo dei simboli, quindi <b>COND</b>.<br/>
        /// I nomi delle condizioni sono case sensitive.
        /// </param>
        /// <param name="customTags">
        /// Tag Custom di cui recuperare il valore dall'interno del template.<br/>
        /// I tag custom obbediscono alle stesse regole del soggetto, devono iniziare con '@', terminare con ':', essere posti all'inizio della riga e sono case sensitive.<br/>
        /// Ii carattere iniziale e finale saranno automaticamente aggiunti dopo aver effettuato il trim della voce, quindi per richiedere l'interpretazione dei <br/>
        /// tag { '@AltSubject:', '@Value2' } sarà necessario passare la lista di valori { 'AltSubject' , 'Value2' }, passare '   Value2 ' sarà ugualmente considerato valido<br/>
        /// e sarà ricercato all'interno del template come '@Value2:' ma sarà restituito con la chiave '   Value2 '
        /// Il contenuto dei tag personalizzati può contenere qualsiasi tag valido per il tipo di template.<br/>
        /// Le righe contenenti i tag saranno rimosse.
        /// </param>
        /// <returns>
        /// Una tupla con:<br />
        /// <b>Parsed</b>: le righe sono state correttamente elaborate;<br/>
        /// <b>Subject</b>: oggetto della mail;<br/>
        /// <b>Body</b>: corpo della mail;<br/>
        /// <b>TagFiles</b>: Dizionario contenente in <b>Key</b> il nome del tag richiesto (così come passato in <paramref name="customTags"/>) e in <b>Value</b> il valore rilevato.<br/>
        /// Se non sono richiesti tag custom sarà restituito null, in caso di errore durtante l'esecuzione del metodo sarà restituito un dizionario contenente stringhe vuote come valori.<br/>
        /// Eventuali stringhe vuote passate in <paramref name="customTags"/> saranno ignorate e non saranno presenti come chiavi, qualsiasi altra stringa sarà restituita così come passata.
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
                    // Rimuove le righe di commento
                    foreach (var line in trimmedLines.Where(l => string.IsNullOrWhiteSpace(l) || l[0] == COMMENTSYMBOL).ToList())
                    {
                        trimmedLines.Remove(line);
                    }

                    // Recupera l'oggetto della mail.
                    subject = GetTagValue(SUBJECTTAG, trimmedLines, tags, conditions);
                    
                    // Recupera il valore per i tag custom passati come parametro.
                    if (tagValues != null && tagValues.Count > 0)
                    {
                        foreach (var tag in tagValues)
                        {
                            tagValues[tag.Key] = GetTagValue($"@{tag.Key.Trim()}:", trimmedLines, tags, conditions);
                        }
                    }

                    // Solo se esistono ulteriori righe si effettua il parse del testo, altrimenti il metodo fallisce
                    if (lines.Any())
                    {
                        // Considerando tutte le righe rimanenti come un'unica stringa effettua il parse del body
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
        /// Ricerca se disponibile una riga all'interno di <paramref name="lines"/> che inizi con <paramref name="tag"/> e ne restituisce il valore.<br/>
        /// Se fossero presenti più righe sarà restituito il valore della prima.<br/>
        /// Se non fossero presenti righe sarà restituita una stringa vuota.<br/>
        /// Tutte le righe corrispondenti a <paramref name="tag"/> saranno rimosse dal template.
        /// </summary>
        /// <param name="tag">Tag da ricercare all'interno di <paramref name="lines"/>.</param>
        /// <returns>Il valore di <paramref name="tag"/> trovato secondo le indicazioni di cui sopra.</returns>
        /// <inheritdoc cref="ParseFile(List{string}, List{ValueTuple{string, string}}, List{ValueTuple{string, Func{bool}}}, List{string})"/>
        private static string GetTagValue(string tag, List<string> lines, List<(string Name, string Value)> tags, List<(string Name, bool Check)> conditions)
        {
            // Valore che sarà restituito.
            string value = string.Empty;

            // Linea identificata come contenente il soggetto.
            string line = null;

            // Cerca l'eventuale riga relativa all'oggetto e ne effettua il parse.
            // Il ciclo viene utilizzato per far sì che se ci fossero più righe di soggetto venga presa la prima valida
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