using System.Net;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="OSKIPAddress"/> class.
    /// </summary>
    [TestClass]
    public class OSKIPAddressTests
    {
        #region Constructors

        /// <summary>
        /// Verifies the constructor from a byte array.
        /// </summary>
        [TestMethod]
        public void Constructor_ByteArray_CreatesAddress()
        {
            var address = new OSKIPAddress(new byte[] { 192, 168, 1, 10 });

            Assert.AreEqual("192.168.1.10", address.ToString());
        }

        /// <summary>
        /// Verifies the constructor from a long value.
        /// </summary>
        [TestMethod]
        public void Constructor_Long_CreatesAddress()
        {
            // 0x0100007F is 127.0.0.1 in network byte order
            var address = new OSKIPAddress(0x0100007F);

            Assert.AreEqual(IPAddress.Loopback, address);
        }

        /// <summary>
        /// Verifies the constructor from a byte array and a scope identifier (IPv6).
        /// </summary>
        [TestMethod]
        public void Constructor_ByteArrayAndScopeId_CreatesIPv6Address()
        {
            var bytes       = IPAddress.IPv6Loopback.GetAddressBytes();
            var address     = new OSKIPAddress(bytes, 3);

            Assert.AreEqual(3, address.ScopeId);
            CollectionAssert.AreEqual(bytes, address.GetAddressBytes());
        }

        #endregion

        #region Match

        /// <summary>
        /// Verifies that the x.x.x.x quad is extracted from the text.
        /// </summary>
        [TestMethod]
        [DataRow("192.168.1.10", "192.168.1.10")]
        [DataRow("Address: 10.0.0.1:8080", "10.0.0.1")]
        [DataRow("first 1.2.3.4 then 5.6.7.8", "1.2.3.4")]
        [DataRow("999.999.999.999", "999.999.999.999")]
        public void Match_TextWithQuad_ReturnsFirstQuad(string text, string expected)
        {
            var match = OSKIPAddress.Match(text);

            Assert.IsTrue(match.Success);
            Assert.AreEqual(expected, match.Value);
        }

        /// <summary>
        /// Verifies that a text without quad, empty or null does not match.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("no address")]
        [DataRow("1.2.3")]
        public void Match_TextWithoutQuad_ReturnsNoMatch(string text)
        {
            Assert.IsFalse(OSKIPAddress.Match(text).Success);
        }

        #endregion

        #region TryParseMatch

        /// <summary>
        /// Verifies that a valid address contained in the text is parsed.
        /// </summary>
        [TestMethod]
        public void TryParseMatch_ValidAddressInText_ReturnsTrueAndAddress()
        {
            var parsed = OSKIPAddress.TryParseMatch("host 192.168.1.10 port 80", out var address);

            Assert.IsTrue(parsed);
            Assert.AreEqual(IPAddress.Parse("192.168.1.10"), address);
        }

        /// <summary>
        /// Verifies that a quad with octets greater than 255 or a text without quad are not parsed.
        /// </summary>
        [TestMethod]
        [DataRow("999.1.1.1")]
        [DataRow("no address")]
        [DataRow(null)]
        public void TryParseMatch_InvalidAddress_ReturnsFalse(string text)
        {
            Assert.IsFalse(OSKIPAddress.TryParseMatch(text, out _));
        }

        #endregion
    }
}
