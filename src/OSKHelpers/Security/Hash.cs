using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OSKHelpers.Security
{
    /// <summary>
    /// Static helper to compute cryptographic hashes of a string.<br/>
    /// Not safe to be used for password.
    /// </summary>
    public static class Hash
    {
        /// <summary>
        /// Computes the hash of the given string.<br/>
        /// https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
        /// </summary>
        /// <param name="algoritm">Hashing algorithm to use.</param>
        /// <param name="rawData">String to hash.</param>
        /// <returns>The hash of the string.</returns>
        /// <exception cref="ArgumentNullException" />
        private static string ComputeHash(HashAlgorithm algoritm, string rawData)
        {
            if (rawData == null) 
                throw new ArgumentNullException(nameof(rawData));
            if (algoritm == null)
                throw new ArgumentNullException(nameof(algoritm));
            // ComputeHash - returns byte array
            byte[] bytes = algoritm.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            // Build the hash string
            var hash = string.Concat(bytes.Select(b => b.ToString("x2")));

            return hash;

        }

        /// <inheritdoc cref="ComputeHash(HashAlgorithm, string)"/>
        public static string ComputeSha256Hash(string rawData)
        {
            using (var algoritm = SHA256.Create())
                return ComputeHash(algoritm, rawData);
        }

        /// <inheritdoc cref="ComputeHash(HashAlgorithm, string)"/>
        public static string ComputeSha512Hash(string rawData)
        {
            using (var algoritm = SHA512.Create())
                return ComputeHash(algoritm, rawData);
        }

    }
}
