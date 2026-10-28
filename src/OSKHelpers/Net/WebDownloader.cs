using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OSKHelpers.Net
{
    /// <summary>
    /// Helper class providing methods for downloading (synchronously or asynchronously) a file from the web.
    /// </summary>
    public class WebDownloader
    {
        #region Constants

        private const int COPYBUFFERSIZE = 81920;

        #endregion

        #region Methods

        /// <summary>
        /// Downloads a file from <paramref name="url"/> and saves it to <paramref name="outputFile"/>.<br/>
        /// <b>Warning</b>: wrap the method in a try / catch block to handle possible exceptions;<br/>
        /// communication exceptions are not filtered to allow easier debugging.
        /// </summary>
        /// <param name="httpClient">
        /// <see cref="HttpClient"/> instance to use for the requests.<br/>
        /// Must not be null and should be instantiated only once during the application lifetime.<br/>
        /// <see cref="OSKHttpClient.Instance"/> can be used to simplify client management.<br/>
        /// Refer to the Microsoft documentation at the following links:<br/>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netstandard-2.0"/><br/>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-http-httpclient"/>
        /// </param>
        /// <param name="url">URL from which to download the file.</param>
        /// <param name="outputFile">
        /// Full path of the output file.<br/>
        /// To save the file in the execution folder, prefix the name with ".\" (e.g. ".\testFile.txt").
        /// </param>
        /// <param name="cancellationToken">Token that interrupts the request and the transfer of the content.</param>
        /// <param name="inactivityTimeout">
        /// Maximum time allowed without receiving data, both while waiting for the response headers and during the transfer of
        /// the content; the timer restarts every time a block of data is received, so a slow but active download is not
        /// interrupted.<br/>
        /// Null (default) disables the check: only <see cref="HttpClient.Timeout"/> applies, which does not cover the transfer
        /// of the content.
        /// </param>
        /// <returns>
        /// True if the download succeeded.<br/>
        /// False if the server returned a status code that does not indicate success; in this case the output file is not created.
        /// </returns>
        /// <remarks>
        /// If the download is interrupted (cancellation, inactivity timeout or communication error) the partially written output
        /// file is deleted.
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inactivityTimeout"/> is not greater than zero.</exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> has been canceled.</exception>
        /// <exception cref="TimeoutException">No data was received within <paramref name="inactivityTimeout"/>.</exception>
        public static async Task<bool> DownloadAsync(HttpClient httpClient, string url, string outputFile,
            CancellationToken cancellationToken = default, TimeSpan? inactivityTimeout = null)
        {
            var downloaded  = false;
            var fileCreated = false;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }
            if (inactivityTimeout.HasValue && inactivityTimeout.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(inactivityTimeout));
            }
            var outputDir = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputDir))
            {
                throw new DirectoryNotFoundException(outputDir);
            }

            // The linked source is canceled either by the caller's token or by the inactivity timer
            using (var transferSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    RestartInactivityTimer(transferSource, inactivityTimeout);

                    // A single request: the content is read as a stream once the headers have been received
                    using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, transferSource.Token)
                                                          .ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                // Disposing the stream unblocks a pending read on platforms whose content stream ignores the token
                                using (transferSource.Token.Register(contentStream.Dispose))
                                {
                                    using (var fileStream = new FileStream(outputFile, FileMode.Create))
                                    {
                                        fileCreated = true;
                                        await CopyContentAsync(contentStream, fileStream, transferSource, inactivityTimeout)
                                            .ConfigureAwait(false);
                                        downloaded = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (fileCreated)
                    {
                        DeleteIncompleteFile(outputFile);
                    }

                    // The exception raised by an interrupted transfer depends on the platform (OperationCanceledException,
                    // ObjectDisposedException, IOException): it is converted to report the actual cause
                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (ex is OperationCanceledException)
                        {
                            throw;
                        }
                        throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                    }
                    if (transferSource.IsCancellationRequested)
                    {
                        throw new TimeoutException(
                            string.Format(NetMessages.DownloadInactivityTimeoutException, inactivityTimeout), ex);
                    }
                    throw;
                }
            }

            return downloaded;
        }

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string, CancellationToken, TimeSpan?)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static async Task<bool> DownloadAsync(string url, string outputFile,
            CancellationToken cancellationToken = default, TimeSpan? inactivityTimeout = null)
            => await DownloadAsync(OSKHttpClient.Instance, url, outputFile, cancellationToken, inactivityTimeout).ConfigureAwait(false);

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string, CancellationToken, TimeSpan?)"/>
        public static bool Download(HttpClient httpClient, string url, string outputFile,
            CancellationToken cancellationToken = default, TimeSpan? inactivityTimeout = null)
        {
            return DownloadAsync(httpClient, url, outputFile, cancellationToken, inactivityTimeout)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="Download(HttpClient, string, string, CancellationToken, TimeSpan?)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static bool Download(string url, string outputFile,
            CancellationToken cancellationToken = default, TimeSpan? inactivityTimeout = null)
            => Download(OSKHttpClient.Instance, url, outputFile, cancellationToken, inactivityTimeout);

        #region Utility methods

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/> block by block, restarting the inactivity timer
        /// before every read.
        /// </summary>
        /// <param name="source">Content stream of the response.</param>
        /// <param name="destination">Output file stream.</param>
        /// <param name="transferSource">Source of the token that interrupts the copy; its timer is restarted at every read.</param>
        /// <param name="inactivityTimeout">Maximum time allowed for each read; null to disable the timer.</param>
        private static async Task CopyContentAsync(Stream source, Stream destination, CancellationTokenSource transferSource,
            TimeSpan? inactivityTimeout)
        {
            var buffer = new byte[COPYBUFFERSIZE];
            int read;

            do
            {
                RestartInactivityTimer(transferSource, inactivityTimeout);
                read = await source.ReadAsync(buffer, 0, buffer.Length, transferSource.Token).ConfigureAwait(false);
                if (read > 0)
                {
                    await destination.WriteAsync(buffer, 0, read, transferSource.Token).ConfigureAwait(false);
                }
            }
            while (read > 0);
        }

        /// <summary>
        /// Restarts the inactivity timer of <paramref name="transferSource"/>, if <paramref name="inactivityTimeout"/> has a value.
        /// </summary>
        /// <param name="transferSource">Source to cancel when the timer expires.</param>
        /// <param name="inactivityTimeout">Interval of the timer; null to leave the source without timer.</param>
        private static void RestartInactivityTimer(CancellationTokenSource transferSource, TimeSpan? inactivityTimeout)
        {
            if (inactivityTimeout.HasValue)
            {
                transferSource.CancelAfter(inactivityTimeout.Value);
            }
        }

        /// <summary>
        /// Deletes the output file of an interrupted download; a deletion error is logged and does not hide the original exception.
        /// </summary>
        /// <param name="outputFile">Full path of the file to delete.</param>
        private static void DeleteIncompleteFile(string outputFile)
        {
            try
            {
                File.Delete(outputFile);
            }
            catch (Exception ex)
            {
                Logging.SimpleLog.LogError(ex);
            }
        }

        #endregion

        #endregion
    }
}
