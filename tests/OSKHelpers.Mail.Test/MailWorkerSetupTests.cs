using System.Collections.Generic;

namespace OSKHelpers.Mail.Test
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
        public void SetupSmtpValidParametersSetsAllProperties()
        {
            MailWorker.SetupSmtp("smtp.example.com", 587, true, "user", "pass", "from@example.com");

            Assert.AreEqual("smtp.example.com", MailWorker.SmtpServer);
            Assert.AreEqual(587, MailWorker.SmtpPort);
            Assert.IsTrue(MailWorker.SmtpSSL);
            Assert.AreEqual("user", MailWorker.SmtpUsername);
            Assert.AreEqual("pass", MailWorker.SmtpPassword);
            Assert.AreEqual("from@example.com", MailWorker.SmtpFrom);
        }

        #endregion

        #region SetupImap

        /// <summary>
        /// Verifies that SetupImap correctly sets all parameters.
        /// </summary>
        [TestMethod]
        public void SetupImapValidParametersSetsAllProperties()
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
        public void SmtpAuthEnabledBothCredentialsReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", "pass");

            Assert.IsTrue(MailWorker.SmtpAuthEnabled);
        }

        /// <summary>
        /// Verifies SmtpAuthEnabled when both credentials are absent.
        /// </summary>
        [TestMethod]
        public void SmtpAuthEnabledNoCredentialsReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, null);

            Assert.IsFalse(MailWorker.SmtpAuthEnabled);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when only the username is present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigErrorOnlyUsernameReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", null);

            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when only the password is present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigErrorOnlyPasswordReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, null, "pass");

            Assert.IsTrue(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when both credentials are present.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigErrorBothCredentialsReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, false, "user", "pass");

            Assert.IsFalse(MailWorker.SmtpAuthConfigError);
        }

        /// <summary>
        /// Verifies SmtpAuthConfigError when both credentials are absent.
        /// </summary>
        [TestMethod]
        public void SmtpAuthConfigErrorNoCredentialsReturnsFalse()
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
        public void CheckSmtpParametersValidConfigReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, true, "user", "pass", "from@test.com");

            Assert.IsTrue(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without a server.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParametersNoServerReturnsFalse()
        {
            MailWorker.SetupSmtp(null, 587, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without a port.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParametersZeroPortReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 0, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters with an authentication error.
        /// </summary>
        [TestMethod]
        public void CheckSmtpParametersAuthErrorReturnsFalse()
        {
            MailWorker.SetupSmtp("smtp.test.com", 587, true, "user", null);

            Assert.IsFalse(MailWorker.CheckSmtpParameters(false));
        }

        /// <summary>
        /// Verifies CheckSmtpParameters without authentication (both null).
        /// </summary>
        [TestMethod]
        public void CheckSmtpParametersNoAuthReturnsTrue()
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
        public void CheckImapParametersValidConfigReturnsTrue()
        {
            MailWorker.SetupSmtp("smtp.test.com", 25, false, null, null);
            MailWorker.SetupImap("imap.test.com", 993, true, "user", "pass");

            Assert.IsTrue(MailWorker.CheckImapParameters(false));
        }

        /// <summary>
        /// Verifies CheckImapParameters without a server.
        /// </summary>
        [TestMethod]
        public void CheckImapParametersNoServerReturnsFalse()
        {
            MailWorker.SetupImap(null, 993, true, "user", "pass");

            Assert.IsFalse(MailWorker.CheckImapParameters(false));
        }

        /// <summary>
        /// Verifies ImapAuthConfigError when the password is missing.
        /// </summary>
        [TestMethod]
        public void ImapAuthConfigErrorMissingPasswordReturnsTrue()
        {
            MailWorker.SetupImap("imap.test.com", 993, true, "user", null);

            Assert.IsTrue(MailWorker.ImapAuthConfigError);
        }

        /// <summary>
        /// Verifies ImapAuthConfigError when the username is missing.
        /// </summary>
        [TestMethod]
        public void ImapAuthConfigErrorMissingUsernameReturnsTrue()
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
        public void ParseStringConditionTrueContentVisible()
        {
            var conditions  = new List<(string Name, bool Check)> { ("SHOW", true) };
            var result      = MailWorker.ParseString("A<<SHOW>>B<</SHOW>>C", conditions: conditions);

            Assert.AreEqual("ABC", result);
        }

        /// <summary>
        /// Verifies ParseString with condition false (content removed).
        /// </summary>
        [TestMethod]
        public void ParseStringConditionFalseContentRemoved()
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
        public void ParseFileWithCustomTagsExtractsTagValues()
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
        public void ParseFileNoSubjectReturnsFalse()
        {
            var lines                    = new List<string> { "Only body text" };
            List<(string, bool)> noConds = null;
            var result                   = MailWorker.ParseFile(lines, conditions: noConds);

            Assert.IsFalse(result.Parsed);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Verifies the public constants of MailWorker.
        /// </summary>
        [TestMethod]
        public void ConstantsExpectedValues()
        {
            Assert.AreEqual('#', MailWorker.COMMENTSYMBOL);
            Assert.AreEqual("@Subject:", MailWorker.SUBJECTTAG);
            Assert.IsTrue(MailWorker.MAXITERATIONS > 0);
        }

        #endregion
    }
}
