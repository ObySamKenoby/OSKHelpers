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
    /// 
    /// </code>
    /// </summary>
    public sealed class PasswordHash
    {
        #region Members

        private const int SALTSIZE = 16;
        private const int HASHSIZE = 20;
        private const int HASHITERATIONS = 10000;
        private readonly byte[] _salt;
        private readonly byte[] _hash;

        #endregion

        #region Properties

        public byte[] Salt => (byte[])_salt.Clone();

        public byte[] Hash => (byte[])_hash.Clone();

        #endregion

        #region Methods

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

        public PasswordHash(byte[] hashBytes)
        {
            Array.Copy(hashBytes, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
            Array.Copy(hashBytes, SALTSIZE, _hash = new byte[HASHSIZE], 0, HASHSIZE);
        }

        public PasswordHash(byte[] salt, byte[] hash)
        {
            Array.Copy(salt, 0, _salt = new byte[SALTSIZE], 0, SALTSIZE);
            Array.Copy(hash, 0, _hash = new byte[HASHSIZE], 0, HASHSIZE);
        }

        public byte[] ToArray()
        {
            byte[] hashBytes = new byte[SALTSIZE + HASHSIZE];
            Array.Copy(_salt, 0, hashBytes, 0, SALTSIZE);
            Array.Copy(_hash, 0, hashBytes, SALTSIZE, HASHSIZE);
            return hashBytes;
        }

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