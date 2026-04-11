using System;
using System.Text.RegularExpressions;
using OSKHelpers.Security;

namespace OSKHelpers.Tests.Security
{
    /// <summary>
    /// Tests for the <see cref="Hash"/> class.
    /// </summary>
    [TestClass]
    public class HashTests
    {
        #region ComputeSha256Hash

        /// <summary>
        /// Verifies that the same input always produces the same SHA-256 hash.
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashSameInputReturnsSameHash()
        {
            var hash1 = Hash.ComputeSha256Hash("test");
            var hash2 = Hash.ComputeSha256Hash("test");

            Assert.AreEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies the expected SHA-256 hash value for the string "test" (known test vector).
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashKnownInputReturnsExpectedHash()
        {
            var expected = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

            var hash = Hash.ComputeSha256Hash("test");

            Assert.AreEqual(expected, hash);
        }

        /// <summary>
        /// Verifies that different inputs produce different SHA-256 hashes.
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashDifferentInputsReturnsDifferentHashes()
        {
            var hash1 = Hash.ComputeSha256Hash("test1");
            var hash2 = Hash.ComputeSha256Hash("test2");

            Assert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that a null input throws <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashNullInputThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Hash.ComputeSha256Hash(null));
        }

        /// <summary>
        /// Verifies that the SHA-256 hash consists of 64 lowercase hexadecimal characters.
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashValidInputReturns64LowercaseHexChars()
        {
            var hash = Hash.ComputeSha256Hash("test");

            Assert.AreEqual(64, hash.Length);
            Assert.IsTrue(Regex.IsMatch(hash, "^[0-9a-f]{64}$"));
        }

        /// <summary>
        /// Verifies that an empty string produces a valid hash.
        /// </summary>
        [TestMethod]
        public void ComputeSha256HashEmptyStringReturnsValidHash()
        {
            var hash = Hash.ComputeSha256Hash(string.Empty);

            Assert.AreEqual(64, hash.Length);
        }

        #endregion

        #region ComputeSha512Hash

        /// <summary>
        /// Verifies that the same input always produces the same SHA-512 hash.
        /// </summary>
        [TestMethod]
        public void ComputeSha512HashSameInputReturnsSameHash()
        {
            var hash1 = Hash.ComputeSha512Hash("test");
            var hash2 = Hash.ComputeSha512Hash("test");

            Assert.AreEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies the expected SHA-512 hash value for the string "test" (known test vector).
        /// </summary>
        [TestMethod]
        public void ComputeSha512HashKnownInputReturnsExpectedHash()
        {
            var expected = "ee26b0dd4af7e749aa1a8ee3c10ae9923f618980772e473f8819a5d4940e0db2"
                         + "7ac185f8a0e1d5f84f88bc887fd67b143732c304cc5fa9ad8e6f57f50028a8ff";

            var hash = Hash.ComputeSha512Hash("test");

            Assert.AreEqual(expected, hash);
        }

        /// <summary>
        /// Verifies that different inputs produce different SHA-512 hashes.
        /// </summary>
        [TestMethod]
        public void ComputeSha512HashDifferentInputsReturnsDifferentHashes()
        {
            var hash1 = Hash.ComputeSha512Hash("test1");
            var hash2 = Hash.ComputeSha512Hash("test2");

            Assert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that a null input throws <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeSha512HashNullInputThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Hash.ComputeSha512Hash(null));
        }

        /// <summary>
        /// Verifies that the SHA-512 hash consists of 128 lowercase hexadecimal characters.
        /// </summary>
        [TestMethod]
        public void ComputeSha512HashValidInputReturns128LowercaseHexChars()
        {
            var hash = Hash.ComputeSha512Hash("test");

            Assert.AreEqual(128, hash.Length);
            Assert.IsTrue(Regex.IsMatch(hash, "^[0-9a-f]{128}$"));
        }

        #endregion

        #region Cross-algorithm

        /// <summary>
        /// Verifies that SHA-256 and SHA-512 produce different results for the same input.
        /// </summary>
        [TestMethod]
        public void ComputeHashSha256VsSha512ReturnsDifferentResults()
        {
            var sha256 = Hash.ComputeSha256Hash("test");
            var sha512 = Hash.ComputeSha512Hash("test");

            Assert.AreNotEqual(sha256, sha512);
        }

        #endregion
    }
}
