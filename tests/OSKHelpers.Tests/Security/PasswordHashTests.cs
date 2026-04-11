using System;
using System.Linq;
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
        /// Expected hash size (bytes).
        /// </summary>
        private const int EXPECTEDHASHSIZE = 20;

        #endregion

        #region Constructor(string)

        /// <summary>
        /// Verifies that the constructor with a string produces salt and hash of the correct size.
        /// </summary>
        [TestMethod]
        public void ConstructorStringPasswordProducesSaltAndHashOfCorrectSize()
        {
            var ph = new PasswordHash("MyPassword123");

            Assert.AreEqual(EXPECTEDSALTSIZE, ph.Salt.Length);
            Assert.AreEqual(EXPECTEDHASHSIZE, ph.Hash.Length);
        }

        /// <summary>
        /// Verifies that two instances with the same password produce different salts.
        /// </summary>
        [TestMethod]
        public void ConstructorSamePasswordProducesDifferentSalts()
        {
            var ph1 = new PasswordHash("MyPassword123");
            var ph2 = new PasswordHash("MyPassword123");

            Assert.IsFalse(ph1.Salt.SequenceEqual(ph2.Salt));
        }

        /// <summary>
        /// Verifies that two instances with the same password produce different hashes (due to random salt).
        /// </summary>
        [TestMethod]
        public void ConstructorSamePasswordProducesDifferentHashes()
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
        public void ConstructorByteArrayReconstructsSaltAndHash()
        {
            var original    = new PasswordHash("TestPassword");
            var combined    = original.ToArray();
            var restored    = new PasswordHash(combined);

            Assert.IsTrue(original.Salt.SequenceEqual(restored.Salt));
            Assert.IsTrue(original.Hash.SequenceEqual(restored.Hash));
        }

        #endregion

        #region Constructor(byte[], byte[])

        /// <summary>
        /// Verifies that the constructor with separate salt and hash correctly reconstructs the data.
        /// </summary>
        [TestMethod]
        public void ConstructorSeparateSaltAndHashReconstructsCorrectly()
        {
            var original    = new PasswordHash("TestPassword");
            var restored    = new PasswordHash(original.Salt, original.Hash);

            Assert.IsTrue(original.Salt.SequenceEqual(restored.Salt));
            Assert.IsTrue(original.Hash.SequenceEqual(restored.Hash));
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Verifies that ToArray returns an array of size salt + hash.
        /// </summary>
        [TestMethod]
        public void ToArrayValidInstanceReturnsCombinedArray()
        {
            var ph      = new PasswordHash("TestPassword");
            var array   = ph.ToArray();

            Assert.AreEqual(EXPECTEDSALTSIZE + EXPECTEDHASHSIZE, array.Length);
        }

        /// <summary>
        /// Verifies that the first part of ToArray corresponds to the Salt and the second to the Hash.
        /// </summary>
        [TestMethod]
        public void ToArrayValidInstanceContainsSaltThenHash()
        {
            var ph      = new PasswordHash("TestPassword");
            var array   = ph.ToArray();
            var salt    = new byte[EXPECTEDSALTSIZE];
            var hash    = new byte[EXPECTEDHASHSIZE];
            Array.Copy(array, 0, salt, 0, EXPECTEDSALTSIZE);
            Array.Copy(array, EXPECTEDSALTSIZE, hash, 0, EXPECTEDHASHSIZE);

            Assert.IsTrue(ph.Salt.SequenceEqual(salt));
            Assert.IsTrue(ph.Hash.SequenceEqual(hash));
        }

        #endregion

        #region Verify

        /// <summary>
        /// Verifies that Verify returns true for the correct password.
        /// </summary>
        [TestMethod]
        public void VerifyCorrectPasswordReturnsTrue()
        {
            var ph = new PasswordHash("CorrectPassword");

            Assert.IsTrue(ph.Verify("CorrectPassword"));
        }

        /// <summary>
        /// Verifies that Verify returns false for a wrong password.
        /// </summary>
        [TestMethod]
        public void VerifyWrongPasswordReturnsFalse()
        {
            var ph = new PasswordHash("CorrectPassword");

            Assert.IsFalse(ph.Verify("WrongPassword"));
        }

        /// <summary>
        /// Verifies that reconstruction from a byte array preserves the ability to verify.
        /// </summary>
        [TestMethod]
        public void VerifyReconstructedFromByteArrayStillVerifies()
        {
            var original    = new PasswordHash("TestPassword");
            var combined    = original.ToArray();
            var restored    = new PasswordHash(combined);

            Assert.IsTrue(restored.Verify("TestPassword"));
            Assert.IsFalse(restored.Verify("WrongPassword"));
        }

        #endregion

        #region Properties (clone)

        /// <summary>
        /// Verifies that Salt returns a copy and not the internal reference.
        /// </summary>
        [TestMethod]
        public void SaltModifyReturnedArrayDoesNotAffectOriginal()
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
        public void HashModifyReturnedArrayDoesNotAffectOriginal()
        {
            var ph      = new PasswordHash("TestPassword");
            var hash1   = ph.Hash;
            hash1[0]    = (byte)(hash1[0] ^ 0xFF);
            var hash2   = ph.Hash;

            Assert.IsFalse(hash1.SequenceEqual(hash2));
        }

        #endregion
    }
}
