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
            // Verifica stato iniziale
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            // Creazione di Instance con AcceptInsecureConnections = false
            OSKHttpClient.AcceptInsecureConnections = false;
            Assert.IsNotNull(OSKHttpClient.Instance);
            Assert.IsNotNull(OSKHttpClient.GetHttpClientPrivateInstance());
            // Quando Instance non è null è atteso che la modifica di AcceptInsecureConnections restituisca InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => OSKHttpClient.AcceptInsecureConnections = true);
            // E' necessario richiamare il metodo dispose prima di poter modificare il valore di AcceptInsecureConnections
            OSKHttpClient.Dispose();
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            OSKHttpClient.AcceptInsecureConnections = true;
            Assert.IsNull(OSKHttpClient.GetHttpClientPrivateInstance());
            Assert.IsNotNull(OSKHttpClient.Instance);
            Assert.IsNotNull(OSKHttpClient.GetHttpClientPrivateInstance());
            OSKHttpClient.Dispose();
            // Test per SetInstance. Può essere utilizzato esclusivamente quando Instane è null.
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
