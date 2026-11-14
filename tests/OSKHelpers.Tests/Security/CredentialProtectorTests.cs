using System;
using System.IO;
using System.Security.Cryptography;
using OSKHelpers.Common;
using OSKHelpers.Security;

namespace OSKHelpers.Tests.Security;

/// <summary>
/// Tests of <see cref="CredentialProtector"/>: DPAPI, AES-GCM and AES-CBC encryption, key file, tagged credentials and
/// handling of values that cannot be decrypted.
/// </summary>
[TestClass]
public class CredentialProtectorTests
{
    #region Constants

    private const string PLAINPASSWORD  = "Pàssw0rd!\"<>;'";
    private const string ENTROPY        = "OSKHelpers.Tests.Credentials";

    /// <summary>
    /// Prefix produced by <see cref="CredentialProtector.ProtectWithKey"/> with the build of OSKHelpers under test: AES-GCM
    /// with net8.0, AES-CBC with netstandard2.0 (project OSKHelpers.Tests.NetStandard).
    /// </summary>
#if OSKHELPERS_NETSTANDARD
    private const string KEYPREFIX      = CredentialProtector.AESCBCPREFIX;
#else
    private const string KEYPREFIX      = CredentialProtector.AESPREFIX;
#endif

    #endregion

    #region Members

    private string _tempDirectory;
    private string _keyFilePath;
    private CredentialProtector _sut;

    #endregion

    #region Methods

    [TestInitialize]
    public void Initialize()
    {
        _tempDirectory  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _keyFilePath    = Path.Combine(_tempDirectory, CredentialProtector.DEFAULTKEYFILENAME);
        _sut            = new CredentialProtector(ENTROPY, _keyFilePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    /// <summary>
    /// Creates a random AES key.
    /// </summary>
    private static byte[] CreateKey() => RandomNumberGenerator.GetBytes(CredentialProtector.AESKEYSIZE);

    #region Constructor

    [TestMethod]
    public void Constructor_Defaults_UsesDefaultTagsAndKeyFile()
    {
        var protector = new CredentialProtector(ENTROPY);

        Assert.AreEqual(ENTROPY, protector.Entropy);
        Assert.AreEqual(CredentialProtector.DEFAULTOPENTAG, protector.OpenTag);
        Assert.AreEqual(CredentialProtector.DEFAULTCLOSETAG, protector.CloseTag);
        Assert.AreEqual(CredentialProtector.DEFAULTKEYFILENAME, Path.GetFileName(protector.KeyFilePath));
    }

    [TestMethod]
    [DataRow("", "[PWD]", "[/PWD]")]
    [DataRow(ENTROPY, null, "[/PWD]")]
    [DataRow(ENTROPY, "[PWD]", "")]
    [DataRow(ENTROPY, "[PWD]", "[PWD]")]
    [DataRow(ENTROPY, "PWD", "[/PWD]")]
    [DataRow(ENTROPY, "[PWD]", "PWD:")]
    public void Constructor_InvalidArguments_ThrowsArgumentException(string entropy, string openTag, string closeTag)
    {
        Assert.Throws<ArgumentException>(() => new CredentialProtector(entropy, null, openTag, closeTag));
    }

    [TestMethod]
    public void Constructor_EmptyKeyFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CredentialProtector(ENTROPY, " "));
    }

    [TestMethod]
    [DoNotParallelize]
    public void Constructor_NullEntropy_UsesDefaultEntropyFile()
    {
#if DEBUG
        var originalAppdata = Paths.AppdataDirectory;
        try
        {
            Paths.OverrideAppdataDirectory(_tempDirectory);
            var entropyFile = Path.Combine(Paths.DefaultConfigsDirectory, CredentialProtector.DEFAULTENTROPYFILENAME);

            var protector = new CredentialProtector(null, _keyFilePath);
            var other     = new CredentialProtector(null, _keyFilePath);

            Assert.IsTrue(File.Exists(entropyFile));
            Assert.AreEqual(CredentialProtector.CreateOrReadEntropyFile(entropyFile), protector.Entropy);
            Assert.AreEqual(protector.Entropy, other.Entropy);
            Assert.AreEqual(PLAINPASSWORD, other.Reveal(protector.Protect(PLAINPASSWORD)));
        }
        finally
        {
            Paths.OverrideAppdataDirectory(originalAppdata);
        }
#endif
    }

    #endregion

    #region Single value

    [TestMethod]
    public void Protect_PlainText_ReturnsPrefixedValueThatRevealsPlainText()
    {
        var protectedValue = _sut.Protect(PLAINPASSWORD);

        Assert.StartsWith(CredentialProtector.ProtectedPrefix, protectedValue);
        Assert.DoesNotContain(PLAINPASSWORD, protectedValue);
        Assert.IsTrue(CredentialProtector.IsProtected(protectedValue));
        Assert.AreEqual(PLAINPASSWORD, _sut.Reveal(protectedValue));
    }

    [TestMethod]
    public void Protect_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Protect(null));
    }

    [TestMethod]
    public void ProtectedPrefix_CurrentSystem_IsDpapiOnWindowsAndAesElsewhere()
    {
        Assert.AreEqual(OperatingSystem.IsWindows() ? CredentialProtector.DPAPIPREFIX : KEYPREFIX,
            CredentialProtector.ProtectedPrefix);
    }

    [TestMethod]
    public void TryReveal_PlainValue_ReturnsValueUnchanged()
    {
        var revealed = _sut.TryReveal(PLAINPASSWORD);

        Assert.IsTrue(revealed.Success);
        Assert.AreEqual(PLAINPASSWORD, revealed.Value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void TryReveal_NullOrEmpty_ReturnsEmpty(string value)
    {
        var revealed = _sut.TryReveal(value);

        Assert.IsTrue(revealed.Success);
        Assert.AreEqual(string.Empty, revealed.Value);
    }

    [TestMethod]
    [DataRow(CredentialProtector.DPAPIPREFIX + "non-base64!")]
    [DataRow(CredentialProtector.DPAPIPREFIX + "AAECAwQFBgcICQ==")]
    [DataRow(CredentialProtector.AESPREFIX + "AAECAwQFBgcICQ==")]
    [DataRow(CredentialProtector.AESCBCPREFIX + "AAECAwQFBgcICQ==")]
    public void TryReveal_InvalidProtectedValue_ReturnsFailureAndEmpty(string value)
    {
        CredentialProtector.GetOrCreateKey(_keyFilePath);

        var revealed = _sut.TryReveal(value);

        Assert.IsFalse(revealed.Success);
        Assert.AreEqual(string.Empty, revealed.Value);
        Assert.AreEqual(string.Empty, _sut.Reveal(value));
    }

    [TestMethod]
    public void TryReveal_DifferentEntropy_ReturnsFailure()
    {
        var protectedValue = _sut.Protect(PLAINPASSWORD);
        var other          = new CredentialProtector("Other.Entropy", _keyFilePath);

        Assert.IsFalse(other.TryReveal(protectedValue).Success);
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("password", true)]
    [DataRow(CredentialProtector.DPAPIPREFIX + "AAAA", false)]
    [DataRow(CredentialProtector.AESPREFIX + "AAAA", false)]
    [DataRow(CredentialProtector.AESCBCPREFIX + "AAAA", false)]
    public void RequiresProtection_Value_ReturnsExpected(string value, bool expected)
    {
        Assert.AreEqual(expected, CredentialProtector.RequiresProtection(value));
    }

    [TestMethod]
    public void TryReveal_AesValuesWithKeyFile_ReturnsPlainText()
    {
        var key = CredentialProtector.GetOrCreateKey(_keyFilePath);

        var gcm = _sut.TryReveal(CredentialProtector.ProtectWithKey(PLAINPASSWORD, key, ENTROPY));
        var cbc = _sut.TryReveal(CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, key, ENTROPY));

        Assert.IsTrue(gcm.Success);
        Assert.AreEqual(PLAINPASSWORD, gcm.Value);
        Assert.IsTrue(cbc.Success);
        Assert.AreEqual(PLAINPASSWORD, cbc.Value);
    }

    [TestMethod]
    public void TryReveal_AesValueWithoutKeyFile_ReturnsFailure()
    {
        var protectedValue = CredentialProtector.ProtectWithKey(PLAINPASSWORD, CreateKey(), ENTROPY);

        var revealed = _sut.TryReveal(protectedValue);

        Assert.IsFalse(revealed.Success);
        Assert.AreEqual(string.Empty, revealed.Value);
    }

    #endregion

    #region Tagged values

    [TestMethod]
    public void ProtectTagged_ConnectionString_EncryptsOnlyTaggedContent()
    {
        const string CONNECTIONSTRING = "Server=db;User=SA;Password=[PWD]" + PLAINPASSWORD + "[/PWD];Timeout=30";

        var protectedText = _sut.ProtectTagged(CONNECTIONSTRING);

        Assert.StartsWith("Server=db;User=SA;Password=[PWD]" + CredentialProtector.ProtectedPrefix, protectedText);
        Assert.EndsWith("[/PWD];Timeout=30", protectedText);
        Assert.DoesNotContain(PLAINPASSWORD, protectedText);
        Assert.IsFalse(_sut.RequiresTaggedProtection(protectedText));
        Assert.AreEqual("Server=db;User=SA;Password=" + PLAINPASSWORD + ";Timeout=30", _sut.RevealTagged(protectedText));
    }

    [TestMethod]
    public void ProtectTagged_MultipleAndAlreadyProtectedValues_EncryptsOnlyPlainValues()
    {
        var alreadyProtected = _sut.Protect("first");
        var text             = $"A=[PWD]{alreadyProtected}[/PWD];B=[PWD]second[/PWD];C=[PWD][/PWD]";

        var protectedText = _sut.ProtectTagged(text);

        Assert.StartsWith($"A=[PWD]{alreadyProtected}[/PWD];B=[PWD]{CredentialProtector.ProtectedPrefix}", protectedText);
        Assert.EndsWith(";C=[PWD][/PWD]", protectedText);
        Assert.DoesNotContain("second", protectedText);
        Assert.AreEqual("A=first;B=second;C=", _sut.RevealTagged(protectedText));
    }

    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("Server=db;Password=secret", "Server=db;Password=secret")]
    [DataRow("Password=[PWD]secret", "Password=[PWD]secret")]
    [DataRow("Password=[/PWD]secret[PWD]", "Password=[/PWD]secret[PWD]")]
    public void ProtectTagged_NoCompleteTags_ReturnsTextUnchanged(string text, string expected)
    {
        Assert.AreEqual(expected, _sut.ProtectTagged(text));
        Assert.IsFalse(_sut.RequiresTaggedProtection(text));
    }

    [TestMethod]
    [DataRow("Password=[PWD]secret[/PWD]", true)]
    [DataRow("Password=[PWD][/PWD]", false)]
    [DataRow("Password=[PWD]DPAPI:AAAA[/PWD]", false)]
    [DataRow("A=[PWD]AES:AAAA[/PWD];B=[PWD]plain[/PWD]", true)]
    public void RequiresTaggedProtection_Text_ReturnsExpected(string text, bool expected)
    {
        Assert.AreEqual(expected, _sut.RequiresTaggedProtection(text));
    }

    [TestMethod]
    public void ProtectTagged_CustomTags_UsesConfiguredTags()
    {
        var protector = new CredentialProtector(ENTROPY, _keyFilePath, "{{", "}}");

        var protectedText = protector.ProtectTagged("Password={{secret}};[PWD]other[/PWD]");

        Assert.StartsWith("Password={{" + CredentialProtector.ProtectedPrefix, protectedText);
        Assert.EndsWith("}};[PWD]other[/PWD]", protectedText);
        Assert.AreEqual("Password=secret;[PWD]other[/PWD]", protector.RevealTagged(protectedText));
    }

    [TestMethod]
    public void TryRevealTagged_PlainTaggedValue_RemovesTags()
    {
        var revealed = _sut.TryRevealTagged("User=SA;Password=[PWD]secret[/PWD];");

        Assert.IsTrue(revealed.Success);
        Assert.AreEqual("User=SA;Password=secret;", revealed.Value);
    }

    [TestMethod]
    public void TryRevealTagged_InvalidProtectedValue_ReturnsFailureWithEmptyCredential()
    {
        var text = $"A=[PWD]{_sut.Protect("first")}[/PWD];B=[PWD]{CredentialProtector.DPAPIPREFIX}AAECAwQFBgcICQ==[/PWD];";

        var revealed = _sut.TryRevealTagged(text);

        Assert.IsFalse(revealed.Success);
        Assert.AreEqual("A=first;B=;", revealed.Value);
    }

    #endregion

    #region AES

    [TestMethod]
    public void ProtectWithKey_PlainText_ReturnsBuildFormatValueThatRevealsPlainText()
    {
        var key = CreateKey();

        var protectedValue = CredentialProtector.ProtectWithKey(PLAINPASSWORD, key, ENTROPY);

        Assert.StartsWith(KEYPREFIX, protectedValue);
        Assert.DoesNotContain(PLAINPASSWORD, protectedValue);
        Assert.AreEqual(PLAINPASSWORD, CredentialProtector.RevealWithKey(protectedValue, key, ENTROPY));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("a")]
    [DataRow("0123456789ABCDEF")]
    [DataRow(PLAINPASSWORD)]
    public void TestProtectWithKeyCBC_PlainText_ReturnsCbcValueThatRevealsPlainText(string plainText)
    {
        var key = CreateKey();

        var protectedValue = CredentialProtector.TestProtectWithKeyCBC(plainText, key, ENTROPY);

        Assert.StartsWith(CredentialProtector.AESCBCPREFIX, protectedValue);
        Assert.IsTrue(CredentialProtector.IsProtected(protectedValue));
        Assert.AreEqual(plainText, CredentialProtector.RevealWithKey(protectedValue, key, ENTROPY));
    }

    [TestMethod]
    public void ProtectWithKey_SameText_ReturnsDifferentValues()
    {
        var key = CreateKey();

        // Random nonce/IV: the same password encrypted twice never produces the same value.
        Assert.AreNotEqual(CredentialProtector.ProtectWithKey(PLAINPASSWORD, key, ENTROPY),
            CredentialProtector.ProtectWithKey(PLAINPASSWORD, key, ENTROPY));
        Assert.AreNotEqual(CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, key, ENTROPY),
            CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, key, ENTROPY));
    }

    [TestMethod]
    public void RevealWithKey_GcmDifferentKey_ThrowsCryptographicException()
    {
        var protectedValue = CredentialProtector.ProtectWithKey(PLAINPASSWORD, CreateKey(), ENTROPY);

        Assert.Throws<CryptographicException>(() => CredentialProtector.RevealWithKey(protectedValue, CreateKey(), ENTROPY));
    }

    [TestMethod]
    public void RevealWithKey_CbcDifferentKeyOrEntropy_ThrowsCryptographicException()
    {
        var key            = CreateKey();
        var protectedValue = CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, key, ENTROPY);

        Assert.Throws<CryptographicException>(() => CredentialProtector.RevealWithKey(protectedValue, CreateKey(), ENTROPY));
        Assert.Throws<CryptographicException>(() => CredentialProtector.RevealWithKey(protectedValue, key, "Other.Entropy"));
    }

    [TestMethod]
    public void RevealWithKey_CbcAlteredData_ThrowsCryptographicException()
    {
        var key            = CreateKey();
        var protectedValue = CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, key, ENTROPY);
        var data           = Convert.FromBase64String(protectedValue.Substring(CredentialProtector.AESCBCPREFIX.Length));
        data[^1] ^= 0x01;
        var altered        = CredentialProtector.AESCBCPREFIX + Convert.ToBase64String(data);

        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.RevealWithKey(altered, key, ENTROPY));
    }

    [TestMethod]
    [DataRow(CredentialProtector.AESPREFIX + "AAECAwQFBgcICQ==")]
    [DataRow(CredentialProtector.AESCBCPREFIX + "AAECAwQFBgcICQ==")]
    [DataRow(CredentialProtector.DPAPIPREFIX + "AAECAwQFBgcICQ==")]
    [DataRow("plain")]
    public void RevealWithKey_TooShortOrNotAesValue_ThrowsCryptographicException(string value)
    {
        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.RevealWithKey(value, CreateKey(), ENTROPY));
    }

#if OSKHELPERS_NETSTANDARD

    [TestMethod]
    public void RevealWithKey_GcmValueWithNetStandardBuild_ThrowsCryptographicException()
    {
        // Value long enough for AES-GCM (nonce, tag and data): the netstandard2.0 build cannot decrypt it in any case.
        var value = CredentialProtector.AESPREFIX + Convert.ToBase64String(new byte[64]);

        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.RevealWithKey(value, CreateKey(), ENTROPY));
    }

#endif

    [TestMethod]
    public void TestProtectWithKeyCBC_InvalidKey_ThrowsCryptographicException()
    {
        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.TestProtectWithKeyCBC(PLAINPASSWORD, new byte[16], ENTROPY));
    }

    #endregion

    #region Key and entropy files

    [TestMethod]
    public void CreateOrReadEntropyFile_MissingFile_CreatesEntropyAndReusesIt()
    {
        var path = Path.Combine(_tempDirectory, "sub", CredentialProtector.DEFAULTENTROPYFILENAME);

        var created = CredentialProtector.CreateOrReadEntropyFile(path);
        var reused  = CredentialProtector.CreateOrReadEntropyFile(path);

        Assert.HasCount(CredentialProtector.DEFAULTENTROPYSIZE, Convert.FromBase64String(created));
        Assert.AreEqual(created, reused);
        Assert.AreEqual(Convert.ToBase64String(File.ReadAllBytes(path)), created);
    }

    [TestMethod]
    public void CreateOrReadEntropyFile_InvalidFile_ThrowsWithoutOverwriting()
    {
        var path = Path.Combine(_tempDirectory, CredentialProtector.DEFAULTENTROPYFILENAME);
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(path, "short");

        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.CreateOrReadEntropyFile(path));
        Assert.AreEqual("short", File.ReadAllText(path));
    }

    [TestMethod]
    public void CreateOrReadEntropyFile_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CredentialProtector.CreateOrReadEntropyFile(" "));
    }

    [TestMethod]
    public void GetOrCreateKey_MissingFile_CreatesKeyAndReusesIt()
    {
        var path = Path.Combine(_tempDirectory, "sub", CredentialProtector.DEFAULTKEYFILENAME);

        var created = CredentialProtector.GetOrCreateKey(path);
        var reused  = CredentialProtector.GetOrCreateKey(path);

        Assert.HasCount(CredentialProtector.AESKEYSIZE, created);
        CollectionAssert.AreEqual(created, reused);
        CollectionAssert.AreEqual(created, CredentialProtector.LoadKey(path));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Linux | OperatingSystems.OSX)]
    public void GetOrCreateKey_MissingFileNotWindows_CreatesFileReadableByOwnerOnly()
    {
        // Mode 600 is set on creation with net8.0 and through chmod of libc with netstandard2.0.
        CredentialProtector.GetOrCreateKey(_keyFilePath);

        // Platform guard required by the analyzer (CA1416), which does not consider OSCondition
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(_keyFilePath));
        }
    }

    [TestMethod]
    public void GetOrCreateKey_InvalidFile_ThrowsWithoutOverwriting()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(_keyFilePath, "short");

        Assert.ThrowsExactly<CryptographicException>(() => CredentialProtector.GetOrCreateKey(_keyFilePath));
        Assert.AreEqual("short", File.ReadAllText(_keyFilePath));
    }

    [TestMethod]
    public void LoadKey_MissingFile_ReturnsNull()
    {
        Assert.IsNull(CredentialProtector.LoadKey(_keyFilePath));
    }

    #endregion

    #endregion
}
