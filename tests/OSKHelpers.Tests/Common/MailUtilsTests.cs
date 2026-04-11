using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="MailUtils"/> class.
    /// </summary>
    [TestClass]
    public class MailUtilsTests
    {
        #region IsValidEmail

        /// <summary>
        /// Verifies that a valid e-mail address is recognized as such.
        /// </summary>
        [TestMethod]
        [DataRow("user@example.com",        true)]
        [DataRow("user.name@example.com",   true)]
        [DataRow("user+tag@example.com",    true)]
        [DataRow("user@sub.domain.com",     true)]
        [DataRow("a@b.co",                  true)]
        [DataRow("user@example",            false)]
        [DataRow("@example.com",            false)]
        [DataRow("user@",                   false)]
        [DataRow("user@@example.com",       false)]
        [DataRow("",                        false)]
        [DataRow(" ",                       false)]
        [DataRow(null,                      false)]
        public void IsValidEmailVariousInputsReturnsExpected(string email, bool expected)
        {
            var result = MailUtils.IsValidEmail(email);

            Assert.AreEqual(expected, result);
        }

        #endregion

        #region ContainsValidEmails

        /// <summary>
        /// Verifies the recognition of text containing one or more e-mail addresses.
        /// </summary>
        [TestMethod]
        [DataRow("user@example.com",                    true)]
        [DataRow("Contact us at user@example.com!",     true)]
        [DataRow("a@b.co and c@d.co",                   true)]
        [DataRow("no email here",                       false)]
        [DataRow("",                                    false)]
        [DataRow(null,                                  false)]
        public void ContainsValidEmailsVariousInputsReturnsExpected(string text, bool expected)
        {
            var result = MailUtils.ContainsValidEmails(text);

            Assert.AreEqual(expected, result);
        }

        #endregion

        #region ContainsSingleEmail

        /// <summary>
        /// Verifies the recognition of text containing exactly one e-mail address.
        /// </summary>
        [TestMethod]
        [DataRow("user@example.com",                    true)]
        [DataRow("Contact us at user@example.com!",     true)]
        [DataRow("a@b.co and c@d.co",                   false)]
        [DataRow("no email here",                       false)]
        [DataRow("",                                    false)]
        [DataRow(null,                                  false)]
        public void ContainsSingleEmailVariousInputsReturnsExpected(string text, bool expected)
        {
            var result = MailUtils.ContainsSingleEmail(text);

            Assert.AreEqual(expected, result);
        }

        #endregion
    }
}
