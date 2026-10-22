using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System;

namespace OSKHelpers.Security
{
    /// <summary>
    /// Class used to generate secure passwords suitable for storage in a database.<br/>
    /// Source: http://csharptest.net/470/another-example-of-how-to-store-a-salted-password-hash/<br/>
    /// <b>Note:</b> the method does not validate the supplied parameters; wrap its usage in a try...catch block.<br/>
    /// Usage:
    /// <code>
    /// // Create a hash to save:
    /// PasswordHash hash = new PasswordHash("password");
    /// byte[] hashBytes = hash.ToArray();
    ///
    /// // Verify a saved password
    /// byte[] hashBytes = // read the saved password.
    /// PasswordHash hash = new PasswordHash(hashBytes);
    /// if (!hash.Verify("newly entered password"))
    ///    throw new System.UnauthorizedAccessException();
    /// if (hash.RequiresRehash)
    ///    // save new PasswordHash("newly entered password").ToArray() in place of hashBytes
    /// </code>
    /// </summary>
    /// <remarks>
    /// Supported formats of the array returned by <see cref="ToArray"/>:<br/>
    /// - <b>version 1</b> (current, 49 bytes): version byte (1) + 16 bytes of salt + 32 bytes of PBKDF2-HMAC-SHA256 hash,
    /// 600,000 iterations;<br/>
    /// - <b>legacy</b> (36 bytes, no version byte): 16 bytes of salt + 20 bytes of PBKDF2-HMAC-SHA1 hash, 10,000 iterations.<br/>
    /// New hashes are always created in the current format; legacy hashes can still be loaded and verified,
    /// and <see cref="RequiresRehash"/> signals that they should be regenerated after a successful verification.
    /// </remarks>
    public sealed class PasswordHash
    {
        #region Constants

        /// <summary>
        /// Salt size (bytes), the same for all formats.
        /// </summary>
        private const int SALTSIZE = 16;

        /// <summary>
        /// Identifier of the legacy format (not stored in the array).
        /// </summary>
        private const byte LEGACYVERSION = 0;
        /// <summary>
        /// Hash size of the legacy format (bytes).
        /// </summary>
        private const int LEGACYHASHSIZE = 20;
        /// <summary>
        /// Number of hash iterations of the legacy format.
        /// </summary>
        private const int LEGACYHASHITERATIONS = 10000;

        /// <summary>
        /// Identifier of the current format, stored as first byte of the array.
        /// </summary>
        private const byte CURRENTVERSION = 1;
        /// <summary>
        /// Size of the version identifier (bytes).
        /// </summary>
        private const int VERSIONSIZE = 1;
        /// <summary>
        /// Hash size of the current format (bytes).
        /// </summary>
        private const int HASHSIZE = 32;
        /// <summary>
        /// Number of hash iterations of the current format (OWASP recommendation for PBKDF2-HMAC-SHA256).
        /// </summary>
        private const int HASHITERATIONS = 600000;

        #endregion

        #region Members

        /// <summary>
        /// Format version.
        /// </summary>
        private readonly byte _version;
        /// <summary>
        /// Salt bytes.
        /// </summary>
        private readonly byte[] _salt;
        /// <summary>
        /// Password hash bytes.
        /// </summary>
        private readonly byte[] _hash;

        #endregion

        #region Properties

        /// <summary>
        /// Returns a copy of Salt bytes.
        /// </summary>
        public byte[] Salt => (byte[])_salt.Clone();

        /// <summary>
        /// Returns a copy of Hash bytes.
        /// </summary>
        public byte[] Hash => (byte[])_hash.Clone();

        /// <summary>
        /// True if the hash uses a format older than the current one (legacy PBKDF2-HMAC-SHA1).<br/>
        /// After a successful <see cref="Verify(string)"/> the password should be hashed again with
        /// <see cref="PasswordHash(string)"/> and the stored value replaced.
        /// </summary>
        public bool RequiresRehash => _version != CURRENTVERSION;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from <paramref name="password"/>.<br/>
        /// <see cref="Salt"/> will be randomly generated, so each object created for a password will have unique <see cref="Hash"/>.<br/>
        /// The current format (PBKDF2-HMAC-SHA256) is always used.
        /// </summary>
        /// <param name="password">The password to be stored.</param>
        public PasswordHash(string password)
        {
            _version = CURRENTVERSION;
            _salt    = new byte[SALTSIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(_salt);
            }
            _hash    = DeriveHash(password, _salt, _version);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from a byte array generated by <see cref="ToArray"/>.<br/>
        /// An array of the current format (version byte followed by salt and hash) is recognised by its length and first byte;
        /// any other array is read as legacy format (salt followed by hash).
        /// </summary>
        /// <param name="hashBytes">The byte array containing the stored hash.</param>
        public PasswordHash(byte[] hashBytes)
        {
            if (hashBytes.Length == VERSIONSIZE + SALTSIZE + HASHSIZE && hashBytes[0] == CURRENTVERSION)
            {
                _version = CURRENTVERSION;
                Array.Copy(hashBytes, VERSIONSIZE, _salt = new byte[SALTSIZE], 0, SALTSIZE);
                Array.Copy(hashBytes, VERSIONSIZE + SALTSIZE, _hash = new byte[HASHSIZE], 0, HASHSIZE);
            }
            else
            {
                _version = LEGACYVERSION;
                Array.Copy(hashBytes, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
                Array.Copy(hashBytes, SALTSIZE, _hash = new byte[LEGACYHASHSIZE], 0, LEGACYHASHSIZE);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from separate salt and hash byte arrays.<br/>
        /// The format is recognised by the hash length: 32 bytes for the current format, otherwise legacy format.
        /// </summary>
        /// <param name="salt">The salt byte array.</param>
        /// <param name="hash">The hash byte array.</param>
        public PasswordHash(byte[] salt, byte[] hash)
        {
            _version    = hash.Length == HASHSIZE ? CURRENTVERSION : LEGACYVERSION;
            var size    = _version == CURRENTVERSION ? HASHSIZE : LEGACYHASHSIZE;
            Array.Copy(salt, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
            Array.Copy(hash, 0, _hash = new byte[size], 0, size);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a byte array containing the stored hash in the format of the instance:<br/>
        /// version byte, salt and hash for the current format; salt and hash for the legacy format.
        /// </summary>
        /// <returns>A byte array with the stored hash.</returns>
        public byte[] ToArray()
        {
            byte[] hashBytes;

            if (_version == CURRENTVERSION)
            {
                hashBytes    = new byte[VERSIONSIZE + SALTSIZE + HASHSIZE];
                hashBytes[0] = _version;
                Array.Copy(_salt, 0, hashBytes, VERSIONSIZE, SALTSIZE);
                Array.Copy(_hash, 0, hashBytes, VERSIONSIZE + SALTSIZE, HASHSIZE);
            }
            else
            {
                hashBytes = new byte[SALTSIZE + LEGACYHASHSIZE];
                Array.Copy(_salt, 0, hashBytes, 0, SALTSIZE);
                Array.Copy(_hash, 0, hashBytes, SALTSIZE, LEGACYHASHSIZE);
            }

            return hashBytes;
        }

        /// <summary>
        /// Verify if <paramref name="password"/> equals the stored password.<br/>
        /// The hash is computed with the algorithm of the stored format and compared in constant time.
        /// </summary>
        /// <param name="password">Password to check.</param>
        /// <returns>True if the password is correct.</returns>
        public bool Verify(string password)
        {
            var test = DeriveHash(password, _salt, _version);
            return FixedTimeEquals(test, _hash);
        }

        /// <summary>
        /// Computes the PBKDF2 hash of <paramref name="password"/> with the parameters of the format <paramref name="version"/>.
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <param name="salt">Salt bytes.</param>
        /// <param name="version">Format version.</param>
        /// <returns>The hash bytes.</returns>
        private static byte[] DeriveHash(string password, byte[] salt, byte version)
        {
            byte[] hash;

            if (version == CURRENTVERSION)
            {
                hash = Pbkdf2(password, salt, HASHITERATIONS, HashAlgorithmName.SHA256, HASHSIZE);
            }
            else
            {
                hash = Pbkdf2(password, salt, LEGACYHASHITERATIONS, HashAlgorithmName.SHA1, LEGACYHASHSIZE);
            }

            return hash;
        }

        /// <summary>
        /// Computes the PBKDF2 (RFC 2898) derived key of <paramref name="password"/> (UTF-8 encoded).<br/>
        /// On .NET 6 or later the framework implementation is used; on .NET Standard 2.0, where
        /// <see cref="Rfc2898DeriveBytes"/> supports only SHA-1, <see cref="Pbkdf2Hmac"/> is used.
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <param name="salt">Salt bytes.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <param name="hashAlgorithm">HMAC hash algorithm (SHA1 or SHA256).</param>
        /// <param name="length">Length of the derived key (bytes).</param>
        /// <returns>The derived key.</returns>
        private static byte[] Pbkdf2(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int length)
        {
#if NET6_0_OR_GREATER
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, length);
#else
            return Pbkdf2Hmac(System.Text.Encoding.UTF8.GetBytes(password), salt, iterations, hashAlgorithm, length);
#endif
        }

        /// <summary>
        /// Managed implementation of PBKDF2 with HMAC (RFC 8018, section 5.2), used where the framework
        /// does not provide PBKDF2 with SHA-256.
        /// </summary>
        /// <param name="password">Password bytes.</param>
        /// <param name="salt">Salt bytes.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <param name="hashAlgorithm">HMAC hash algorithm (SHA1 or SHA256).</param>
        /// <param name="length">Length of the derived key (bytes).</param>
        /// <returns>The derived key.</returns>
        private static byte[] Pbkdf2Hmac(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int length)
        {
            var derivedKey = new byte[length];

            using (HMAC hmac = hashAlgorithm == HashAlgorithmName.SHA256 ? (HMAC)new HMACSHA256(password) : new HMACSHA1(password))
            {
                var blockSize   = hmac.HashSize / 8;
                var saltBlock   = new byte[salt.Length + 4];
                Array.Copy(salt, saltBlock, salt.Length);

                // Each block T(i) is the XOR of the U(1)..U(c) values: U(1) = HMAC(salt || INT(i)), U(j) = HMAC(U(j-1))
                for (int block = 1, offset = 0; offset < length; block++, offset += blockSize)
                {
                    saltBlock[salt.Length]     = (byte)(block >> 24);
                    saltBlock[salt.Length + 1] = (byte)(block >> 16);
                    saltBlock[salt.Length + 2] = (byte)(block >> 8);
                    saltBlock[salt.Length + 3] = (byte)block;

                    var u       = hmac.ComputeHash(saltBlock);
                    var result  = (byte[])u.Clone();
                    for (var i = 1; i < iterations; i++)
                    {
                        u = hmac.ComputeHash(u);
                        for (var j = 0; j < blockSize; j++)
                        {
                            result[j] ^= u[j];
                        }
                    }

                    Array.Copy(result, 0, derivedKey, offset, Math.Min(blockSize, length - offset));
                }
            }

            return derivedKey;
        }

        /// <summary>
        /// Compares two byte arrays in a time that depends only on their length, to prevent timing attacks.
        /// </summary>
        /// <param name="left">First array.</param>
        /// <param name="right">Second array.</param>
        /// <returns>True if the arrays have the same length and content.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
#if NET6_0_OR_GREATER
            return CryptographicOperations.FixedTimeEquals(left, right);
#else
            var equal = left.Length == right.Length;

            if (equal)
            {
                var difference = 0;
                for (var i = 0; i < left.Length; i++)
                {
                    difference |= left[i] ^ right[i];
                }
                equal = difference == 0;
            }

            return equal;
#endif
        }

#if DEBUG

        /// <inheritdoc cref="Pbkdf2Hmac(byte[], byte[], int, HashAlgorithmName, int)"/>
        /// <remarks>For use in tests only.</remarks>
        public static byte[] TestPbkdf2Hmac(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int length)
            => Pbkdf2Hmac(password, salt, iterations, hashAlgorithm, length);

#endif

        #endregion
    }
}
