using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OSKHelpers.Net;

namespace OSKHelpers.Tests.Net
{
    [TestClass]
    public class WebDownloaderTests
    {
        #region Constants

        private const string URL        = "http://localhost/file.txt";
        private const string CONTENT    = "Downloaded content";

        #endregion

        #region Members

        private string _outputDirectory;

        #endregion

        #region Methods

        #region Setup

        [TestInitialize]
        public void Setup()
        {
            _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_outputDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_outputDirectory))
            {
                Directory.Delete(_outputDirectory, true);
            }
        }

        #endregion

        [TestMethod]
        public async Task DownloadAsync_SuccessStatus_SavesFileWithSingleRequest()
        {
            // Arrange
            var handler     = new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT);
            var outputFile  = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(handler);

            // Act
            var downloaded = await WebDownloader.DownloadAsync(httpClient, URL, outputFile);

            // Assert
            Assert.IsTrue(downloaded);
            Assert.AreEqual(1, handler.RequestsCount);
            Assert.AreEqual(CONTENT, File.ReadAllText(outputFile));
        }

        [TestMethod]
        public async Task DownloadAsync_ErrorStatus_ReturnsFalseWithoutFile()
        {
            // Arrange
            var handler     = new FakeHttpMessageHandler(HttpStatusCode.NotFound, string.Empty);
            var outputFile  = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(handler);

            // Act
            var downloaded = await WebDownloader.DownloadAsync(httpClient, URL, outputFile);

            // Assert
            Assert.IsFalse(downloaded);
            Assert.AreEqual(1, handler.RequestsCount);
            Assert.IsFalse(File.Exists(outputFile));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(" ")]
        public async Task DownloadAsync_EmptyOutputFile_ThrowsArgumentNullExceptionForOutputFile(string outputFile)
        {
            using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT));

            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => WebDownloader.DownloadAsync(httpClient, URL, outputFile));

            Assert.AreEqual("outputFile", exception.ParamName);
        }

        /// <summary>
        /// Verifies that an empty URL throws ArgumentNullException for the url parameter, without requests.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public async Task DownloadAsync_EmptyUrl_ThrowsArgumentNullExceptionForUrl(string url)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT);
            using var httpClient = new HttpClient(handler);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => WebDownloader.DownloadAsync(httpClient, url, Path.Combine(_outputDirectory, "file.txt")));

            Assert.AreEqual("url", exception.ParamName);
            Assert.AreEqual(0, handler.RequestsCount);
        }

        /// <summary>
        /// Verifies that a missing output directory throws DirectoryNotFoundException, without requests.
        /// </summary>
        [TestMethod]
        public async Task DownloadAsync_MissingOutputDirectory_ThrowsDirectoryNotFoundException()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT);
            using var httpClient = new HttpClient(handler);

            await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => WebDownloader.DownloadAsync(httpClient, URL, Path.Combine(_outputDirectory, "Missing", "file.txt")));

            Assert.AreEqual(0, handler.RequestsCount);
        }

        /// <summary>
        /// Verifies that the synchronous Download saves the file and overwrites an existing one.
        /// </summary>
        [TestMethod]
        public void Download_SuccessStatus_SavesFile()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            File.WriteAllText(outputFile, "Previous content that is longer than the new one");
            using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT));

            var downloaded = WebDownloader.Download(httpClient, URL, outputFile);

            Assert.IsTrue(downloaded);
            Assert.AreEqual(CONTENT, File.ReadAllText(outputFile));
        }

        /// <summary>
        /// Verifies that the synchronous Download returns false for an error status.
        /// </summary>
        [TestMethod]
        public void Download_ErrorStatus_ReturnsFalse()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, string.Empty));

            Assert.IsFalse(WebDownloader.Download(httpClient, URL, outputFile));
            Assert.IsFalse(File.Exists(outputFile));
        }

        #region Integration (local HTTP server)

        /// <summary>
        /// Integration test: downloads a file from a local HTTP server through OSKHttpClient.Instance, with the asynchronous
        /// and the synchronous overloads.<br/>
        /// The instance is created without proxy: the default handler uses the system proxy, which may not bypass localhost.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [DoNotParallelize]
        public async Task DownloadAsyncAndDownload_LocalServer_SaveFiles()
        {
            OSKHttpClient.Dispose();
            OSKHttpClient.SetInstance(new HttpClient(new HttpClientHandler { UseProxy = false }));

            var port            = GetFreePort();
            using var listener  = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            var server          = ServeRequestsAsync(listener, 2);
            var asyncFile       = Path.Combine(_outputDirectory, "async.txt");
            var syncFile        = Path.Combine(_outputDirectory, "sync.txt");
            try
            {
                Assert.IsTrue(await WebDownloader.DownloadAsync($"http://localhost:{port}/file.txt", asyncFile));
                Assert.IsTrue(WebDownloader.Download($"http://localhost:{port}/file.txt", syncFile));
                await server;

                Assert.AreEqual(CONTENT, File.ReadAllText(asyncFile));
                Assert.AreEqual(CONTENT, File.ReadAllText(syncFile));
            }
            finally
            {
                listener.Stop();
                OSKHttpClient.Dispose();
            }
        }

        /// <summary>
        /// Returns a free TCP port on the loopback interface.
        /// </summary>
        private static int GetFreePort()
        {
            var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            return port;
        }

        /// <summary>
        /// Answers the given number of requests with the test content.
        /// </summary>
        private static async Task ServeRequestsAsync(HttpListener listener, int requests)
        {
            for (var i = 0; i < requests; i++)
            {
                var context = await listener.GetContextAsync();
                var buffer  = Encoding.UTF8.GetBytes(CONTENT);
                context.Response.StatusCode         = (int)HttpStatusCode.OK;
                context.Response.ContentLength64    = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer);
                context.Response.Close();
            }
        }

        #endregion

        #endregion

        #region FakeHttpMessageHandler

        /// <summary>
        /// Handler that answers every request with a fixed status code and content, counting the requests received.
        /// </summary>
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public int RequestsCount { get; private set; }

            public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode     = statusCode;
                _content        = content;
                RequestsCount   = 0;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestsCount++;
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content, Encoding.UTF8)
                };

                return Task.FromResult(response);
            }
        }

        #endregion
    }
}
