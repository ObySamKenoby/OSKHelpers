using System;
using OSKHelpers.Common;
using OSKHelpers.Security;

namespace OSKHelpers.Tests.Security;

[TestClass]
[DoNotParallelize]
public class PasswordObfuscatorTests
{
    [TestMethod]
    public void TestPasswordObfuscator()
    {
        var rnd = new Random();

        for (var  i = 0; i < 1000; i++)
        {
            var pwd = StringUtils.GenerateAlphaNumSpecialsString(rnd.Next(9) + 8).Trim();
            var obfuscated = PasswordObfuscator.EncodePassword(pwd);
            Assert.AreNotEqual(pwd, obfuscated);
            Assert.AreEqual(pwd, PasswordObfuscator.DecodePassword(obfuscated));
            PasswordObfuscator.ReSeed();
            var newObfuscated = PasswordObfuscator.EncodePassword(pwd);
            Assert.AreNotEqual(obfuscated, newObfuscated);
            Assert.AreNotEqual(pwd, obfuscated);
            Assert.AreEqual(pwd, PasswordObfuscator.DecodePassword(newObfuscated));
        }
    }

    [TestMethod]
    public void EncodePasswordTests()
    {
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.EncodePassword(null));
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.EncodePassword(string.Empty));
        var obfuscated = PasswordObfuscator.EncodePassword("A");
        Assert.AreEqual(8, obfuscated.Length);
        Assert.AreEqual("A", PasswordObfuscator.DecodePassword(obfuscated));
    }

    [TestMethod]
    public void DecodePasswordTTests()
    {
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.DecodePassword(null));
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.DecodePassword(string.Empty));
        // The string to decode must be at least 5 characters long (4 are used for
        // encoding the password start index within the hash).
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.DecodePassword("1"));
        Assert.Throws<ArgumentException>(() => PasswordObfuscator.DecodePassword("1234567"));
        Assert.IsNotEmpty(PasswordObfuscator.DecodePassword("12345678"));
    }

    [TestMethod]
    public void EncodeDecodeIndexTests()
    {
        var strIndex = PasswordObfuscator.TestEncodeIndex(1234);
        Assert.AreEqual(4, strIndex.Length);
        Assert.AreEqual(1234, PasswordObfuscator.TestDecodeIndex(strIndex));
        strIndex = PasswordObfuscator.TestEncodeIndex(-1234);
        Assert.AreEqual(4, strIndex.Length);
        Assert.AreEqual(-1234, PasswordObfuscator.TestDecodeIndex(strIndex));
    }
}
