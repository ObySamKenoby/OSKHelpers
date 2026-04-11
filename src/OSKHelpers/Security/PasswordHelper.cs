using System;
using System.Security.Cryptography;

namespace OSKHelpers.Security
{
    /// <summary>
    /// Provides utilities for secure password hashing and verification<br/>
    /// using PBKDF2 with SHA-1 and a random per-user salt.
    /// </summary>
    /// <remarks>
    /// SHA-1 is used to make this work with .NET Standard 2.0,<br/>
    /// SHA-256 will be implemented soon, still the methods aren't<br/>
    /// to be considered broken for the intended purpose.
    /// </remarks>
    public static class PasswordHelper
    {
        #region Methods

        /// <summary>
        /// Hashes a plain-text password using PBKDF2 with a randomly generated salt.
        /// </summary>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>
        /// A tuple containing the computed <c>Hash</c> and the generated <c>Salt</c>
        /// as byte arrays. Both must be stored to allow future verification.
        /// </returns>
        /// <example>
        /// <code>
        /// var (hash, salt) = PasswordHelper.HashPassword("myPassword");
        /// user.PasswordHash = hash;
        /// user.PasswordSalt = salt;
        /// </code>
        /// </example>
        public static byte[] HashPassword(string password)
        {
            var hash = new PasswordHash(password);
            return hash.ToArray();
        }

        /// <summary>
        /// Verifies a plain-text password against a stored hash and salt.<br/>
        /// Uses a constant-time comparison to prevent timing attacks.
        /// </summary>
        /// <param name="password">The plain-text password to verify.</param>
        /// <param name="hash">The stored password hash.</param>
        /// <param name="salt">The stored password salt.</param>
        /// <returns>
        /// <c>true</c> if the password matches the stored hash; otherwise <c>false</c>.
        /// </returns>
        public static bool VerifyPassword(string password, byte[] hash)
        {
            var challenge = new PasswordHash(hash);
            return challenge.Verify(password);
        }

        #endregion
    }
}