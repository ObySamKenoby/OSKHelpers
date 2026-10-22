using System.Net;
using OSKHelpers.ExtensionMethods;

namespace OSKHelpers.Tests.ExtensionMethods
{
    [TestClass]
    public class IPAddressExtensionMethodsTests
    {
        #region Methods

        [TestMethod]
        [DataRow("Server: 192.168.1.10:8080",   true,   "192.168.1.10")]
        [DataRow("10.0.0.1",                    true,   "10.0.0.1")]
        [DataRow("No address",                  false,  null)]
        [DataRow(null,                          false,  null)]
        public void TryParseMatch_Address_ReturnsParsedAddress(string address, bool expected, string expectedAddress)
        {
            var parsed = IPAddress.None.TryParseMatch(address, out var result);

            Assert.AreEqual(expected, parsed);
            Assert.AreEqual(expectedAddress, result?.ToString());
        }

        /// <summary>
        /// Verifies that the static TryParseMatch delegates to OSKIPAddress.TryParseMatch.
        /// </summary>
        [TestMethod]
        [DataRow("Server: 192.168.1.10:8080",   true,   "192.168.1.10")]
        [DataRow("300.1.1.1",                   false,  null)]
        public void TryParseMatch_StaticCall_ReturnsParsedAddress(string address, bool expected, string expectedAddress)
        {
            // The method is obsolete but still public: the test verifies it keeps working until it is removed
#pragma warning disable CS0618
            var parsed = IPAddressExtensionMethods.TryParseMatch(address, out var result);
#pragma warning restore CS0618

            Assert.AreEqual(expected, parsed);
            Assert.AreEqual(expectedAddress, result?.ToString());
        }

        /// <summary>
        /// Verifies that Match returns the x.x.x.x quad contained in the text.
        /// </summary>
        [TestMethod]
        [DataRow("Server: 192.168.1.10:8080",   true,   "192.168.1.10")]
        [DataRow("No address",                  false,  "")]
        [DataRow(null,                          false,  "")]
        public void Match_Text_ReturnsQuad(string address, bool expected, string expectedValue)
        {
            var match = IPAddress.None.Match(address);

            Assert.AreEqual(expected, match.Success);
            Assert.AreEqual(expectedValue, match.Value);
        }

        #endregion
    }
}
