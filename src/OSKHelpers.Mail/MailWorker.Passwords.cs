using OSKHelpers.Common;
using OSKHelpers.INIFile;
using OSKHelpers.Json;
using OSKHelpers.Logging;
using OSKHelpers.Security;
using System;
using System.IO;

namespace OSKHelpers.Mail
{
    /// <summary>
    /// Password management of <see cref="MailWorker"/>: storage in memory, encryption with <see cref="CredentialProtector"/>
    /// and default protector, whose entropy is read from <see cref="DEFAULTENTROPYFILENAME"/>.
    /// </summary>
    public partial class MailWorker
    {
        #region Constants

        /// <summary>
        /// Name of the file, in <see cref="Paths.DefaultConfigsDirectory"/>, containing the entropy of the default
        /// <see cref="CredentialProtector"/>, used when no protector is passed to the setup methods.<br/>
        /// The file is created on first use with <see cref="DEFAULTENTROPYSIZE"/> random bytes and, on systems other than
        /// Windows, is readable only by its owner. Deleting or replacing it makes the passwords encrypted with the default
        /// protector no longer decryptable: they must be written again in plain text in the configuration.
        /// </summary>
        public const string DEFAULTENTROPYFILENAME = "__mail.entropy";

        /// <summary>
        /// Length in bytes of the default entropy (256 bit), stored in <see cref="DEFAULTENTROPYFILENAME"/>.
        /// </summary>
        public const int DEFAULTENTROPYSIZE = CredentialProtector.AESKEYSIZE;

        #endregion

        #region Members

        /// <summary>
        /// SMTP password as stored in the configuration (plain text or encrypted with <see cref="_smtpProtector"/>), obfuscated
        /// with <see cref="PasswordObfuscator"/> (see <see cref="SmtpPassword"/>).
        /// </summary>
        private static string _smtpPassword;
        /// <summary>
        /// Protector received by the setup to decrypt <see cref="_smtpPassword"/>; null for a plain-text password or for the
        /// default protector.
        /// </summary>
        private static CredentialProtector _smtpProtector;
        /// <summary>
        /// IMAP password as stored in the configuration (plain text or encrypted with <see cref="_imapProtector"/>), obfuscated
        /// with <see cref="PasswordObfuscator"/> (see <see cref="ImapPassword"/>).
        /// </summary>
        private static string _imapPassword;
        /// <summary>
        /// Protector received by the setup to decrypt <see cref="_imapPassword"/>; null for a plain-text password or for the
        /// default protector.
        /// </summary>
        private static CredentialProtector _imapProtector;

        /// <summary>
        /// Default entropy read from <see cref="_defaultEntropyPath"/>, obfuscated with <see cref="PasswordObfuscator"/>.
        /// </summary>
        private static string _defaultEntropy;
        /// <summary>
        /// SHA-256 hash of the default entropy: detects an obfuscated value no longer readable after
        /// <see cref="PasswordObfuscator.ReSeed"/>, so that the entropy is read again instead of using a wrong value.
        /// </summary>
        private static string _defaultEntropyHash;
        /// <summary>
        /// Path of the file <see cref="_defaultEntropy"/> was read from; when <see cref="Paths.DefaultConfigsDirectory"/>
        /// changes, the entropy is read from the new path.
        /// </summary>
        private static string _defaultEntropyPath;
        /// <summary>
        /// Lock of the default entropy members.
        /// </summary>
        private static readonly object _defaultEntropyLock = new object();

        #endregion

        #region Methods

        #region Password protection

        /// <summary>
        /// Checks which passwords of an INI file are stored in plain text and therefore should be encrypted
        /// (<see cref="CredentialProtector.RequiresProtection(string)"/>).<br/>
        /// The file is not modified: encrypting the passwords of an INI file is up to the application.
        /// </summary>
        /// <param name="iniFile">INI file containing the <c>SmtpPassword</c> and <c>ImapPassword</c> keys.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>SmtpRequiresProtection</b>: true if <c>SmtpPassword</c> is not empty and not encrypted;<br/>
        /// <b>ImapRequiresProtection</b>: true if <c>ImapPassword</c> is not empty and not encrypted.<br/>
        /// A missing or empty key returns false.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="iniFile"/> is null.</exception>
        public static (bool SmtpRequiresProtection, bool ImapRequiresProtection) CheckIniPasswordsProtection(IniFileHelper iniFile)
        {
            if (iniFile == null)
            {
                throw new ArgumentNullException(nameof(iniFile));
            }

            var smtpRequiresProtection = CredentialProtector.RequiresProtection(iniFile.GetString(nameof(SmtpPassword)));
            var imapRequiresProtection = CredentialProtector.RequiresProtection(iniFile.GetString(nameof(ImapPassword)));

            return (smtpRequiresProtection, imapRequiresProtection);
        }

        /// <summary>
        /// Encrypts a password to store in a configuration file, e.g. one reported by
        /// <see cref="CheckIniPasswordsProtection(IniFileHelper)"/>, with <paramref name="protector"/> or, if null, with the
        /// default protector (entropy read from <see cref="DEFAULTENTROPYFILENAME"/>), the same used by the setup methods
        /// when no protector is passed.<br/>
        /// A password already encrypted, or empty, is returned unchanged.
        /// </summary>
        /// <param name="password">Password to encrypt.</param>
        /// <param name="protector">Protector used to encrypt the password; null for the default protector.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Success</b>: false if the password requires encryption but it cannot be encrypted (default protector not
        /// available, encryption error; errors are logged);<br/>
        /// <b>Value</b>: the encrypted password, or <paramref name="password"/> if unchanged or not encryptable.
        /// </returns>
        /// <example>
        /// <code>
        /// var iniFile = new IniFileHelper(true);
        /// if (MailWorker.CheckIniPasswordsProtection(iniFile).SmtpRequiresProtection)
        /// {
        ///     var (success, encrypted) = MailWorker.ProtectPassword(iniFile.GetString("SmtpPassword"));
        ///     if (success)
        ///     {
        ///         iniFile.Set("SmtpPassword", encrypted);
        ///         iniFile.Save();
        ///     }
        /// }
        /// MailWorker.SetupSmtp(iniFile);
        /// </code>
        /// </example>
        public static (bool Success, string Value) ProtectPassword(string password, CredentialProtector protector = null)
        {
            var success = true;
            var result  = password;

            if (CredentialProtector.RequiresProtection(password))
            {
                result  = ProtectSettingsPassword(password, protector ?? CreateDefaultProtector(), nameof(password));
                success = CredentialProtector.IsProtected(result);
            }

            return (success, result);
        }

        #endregion

        #region Default protector

        /// <summary>
        /// Creates the default protector, with the entropy read from <see cref="DEFAULTENTROPYFILENAME"/>
        /// (see <see cref="GetDefaultEntropy"/>).<br/>
        /// The instance contains the entropy in plain text: it must be used and released, never stored.
        /// </summary>
        /// <returns>The default protector; null if the entropy file cannot be read or created (error logged).</returns>
        private static CredentialProtector CreateDefaultProtector()
        {
            CredentialProtector result;

            try
            {
                result = new CredentialProtector(GetDefaultEntropy());
            }
            catch (Exception ex)
            {
                SimpleLog.Write(LogLevel.Error, $"Unable to read or create the default entropy file {DEFAULTENTROPYFILENAME} in " +
                    $"{Paths.DefaultConfigsDirectory}: the passwords cannot be encrypted or decrypted with the default protector.");
                SimpleLog.LogError(ex);
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Returns the default entropy: the Base64 of the <see cref="DEFAULTENTROPYSIZE"/> bytes of
        /// <see cref="DEFAULTENTROPYFILENAME"/> in <see cref="Paths.DefaultConfigsDirectory"/>.<br/>
        /// The file (and its directory) is created on first use with
        /// <see cref="CredentialProtector.GetOrCreateKey(string)"/>: random bytes, exclusive creation and, on systems other than
        /// Windows, permissions restricted to the owner. The value is kept in memory obfuscated, and read again from the file
        /// when the directory changes or the obfuscated value is no longer readable (<see cref="PasswordObfuscator.ReSeed"/>).
        /// </summary>
        /// <returns>The default entropy.</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">Existing but invalid entropy file.</exception>
        /// <exception cref="IOException">Entropy file not readable or not creatable.</exception>
        private static string GetDefaultEntropy()
        {
            string result = null;

            lock (_defaultEntropyLock)
            {
                var path = Path.Combine(Paths.DefaultConfigsDirectory, DEFAULTENTROPYFILENAME);

                if (_defaultEntropy != null && string.Equals(path, _defaultEntropyPath, StringComparison.Ordinal))
                {
                    result = TryRevealObfuscatedValue(_defaultEntropy);
                }
                if (result == null || !string.Equals(Hash.ComputeSha256Hash(result), _defaultEntropyHash, StringComparison.Ordinal))
                {
                    result              = Convert.ToBase64String(CredentialProtector.GetOrCreateKey(path));
                    _defaultEntropy     = PasswordObfuscator.EncodePassword(result);
                    _defaultEntropyHash = Hash.ComputeSha256Hash(result);
                    _defaultEntropyPath = path;
                }
            }

            return result;
        }

        /// <summary>
        /// Decodes a value obfuscated with <see cref="PasswordObfuscator"/> without throwing exceptions.
        /// </summary>
        /// <param name="obfuscated">Obfuscated value.</param>
        /// <returns>The decoded value; null if it cannot be decoded.</returns>
        private static string TryRevealObfuscatedValue(string obfuscated)
        {
            string result;

            try
            {
                result = PasswordObfuscator.DecodePassword(obfuscated);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
            {
                result = null;
            }

            return result;
        }

        #endregion

        #region Stored passwords

        /// <summary>
        /// Stores the SMTP password read from the configuration (see <see cref="CheckStoredPassword"/>) together with the
        /// protector needed to decrypt it.
        /// </summary>
        /// <param name="value">Password read from the configuration, encrypted or in plain text.</param>
        /// <param name="protector">Protector used to decrypt the password; null for the default protector.</param>
        private static void SetStoredSmtpPassword(string value, CredentialProtector protector)
        {
            _smtpPassword   = ObfuscatePassword(CheckStoredPassword(value, protector, nameof(SmtpPassword)));
            _smtpProtector  = protector;
        }

        /// <summary>
        /// Stores the IMAP password read from the configuration (see <see cref="CheckStoredPassword"/>) together with the
        /// protector needed to decrypt it.
        /// </summary>
        /// <inheritdoc cref="SetStoredSmtpPassword(string, CredentialProtector)"/>
        private static void SetStoredImapPassword(string value, CredentialProtector protector)
        {
            _imapPassword   = ObfuscatePassword(CheckStoredPassword(value, protector, nameof(ImapPassword)));
            _imapProtector  = protector;
        }

        /// <summary>
        /// Returns the password read from the configuration as it must be stored in memory: unchanged if it is in plain text or
        /// if it is encrypted (see <see cref="CredentialProtector.IsProtected(string)"/>) and can be decrypted with
        /// <paramref name="protector"/> or, if null, with the default protector.<br/>
        /// An encrypted password that cannot be decrypted is logged as an error and returned empty, so that the parameter check
        /// fails.
        /// </summary>
        /// <param name="value">Password read from the configuration, encrypted or in plain text.</param>
        /// <param name="protector">Protector used to decrypt the password; null for the default protector.</param>
        /// <param name="keyName">Name of the configuration key, used in the log messages.</param>
        /// <returns>The password to store, still encrypted if it was; empty if it cannot be decrypted.</returns>
        private static string CheckStoredPassword(string value, CredentialProtector protector, string keyName)
        {
            string result;

            if (CredentialProtector.IsProtected(value))
            {
                var effectiveProtector = protector ?? CreateDefaultProtector();
                if (effectiveProtector != null && effectiveProtector.TryReveal(value).Success)
                {
                    result = value;
                }
                else
                {
                    SimpleLog.Write(LogLevel.Error, $"{keyName} is encrypted but cannot be decrypted on this computer " +
                        "(encrypted on another computer, missing key or entropy file, different entropy): the password is ignored.");
                    result = string.Empty;
                }
            }
            else
            {
                result = value;
            }

            return result;
        }

        /// <summary>
        /// Returns the plain-text password from a stored value (see <see cref="SmtpPassword"/>): decodes it with
        /// <see cref="PasswordObfuscator"/> and, if encrypted, decrypts it with <paramref name="protector"/> or, if null, with
        /// the default protector, created for this decryption only.<br/>
        /// A password that can no longer be decrypted (e.g. key or entropy file deleted after the setup) is logged as an error
        /// and returned empty.
        /// </summary>
        /// <param name="obfuscated">Stored value, or a null, empty or white-space value.</param>
        /// <param name="protector">Protector received by the setup; null for a plain-text password or the default protector.</param>
        /// <param name="keyName">Name of the property, used in the log messages.</param>
        /// <returns>The plain-text password, or the value received if empty.</returns>
        private static string RevealStoredPassword(string obfuscated, CredentialProtector protector, string keyName)
        {
            var result = RevealObfuscatedPassword(obfuscated);

            if (CredentialProtector.IsProtected(result))
            {
                var effectiveProtector  = protector ?? CreateDefaultProtector();
                var revealed            = effectiveProtector != null
                                            ? effectiveProtector.TryReveal(result)
                                            : (Success: false, Value: string.Empty);
                if (!revealed.Success)
                {
                    SimpleLog.Write(LogLevel.Error, $"{keyName} can no longer be decrypted: an empty password is used.");
                }
                result = revealed.Value;
            }

            return result;
        }

        /// <summary>
        /// Encrypts a password found in plain text in the settings.<br/>
        /// If the protector is missing (default protector not available) or the encryption fails, the error is logged and the
        /// password is returned in plain text, so that it remains usable.
        /// </summary>
        /// <param name="password">Plain-text password.</param>
        /// <param name="protector">Protector used to encrypt the password; may be null.</param>
        /// <param name="keyName">Name of the setting, used in the log messages.</param>
        /// <returns>The encrypted password, or <paramref name="password"/> if it cannot be encrypted.</returns>
        private static string ProtectSettingsPassword(string password, CredentialProtector protector, string keyName)
        {
            string result;

            try
            {
                if (protector != null)
                {
                    result = protector.Protect(password);
                    SimpleLog.Write($"{keyName} was stored in plain text: it has been encrypted.");
                }
                else
                {
                    SimpleLog.Write(LogLevel.Error, $"No protector available to encrypt {keyName}: it is kept in plain text.");
                    result = password;
                }
            }
            catch (Exception ex)
            {
                SimpleLog.Write(LogLevel.Error, $"Unable to encrypt {keyName}: it is kept in plain text.");
                SimpleLog.LogError(ex);
                result = password;
            }

            return result;
        }

        /// <summary>
        /// Saves the settings whose passwords have just been encrypted, without a backup copy of the previous file, which
        /// contains the passwords in plain text.
        /// </summary>
        /// <typeparam name="T">Type of the settings.</typeparam>
        /// <param name="settings">Settings to save.</param>
        /// <param name="filename">File to overwrite.</param>
        /// <returns>True if the file has been saved; errors are logged.</returns>
        private static bool SaveProtectedSettings<T>(T settings, string filename) where T : JsonSettings<T>, new()
        {
            bool result;

            try
            {
                settings.Save(filename, null, true);
                SimpleLog.Write($"Settings saved with the encrypted passwords ({filename}).");
                result = true;
            }
            catch (Exception ex)
            {
                SimpleLog.Write(LogLevel.Error, $"Unable to save the settings with the encrypted passwords ({filename}): " +
                    "the file still contains passwords in plain text.");
                SimpleLog.LogError(ex);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Returns the password obfuscated with <see cref="PasswordObfuscator.EncodePassword(string)"/>.<br/>
        /// A null, empty or white-space value is returned as it is: it is not a secret and cannot be obfuscated.
        /// </summary>
        /// <param name="password">Password to obfuscate, in plain text or encrypted.</param>
        /// <returns>The obfuscated password, or the value received if empty.</returns>
        private static string ObfuscatePassword(string password)
        {
            string result;

            if (string.IsNullOrWhiteSpace(password))
            {
                result = password;
            }
            else
            {
                result = PasswordObfuscator.EncodePassword(password);
            }

            return result;
        }

        /// <summary>
        /// Returns the plain-text password from a value generated by <see cref="ObfuscatePassword(string)"/>.
        /// </summary>
        /// <param name="obfuscated">Obfuscated password, or a null, empty or white-space value.</param>
        /// <returns>The plain-text password, or the value received if empty.</returns>
        private static string RevealObfuscatedPassword(string obfuscated)
        {
            string result;

            if (string.IsNullOrWhiteSpace(obfuscated))
            {
                result = obfuscated;
            }
            else
            {
                result = PasswordObfuscator.DecodePassword(obfuscated);
            }

            return result;
        }

        #endregion

        #endregion
    }
}
