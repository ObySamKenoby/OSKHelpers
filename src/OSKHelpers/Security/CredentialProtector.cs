using OSKHelpers.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace OSKHelpers.Security
{
    /// <summary>
    /// Encrypts credentials (passwords, secrets) stored in configuration files, using a method that depends on the operating
    /// system and on the target framework:
    /// <list type="bullet">
    /// <item><b>Windows</b>: DPAPI (<see cref="ProtectedData"/>), scope <see cref="DataProtectionScope.LocalMachine"/>; value
    /// <see cref="DPAPIPREFIX"/> followed by the Base64 encrypted data;</item>
    /// <item><b>other systems, .NET 8+</b>: AES-GCM with a random 256-bit key stored in <see cref="KeyFilePath"/>; value
    /// <see cref="AESPREFIX"/> followed by the Base64 nonce, tag and encrypted data;</item>
    /// <item><b>other systems, .NET Standard 2.0</b>: AES-CBC with HMAC-SHA256 (encrypt-then-MAC) and the same key file; value
    /// <see cref="AESCBCPREFIX"/> followed by the Base64 IV, MAC and encrypted data.</item>
    /// </list>
    /// The key file is created on first use and, on systems other than Windows, is readable only by its owner (mode
    /// <c>600</c>). A value without a prefix is plain text: an operator can write a plain password in a configuration file
    /// and the application encrypts it on the next start (<see cref="RequiresProtection"/>, <see cref="Protect"/>).
    /// <para>
    /// A credential can also be embedded in a longer string (e.g. a connection string) between <see cref="OpenTag"/> and
    /// <see cref="CloseTag"/> (default <see cref="DEFAULTOPENTAG"/> / <see cref="DEFAULTCLOSETAG"/>):
    /// <see cref="ProtectTagged"/> encrypts the content of the tags, <see cref="RevealTagged"/> returns the usable string, with
    /// the tags removed and the credentials in plain text.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Both methods let any account of the same computer that can read the application files decrypt the credential: they
    /// protect against the casual reading of the configuration and against its copy to another computer, not against a user
    /// with access to the computer. With AES the protection holds as long as the key file is not copied together with the
    /// configuration, so it must be excluded from configuration backups. An <see cref="AESPREFIX"/> value can be decrypted by
    /// the .NET 8+ build on any system holding the key; an <see cref="AESCBCPREFIX"/> value by both builds; a
    /// <see cref="DPAPIPREFIX"/> value only on the Windows computer that encrypted it.
    /// <para>
    /// The entropy passed to the constructor must stay the same for the whole life of the encrypted values: changing it makes
    /// them no longer decryptable. Without an entropy, the constructor uses the default entropy file
    /// <see cref="DEFAULTENTROPYFILENAME"/> in <see cref="Paths.DefaultConfigsDirectory"/>, created on first use with random
    /// bytes (<see cref="CreateOrReadEntropyFile"/>): deleting or replacing it has the same effect, so it must be kept together
    /// with the configuration. The instance is immutable and thread-safe.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var protector = new CredentialProtector("MyApplication.Credentials");
    ///
    /// // Entropy read from Configs/__credentials.entropy, created on first use
    /// var defaultProtector = new CredentialProtector();
    ///
    /// // "Server=db;User=SA;Password=[PWD]secret[/PWD];" becomes "Server=db;User=SA;Password=[PWD]DPAPI:AQAAANCM...[/PWD];"
    /// var stored = protector.ProtectTagged(connectionString);
    ///
    /// // "Server=db;User=SA;Password=secret;"
    /// var usable = protector.RevealTagged(stored);
    /// </code>
    /// </example>
    public class CredentialProtector
    {
        #region Constants

        /// <summary>
        /// Prefix of a credential encrypted with DPAPI (Windows).
        /// </summary>
        public const string DPAPIPREFIX = "DPAPI:";

        /// <summary>
        /// Prefix of a credential encrypted with AES-GCM and the key of <see cref="KeyFilePath"/> (.NET 8+, systems other than
        /// Windows).
        /// </summary>
        public const string AESPREFIX = "AES:";

        /// <summary>
        /// Prefix of a credential encrypted with AES-CBC and HMAC-SHA256 and the key of <see cref="KeyFilePath"/>
        /// (.NET Standard 2.0, systems other than Windows).
        /// </summary>
        public const string AESCBCPREFIX = "AESCBC:";

        /// <summary>
        /// Default name of the AES key file, in <see cref="Paths.DefaultDatabaseDirectory"/>.
        /// </summary>
        public const string DEFAULTKEYFILENAME = "__credentials.key";

        /// <summary>
        /// Default name of the entropy file, in <see cref="Paths.DefaultConfigsDirectory"/>, used when no entropy is passed to
        /// the constructor (see <see cref="CreateOrReadEntropyFile"/>).
        /// </summary>
        public const string DEFAULTENTROPYFILENAME = "__credentials.entropy";

        /// <summary>
        /// Length in bytes of the entropy created by <see cref="CreateOrReadEntropyFile"/> (256 bit).
        /// </summary>
        public const int DEFAULTENTROPYSIZE = AESKEYSIZE;

        /// <summary>
        /// Default tag opening a credential embedded in a string.
        /// </summary>
        public const string DEFAULTOPENTAG = "[PWD]";

        /// <summary>
        /// Default tag closing a credential embedded in a string.
        /// </summary>
        public const string DEFAULTCLOSETAG = "[/PWD]";

        /// <summary>
        /// Length in bytes of the AES key (256 bit).
        /// </summary>
        public const int AESKEYSIZE = 32;

        /// <summary>
        /// Length in bytes of the AES-GCM nonce.
        /// </summary>
        private const int GCMNONCESIZE = 12;

        /// <summary>
        /// Length in bytes of the AES-GCM authentication tag.
        /// </summary>
        private const int GCMTAGSIZE = 16;

        /// <summary>
        /// Length in bytes of the AES-CBC IV (one AES block).
        /// </summary>
        private const int CBCIVSIZE = 16;

        /// <summary>
        /// Length in bytes of the HMAC-SHA256 of the AES-CBC format.
        /// </summary>
        private const int CBCMACSIZE = 32;

        /// <summary>
        /// Label used to derive the AES-CBC encryption key from the key file.
        /// </summary>
        private const string CBCENCRYPTIONLABEL = "OSKHelpers.CredentialProtector.Encryption";

        /// <summary>
        /// Label used to derive the HMAC-SHA256 key from the key file.
        /// </summary>
        private const string CBCAUTHENTICATIONLABEL = "OSKHelpers.CredentialProtector.Authentication";

        /// <summary>
        /// Characters that can appear in an encrypted value (Base64 alphabet and prefix separator): a tag must contain at least
        /// one character outside this set, so it can never be found inside an encrypted value.
        /// </summary>
        private const string ENCRYPTEDVALUECHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=:";

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Unix permissions of the key file: read and write for the owner only (octal 600).
        /// </summary>
        private const int UNIXKEYFILEMODE = 0x180;
#endif

        #endregion

        #region Members

        /// <summary>
        /// Bytes of <see cref="Entropy"/>.
        /// </summary>
        private readonly byte[] _entropyBytes;

        #endregion

        #region Properties

        /// <summary>
        /// Additional entropy of the encryption (associated data for AES): it is not a secret, it distinguishes the data
        /// encrypted by an application from the data encrypted by other applications. When it is read from the default entropy
        /// file (<see cref="CreateOrReadEntropyFile"/>) it is the Base64 of <see cref="DEFAULTENTROPYSIZE"/> random bytes.
        /// </summary>
        public string Entropy { get; }

        /// <summary>
        /// Path of the AES key file, used on systems other than Windows.
        /// </summary>
        public string KeyFilePath { get; }

        /// <summary>
        /// Tag opening a credential embedded in a string.
        /// </summary>
        public string OpenTag { get; }

        /// <summary>
        /// Tag closing a credential embedded in a string.
        /// </summary>
        public string CloseTag { get; }

        /// <summary>
        /// Prefix of the credentials encrypted on the current system: <see cref="DPAPIPREFIX"/> on Windows,
        /// <see cref="AESPREFIX"/> elsewhere with .NET 8+, <see cref="AESCBCPREFIX"/> elsewhere with .NET Standard 2.0.
        /// </summary>
        public static string ProtectedPrefix
        {
            get
            {
                string result;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result = DPAPIPREFIX;
                }
                else
                {
#if NET8_0_OR_GREATER
                    result = AESPREFIX;
#else
                    result = AESCBCPREFIX;
#endif
                }

                return result;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a protector with the given entropy, key file and tags.
        /// </summary>
        /// <param name="entropy">Additional entropy of the encryption (see <see cref="Entropy"/>), e.g.
        /// <c>"MyApplication.Credentials"</c>; it must not change after values have been encrypted. Null to read it from
        /// <see cref="DEFAULTENTROPYFILENAME"/> in <see cref="Paths.DefaultConfigsDirectory"/>, created if missing
        /// (<see cref="CreateOrReadEntropyFile"/>).</param>
        /// <param name="keyFilePath">Path of the AES key file; null for <see cref="DEFAULTKEYFILENAME"/> in
        /// <see cref="Paths.DefaultDatabaseDirectory"/>.</param>
        /// <param name="openTag">Tag opening an embedded credential.</param>
        /// <param name="closeTag">Tag closing an embedded credential.</param>
        /// <exception cref="ArgumentException">Empty entropy or key file path; empty or equal tags; tag made only of characters
        /// that can appear in an encrypted value (Base64 alphabet and <c>:</c>).</exception>
        /// <exception cref="CryptographicException">Null entropy and existing but invalid entropy file, or permissions of a new
        /// entropy file not restrictable.</exception>
        /// <exception cref="IOException">Null entropy and entropy file not readable or not creatable.</exception>
        /// <exception cref="UnauthorizedAccessException">Null entropy and insufficient permissions on the entropy file.</exception>
        public CredentialProtector(string entropy = null, string keyFilePath = null, string openTag = DEFAULTOPENTAG,
            string closeTag = DEFAULTCLOSETAG)
        {
            if (entropy == null)
            {
                entropy = CreateOrReadEntropyFile();
            }
            if (string.IsNullOrEmpty(entropy))
            {
                throw new ArgumentException("The entropy must not be empty.", nameof(entropy));
            }
            if (keyFilePath != null && keyFilePath.Trim().Length == 0)
            {
                throw new ArgumentException("The key file path must not be empty.", nameof(keyFilePath));
            }
            ValidateTag(openTag, nameof(openTag));
            ValidateTag(closeTag, nameof(closeTag));
            if (string.Equals(openTag, closeTag, StringComparison.Ordinal))
            {
                throw new ArgumentException("The open and close tags must be different.", nameof(closeTag));
            }

            Entropy         = entropy;
            KeyFilePath     = keyFilePath ?? Path.Combine(Paths.DefaultDatabaseDirectory, DEFAULTKEYFILENAME);
            OpenTag         = openTag;
            CloseTag        = closeTag;
            _entropyBytes   = Encoding.UTF8.GetBytes(entropy);
        }

        #endregion

        #region Methods

        #region Single value

        /// <summary>
        /// Checks whether the value is an encrypted credential (starts with <see cref="DPAPIPREFIX"/>,
        /// <see cref="AESPREFIX"/> or <see cref="AESCBCPREFIX"/>).
        /// </summary>
        /// <param name="value">Value read from the configuration.</param>
        /// <returns>True if the value is encrypted.</returns>
        public static bool IsProtected(string value)
            => value != null
                && (value.StartsWith(DPAPIPREFIX, StringComparison.Ordinal)
                    || value.StartsWith(AESPREFIX, StringComparison.Ordinal)
                    || value.StartsWith(AESCBCPREFIX, StringComparison.Ordinal));

        /// <summary>
        /// Checks whether the value is a plain credential to encrypt: not empty and without an encryption prefix.
        /// </summary>
        /// <param name="value">Value read from the configuration.</param>
        /// <returns>True if the value must be encrypted.</returns>
        public static bool RequiresProtection(string value) => !string.IsNullOrEmpty(value) && !IsProtected(value);

        /// <summary>
        /// Encrypts a plain credential with the method of the current system: DPAPI on Windows, AES with the key of
        /// <see cref="KeyFilePath"/> (created if missing) elsewhere.
        /// </summary>
        /// <param name="plainText">Plain credential.</param>
        /// <returns>The encrypted credential, with the prefix <see cref="ProtectedPrefix"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="plainText"/> is null.</exception>
        /// <exception cref="CryptographicException">Encryption failed or invalid key file.</exception>
        /// <exception cref="IOException">Key file not readable or not creatable.</exception>
        public string Protect(string plainText)
        {
            string result;

            if (plainText == null)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = ProtectDPAPI(plainText, _entropyBytes);
            }
            else
            {
                result = ProtectWithKey(plainText, GetOrCreateKey(KeyFilePath), Entropy);
            }

            return result;
        }

        /// <summary>
        /// Returns the plain credential, decrypting it if encrypted, without throwing exceptions.
        /// </summary>
        /// <param name="value">Value read from the configuration, encrypted or plain.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Success</b>: false if the value is encrypted but cannot be decrypted (e.g. configuration copied from another
        /// computer, DPAPI value on a system other than Windows, AES-GCM value with the .NET Standard 2.0 build, missing key
        /// file, altered data);<br/>
        /// <b>Value</b>: the plain credential; empty if it cannot be decrypted or if the value is empty.
        /// </returns>
        public (bool Success, string Value) TryReveal(string value)
        {
            var success = true;
            var plain   = value ?? string.Empty;

            if (IsProtected(plain))
            {
                try
                {
                    if (plain.StartsWith(DPAPIPREFIX, StringComparison.Ordinal))
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            plain = RevealDPAPI(plain, _entropyBytes);
                        }
                        else
                        {
                            success = false;
                            plain   = string.Empty;
                        }
                    }
                    else
                    {
                        var key = LoadKey(KeyFilePath);
                        success = key != null;
                        plain   = key != null ? RevealWithKey(plain, key, Entropy) : string.Empty;
                    }
                }
                catch (Exception ex) when (ex is CryptographicException || ex is FormatException || ex is IOException
                    || ex is UnauthorizedAccessException)
                {
                    success = false;
                    plain   = string.Empty;
                }
            }

            return (success, plain);
        }

        /// <summary>
        /// Returns the plain credential (<see cref="TryReveal"/>), empty if it cannot be decrypted.
        /// </summary>
        /// <param name="value">Value read from the configuration, encrypted or plain.</param>
        /// <returns>The plain credential.</returns>
        public string Reveal(string value) => TryReveal(value).Value;

        #endregion

        #region Tagged values

        /// <summary>
        /// Checks whether the text contains at least one credential between <see cref="OpenTag"/> and <see cref="CloseTag"/>
        /// that must be encrypted (<see cref="RequiresProtection"/>).
        /// </summary>
        /// <param name="text">Text containing tagged credentials, e.g. a connection string.</param>
        /// <returns>True if at least one tagged credential is plain and not empty.</returns>
        public bool RequiresTaggedProtection(string text)
        {
            var result = false;

            TransformTagged(text, true, content =>
            {
                result |= RequiresProtection(content);
                return content;
            });

            return result;
        }

        /// <summary>
        /// Encrypts (<see cref="Protect"/>) every plain credential between <see cref="OpenTag"/> and <see cref="CloseTag"/>,
        /// keeping the tags; credentials already encrypted and empty ones are left unchanged, as is the rest of the text.
        /// </summary>
        /// <param name="text">Text containing tagged credentials, e.g.
        /// <c>Server=db;User=SA;Password=[PWD]secret[/PWD];</c>.</param>
        /// <returns>The text with encrypted credentials, e.g.
        /// <c>Server=db;User=SA;Password=[PWD]DPAPI:AQAAANCM...[/PWD];</c>; empty if <paramref name="text"/> is null.</returns>
        /// <remarks>An open tag without a matching close tag is left as it is. A credential must not contain
        /// <see cref="CloseTag"/>.</remarks>
        /// <exception cref="CryptographicException">Encryption failed or invalid key file.</exception>
        /// <exception cref="IOException">Key file not readable or not creatable.</exception>
        public string ProtectTagged(string text)
            => TransformTagged(text, true, content => RequiresProtection(content) ? Protect(content) : content);

        /// <summary>
        /// Returns the text with the tags removed and every credential between <see cref="OpenTag"/> and
        /// <see cref="CloseTag"/> in plain text (<see cref="TryReveal"/>), without throwing exceptions.
        /// </summary>
        /// <param name="text">Text containing tagged credentials, encrypted or plain.</param>
        /// <returns>
        /// A tuple made of:<br/>
        /// <b>Success</b>: false if at least one credential cannot be decrypted;<br/>
        /// <b>Value</b>: the usable text, e.g. <c>Server=db;User=SA;Password=secret;</c>, where credentials that cannot be
        /// decrypted are empty; empty if <paramref name="text"/> is null.
        /// </returns>
        public (bool Success, string Value) TryRevealTagged(string text)
        {
            var success = true;

            var value = TransformTagged(text, false, content =>
            {
                var revealed = TryReveal(content);
                success &= revealed.Success;
                return revealed.Value;
            });

            return (success, value);
        }

        /// <summary>
        /// Returns the text with the tags removed and the credentials in plain text (<see cref="TryRevealTagged"/>);
        /// credentials that cannot be decrypted are empty.
        /// </summary>
        /// <param name="text">Text containing tagged credentials, encrypted or plain.</param>
        /// <returns>The usable text.</returns>
        public string RevealTagged(string text) => TryRevealTagged(text).Value;

        /// <summary>
        /// Applies a transformation to the content of every <see cref="OpenTag"/>...<see cref="CloseTag"/> pair of the text,
        /// copying the rest of the text unchanged. An open tag without a matching close tag is copied as it is.
        /// </summary>
        /// <param name="text">Text to process; null is treated as empty.</param>
        /// <param name="keepTags">True to keep the tags around the transformed content, false to remove them.</param>
        /// <param name="transform">Transformation of the tagged content.</param>
        /// <returns>The transformed text.</returns>
        private string TransformTagged(string text, bool keepTags, Func<string, string> transform)
        {
            var source      = text ?? string.Empty;
            var builder     = new StringBuilder(source.Length);
            var position    = 0;
            var completed   = false;

            while (!completed)
            {
                var openIndex   = source.IndexOf(OpenTag, position, StringComparison.Ordinal);
                var closeIndex  = openIndex >= 0 ? source.IndexOf(CloseTag, openIndex + OpenTag.Length, StringComparison.Ordinal) : -1;

                if (closeIndex >= 0)
                {
                    var contentStart = openIndex + OpenTag.Length;

                    builder.Append(source, position, openIndex - position);
                    if (keepTags)
                    {
                        builder.Append(OpenTag);
                    }
                    builder.Append(transform(source.Substring(contentStart, closeIndex - contentStart)));
                    if (keepTags)
                    {
                        builder.Append(CloseTag);
                    }
                    position = closeIndex + CloseTag.Length;
                }
                else
                {
                    builder.Append(source, position, source.Length - position);
                    completed = true;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Checks that a tag is not empty and contains at least one character that cannot appear in an encrypted value.
        /// </summary>
        /// <param name="tag">Tag to check.</param>
        /// <param name="parameterName">Name of the constructor parameter, for the exception.</param>
        /// <exception cref="ArgumentException">Invalid tag.</exception>
        private static void ValidateTag(string tag, string parameterName)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentException("The tag must not be empty.", parameterName);
            }
            if (tag.All(c => ENCRYPTEDVALUECHARS.IndexOf(c) >= 0))
            {
                throw new ArgumentException(
                    "The tag must contain at least one character outside the Base64 alphabet and ':' (e.g. '[').", parameterName);
            }
        }

        #endregion

        #region DPAPI

        /// <summary>
        /// Encrypts a credential with DPAPI, scope <see cref="DataProtectionScope.LocalMachine"/>.
        /// </summary>
        /// <param name="plainText">Plain credential.</param>
        /// <param name="entropy">Additional entropy.</param>
        /// <returns><see cref="DPAPIPREFIX"/> followed by the Base64 encrypted data.</returns>
#if NET8_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        private static string ProtectDPAPI(string plainText, byte[] entropy)
        {
            var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), entropy, DataProtectionScope.LocalMachine);
            return DPAPIPREFIX + Convert.ToBase64String(data);
        }

        /// <summary>
        /// Decrypts a credential encrypted by <see cref="ProtectDPAPI"/>.
        /// </summary>
        /// <param name="value">Encrypted value, with the prefix <see cref="DPAPIPREFIX"/>.</param>
        /// <param name="entropy">Additional entropy used for the encryption.</param>
        /// <returns>The plain credential.</returns>
        /// <exception cref="FormatException">Value not in Base64.</exception>
        /// <exception cref="CryptographicException">Value encrypted on another computer, different entropy or altered data.</exception>
#if NET8_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        private static string RevealDPAPI(string value, byte[] entropy)
        {
            var data = Convert.FromBase64String(value.Substring(DPAPIPREFIX.Length));
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, entropy, DataProtectionScope.LocalMachine));
        }

        #endregion

        #region AES

        /// <summary>
        /// Encrypts a credential with the given key: AES-GCM (<see cref="AESPREFIX"/>) with .NET 8+, AES-CBC with HMAC-SHA256
        /// (<see cref="AESCBCPREFIX"/>) with .NET Standard 2.0. Random nonce/IV, <paramref name="entropy"/> as associated data.
        /// </summary>
        /// <param name="plainText">Plain credential.</param>
        /// <param name="key">Key of <see cref="AESKEYSIZE"/> bytes.</param>
        /// <param name="entropy">Additional entropy (see <see cref="Entropy"/>).</param>
        /// <returns>The encrypted credential, with its prefix.</returns>
        /// <exception cref="CryptographicException">Invalid key or encryption failed.</exception>
        public static string ProtectWithKey(string plainText, byte[] key, string entropy)
        {
#if NET8_0_OR_GREATER
            return ProtectWithKeyGCM(plainText, key, Encoding.UTF8.GetBytes(entropy));
#else
            return ProtectWithKeyCBC(plainText, key, Encoding.UTF8.GetBytes(entropy));
#endif
        }

        /// <summary>
        /// Decrypts a credential encrypted by <see cref="ProtectWithKey"/>, in either format (<see cref="AESPREFIX"/> or
        /// <see cref="AESCBCPREFIX"/>).
        /// </summary>
        /// <param name="value">Encrypted value, with its prefix.</param>
        /// <param name="key">Key used for the encryption.</param>
        /// <param name="entropy">Additional entropy used for the encryption.</param>
        /// <returns>The plain credential.</returns>
        /// <exception cref="FormatException">Value not in Base64.</exception>
        /// <exception cref="CryptographicException">Unknown prefix, value too short, different key or entropy, altered data,
        /// AES-GCM value with the .NET Standard 2.0 build.</exception>
        public static string RevealWithKey(string value, byte[] key, string entropy)
        {
            string result;
            var entropyBytes = Encoding.UTF8.GetBytes(entropy);

            if (value != null && value.StartsWith(AESCBCPREFIX, StringComparison.Ordinal))
            {
                result = RevealWithKeyCBC(value, key, entropyBytes);
            }
            else if (value != null && value.StartsWith(AESPREFIX, StringComparison.Ordinal))
            {
#if NET8_0_OR_GREATER
                result = RevealWithKeyGCM(value, key, entropyBytes);
#else
                throw new CryptographicException("AES-GCM credentials can be decrypted only with the .NET 8+ build.");
#endif
            }
            else
            {
                throw new CryptographicException("The value is not an AES encrypted credential.");
            }

            return result;
        }

#if NET8_0_OR_GREATER

        /// <summary>
        /// Encrypts a credential with AES-GCM.
        /// </summary>
        /// <param name="plainText">Plain credential.</param>
        /// <param name="key">Key of <see cref="AESKEYSIZE"/> bytes.</param>
        /// <param name="entropy">Associated data.</param>
        /// <returns><see cref="AESPREFIX"/> followed by the Base64 nonce, tag and encrypted data.</returns>
        private static string ProtectWithKeyGCM(string plainText, byte[] key, byte[] entropy)
        {
            var plainBytes  = Encoding.UTF8.GetBytes(plainText);
            var data        = new byte[GCMNONCESIZE + GCMTAGSIZE + plainBytes.Length];
            var nonce       = data.AsSpan(0, GCMNONCESIZE);
            var tag         = data.AsSpan(GCMNONCESIZE, GCMTAGSIZE);
            var cipher      = data.AsSpan(GCMNONCESIZE + GCMTAGSIZE);

            RandomNumberGenerator.Fill(nonce);
            using (var aes = new AesGcm(key, GCMTAGSIZE))
            {
                aes.Encrypt(nonce, plainBytes, cipher, tag, entropy);
            }

            return AESPREFIX + Convert.ToBase64String(data);
        }

        /// <summary>
        /// Decrypts a credential encrypted by <see cref="ProtectWithKeyGCM"/>.
        /// </summary>
        /// <param name="value">Encrypted value, with the prefix <see cref="AESPREFIX"/>.</param>
        /// <param name="key">Key used for the encryption.</param>
        /// <param name="entropy">Associated data used for the encryption.</param>
        /// <returns>The plain credential.</returns>
        /// <exception cref="FormatException">Value not in Base64.</exception>
        /// <exception cref="CryptographicException">Value too short, different key or entropy, altered data.</exception>
        private static string RevealWithKeyGCM(string value, byte[] key, byte[] entropy)
        {
            var data = Convert.FromBase64String(value.Substring(AESPREFIX.Length));
            byte[] plainBytes;

            if (data.Length >= GCMNONCESIZE + GCMTAGSIZE)
            {
                plainBytes = new byte[data.Length - GCMNONCESIZE - GCMTAGSIZE];
                using (var aes = new AesGcm(key, GCMTAGSIZE))
                {
                    aes.Decrypt(data.AsSpan(0, GCMNONCESIZE), data.AsSpan(GCMNONCESIZE + GCMTAGSIZE),
                        data.AsSpan(GCMNONCESIZE, GCMTAGSIZE), plainBytes, entropy);
                }
            }
            else
            {
                throw new CryptographicException("AES encrypted credential too short.");
            }

            return Encoding.UTF8.GetString(plainBytes);
        }

#endif

        /// <summary>
        /// Encrypts a credential with AES-CBC (PKCS7 padding) and authenticates it with HMAC-SHA256 (encrypt-then-MAC); the
        /// encryption and MAC keys are derived from <paramref name="key"/>, the MAC covers entropy, IV and encrypted data.
        /// </summary>
        /// <param name="plainText">Plain credential.</param>
        /// <param name="key">Key of <see cref="AESKEYSIZE"/> bytes.</param>
        /// <param name="entropy">Associated data.</param>
        /// <returns><see cref="AESCBCPREFIX"/> followed by the Base64 IV, MAC and encrypted data.</returns>
        /// <exception cref="CryptographicException">Invalid key.</exception>
        private static string ProtectWithKeyCBC(string plainText, byte[] key, byte[] entropy)
        {
            ValidateKey(key);

            var iv          = GetRandomBytes(CBCIVSIZE);
            var plainBytes  = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher;

            using (var aes = CreateCBCAes(key, iv))
            using (var encryptor = aes.CreateEncryptor())
            {
                cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            var mac     = ComputeCBCMac(key, entropy, iv, cipher);
            var data    = new byte[CBCIVSIZE + CBCMACSIZE + cipher.Length];
            Buffer.BlockCopy(iv,     0, data, 0,                      CBCIVSIZE);
            Buffer.BlockCopy(mac,    0, data, CBCIVSIZE,              CBCMACSIZE);
            Buffer.BlockCopy(cipher, 0, data, CBCIVSIZE + CBCMACSIZE, cipher.Length);

            return AESCBCPREFIX + Convert.ToBase64String(data);
        }

        /// <summary>
        /// Decrypts a credential encrypted by <see cref="ProtectWithKeyCBC"/>, verifying the MAC before decrypting.
        /// </summary>
        /// <param name="value">Encrypted value, with the prefix <see cref="AESCBCPREFIX"/>.</param>
        /// <param name="key">Key used for the encryption.</param>
        /// <param name="entropy">Associated data used for the encryption.</param>
        /// <returns>The plain credential.</returns>
        /// <exception cref="FormatException">Value not in Base64.</exception>
        /// <exception cref="CryptographicException">Invalid key, value too short, different key or entropy, altered data.</exception>
        private static string RevealWithKeyCBC(string value, byte[] key, byte[] entropy)
        {
            ValidateKey(key);

            var data = Convert.FromBase64String(value.Substring(AESCBCPREFIX.Length));
            byte[] plainBytes;

            // At least one encrypted block: PKCS7 padding always produces a block, even for an empty credential.
            if (data.Length >= CBCIVSIZE + CBCMACSIZE + CBCIVSIZE)
            {
                var iv      = new byte[CBCIVSIZE];
                var mac     = new byte[CBCMACSIZE];
                var cipher  = new byte[data.Length - CBCIVSIZE - CBCMACSIZE];
                Buffer.BlockCopy(data, 0,                      iv,     0, CBCIVSIZE);
                Buffer.BlockCopy(data, CBCIVSIZE,              mac,    0, CBCMACSIZE);
                Buffer.BlockCopy(data, CBCIVSIZE + CBCMACSIZE, cipher, 0, cipher.Length);

                if (FixedTimeEquals(mac, ComputeCBCMac(key, entropy, iv, cipher)))
                {
                    using (var aes = CreateCBCAes(key, iv))
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    }
                }
                else
                {
                    throw new CryptographicException("AES-CBC encrypted credential not authentic: different key or entropy, or altered data.");
                }
            }
            else
            {
                throw new CryptographicException("AES-CBC encrypted credential too short.");
            }

            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Creates the AES-CBC algorithm with the encryption key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the key file.</param>
        /// <param name="iv">Initialization vector.</param>
        /// <returns>The configured algorithm, to dispose.</returns>
        private static Aes CreateCBCAes(byte[] key, byte[] iv)
        {
            var aes     = Aes.Create();
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key     = DeriveKey(key, CBCENCRYPTIONLABEL);
            aes.IV      = iv;
            return aes;
        }

        /// <summary>
        /// Computes the HMAC-SHA256 of entropy, IV and encrypted data, with the MAC key derived from <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the key file.</param>
        /// <param name="entropy">Associated data.</param>
        /// <param name="iv">Initialization vector.</param>
        /// <param name="cipher">Encrypted data.</param>
        /// <returns>The MAC, <see cref="CBCMACSIZE"/> bytes.</returns>
        private static byte[] ComputeCBCMac(byte[] key, byte[] entropy, byte[] iv, byte[] cipher)
        {
            var authenticated = new byte[entropy.Length + iv.Length + cipher.Length];
            Buffer.BlockCopy(entropy, 0, authenticated, 0,                           entropy.Length);
            Buffer.BlockCopy(iv,      0, authenticated, entropy.Length,              iv.Length);
            Buffer.BlockCopy(cipher,  0, authenticated, entropy.Length + iv.Length,  cipher.Length);

            using (var hmac = new HMACSHA256(DeriveKey(key, CBCAUTHENTICATIONLABEL)))
            {
                return hmac.ComputeHash(authenticated);
            }
        }

        /// <summary>
        /// Derives a 256-bit key for a specific use from the key of the key file (HMAC-SHA256 of the label).
        /// </summary>
        /// <param name="key">Key of the key file.</param>
        /// <param name="label">Use of the derived key.</param>
        /// <returns>The derived key.</returns>
        private static byte[] DeriveKey(byte[] key, string label)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(label));
            }
        }

        /// <summary>
        /// Checks that the key is <see cref="AESKEYSIZE"/> bytes long (AES-GCM checks it by itself).
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <exception cref="CryptographicException">Invalid key.</exception>
        private static void ValidateKey(byte[] key)
        {
            if (key == null || key.Length != AESKEYSIZE)
            {
                throw new CryptographicException($"The AES key must be {AESKEYSIZE} bytes long.");
            }
        }

        /// <summary>
        /// Compares two byte arrays in a time that does not depend on their content, to avoid timing attacks on the MAC.
        /// </summary>
        /// <param name="left">First array.</param>
        /// <param name="right">Second array.</param>
        /// <returns>True if the arrays are equal.</returns>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            var difference = left.Length ^ right.Length;

            for (var i = 0; i < left.Length && i < right.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        /// <param name="length">Number of bytes.</param>
        /// <returns>The random bytes.</returns>
        private static byte[] GetRandomBytes(int length)
        {
            var result = new byte[length];

            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(result);
            }

            return result;
        }

        #endregion

        #region Key and entropy files

        /// <summary>
        /// Returns the entropy stored in the given file, creating the file if it does not exist: the entropy is the Base64 of
        /// <see cref="DEFAULTENTROPYSIZE"/> random bytes, and the file is created like the key file
        /// (<see cref="GetOrCreateKey"/>: directory created if missing, exclusive creation, permissions restricted to the owner
        /// on systems other than Windows). Used by the constructor when no entropy is passed.
        /// </summary>
        /// <param name="path">Path of the entropy file; null for <see cref="DEFAULTENTROPYFILENAME"/> in
        /// <see cref="Paths.DefaultConfigsDirectory"/>.</param>
        /// <returns>The entropy, to pass to the constructor.</returns>
        /// <remarks>Deleting or replacing the file makes the credentials encrypted with its entropy no longer decryptable: they
        /// must be written again in plain text in the configuration. The file has the same format as the key file, so an
        /// existing file is never overwritten.</remarks>
        /// <exception cref="ArgumentException">Empty path.</exception>
        /// <exception cref="CryptographicException">Existing but invalid file (not <see cref="DEFAULTENTROPYSIZE"/> bytes
        /// long); permissions of a new file not restrictable.</exception>
        /// <exception cref="IOException">File not readable or not creatable.</exception>
        /// <exception cref="UnauthorizedAccessException">Insufficient permissions on the file.</exception>
        public static string CreateOrReadEntropyFile(string path = null)
        {
            if (path != null && path.Trim().Length == 0)
            {
                throw new ArgumentException("The entropy file path must not be empty.", nameof(path));
            }

            var entropyPath = path ?? Path.Combine(Paths.DefaultConfigsDirectory, DEFAULTENTROPYFILENAME);

            return Convert.ToBase64String(GetOrCreateKey(entropyPath));
        }

        /// <summary>
        /// Reads the AES key from the given file.
        /// </summary>
        /// <param name="path">Path of the key file (usually <see cref="KeyFilePath"/>).</param>
        /// <returns>The key, null if the file does not exist or is not <see cref="AESKEYSIZE"/> bytes long.</returns>
        /// <exception cref="IOException">File not readable.</exception>
        /// <exception cref="UnauthorizedAccessException">Insufficient permissions on the file.</exception>
        public static byte[] LoadKey(string path)
        {
            byte[] result = null;

            if (File.Exists(path))
            {
                var key = File.ReadAllBytes(path);
                if (key.Length == AESKEYSIZE)
                {
                    result = key;
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the AES key from the given file, creating it if the file does not exist: random key of
        /// <see cref="AESKEYSIZE"/> bytes, file created exclusively and, on systems other than Windows, readable and writable
        /// only by its owner (mode <c>600</c>). If another process creates the file in the meantime, its key is used.
        /// </summary>
        /// <param name="path">Path of the key file (usually <see cref="KeyFilePath"/>).</param>
        /// <returns>The key.</returns>
        /// <exception cref="CryptographicException">Existing but invalid file: it is not overwritten, because the credentials
        /// encrypted with the original key would no longer be decryptable; permissions of a new file not restrictable.</exception>
        /// <exception cref="IOException">File not readable or not creatable.</exception>
        public static byte[] GetOrCreateKey(string path)
        {
            byte[] key = null;

            if (!File.Exists(path))
            {
                var newKey = GetRandomBytes(AESKEYSIZE);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                    WriteNewKeyFile(path, newKey);
                    key = newKey;
                }
                catch (IOException) when (File.Exists(path))
                {
                    // The file was created by another process between the check and the creation: its key is used.
                }
            }

            return key ?? LoadKey(path) ?? throw new CryptographicException($"The key file '{path}' is not valid.");
        }

        /// <summary>
        /// Creates the key file exclusively (fails if it exists) and writes the key, restricting its permissions to the owner
        /// on systems other than Windows before writing it.
        /// </summary>
        /// <param name="path">Path of the key file.</param>
        /// <param name="key">Key to write.</param>
        /// <exception cref="IOException">File already existing or not creatable.</exception>
        /// <exception cref="CryptographicException">Permissions not restrictable (.NET Standard 2.0): the file is deleted.</exception>
        private static void WriteNewKeyFile(string path, byte[] key)
        {
#if NET8_0_OR_GREATER
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }
            using (var stream = new FileStream(path, options))
            {
                stream.Write(key, 0, key.Length);
            }
#else
            var restricted = true;

            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                // .NET Standard 2.0 cannot set the mode on creation: the still empty file is restricted before writing the key.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    restricted = SetOwnerOnlyPermissions(path);
                }
                if (restricted)
                {
                    stream.Write(key, 0, key.Length);
                }
            }

            if (!restricted)
            {
                File.Delete(path);
                throw new CryptographicException($"Cannot restrict the permissions of the key file '{path}' to its owner.");
            }
#endif
        }

#if !NET8_0_OR_GREATER

        /// <summary>
        /// Sets the permissions of a file to read and write for the owner only (mode <c>600</c>) through <c>chmod</c> of the
        /// C library, the only way available to .NET Standard 2.0.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>True if the permissions were set.</returns>
        private static bool SetOwnerOnlyPermissions(string path)
        {
            bool result;

            try
            {
                result = UnixChmod(path, UNIXKEYFILEMODE) == 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// <c>chmod</c> function of the C library.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="mode">Permissions to set.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int UnixChmod(string path, int mode);

#endif

        #endregion

#if DEBUG

        /// <inheritdoc cref="ProtectWithKeyCBC(string, byte[], byte[])"/>
        /// <remarks>For use in tests only: the .NET 8+ build encrypts with AES-GCM.</remarks>
        public static string TestProtectWithKeyCBC(string plainText, byte[] key, string entropy)
            => ProtectWithKeyCBC(plainText, key, Encoding.UTF8.GetBytes(entropy));

#endif

        #endregion
    }
}
