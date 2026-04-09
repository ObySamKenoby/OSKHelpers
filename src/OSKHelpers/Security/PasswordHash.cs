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
    /// </code>
    /// </summary>
    public sealed class PasswordHash
    {
        #region Members

        /// <summary>
        /// Salt size (bytes).
        /// </summary>
        private const int SALTSIZE = 16;
        /// <summary>
        /// Hash size (bytes).
        /// </summary>
        private const int HASHSIZE = 20;
        /// <summary>
        /// Number od hash iterations.
        /// </summary>
        private const int HASHITERATIONS = 10000;
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

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from <paramref name="password"/>.<br/>
        /// <see cref="Salt"/> will be randomly generated, so each object created for a password will have unique <see cref="Hash"/>.
        /// </summary>
        /// <param name="password">The password to be stored.</param>
        /// <remarks>
        /// The method use a deprecated constructor of <see cref="Rfc2898DeriveBytes"/> to maintain compatibility<br/>
        /// with the version in .NET  Standard 2.0, to be fixed implementing a cross-compatible version<br/>
        /// using SHA256 as encryption algorithm.<br/>
        /// The method is still safe to use.
        /// </remarks>
        public PasswordHash(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(_salt = new byte[SALTSIZE]);
                using (var rfc = new Rfc2898DeriveBytes(password, _salt, HASHITERATIONS))
                {
                    _hash = rfc.GetBytes(HASHSIZE);
                    rfc.Dispose();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from a combined salt+hash byte array.
        /// </summary>
        /// <param name="hashBytes">The byte array containing salt and hash (salt first, then hash).</param>
        public PasswordHash(byte[] hashBytes)
        {
            Array.Copy(hashBytes, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
            Array.Copy(hashBytes, SALTSIZE, _hash = new byte[HASHSIZE], 0, HASHSIZE);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHash"/> class from separate salt and hash byte arrays.
        /// </summary>
        /// <param name="salt">The salt byte array.</param>
        /// <param name="hash">The hash byte array.</param>
        public PasswordHash(byte[] salt, byte[] hash)
        {
            Array.Copy(salt, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
            Array.Copy(hash, 0, _hash = new byte[HASHSIZE], 0, HASHSIZE);
        }

        /// <summary>
        /// Returns a byte array containing the salt followed by the hash.
        /// </summary>
        /// <returns>A byte array with salt and hash concatenated.</returns>
        public byte[] ToArray()
        {
            byte[] hashBytes = new byte[SALTSIZE + HASHSIZE];
            Array.Copy(_salt, 0, hashBytes, 0, SALTSIZE);
            Array.Copy(_hash, 0, hashBytes, SALTSIZE, HASHSIZE);
            return hashBytes;
        }

        /// <summary>
        /// Verify if <paramref name="password"/> equals the stored password.
        /// </summary>
        /// <param name="password">Password to check.</param>
        /// <returns>True if the password is correct.</returns>
        /// <remarks>
        /// The method use a deprecated constructor of <see cref="Rfc2898DeriveBytes"/> to maintain compatibility<br/>
        /// with the version in .NET  Standard 2.0, to be fixed implementing a cross-compatible version<br/>
        /// using SHA256 as encryption algorithm.<br/>
        /// The method is still safe to use.
        /// </remarks>
        public bool Verify(string password)
        {
            var result = true;

            using (var rfc = new Rfc2898DeriveBytes(password, _salt, HASHITERATIONS))
            {
                byte[] test = rfc.GetBytes(HASHSIZE);

                for (int i = 0; i < HASHSIZE; i++)
                {
                    if (test[i] != _hash[i])
                    {
                        result = false;
                        break;
                    }
                }
                rfc.Dispose();
            }

            return result;
        }

#endregion
    }
}