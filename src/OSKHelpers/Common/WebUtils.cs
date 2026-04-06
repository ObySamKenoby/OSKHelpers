using OSKHelpers.Logging;
using System;

namespace OSKHelpers.Common
{
    public class WebUtils
    {
        /// <inheritdoc cref="IsWellFormedAddress(string, bool, bool)"/>
        public static bool IsWellFormedAddress(string address) => GetUri(address).IsValid;
        /// <summary>
        /// Verifica se la stringa passata come parametro costituisce un Uri valido
        /// </summary>
        /// <param name="address">Indirizzo da verificare.</param>
        /// <param name="onlyHttp">L'indirizzo deve essere di tipo http://</param>
        /// <param name="onlyHttps">L'indirizzo deve essere di tipo https://</param>
        public static bool IsWellFormedAddress(string address, bool onlyHttps, bool onlyHttp = false)
        {
            var uri = GetUri(address);
            return uri.IsValid && (!onlyHttps || uri.Scheme == Uri.UriSchemeHttps) && (!onlyHttp || uri.Scheme == Uri.UriSchemeHttp);
        }
        /// <inheritdoc cref="IsWellFormedAddress(string, bool, bool)"/>
        public static bool IsWellFormedHttpHttpsAddress(string address)
        {
            var uri = GetUri(address);
            return uri.IsValid && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        /// <summary>
        /// Tenta la conversione della stringa passata come parametro in un Uri restituendo il risultato della conversione, l'Uri generato e lo schema.
        /// </summary>
        private static (bool IsValid, Uri Uri, string Scheme) GetUri(string address)
        {
            bool isValid    = false;
            Uri uri         = null;
            string scheme   = null;

            try
            {
                isValid = Uri.TryCreate(address, UriKind.Absolute, out uri);
                scheme  = uri?.Scheme;
            }
            catch (Exception ex)
            {
                SimpleLog.LogError(ex);
                isValid = false;
                uri     = null;
                scheme  = null;
            }
            return (isValid, uri, scheme);
        }


    }
}
