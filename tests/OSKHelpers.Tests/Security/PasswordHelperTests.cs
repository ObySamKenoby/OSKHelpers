using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using OSKHelpers.Common;
using OSKHelpers.Security;

namespace OSKHelpers.Tests.Security;

[TestClass]
public class PasswordHelperTests
{
    /// <summary>
    /// Verifies that five different iterations of hash / verify for a random password succeed<br/>
    /// and that the hashes are different each from others.<br/>
    /// Repeated for three different passwords.
    /// </summary>
    [TestMethod]
    public void HashPasswordAndVerifyPassword_RandomPasswords_VerifyWithUniqueHashes()
    {
        // For simplicity the hashes will be stored as string representation of the byte arrays.
        var hashes = new List<string>();

        // Three different passwwords...
        for (var p = 0; p < 3; p++)
        {
            var password = StringUtils.GenerateAlphaNumSpecialsString(8);
            // ... five iterations each.
            for (var i = 0; i < 5; i++)
            {
                var hash = PasswordHelper.HashPassword(password);
                hashes.Add(BitConverter.ToString(hash));
                Assert.IsTrue(PasswordHelper.VerifyPassword(password, hash));
            }
        }
        // There must be no duplicates in hashes.
        Assert.IsFalse(hashes.GroupBy(h => h).Any(g => g.Count() > 1));
    }

    /// <summary>
    /// Verifies that a new hash does not require a rehash, while a legacy one (36 bytes, PBKDF2-HMAC-SHA1) does
    /// and is still verified.
    /// </summary>
    [TestMethod]
    public void RequiresRehash_CurrentAndLegacyHash_ReturnsExpected()
    {
        var salt    = RandomNumberGenerator.GetBytes(16);
        var legacy  = salt.Concat(Rfc2898DeriveBytes.Pbkdf2("Password", salt, 10000, HashAlgorithmName.SHA1, 20)).ToArray();
        var current = PasswordHelper.HashPassword("Password");

        Assert.IsFalse(PasswordHelper.RequiresRehash(current));
        Assert.IsTrue(PasswordHelper.RequiresRehash(legacy));
        Assert.IsTrue(PasswordHelper.VerifyPassword("Password", legacy));
    }

    /// <summary>
    /// Verifies that a wrong password is rejected, for both the current and the legacy format.
    /// </summary>
    [TestMethod]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var salt    = RandomNumberGenerator.GetBytes(16);
        var legacy  = salt.Concat(Rfc2898DeriveBytes.Pbkdf2("Password", salt, 10000, HashAlgorithmName.SHA1, 20)).ToArray();
        var current = PasswordHelper.HashPassword("Password");

        Assert.IsFalse(PasswordHelper.VerifyPassword("password", current));
        Assert.IsFalse(PasswordHelper.VerifyPassword("password", legacy));
    }
}
