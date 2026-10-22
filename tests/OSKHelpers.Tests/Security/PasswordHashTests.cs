using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OSKHelpers.Security;

namespace OSKHelpers.Tests.Security
{
    /// <summary>
    /// Tests for the <see cref="PasswordHash"/> class.
    /// </summary>
    [TestClass]
    public class PasswordHashTests
    {
        #region Constants

        /// <summary>
        /// Expected salt size (bytes).
        /// </summary>
        private const int EXPECTEDSALTSIZE = 16;

        /// <summary>
        /// Expected hash size of the current format (bytes).
        /// </summary>
        private const int EXPECTEDHASHSIZE = 32;

        /// <summary>
        /// Expected version byte of the current format.
        /// </summary>
        private const byte EXPECTEDVERSION = 1;

        /// <summary>
        /// Hash size of the legacy format (bytes).
        /// </summary>
        private const int LEGACYHASHSIZE = 20;

        /// <summary>
        /// Number of iterations of the legacy format.
        /// </summary>
        private const int LEGACYITERATIONS = 10000;

        #endregion

        #region Constructor(string)

        /// <summary>
        /// Verifies that the constructor with a string produces salt and hash of the correct size.
        /// </summary>
        [TestMethod]
        public void Constructor_StringPassword_ProducesSaltAndHashOfCorrectSize()
        {
            var ph = new PasswordHash("MyPassword123");

            Assert.AreEqual(EXPECTEDSALTSIZE, ph.Salt.Length);
            Assert.AreEqual(EXPECTEDHASHSIZE, ph.Hash.Length);
            Assert.IsFalse(ph.RequiresRehash);
        }

        /// <summary>
        /// Verifies that the hash is PBKDF2-HMAC-SHA256 with 600,000 iterations.
        /// </summary>
        [TestMethod]
        public void Constructor_StringPassword_UsesPbkdf2Sha256()
        {
            var ph          = new PasswordHash("MyPassword123");
            var expected    = Rfc2898DeriveBytes.Pbkdf2("MyPassword123", ph.Salt, 600000, HashAlgorithmName.SHA256, EXPECTEDHASHSIZE);

            Assert.IsTrue(expected.SequenceEqual(ph.Hash));
        }

        /// <summary>
        /// Verifies that two instances with the same password produce different salts.
        /// </summary>
        [TestMethod]
        public void Constructor_SamePassword_ProducesDifferentSalts()
        {
            var ph1 = new PasswordHash("MyPassword123");
            var ph2 = new PasswordHash("MyPassword123");

            Assert.IsFalse(ph1.Salt.SequenceEqual(ph2.Salt));
        }

        /// <summary>
        /// Verifies that two instances with the same password produce different hashes (due to random salt).
        /// </summary>
        [TestMethod]
        public void Constructor_SamePassword_ProducesDifferentHashes()
        {
            var ph1 = new PasswordHash("MyPassword123");
            var ph2 = new PasswordHash("MyPassword123");

            Assert.IsFalse(ph1.Hash.SequenceEqual(ph2.Hash));
        }

        #endregion

        #region Constructor(byte[])

        /// <summary>
        /// Verifies that the constructor from a combined array correctly reconstructs salt and hash.
        /// </summary>
        [TestMethod]
        public void Constructor_ByteArray_ReconstructsSaltAndHash()
        {
            var original    = new PasswordHash("TestPassword");
            var combined    = original.ToArray();
            var restored    = new PasswordHash(combined);

            Assert.IsTrue(original.Salt.SequenceEqual(restored.Salt));
            Assert.IsTrue(original.Hash.SequenceEqual(restored.Hash));
            Assert.IsFalse(restored.RequiresRehash);
        }

        /// <summary>
        /// Verifies that a 36-byte array is read as legacy format.
        /// </summary>
        [TestMethod]
        public void Constructor_LegacyByteArray_ReconstructsSaltAndHash()
        {
            var legacy      = CreateLegacyHash("TestPassword", out var salt, out var hash);
            var restored    = new PasswordHash(legacy);

            Assert.IsTrue(salt.SequenceEqual(restored.Salt));
            Assert.IsTrue(hash.SequenceEqual(restored.Hash));
            Assert.IsTrue(restored.RequiresRehash);
        }

        #endregion

        #region Constructor(byte[], byte[])

        /// <summary>
        /// Verifies that the constructor with separate salt and hash correctly reconstructs the data.
        /// </summary>
        [TestMethod]
        public void Constructor_SeparateSaltAndHash_ReconstructsData()
        {
            var original    = new PasswordHash("TestPassword");
            var restored    = new PasswordHash(original.Salt, original.Hash);

            Assert.IsTrue(original.Salt.SequenceEqual(restored.Salt));
            Assert.IsTrue(original.Hash.SequenceEqual(restored.Hash));
            Assert.IsTrue(restored.Verify("TestPassword"));
        }

        /// <summary>
        /// Verifies that the constructor with separate legacy salt and hash recognises the legacy format.
        /// </summary>
        [TestMethod]
        public void Constructor_SeparateLegacySaltAndHash_Verifies()
        {
            CreateLegacyHash("TestPassword", out var salt, out var hash);
            var restored = new PasswordHash(salt, hash);

            Assert.IsTrue(restored.RequiresRehash);
            Assert.IsTrue(restored.Verify("TestPassword"));
            Assert.IsFalse(restored.Verify("WrongPassword"));
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Verifies that ToArray returns an array of size version + salt + hash.
        /// </summary>
        [TestMethod]
        public void ToArray_ValidInstance_ReturnsCombinedArray()
        {
            var ph      = new PasswordHash("TestPassword");
            var array   = ph.ToArray();

            Assert.AreEqual(1 + EXPECTEDSALTSIZE + EXPECTEDHASHSIZE, array.Length);
        }

        /// <summary>
        /// Verifies that ToArray contains the version byte followed by Salt and Hash.
        /// </summary>
        [TestMethod]
        public void ToArray_ValidInstance_ContainsVersionSaltThenHash()
        {
            var ph      = new PasswordHash("TestPassword");
            var array   = ph.ToArray();
            var salt    = new byte[EXPECTEDSALTSIZE];
            var hash    = new byte[EXPECTEDHASHSIZE];
            Array.Copy(array, 1, salt, 0, EXPECTEDSALTSIZE);
            Array.Copy(array, 1 + EXPECTEDSALTSIZE, hash, 0, EXPECTEDHASHSIZE);

            Assert.AreEqual(EXPECTEDVERSION, array[0]);
            Assert.IsTrue(ph.Salt.SequenceEqual(salt));
            Assert.IsTrue(ph.Hash.SequenceEqual(hash));
        }

        /// <summary>
        /// Verifies that ToArray of a legacy instance returns the original 36-byte array.
        /// </summary>
        [TestMethod]
        public void ToArray_LegacyInstance_ReturnsLegacyArray()
        {
            var legacy  = CreateLegacyHash("TestPassword", out _, out _);
            var ph      = new PasswordHash(legacy);

            Assert.IsTrue(legacy.SequenceEqual(ph.ToArray()));
        }

        #endregion

        #region Verify

        /// <summary>
        /// Verifies that Verify returns true for the correct password.
        /// </summary>
        [TestMethod]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var ph = new PasswordHash("CorrectPassword");

            Assert.IsTrue(ph.Verify("CorrectPassword"));
        }

        /// <summary>
        /// Verifies that Verify returns false for a wrong password.
        /// </summary>
        [TestMethod]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var ph = new PasswordHash("CorrectPassword");

            Assert.IsFalse(ph.Verify("WrongPassword"));
        }

        /// <summary>
        /// Verifies that reconstruction from a byte array preserves the ability to verify.
        /// </summary>
        [TestMethod]
        public void Verify_ReconstructedFromByteArray_StillVerifies()
        {
            var original    = new PasswordHash("TestPassword");
            var combined    = original.ToArray();
            var restored    = new PasswordHash(combined);

            Assert.IsTrue(restored.Verify("TestPassword"));
            Assert.IsFalse(restored.Verify("WrongPassword"));
        }

        /// <summary>
        /// Verifies that a hash created with the legacy format (PBKDF2-HMAC-SHA1, 10,000 iterations) is still verified.
        /// </summary>
        [TestMethod]
        public void Verify_LegacyHash_StillVerifies()
        {
            var legacy      = CreateLegacyHash("TestPassword", out _, out _);
            var restored    = new PasswordHash(legacy);

            Assert.IsTrue(restored.Verify("TestPassword"));
            Assert.IsFalse(restored.Verify("WrongPassword"));
        }

        #endregion

        #region Properties (clone)

        /// <summary>
        /// Verifies that Salt returns a copy and not the internal reference.
        /// </summary>
        [TestMethod]
        public void Salt_ModifyReturnedArray_DoesNotAffectOriginal()
        {
            var ph      = new PasswordHash("TestPassword");
            var salt1   = ph.Salt;
            salt1[0]    = (byte)(salt1[0] ^ 0xFF);
            var salt2   = ph.Salt;

            Assert.IsFalse(salt1.SequenceEqual(salt2));
        }

        /// <summary>
        /// Verifies that Hash returns a copy and not the internal reference.
        /// </summary>
        [TestMethod]
        public void Hash_ModifyReturnedArray_DoesNotAffectOriginal()
        {
            var ph      = new PasswordHash("TestPassword");
            var hash1   = ph.Hash;
            hash1[0]    = (byte)(hash1[0] ^ 0xFF);
            var hash2   = ph.Hash;

            Assert.IsFalse(hash1.SequenceEqual(hash2));
        }

        #endregion

#if DEBUG

        #region Pbkdf2Hmac (managed implementation used on .NET Standard 2.0)

        /// <summary>
        /// Verifies that the managed PBKDF2 implementation produces the same result as the framework, for SHA-1 and SHA-256
        /// and for lengths that require more than one block.
        /// </summary>
        [TestMethod]
        [DataRow("SHA1", 20)]
        [DataRow("SHA1", 50)]
        [DataRow("SHA256", 32)]
        [DataRow("SHA256", 70)]
        public void TestPbkdf2Hmac_VariousAlgorithmsAndLengths_MatchesFrameworkImplementation(string algorithm, int length)
        {
            var hashAlgorithm   = new HashAlgorithmName(algorithm);
            var password        = Encoding.UTF8.GetBytes("P@ssw0rd àèì");
            var salt            = Encoding.UTF8.GetBytes("0123456789abcdef");
            var expected        = Rfc2898DeriveBytes.Pbkdf2(password, salt, 1000, hashAlgorithm, length);
            var actual          = PasswordHash.TestPbkdf2Hmac(password, salt, 1000, hashAlgorithm, length);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// Verifies the managed PBKDF2 implementation against the test vector of RFC 6070 (SHA-1, 2 iterations).
        /// </summary>
        [TestMethod]
        public void TestPbkdf2Hmac_Rfc6070Vector_ReturnsExpected()
        {
            var actual = PasswordHash.TestPbkdf2Hmac(
                Encoding.ASCII.GetBytes("password"), Encoding.ASCII.GetBytes("salt"), 2, HashAlgorithmName.SHA1, 20);

            Assert.AreEqual("ea6c014dc72d6f8ccd1ed92ace1d41f0d8de8957", Convert.ToHexString(actual).ToLowerInvariant());
        }

        #endregion

#endif

        #region Support methods

        /// <summary>
        /// Creates a hash in the legacy format (16 bytes of salt + 20 bytes of PBKDF2-HMAC-SHA1, 10,000 iterations),
        /// as generated by the previous versions of <see cref="PasswordHash"/>.
        /// </summary>
        private static byte[] CreateLegacyHash(string password, out byte[] salt, out byte[] hash)
        {
            salt        = RandomNumberGenerator.GetBytes(EXPECTEDSALTSIZE);
            hash        = Rfc2898DeriveBytes.Pbkdf2(password, salt, LEGACYITERATIONS, HashAlgorithmName.SHA1, LEGACYHASHSIZE);
            var legacy  = new byte[EXPECTEDSALTSIZE + LEGACYHASHSIZE];
            Array.Copy(salt, 0, legacy, 0, EXPECTEDSALTSIZE);
            Array.Copy(hash, 0, legacy, EXPECTEDSALTSIZE, LEGACYHASHSIZE);

            return legacy;
        }

        #endregion
    }
}
