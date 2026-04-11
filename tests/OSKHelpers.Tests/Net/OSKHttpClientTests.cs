using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Net;
using System;
using System.Net.Http;

namespace OSKHelpers.Tests.Net
{
    [TestClass]
    public class OSKHttpClientTests
    {
        [TestMethod]
        public void InitTests()
        {
#if DEBUG
            // Check initial state
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            // Create Instance with AcceptInsecureConnections = false
            OSKHttpClient.AcceptInsecureConnections = false;
            Assert.IsNotNull(OSKHttpClient.Instance);
            Assert.IsNotNull(OSKHttpClient.GetHttpClientPrivateInstance());
            // When Instance is not null, modifying AcceptInsecureConnections is expected to throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => OSKHttpClient.AcceptInsecureConnections = true);
            // Dispose must be called before modifying AcceptInsecureConnections
            OSKHttpClient.Dispose();
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            OSKHttpClient.AcceptInsecureConnections = true;
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            Assert.IsNotNull(OSKHttpClient.Instance);
            Assert.IsNotNull(OSKHttpClient.GetHttpClientPrivateInstance());
            OSKHttpClient.Dispose();
            // Test for SetInstance. Can only be used when Instance is null.
            var instance = new HttpClient();
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            OSKHttpClient.SetInstance(instance);
            ReferenceEquals(instance, OSKHttpClient.Instance);
            Assert.Throws<InvalidOperationException>(() => OSKHttpClient.SetInstance(instance));
            OSKHttpClient.Dispose();
#endif
        }
    }
}
