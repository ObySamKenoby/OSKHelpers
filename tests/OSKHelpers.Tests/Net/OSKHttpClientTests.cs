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
        public void Instance_AcceptInsecureConnectionsAndDispose_BehavesAsDocumented()
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

        /// <summary>
        /// Verifies that SetInstance with a null instance throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void SetInstance_NullInstance_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => OSKHttpClient.SetInstance(null));

            Assert.AreEqual("instance", ex.ParamName);
        }

        /// <summary>
        /// Verifies InstanceIsNotNull before and after the first access to Instance, and that Dispose can be repeated.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void InstanceIsNotNull_AccessAndDispose_ReturnsExpected()
        {
            try
            {
                OSKHttpClient.Dispose();
                Assert.IsFalse(OSKHttpClient.InstanceIsNotNull);

                var instance = OSKHttpClient.Instance;
                Assert.IsNotNull(instance);
                Assert.IsTrue(OSKHttpClient.InstanceIsNotNull);
                Assert.AreSame(instance, OSKHttpClient.Instance);

                OSKHttpClient.Dispose();
                OSKHttpClient.Dispose();
                Assert.IsFalse(OSKHttpClient.InstanceIsNotNull);
            }
            finally
            {
                OSKHttpClient.Dispose();
            }
        }

        /// <summary>
        /// Verifies that setting AcceptInsecureConnections to its current value does not throw, even with an instance.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void AcceptInsecureConnections_SameValueWithInstance_DoesNotThrow()
        {
            try
            {
                OSKHttpClient.Dispose();
                OSKHttpClient.AcceptInsecureConnections = false;
                Assert.IsNotNull(OSKHttpClient.Instance);

                OSKHttpClient.AcceptInsecureConnections = false;

                Assert.IsFalse(OSKHttpClient.AcceptInsecureConnections);
            }
            finally
            {
                OSKHttpClient.Dispose();
            }
        }

        /// <summary>
        /// Verifies that the insecure handler accepts any server certificate.
        /// </summary>
        [TestMethod]
        public void GetInsecureHttpHandler_AnyCertificate_IsAccepted()
        {
            using var handler = OSKHttpClient.GetInsecureHttpHandler();

            Assert.AreEqual(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
            Assert.IsTrue(handler.ServerCertificateCustomValidationCallback(null, null, null, System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors));
        }
    }
}
