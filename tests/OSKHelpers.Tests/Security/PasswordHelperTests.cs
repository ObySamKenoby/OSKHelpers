using System;
using System.Collections.Generic;
using System.Linq;
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
    public void HashAndVerifyTests()
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
}
