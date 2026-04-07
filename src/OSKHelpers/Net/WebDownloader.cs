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
        #region Members

        private static HttpClient _httpClient;
        private static readonly object _lock = new object();

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
        /// <returns>True if the download succeeded.</returns>
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
                throw new ArgumentNullException(nameof(url));
            }
            var outputDir = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputDir))
            {
                throw new DirectoryNotFoundException(outputDir);
            }

            using (var result = await httpClient.GetAsync(url))
            {
                using (var s = await httpClient.GetStreamAsync(url))
                {
                    using (var fs = new FileStream(outputFile, FileMode.Create))
                    {
                        await s.CopyToAsync(fs);
                        downloaded = true;
                    }
                }
            }

            return downloaded;
        }

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static async Task<bool> DownloadAsync(string url, string outputFile)
            => await DownloadAsync(OSKHttpClient.Instance, url, outputFile);

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string)"/>
        public static bool Download(HttpClient httpClient, string url, string outputFile)
        {
            return DownloadAsync(httpClient, url, outputFile).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="Download(HttpClient, string, string)"/>
        /// <remarks><b>Warning</b>: calling this method will trigger the creation of <see cref="OSKHttpClient.Instance"/> with the current settings.</remarks>
        public static bool Download(string url, string outputFile)
            => Download(OSKHttpClient.Instance, url, outputFile);

        #endregion
    }
}
