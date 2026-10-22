using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="WebUtils"/> class.
    /// </summary>
    [TestClass]
    public class WebUtilsTests
    {
        #region IsWellFormedAddress

        /// <summary>
        /// Verifies that absolute URIs are valid and relative or invalid ones are not.
        /// </summary>
        [TestMethod]
        [DataRow("https://www.example.com", true)]
        [DataRow("http://example.com/path?query=1", true)]
        [DataRow("ftp://example.com/file.txt", true)]
        [DataRow("/relative/path", false)]
        [DataRow("not an address", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void IsWellFormedAddress_VariousAddresses_ReturnsExpected(string address, bool expected)
        {
            Assert.AreEqual(expected, WebUtils.IsWellFormedAddress(address));
        }

        /// <summary>
        /// Verifies the onlyHttps and onlyHttp constraints.
        /// </summary>
        [TestMethod]
        [DataRow("https://example.com", true, false, true)]
        [DataRow("http://example.com", true, false, false)]
        [DataRow("http://example.com", false, true, true)]
        [DataRow("https://example.com", false, true, false)]
        [DataRow("ftp://example.com", false, false, true)]
        [DataRow("https://example.com", true, true, false)]
        [DataRow("not an address", false, false, false)]
        public void IsWellFormedAddress_SchemeConstraints_ReturnsExpected(string address, bool onlyHttps, bool onlyHttp, bool expected)
        {
            Assert.AreEqual(expected, WebUtils.IsWellFormedAddress(address, onlyHttps, onlyHttp));
        }

        #endregion

        #region IsWellFormedHttpHttpsAddress

        /// <summary>
        /// Verifies that only http and https addresses are accepted.
        /// </summary>
        [TestMethod]
        [DataRow("https://example.com", true)]
        [DataRow("http://example.com", true)]
        [DataRow("ftp://example.com", false)]
        [DataRow("file:///C:/temp/file.txt", false)]
        [DataRow("example.com", false)]
        [DataRow(null, false)]
        public void IsWellFormedHttpHttpsAddress_VariousAddresses_ReturnsExpected(string address, bool expected)
        {
            Assert.AreEqual(expected, WebUtils.IsWellFormedHttpHttpsAddress(address));
        }

        #endregion
    }
}
