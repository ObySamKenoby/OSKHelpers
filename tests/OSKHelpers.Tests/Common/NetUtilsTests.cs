using System.Linq;
using System.Net;
using System.Threading.Tasks;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Integration tests for the <see cref="NetUtils"/> class: they use the name resolution of the operating system.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class NetUtilsTests
    {
        #region Constants

        /// <summary>
        /// Host name that can never be resolved (reserved .invalid domain, RFC 6761).
        /// </summary>
        private const string INVALIDHOST = "oskhelpers-test.invalid";

        #endregion

        #region CheckDNS

        /// <summary>
        /// Verifies that localhost is resolved to a loopback address.
        /// </summary>
        [TestMethod]
        public void CheckDNS_Localhost_ReturnsLoopbackAddress()
        {
            var (reachable, addresses) = NetUtils.CheckDNS("localhost");

            Assert.IsTrue(reachable);
            Assert.IsTrue(addresses.Any(IPAddress.IsLoopback));
        }

        /// <summary>
        /// Verifies that the asynchronous version resolves localhost.
        /// </summary>
        [TestMethod]
        public async Task CheckDNSAsync_Localhost_ReturnsReachable()
        {
            var (reachable, addresses) = await NetUtils.CheckDNSAsync("localhost");

            Assert.IsTrue(reachable);
            Assert.IsNotEmpty(addresses);
        }

        /// <summary>
        /// Verifies that a host that cannot be resolved is reported as not reachable, as documented, without exceptions.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-040")]
        public void CheckDNS_UnresolvableHost_ReturnsNotReachable()
        {
            var (reachable, addresses) = NetUtils.CheckDNS(INVALIDHOST);

            Assert.IsFalse(reachable);
            Assert.IsEmpty(addresses ?? System.Array.Empty<IPAddress>());
        }

        #endregion
    }
}
