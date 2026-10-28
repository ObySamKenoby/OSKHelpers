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

        #region Cancellation and inactivity timeout

        /// <summary>
        /// Verifies that an inactivity timeout not greater than zero throws ArgumentOutOfRangeException, without requests.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public async Task DownloadAsync_InactivityTimeoutNotPositive_ThrowsArgumentOutOfRangeException(int milliseconds)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT);
            using var httpClient = new HttpClient(handler);

            var exception = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => WebDownloader.DownloadAsync(
                httpClient, URL, Path.Combine(_outputDirectory, "file.txt"), inactivityTimeout: TimeSpan.FromMilliseconds(milliseconds)));

            Assert.AreEqual("inactivityTimeout", exception.ParamName);
            Assert.AreEqual(0, handler.RequestsCount);
        }

        /// <summary>
        /// Verifies that an already canceled token throws OperationCanceledException without creating the file.
        /// </summary>
        [TestMethod]
        public async Task DownloadAsync_CanceledToken_ThrowsOperationCanceledExceptionWithoutFile()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, CONTENT));
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => WebDownloader.DownloadAsync(httpClient, URL, outputFile, cancellationSource.Token));

            Assert.IsFalse(File.Exists(outputFile));
        }

        /// <summary>
        /// Verifies that canceling the token during a stalled transfer interrupts the download and deletes the partial file.
        /// </summary>
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task DownloadAsync_CanceledDuringStalledTransfer_ThrowsOperationCanceledExceptionAndDeletesFile()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(new StreamHttpMessageHandler(() => new SlowStream(1, TimeSpan.Zero, true)));
            using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => WebDownloader.DownloadAsync(httpClient, URL, outputFile, cancellationSource.Token));

            Assert.IsFalse(File.Exists(outputFile));
        }

        /// <summary>
        /// Verifies that a transfer stalled for longer than the inactivity timeout throws TimeoutException and deletes the
        /// partial file.
        /// </summary>
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task DownloadAsync_StalledTransfer_ThrowsTimeoutExceptionAndDeletesFile()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(new StreamHttpMessageHandler(() => new SlowStream(1, TimeSpan.Zero, true)));

            await Assert.ThrowsExactlyAsync<TimeoutException>(
                () => WebDownloader.DownloadAsync(httpClient, URL, outputFile, inactivityTimeout: TimeSpan.FromMilliseconds(300)));

            Assert.IsFalse(File.Exists(outputFile));
        }

        /// <summary>
        /// Verifies that the synchronous Download also throws TimeoutException for a stalled transfer.
        /// </summary>
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public void Download_StalledTransfer_ThrowsTimeoutException()
        {
            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(new StreamHttpMessageHandler(() => new SlowStream(1, TimeSpan.Zero, true)));

            Assert.ThrowsExactly<TimeoutException>(
                () => WebDownloader.Download(httpClient, URL, outputFile, inactivityTimeout: TimeSpan.FromMilliseconds(300)));
            Assert.IsFalse(File.Exists(outputFile));
        }

        /// <summary>
        /// Verifies that a slow but active transfer, whose total duration exceeds the inactivity timeout, is completed: the timer
        /// restarts at every block received.
        /// </summary>
        [TestMethod]
        [Timeout(10000, CooperativeCancellation = true)]
        public async Task DownloadAsync_SlowActiveTransfer_SavesFile()
        {
            const int BLOCKS = 6;

            var outputFile = Path.Combine(_outputDirectory, "file.txt");
            using var httpClient = new HttpClient(
                new StreamHttpMessageHandler(() => new SlowStream(BLOCKS, TimeSpan.FromMilliseconds(200), false)));

            var downloaded = await WebDownloader.DownloadAsync(
                httpClient, URL, outputFile, inactivityTimeout: TimeSpan.FromMilliseconds(800));

            Assert.IsTrue(downloaded);
            Assert.AreEqual(BLOCKS * SlowStream.BLOCK.Length, new FileInfo(outputFile).Length);
        }

        #endregion

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

        /// <summary>
        /// Handler that answers every request with status OK and a content read from the stream created by the factory.
        /// </summary>
        private class StreamHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<Stream> _streamFactory;

            public StreamHttpMessageHandler(Func<Stream> streamFactory)
            {
                _streamFactory = streamFactory;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(_streamFactory())
                };

                return Task.FromResult(response);
            }
        }

        /// <summary>
        /// Read-only stream that returns a fixed number of blocks, each one after a delay; when stalled, after the last block
        /// the read waits until the token is canceled instead of signaling the end of the stream.
        /// </summary>
        private class SlowStream : Stream
        {
            public static readonly byte[] BLOCK = Encoding.UTF8.GetBytes(CONTENT);

            private readonly TimeSpan _delay;
            private readonly bool _stalled;
            private int _remainingBlocks;

            public SlowStream(int blocks, TimeSpan delay, bool stalled)
            {
                _remainingBlocks    = blocks;
                _delay              = delay;
                _stalled            = stalled;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int read;

                if (_remainingBlocks > 0)
                {
                    await Task.Delay(_delay, cancellationToken);
                    _remainingBlocks--;
                    Array.Copy(BLOCK, 0, buffer, offset, BLOCK.Length);
                    read = BLOCK.Length;
                }
                else
                {
                    if (_stalled)
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    read = 0;
                }

                return read;
            }

            public override int Read(byte[] buffer, int offset, int count)
                => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        #endregion
    }
}
