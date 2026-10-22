using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace OSKHelpers.Net
{
    /// <summary>
    /// Helper class providing methods for downloading (synchronously or asynchronously) a file from the web.
    /// </summary>
    public class WebDownloader
    {
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
        /// <returns>
        /// True if the download succeeded.<br/>
        /// False if the server returned a status code that does not indicate success; in this case the output file is not created.
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static async Task<bool> DownloadAsync(HttpClient httpClient, string url, string outputFile)
        {
            var downloaded = false;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }
            var outputDir = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputDir))
            {
                throw new DirectoryNotFoundException(outputDir);
            }

            // A single request: the content is read as a stream once the headers have been received
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        using (var fileStream = new FileStream(outputFile, FileMode.Create))
                        {
                            await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
                            downloaded = true;
                        }
                    }
                }
            }

            return downloaded;
        }

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static async Task<bool> DownloadAsync(string url, string outputFile)
            => await DownloadAsync(OSKHttpClient.Instance, url, outputFile).ConfigureAwait(false);

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string)"/>
        public static bool Download(HttpClient httpClient, string url, string outputFile)
        {
            return DownloadAsync(httpClient, url, outputFile).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="Download(HttpClient, string, string)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static bool Download(string url, string outputFile)
            => Download(OSKHttpClient.Instance, url, outputFile);

        #endregion
    }
}
