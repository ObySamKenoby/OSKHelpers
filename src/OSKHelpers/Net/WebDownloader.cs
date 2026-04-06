using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace OSKHelpers.Net
{
    /// <summary>
    /// Classe di supporto che contiene metodi per il download (sincrono o asincrono) di un file da web.
    /// </summary>
    public class WebDownloader
    {
        #region Membri

        private static HttpClient _httpClient;
        private static readonly object _lock = new object();

        #endregion

        #region Metodi



        /// <summary>
        /// Esegue il download di un file da <paramref name="url"/> e lo salva in <paramref name="outputFile"/>.<br/>
        /// <b>Attenzione</b>: incapsulare il metodo in un blocco try / catch in modo da intercettare le possibili eccezioni,<br/>
        /// quelle di comunicazione non saranno filtrate in modo da permettere  un debug più agevole.
        /// </summary>
        /// <param name="httpClient">
        /// Istanza di <see cref="HttpClient"/> da utilizzare per effettuare le richieste.<br/>
        /// Non può essere nulla e deve essere istanziata una singola volta all'interno del ciclo di vita dell'applicazione.<br/>
        /// Se desiderato è possibile utilizzare <see cref="OSKHttpClient.Instance"/> per semplificare la gestione del client.<br/>
        /// Consultare la documentazione Microsoft ai seguenti indirizzi:<br/>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netstandard-2.0"/><br/>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-http-httpclient"/>
        /// </param>
        /// <param name="url">url da cui effettuare il download.</param>
        /// <param name="outputFile">
        /// Percorso completo del file di output.<br/>
        /// Se si vuole salvare il file nella cartella di esecuzione far precedere il nome da ".\" (es ".\testFile.txt")
        /// </param>
        /// <returns>True se il download è andato a buon fine.</returns>
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
        /// <remarks><b>Attenzione</b>: richiamare questo metodo provocherà la creazione di <see cref="OSKHttpClient.Instance"/> con le attuali impostazioni.</remarks>
        public static async Task<bool> DownloadAsync(string url, string outputFile)
            => await DownloadAsync(OSKHttpClient.Instance, url, outputFile);

        /// <inheritdoc cref="DownloadAsync(HttpClient, string, string)"/>
        public static bool Download(HttpClient httpClient, string url, string outputFile)
        {
            return DownloadAsync(httpClient, url, outputFile).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="Download(HttpClient, string, string)"/>
        /// <remarks><b>Attenzione</b>: richiamare questo metodo provocherà la creazione di <see cref="OSKHttpClient.Instance"/> con le attuali impostazioni.</remarks>
        public static bool Download(string url, string outputFile)
            => Download(OSKHttpClient.Instance, url, outputFile);

        #endregion
    }
}
