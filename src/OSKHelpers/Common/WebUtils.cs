using OSKHelpers.Logging;
using System;

namespace OSKHelpers.Common
{
    public class WebUtils
    {
        /// <inheritdoc cref="IsWellFormedAddress(string, bool, bool)"/>
        public static bool IsWellFormedAddress(string address) => GetUri(address).IsValid;
        /// <summary>
        /// Checks whether the given string is a valid absolute URI.
        /// </summary>
        /// <param name="address">Address to validate.</param>
        /// <param name="onlyHttp">The address must use the http:// scheme.</param>
        /// <param name="onlyHttps">The address must use the https:// scheme.</param>
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
        /// Attempts to convert the given string to a Uri, returning the conversion result, the generated Uri, and the scheme.
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
