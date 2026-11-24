using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailKit.Security;
using Moq;
using OSKHelpers.Common;
using OSKHelpers.INIFile;
using OSKHelpers.Json;
using OSKHelpers.Logging;
using OSKHelpers.Mail.Interfaces;
using OSKHelpers.Security;

namespace OSKHelpers.Mail.Tests
{
    /// <summary>
    /// Tests for the setup and verification methods of <see cref="MailWorker"/>.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MailWorkerSetupTests
    {
        #region Constants

        /// <summary>
        /// Entropy of the credential protector used by the tests.
        /// </summary>
        private const string PROTECTORENTROPY = "OSKHelpers.Mail.Tests.Credentials";

        #endregion

        #region Support classes

        /// <summary>
        /// JSON settings with both SMTP and IMAP parameters, used by the tests of
        /// <see cref="MailWorker.Setup{T}(T, string, CredentialProtector, bool)"/>.
        /// </summary>
        public class MailSettings : JsonSettings<MailSettings>, ISettingsSmtp, ISettingsImap
        {
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public bool SmtpSSL { get; set; }
            public bool SmtpStartTls { get; set; }
            public string SmtpUsername { get; set; }
            public string SmtpPassword { get; set; }
            public string SmtpFrom { get; set; }
            public string ImapServer { get; set; }
            public int ImapPort { get; set; }
            public bool ImapSSL { get; set; }
            public bool ImapStartTls { get; set; }
            public string ImapUsername { get; set; }
            public string ImapPassword { get; set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;
        }

        /// <summary>
        /// JSON settings with the SMTP parameters only.
        /// </summary>
        public class SmtpOnlySettings : JsonSettings<SmtpOnlySettings>, ISettingsSmtp
        {
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public bool SmtpSSL { get; set; }
            public bool SmtpStartTls { get; set; }
            public string SmtpUsername { get; set; }
            public string SmtpPassword { get; set; }
            public string SmtpFrom { get; set; }

            public override void Init() { }

            public override bool CheckIsValid() => IsValid = true;
        }

        #endregion

        #region Members

        /// <summary>
        /// Temporary directory of the test (AES key file of the credential protector).
        /// </summary>
        private string _testDirectory;

        #endregion

        #region Setup

        /// <summary>
        /// Resets the static parameters before each test.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            MailWorker.SetupSmtp(null, 0, false, null, null, null);
            MailWorker.SetupImap(null, 0, false, null, null);
        }

        /// <summary>
        /// Deletes the temporary directory of the test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #endregion

        #region SetupSmtp

        /// <summary>
        /// Verifies that SetupSmtp correctly sets all parameters.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_ValidParameters_SetsAllProperties()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, true, "user", "pass", "from@example.com");

            Assert.AreEqual("smtp.example.com", MailWorker.SmtpServer);
            Assert.AreEqual(587, MailWorker.SmtpPort);
            Assert.IsTrue(MailWorker.SmtpSSL);
            Assert.AreEqual("user", MailWorker.SmtpUsername);
            Assert.AreEqual("pass", MailWorker.SmtpPassword);
            Assert.AreEqual("from@example.com", MailWorker.SmtpFrom);
            Assert.IsFalse(MailWorker.SmtpStartTls);
        }

        /// <summary>
        /// Verifies that SetupSmtp with startTls sets SmtpStartTls.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_StartTls_SetsSmtpStartTls()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, "user", "pass", "from@example.com", startTls: true);

            Assert.IsTrue(MailWorker.SmtpStartTls);
        }

        #endregion

        #region GetSecureSocketOptions

        /// <summary>
        /// Verifies the connection security option: SSL takes precedence over STARTTLS, without both the connection is not encrypted.
        /// </summary>
        [TestMethod]
        [DataRow(true, false, SecureSocketOptions.SslOnConnect)]
        [DataRow(true, true, SecureSocketOptions.SslOnConnect)]
        [DataRow(false, true, SecureSocketOptions.StartTls)]
        [DataRow(false, false, SecureSocketOptions.None)]
        public void GetSecureSocketOptions_VariousFlags_ReturnsExpected(bool ssl, bool startTls, SecureSocketOptions expected)
        {
            Assert.AreEqual(expected, MailWorker.GetSecureSocketOptions(ssl, startTls));
        }

        #endregion

        #region SetupImap

        /// <summary>
        /// Verifies that SetupImap correctly sets all parameters.
        /// </summary>
        [TestMethod]
        public void SetupImap_ValidParameters_SetsAllProperties()
        {
            MailWorker.SetupImap("imap.example.com", 993, true, "user", "pass");

            Assert.AreEqual("imap.example.com", MailWorker.ImapServer);
            Assert.AreEqual(993, MailWorker.ImapPort);
            Assert.IsTrue(MailWorker.ImapSSL);
            Assert.AreEqual("user", MailWorker.ImapUsername);
            Assert.AreEqual("pass", MailWorker.ImapPassword);
        }

        #endregion

        #region SmtpAuthEnabled / SmtpAuthConfigError

        /// <summary>
        /// Verifies SmtpAuthEnabled when both username and password are present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthEnabled_BothCredentials_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", "pass");

            Assert.IsTrue(MailWorker.SmtpAuthEnabled);
        }

        /// <summary>
        /// Verifies SmtpAuthEnabled when both credentials are absent.
        /// </summary>
        [TestMethod]
        public void SmtpAuthEnabled_NoCredentials_ReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, null);

            Assert.IsFalse(MailWorker.SmtpAuthEnabled);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when only the username is present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigError_OnlyUsername_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", null);

            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when only the password is present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigError_OnlyPassword_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, "pass");

            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when both credentials are present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigError_BothCredentials_ReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", "pass");

            Assert.IsFalse(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when both credentials are absent.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigError_NoCredentials_ReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, null);

            Assert.IsFalse(MailWorker.SmtpAuthConfigError);
        }

        #endregion

        #region CheckSmtpParameters

        /// <summary>
        /// Verifies CheckSmtpParameters with a valid configuration.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParameters_ValidConfig_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, true, "user", "pass", "from@test.com");

            Assert.IsTrue(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without a server.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParameters_NoServer_ReturnsFalse()
        {
            MailWorker.SetupSmtp(null, 587, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without a port.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParameters_ZeroPort_ReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 0, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters with an authentication error.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParameters_AuthError_ReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, true, "user", null);

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without authentication (both null).
        /// </summary>
        [TestMethod]
        public void CheckSmtpParameters_NoAuth_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, null);

            Assert.IsTrue(MailWorker.CheckSmtpParameters(false));
        }

        #endregion

        #region CheckImapParameters

        /// <summary>
        /// Verifies CheckImapParameters with a valid configuration.
        /// </summary>
        [TestMethod]
        public void CheckImapParameters_ValidConfig_ReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 25, false, null, null);
            MailWorker.SetupImap("imap.test.com", 993, true, "user", "pass");

            Assert.IsTrue(MailWorker.CheckImapParameters(false));
        }

        /// <summary>
        /// Verifies CheckImapParameters without a server.
        /// </summary>
        [TestMethod]
        public void CheckImapParameters_NoServer_ReturnsFalse()
        {
            MailWorker.SetupImap(null, 993, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckImapParameters(false));
        }

        /// <summary>
        /// Verifies ImapAuthConfigError when the password is missing.
        /// </summary>
        [TestMethod]
        public void ImapAuthConfigError_MissingPassword_ReturnsTrue()
        {
            MailWorker.SetupImap("imap.test.com", 993, true, "user", null);

            Assert.IsTrue(MailWorker.ImapAuthConfigError);
        }

        /// <summary>
        /// Verifies ImapAuthConfigError when the username is missing.
        /// </summary>
        [TestMethod]
        public void ImapAuthConfigError_MissingUsername_ReturnsTrue()
        {
            MailWorker.SetupImap("imap.test.com", 993, true, null, "pass");

            Assert.IsTrue(MailWorker.ImapAuthConfigError);
        }

        #endregion

        #region ParseString (additional cases)

        /// <summary>
        /// Verifies ParseString with condition true (content visible).
        /// </summary>
        [TestMethod]
        public void ParseString_ConditionTrue_KeepsContent()
        {
            var conditions  = new List<(string Name, bool Check)> { ("SHOW", true) };
            var result      = MailWorker.ParseString("A<<SHOW>>B<</SHOW>>C", conditions: conditions);

            Assert.AreEqual("ABC", result);
        }

        /// <summary>
        /// Verifies ParseString with condition false (content removed).
        /// </summary>
        [TestMethod]
        public void ParseString_ConditionFalse_RemovesContent()
        {
            var conditions  = new List<(string Name, bool Check)> { ("HIDE", false) };
            var result      = MailWorker.ParseString("A<<HIDE>>B<</HIDE>>C", conditions: conditions);

            Assert.AreEqual("AC", result);
        }

        #endregion

        #region ParseFile (additional cases)

        /// <summary>
        /// Verifies ParseFile with customTags.
        /// </summary>
        [TestMethod]
        public void ParseFile_CustomTags_ExtractsTagValues()
        {
            var lines = new List<string>
            {
                "@Subject: My Subject",
                "@AltSubject: Alt Value",
                "Body text here"
            };
            var customTags              = new List<string> { "AltSubject" };
            List<(string, bool)> noConds = null;
            var result                   = MailWorker.ParseFile(lines, conditions: noConds, customTags: customTags);

            Assert.IsTrue(result.Parsed);
            Assert.AreEqual("My Subject", result.Subject);
            Assert.IsNotNull(result.TagValues);
            Assert.IsTrue(result.TagValues.ContainsKey("AltSubject"));
            Assert.AreEqual("Alt Value", result.TagValues["AltSubject"]);
        }

        /// <summary>
        /// Verifies that ParseFile without a subject returns Parsed = false.
        /// </summary>
        [TestMethod]
        public void ParseFile_NoSubject_ReturnsNotParsed()
        {
            var lines                    = new List<string> { "Only body text" };
            List<(string, bool)> noConds = null;
            var result                   = MailWorker.ParseFile(lines, conditions: noConds);

            Assert.IsFalse(result.Parsed);
        }

        #endregion

        #region SetupSmtp / SetupImap (IniFileHelper)

        /// <summary>
        /// Verifies that SetupSmtp reads the SMTP keys from the INI file, SmtpStartTls included, and checks the parameters.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_IniFile_ReadsKeys()
        {
            var iniFile = CreateIniFile(
                "SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpSSL = False", "SmtpStartTls = True",
                "SmtpUsername = user", "SmtpPassword = pass", "SmtpFrom = from@example.com");

            var result = MailWorker.SetupSmtp(iniFile);

            Assert.IsTrue(result);
            Assert.AreEqual("smtp.example.com", MailWorker.SmtpServer);
            Assert.AreEqual(587, MailWorker.SmtpPort);
            Assert.IsFalse(MailWorker.SmtpSSL);
            Assert.IsTrue(MailWorker.SmtpStartTls);
            Assert.AreEqual("user", MailWorker.SmtpUsername);
            Assert.AreEqual("pass", MailWorker.SmtpPassword);
            Assert.AreEqual("from@example.com", MailWorker.SmtpFrom);
        }

        /// <summary>
        /// Verifies that the optional SmtpStartTls key is false when missing.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_IniFileWithoutStartTls_SetsStartTlsFalse()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, null, null, null, true);
            var iniFile = CreateIniFile("SmtpServer = smtp.example.com", "SmtpPort = 25");

            MailWorker.SetupSmtp(iniFile);

            Assert.IsFalse(MailWorker.SmtpStartTls);
        }

        /// <summary>
        /// Verifies that SetupImap reads the IMAP keys from the INI file and checks the parameters.
        /// </summary>
        [TestMethod]
        public void SetupImap_IniFile_ReadsKeys()
        {
            var iniFile = CreateIniFile(
                "ImapServer = imap.example.com", "ImapPort = 993", "ImapSSL = True", "ImapUsername = user", "ImapPassword = pass");

            var result = MailWorker.SetupImap(iniFile);

            Assert.IsTrue(result);
            Assert.AreEqual("imap.example.com", MailWorker.ImapServer);
            Assert.AreEqual(993, MailWorker.ImapPort);
            Assert.IsTrue(MailWorker.ImapSSL);
            Assert.AreEqual("user", MailWorker.ImapUsername);
            Assert.AreEqual("pass", MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that a null INI file throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void SetupSmtpAndSetupImap_NullIniFile_ThrowArgumentNullException()
        {
            Assert.AreEqual("iniFile", Assert.Throws<ArgumentNullException>(() => MailWorker.SetupSmtp((IniFileHelper)null)).ParamName);
            Assert.AreEqual("iniFile", Assert.Throws<ArgumentNullException>(() => MailWorker.SetupImap((IniFileHelper)null)).ParamName);
        }

        #endregion

        #region SetupSmtp / SetupImap (ISettings)

        /// <summary>
        /// Verifies that SetupSmtp copies the values of ISettingsSmtp, SmtpStartTls included.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_Settings_CopiesValues()
        {
            var settings = new Mock<ISettingsSmtp>();
            settings.SetupGet(s => s.SmtpServer).Returns("smtp.example.com");
            settings.SetupGet(s => s.SmtpPort).Returns(587);
            settings.SetupGet(s => s.SmtpSSL).Returns(false);
            settings.SetupGet(s => s.SmtpStartTls).Returns(true);
            settings.SetupGet(s => s.SmtpUsername).Returns("user");
            settings.SetupGet(s => s.SmtpPassword).Returns("pass");
            settings.SetupGet(s => s.SmtpFrom).Returns("from@example.com");

            var result = MailWorker.SetupSmtp(settings.Object);

            Assert.IsTrue(result);
            Assert.AreEqual("smtp.example.com", MailWorker.SmtpServer);
            Assert.AreEqual(587, MailWorker.SmtpPort);
            Assert.IsFalse(MailWorker.SmtpSSL);
            Assert.IsTrue(MailWorker.SmtpStartTls);
            Assert.AreEqual("user", MailWorker.SmtpUsername);
            Assert.AreEqual("pass", MailWorker.SmtpPassword);
            Assert.AreEqual("from@example.com", MailWorker.SmtpFrom);
        }

        /// <summary>
        /// Verifies that SetupImap copies the values of ISettingsImap and returns the result of the check.
        /// </summary>
        [TestMethod]
        public void SetupImap_Settings_CopiesValues()
        {
            var settings = new Mock<ISettingsImap>();
            settings.SetupGet(s => s.ImapServer).Returns("imap.example.com");
            settings.SetupGet(s => s.ImapPort).Returns(993);
            settings.SetupGet(s => s.ImapSSL).Returns(true);
            settings.SetupGet(s => s.ImapUsername).Returns("user");
            settings.SetupGet(s => s.ImapPassword).Returns(null as string);

            var result = MailWorker.SetupImap(settings.Object);

            Assert.IsFalse(result);
            Assert.AreEqual("imap.example.com", MailWorker.ImapServer);
            Assert.AreEqual(993, MailWorker.ImapPort);
            Assert.IsTrue(MailWorker.ImapSSL);
            Assert.AreEqual("user", MailWorker.ImapUsername);
            Assert.IsNull(MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that null settings throw ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void SetupSmtpAndSetupImap_NullSettings_ThrowArgumentNullException()
        {
            Assert.AreEqual("settings", Assert.Throws<ArgumentNullException>(() => MailWorker.SetupSmtp((ISettingsSmtp)null)).ParamName);
            Assert.AreEqual("settings", Assert.Throws<ArgumentNullException>(() => MailWorker.SetupImap((ISettingsImap)null)).ParamName);
        }

        #endregion

        #region Encrypted passwords (CredentialProtector)

        /// <summary>
        /// Verifies that SetupSmtp decrypts an encrypted INI password with the protector.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_IniFileEncryptedPassword_DecryptsWithProtector()
        {
            var protector   = CreateProtector(PROTECTORENTROPY);
            var iniFile     = CreateIniFile(
                "SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpUsername = user",
                $"SmtpPassword = {protector.Protect("smtpSecret")}", "SmtpFrom = from@example.com");

            var result = MailWorker.SetupSmtp(iniFile, protector);

            Assert.IsTrue(result);
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.IsTrue(MailWorker.SmtpAuthEnabled);
        }

        /// <summary>
        /// Verifies that a plain-text INI password is used as it is, also when a protector is passed.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_IniFilePlainPasswordWithProtector_UsesPlainPassword()
        {
            var iniFile = CreateIniFile("SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpUsername = user", "SmtpPassword = pass");

            var result = MailWorker.SetupSmtp(iniFile, CreateProtector(PROTECTORENTROPY));

            Assert.IsTrue(result);
            Assert.AreEqual("pass", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that an encrypted password without a protector, which the default protector cannot decrypt (different
        /// entropy), is ignored and the check fails.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_IniFileEncryptedPasswordWithoutProtector_IgnoresPasswordAndReturnsFalse()
        {
            var encrypted   = CreateProtector(PROTECTORENTROPY).Protect("smtpSecret");
            var iniFile     = CreateIniFile(
                "SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpUsername = user", $"SmtpPassword = {encrypted}");

            var result = MailWorker.SetupSmtp(iniFile);

            Assert.IsFalse(result);
            Assert.AreEqual(string.Empty, MailWorker.SmtpPassword);
            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies that a password that the protector cannot decrypt (different entropy) is ignored and the check fails.
        /// </summary>
        [TestMethod]
        public void SetupImap_IniFilePasswordNotDecryptable_IgnoresPasswordAndReturnsFalse()
        {
            var encrypted   = CreateProtector(PROTECTORENTROPY).Protect("imapSecret");
            var iniFile     = CreateIniFile(
                "ImapServer = imap.example.com", "ImapPort = 993", "ImapUsername = user", $"ImapPassword = {encrypted}");

            var result = MailWorker.SetupImap(iniFile, CreateProtector("OSKHelpers.Mail.Tests.OtherEntropy"));

            Assert.IsFalse(result);
            Assert.AreEqual(string.Empty, MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that SetupImap decrypts an encrypted INI password with the protector.
        /// </summary>
        [TestMethod]
        public void SetupImap_IniFileEncryptedPassword_DecryptsWithProtector()
        {
            var protector   = CreateProtector(PROTECTORENTROPY);
            var iniFile     = CreateIniFile(
                "ImapServer = imap.example.com", "ImapPort = 993", "ImapUsername = user",
                $"ImapPassword = {protector.Protect("imapSecret")}");

            var result = MailWorker.SetupImap(iniFile, protector);

            Assert.IsTrue(result);
            Assert.AreEqual("imapSecret", MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that SetupSmtp and SetupImap decrypt the encrypted passwords of the settings classes.
        /// </summary>
        [TestMethod]
        public void SetupSmtpAndSetupImap_SettingsEncryptedPasswords_DecryptWithProtector()
        {
            var protector       = CreateProtector(PROTECTORENTROPY);
            var smtpSettings    = new Mock<ISettingsSmtp>();
            smtpSettings.SetupGet(s => s.SmtpServer).Returns("smtp.example.com");
            smtpSettings.SetupGet(s => s.SmtpPort).Returns(587);
            smtpSettings.SetupGet(s => s.SmtpUsername).Returns("user");
            smtpSettings.SetupGet(s => s.SmtpPassword).Returns(protector.Protect("smtpSecret"));
            var imapSettings    = new Mock<ISettingsImap>();
            imapSettings.SetupGet(s => s.ImapServer).Returns("imap.example.com");
            imapSettings.SetupGet(s => s.ImapPort).Returns(993);
            imapSettings.SetupGet(s => s.ImapUsername).Returns("user");
            imapSettings.SetupGet(s => s.ImapPassword).Returns(protector.Protect("imapSecret"));

            var smtpResult = MailWorker.SetupSmtp(smtpSettings.Object, protector: protector);
            var imapResult = MailWorker.SetupImap(imapSettings.Object, protector);

            Assert.IsTrue(smtpResult);
            Assert.IsTrue(imapResult);
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.AreEqual("imapSecret", MailWorker.ImapPassword);
        }

        #endregion

        #region Obfuscated passwords (PasswordObfuscator)

        /// <summary>
        /// Verifies that the passwords are kept obfuscated: after a new seed of PasswordObfuscator the stored values no longer
        /// decode to the original passwords, which would not happen if they were stored in plain text.
        /// </summary>
        [TestMethod]
        public void Passwords_ReSeed_StoredValuesAreObfuscated()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, "user", "smtpSecret", "from@example.com");
            MailWorker.SetupImap("imap.example.com", 993, true, "user", "imapSecret");
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.AreEqual("imapSecret", MailWorker.ImapPassword);

            PasswordObfuscator.ReSeed();

            Assert.AreNotEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.AreNotEqual("imapSecret", MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that empty or white-space passwords are kept as they are and do not enable the authentication.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void SmtpPassword_EmptyValue_KeptAsIs(string password)
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, "user", password);

            Assert.AreEqual(password, MailWorker.SmtpPassword);
            Assert.IsFalse(MailWorker.SmtpAuthEnabled);
            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        #endregion

        #region Dump

        /// <summary>
        /// Verifies that the dump methods print the parameters masking the passwords.
        /// </summary>
        [TestMethod]
        public void DumpParameters_Console_MasksPasswords()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, "smtpUser", "smtpSecret", "from@example.com");
            MailWorker.SetupImap("imap.example.com", 993, true, "imapUser", "imapSecret");
            var originalOut = Console.Out;
            var writer      = new StringWriter();
            try
            {
                Console.SetOut(writer);
                MailWorker.DumpParameters();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = writer.ToString();
            Assert.Contains("smtp.example.com", output);
            Assert.Contains("imap.example.com", output);
            Assert.Contains("****", output);
            Assert.DoesNotContain("smtpSecret", output);
            Assert.DoesNotContain("imapSecret", output);
        }

        /// <summary>
        /// Verifies that, with log enabled, the parameters are written to the log masking the passwords.
        /// </summary>
        [TestMethod]
        public void DumpParameters_Log_WritesParametersWithoutPasswords()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, false, "smtpUser", "smtpSecret", "from@example.com");
            MailWorker.SetupImap("imap.example.com", 993, true, "imapUser", "imapSecret");
            var originalLogPath = SimpleLog.LogPath;
            var tempLogPath     = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                SimpleLog.LogPath = tempLogPath;

                MailWorker.DumpStmpParameters(false, true);
                MailWorker.DumpImapParameters(false, true);

                var content = File.ReadAllText(SimpleLog.LogFile);
                Assert.Contains("smtp.example.com", content);
                Assert.Contains("imap.example.com", content);
                Assert.DoesNotContain("smtpSecret", content);
                Assert.DoesNotContain("imapSecret", content);
            }
            finally
            {
                SimpleLog.LogPath = originalLogPath;
                if (Directory.Exists(tempLogPath))
                {
                    Directory.Delete(tempLogPath, true);
                }
            }
        }

        #endregion

        #region Setup (JsonSettings)

        /// <summary>
        /// Verifies that the plain-text passwords are encrypted in the settings and saved to the file without a backup copy,
        /// while MailWorker returns them in plain text.
        /// </summary>
        [TestMethod]
        public void Setup_PlainPasswords_EncryptsAndSavesWithoutBackup()
        {
            var protector   = CreateProtector(PROTECTORENTROPY);
            var baseName    = $"MailSettings_{Guid.NewGuid():N}";
            var fileName    = Path.Combine(_testDirectory, $"{baseName}.json");
            var settings    = CreateMailSettings("smtpSecret", "imapSecret");
            settings.Save(fileName);

            var result = MailWorker.Setup(settings, fileName, protector);

            var backups = Paths.CheckDefaultBackupDirectory()
                ? Directory.GetFiles(Paths.DefaultBackupDirectory, $"{baseName}_*").ToList()
                : new List<string>();
            try
            {
                Assert.IsTrue(result.SmtpIsValid);
                Assert.IsTrue(result.ImapIsValid);
                Assert.IsFalse(result.PlainTextPasswords);
                Assert.IsTrue(CredentialProtector.IsProtected(settings.SmtpPassword));
                Assert.IsTrue(CredentialProtector.IsProtected(settings.ImapPassword));
                var content = File.ReadAllText(fileName);
                Assert.DoesNotContain("smtpSecret", content);
                Assert.DoesNotContain("imapSecret", content);
                Assert.IsEmpty(backups);
                Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
                Assert.AreEqual("imapSecret", MailWorker.ImapPassword);
            }
            finally
            {
                backups.ForEach(File.Delete);
            }
        }

        /// <summary>
        /// Verifies that with saveProtectedPasswords false the passwords are encrypted only in the settings, the file keeps
        /// them in plain text and the result reports it.
        /// </summary>
        [TestMethod]
        public void Setup_SaveDisabled_EncryptsOnlyInSettings()
        {
            var fileName    = Path.Combine(_testDirectory, "MailSettings.json");
            var settings    = CreateMailSettings("smtpSecret", "imapSecret");
            settings.Save(fileName);

            var result = MailWorker.Setup(settings, fileName, CreateProtector(PROTECTORENTROPY), saveProtectedPasswords: false);

            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsTrue(result.ImapIsValid);
            Assert.IsTrue(result.PlainTextPasswords);
            Assert.IsTrue(CredentialProtector.IsProtected(settings.SmtpPassword));
            Assert.Contains("smtpSecret", File.ReadAllText(fileName));
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that passwords already encrypted are decrypted and the file is not saved again.
        /// </summary>
        [TestMethod]
        public void Setup_EncryptedPasswords_DoesNotSaveFile()
        {
            var protector   = CreateProtector(PROTECTORENTROPY);
            var fileName    = Path.Combine(_testDirectory, "MailSettings.json");
            var settings    = CreateMailSettings(protector.Protect("smtpSecret"), protector.Protect("imapSecret"));

            var result = MailWorker.Setup(settings, fileName, protector);

            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsTrue(result.ImapIsValid);
            Assert.IsFalse(result.PlainTextPasswords);
            Assert.IsFalse(File.Exists(fileName));
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.AreEqual("imapSecret", MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that a failed save is reported as plain-text passwords, while the parameters are set anyway.
        /// </summary>
        [TestMethod]
        public void Setup_SaveFails_ReportsPlainTextPasswords()
        {
            var fileName    = Path.Combine(_testDirectory, "Missing", "MailSettings.json");
            var settings    = CreateMailSettings("smtpSecret", "imapSecret");

            var result = MailWorker.Setup(settings, fileName, CreateProtector(PROTECTORENTROPY));

            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsTrue(result.ImapIsValid);
            Assert.IsTrue(result.PlainTextPasswords);
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that settings without the IMAP parameters set only SMTP and return ImapIsValid false.
        /// </summary>
        [TestMethod]
        public void Setup_SmtpOnlySettings_ImapIsValidFalse()
        {
            var settings = new SmtpOnlySettings
            {
                SmtpServer      = "smtp.example.com",
                SmtpPort        = 587,
                SmtpStartTls    = true,
                SmtpUsername    = "user",
                SmtpPassword    = "smtpSecret",
                SmtpFrom        = "from@example.com"
            };

            var result = MailWorker.Setup(settings, Path.Combine(_testDirectory, "SmtpSettings.json"),
                CreateProtector(PROTECTORENTROPY));

            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsFalse(result.ImapIsValid);
            Assert.IsFalse(result.PlainTextPasswords);
            Assert.IsTrue(MailWorker.SmtpStartTls);
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that without a protector the passwords are encrypted with the default entropy, read from the entropy file
        /// in the configuration directory, and that the other setup methods decrypt them without a protector.
        /// </summary>
        [TestMethod]
        public void Setup_NullProtector_UsesDefaultEntropyFile()
        {
            var fileName    = Path.Combine(_testDirectory, "MailSettings.json");
            var settings    = CreateMailSettings("smtpSecret", "imapSecret");
            settings.Save(fileName);

            var result = MailWorker.Setup(settings, fileName);

            var entropyFile = Path.Combine(Paths.DefaultConfigsDirectory, MailWorker.DEFAULTENTROPYFILENAME);
            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsFalse(result.PlainTextPasswords);
            Assert.IsTrue(File.Exists(entropyFile));
            Assert.AreEqual(MailWorker.DEFAULTENTROPYSIZE, new FileInfo(entropyFile).Length);
            Assert.IsTrue(CredentialProtector.IsProtected(settings.SmtpPassword));
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);

            // From an INI file, without a protector, the password encrypted with the default entropy is decrypted
            var iniFile = CreateIniFile(
                "SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpUsername = user", $"SmtpPassword = {settings.SmtpPassword}");
            Assert.IsTrue(MailWorker.SetupSmtp(iniFile));
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that after a new seed of PasswordObfuscator the default entropy, kept obfuscated in memory, is read again
        /// from the file instead of being used with a wrong value.
        /// </summary>
        [TestMethod]
        public void Setup_NullProtectorAfterReSeed_ReadsEntropyAgain()
        {
            var settings = CreateMailSettings("smtpSecret", "imapSecret");
            MailWorker.Setup(settings, Path.Combine(_testDirectory, "MailSettings.json"));
            var encrypted = settings.SmtpPassword;

            PasswordObfuscator.ReSeed();
            var result = MailWorker.Setup(CreateMailSettings(encrypted, settings.ImapPassword), saveProtectedPasswords: false);

            Assert.IsTrue(result.SmtpIsValid);
            Assert.IsTrue(result.ImapIsValid);
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
            Assert.AreEqual("imapSecret", MailWorker.ImapPassword);
        }

        /// <summary>
        /// Verifies that null settings throw ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void Setup_NullSettings_ThrowsArgumentNullException()
        {
            Assert.AreEqual("settings", Assert.Throws<ArgumentNullException>(() => MailWorker.Setup<MailSettings>(null)).ParamName);
        }

        #endregion

        #region CheckIniPasswordsProtection

        /// <summary>
        /// Verifies that plain-text passwords are reported as requiring protection.
        /// </summary>
        [TestMethod]
        public void CheckIniPasswordsProtection_PlainPasswords_ReturnsTrue()
        {
            var iniFile = CreateIniFile("SmtpPassword = smtpSecret", "ImapPassword = imapSecret");

            var result = MailWorker.CheckIniPasswordsProtection(iniFile);

            Assert.IsTrue(result.SmtpRequiresProtection);
            Assert.IsTrue(result.ImapRequiresProtection);
        }

        /// <summary>
        /// Verifies that encrypted passwords, and missing or empty ones, do not require protection.
        /// </summary>
        [TestMethod]
        public void CheckIniPasswordsProtection_EncryptedOrMissingPasswords_ReturnsFalse()
        {
            var encrypted   = CreateProtector(PROTECTORENTROPY).Protect("smtpSecret");
            var iniFile     = CreateIniFile($"SmtpPassword = {encrypted}", "ImapUsername = user");

            var result = MailWorker.CheckIniPasswordsProtection(iniFile);

            Assert.IsFalse(result.SmtpRequiresProtection);
            Assert.IsFalse(result.ImapRequiresProtection);
        }

        /// <summary>
        /// Verifies that a null INI file throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void CheckIniPasswordsProtection_NullIniFile_ThrowsArgumentNullException()
        {
            Assert.AreEqual("iniFile", Assert.Throws<ArgumentNullException>(() => MailWorker.CheckIniPasswordsProtection(null)).ParamName);
        }

        #endregion

        #region ProtectPassword

        /// <summary>
        /// Verifies that without a protector the password is encrypted with the default entropy and that SetupSmtp, without a
        /// protector, decrypts it.
        /// </summary>
        [TestMethod]
        public void ProtectPassword_NullProtector_EncryptsWithDefaultEntropy()
        {
            var (success, encrypted) = MailWorker.ProtectPassword("smtpSecret");

            Assert.IsTrue(success);
            Assert.IsTrue(CredentialProtector.IsProtected(encrypted));
            var iniFile = CreateIniFile(
                "SmtpServer = smtp.example.com", "SmtpPort = 587", "SmtpUsername = user", $"SmtpPassword = {encrypted}");
            Assert.IsTrue(MailWorker.SetupSmtp(iniFile));
            Assert.AreEqual("smtpSecret", MailWorker.SmtpPassword);
        }

        /// <summary>
        /// Verifies that the given protector is used for the encryption.
        /// </summary>
        [TestMethod]
        public void ProtectPassword_WithProtector_EncryptsWithProtector()
        {
            var protector = CreateProtector(PROTECTORENTROPY);

            var (success, encrypted) = MailWorker.ProtectPassword("imapSecret", protector);

            Assert.IsTrue(success);
            Assert.AreEqual("imapSecret", protector.Reveal(encrypted));
        }

        /// <summary>
        /// Verifies that an encrypted or empty password is returned unchanged.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("DPAPI:AAAA")]
        public void ProtectPassword_EncryptedOrEmptyPassword_ReturnsUnchanged(string password)
        {
            var (success, value) = MailWorker.ProtectPassword(password);

            Assert.IsTrue(success);
            Assert.AreEqual(password, value);
        }

        #endregion

        #region Support methods

        /// <summary>
        /// Creates valid SMTP and IMAP settings with the given passwords.
        /// </summary>
        private static MailSettings CreateMailSettings(string smtpPassword, string imapPassword)
            => new MailSettings
            {
                SmtpServer      = "smtp.example.com",
                SmtpPort        = 587,
                SmtpUsername    = "user",
                SmtpPassword    = smtpPassword,
                SmtpFrom        = "from@example.com",
                ImapServer      = "imap.example.com",
                ImapPort        = 993,
                ImapSSL         = true,
                ImapUsername    = "user",
                ImapPassword    = imapPassword
            };

        /// <summary>
        /// Creates an INI file helper from the given lines.
        /// </summary>
        private static IniFileHelper CreateIniFile(params string[] lines)
        {
            var iniFile = new IniFileHelper();
            Assert.IsTrue(iniFile.Parse(new List<string>(lines)));

            return iniFile;
        }

        /// <summary>
        /// Creates a credential protector with the given entropy; the AES key file (used only on systems other than Windows) is
        /// in the test directory, so the default database directory is not touched.
        /// </summary>
        private CredentialProtector CreateProtector(string entropy)
            => new CredentialProtector(entropy, Path.Combine(_testDirectory, CredentialProtector.DEFAULTKEYFILENAME));

        #endregion
    }
}
