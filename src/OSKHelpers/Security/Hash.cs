using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OSKHelpers.Security
{
    public static class Hash
    {
        /// <summary>
        /// Genera l'hash della stringa passata come parametro.<br/>
        /// https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
        /// </summary>
        /// <param name="algoritm">Algoritmo da utilizzare per generare l'hash.</param>
        /// <param name="rawData">Stringa di cui generare l'hash.</param>
        /// <returns>L'hash della stringa.</returns>
        /// <exception cref="ArgumentNullException" />
        private static string ComputeHash(HashAlgorithm algoritm, string rawData)
        {
            if (rawData == null) 
                throw new ArgumentNullException(nameof(rawData));
            if (algoritm == null)
                throw new ArgumentNullException(nameof(algoritm));
            // ComputeHash - returns byte array
            byte[] bytes = algoritm.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            // Genera la stringa di hash
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
