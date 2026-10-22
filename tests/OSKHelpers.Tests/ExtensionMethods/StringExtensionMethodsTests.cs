using OSKHelpers.Common;
using OSKHelpers.ExtensionMethods;

namespace OSKHelpers.Tests.ExtensionMethods
{
    /// <summary>
    /// Tests for the <see cref="StringExtensionMethods"/> class.
    /// </summary>
    [TestClass]
    public class StringExtensionMethodsTests
    {
        #region AsASCII

        /// <summary>
        /// Verifies that AsASCII returns the same result as StringUtils.AsASCII with all the options.
        /// </summary>
        [TestMethod]
        [DataRow("Perché così", false, false, null, false)]
        [DataRow("  àèìòù  ", true, true, null, false)]
        [DataRow("abcdef", false, false, 3, false)]
        [DataRow("ab", false, false, 5, true)]
        [DataRow(null, false, false, null, false)]
        public void AsASCII_VariousOptions_MatchesStringUtils(string text, bool toUpper, bool trim, int? length, bool padRight)
        {
            Assert.AreEqual(StringUtils.AsASCII(text, toUpper, trim, length, padRight), text.AsASCII(toUpper, trim, length, padRight));
        }

        #endregion
    }
}
