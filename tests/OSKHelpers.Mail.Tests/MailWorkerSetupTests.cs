using System;
using System.Collections.Generic;
using System.IO;
using MailKit.Security;
using Moq;
using OSKHelpers.INIFile;
using OSKHelpers.Logging;
using OSKHelpers.Mail.Interfaces;

namespace OSKHelpers.Mail.Tests
{
    /// <summary>
    /// Tests for the setup and verification methods of <see cref="MailWorker"/>.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MailWorkerSetupTests
    {
        #region Setup

        /// <summary>
        /// Resets the static parameters before each test.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            MailWorker.SetupSmtp(null, 0, false, null, null, null);
            MailWorker.SetupImap(null, 0, false, null, null);
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
        /// Verifies that SetupSmtp copies the values of ISettingsSmtp and the startTls parameter.
        /// </summary>
        [TestMethod]
        public void SetupSmtp_Settings_CopiesValues()
        {
            var settings = new Mock<ISettingsSmtp>();
            settings.SetupGet(s => s.SmtpServer).Returns("smtp.example.com");
            settings.SetupGet(s => s.SmtpPort).Returns(587);
            settings.SetupGet(s => s.SmtpSSL).Returns(false);
            settings.SetupGet(s => s.SmtpUsername).Returns("user");
            settings.SetupGet(s => s.SmtpPassword).Returns("pass");
            settings.SetupGet(s => s.SmtpFrom).Returns("from@example.com");

            var result = MailWorker.SetupSmtp(settings.Object, true);

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

        #region Support methods

        /// <summary>
        /// Creates an INI file helper from the given lines.
        /// </summary>
        private static IniFileHelper CreateIniFile(params string[] lines)
        {
            var iniFile = new IniFileHelper();
            Assert.IsTrue(iniFile.Parse(new List<string>(lines)));

            return iniFile;
        }

        #endregion
    }
}
