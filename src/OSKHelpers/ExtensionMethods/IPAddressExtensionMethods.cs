using OSKHelpers.Common;
using System.Net;
using System.Text.RegularExpressions;

namespace OSKHelpers.ExtensionMethods
{
    public static class IPAddressExtensionMethods
    {
        /// <summary>
        /// Restituisce, se presente, la quartina x.x.x.x all'interno della stringa.
        /// Il risultato non viene verificato, sarà responsabilità del chiamante procedere in tal senso.
        /// </summary>
        /// <param name="address">Stringa da cui estrarre l'indirizzo</param>
        public static Match Match(this IPAddress ipAddress, string address) => OSKIPAddress.Match(address);

        /// <summary>
        /// Recupera, se presente, la quartina x.x.x.x all'interno della stringa e restituisce l'oggetto IPAddress risultante
        /// </summary>
        /// <param name="address">Stringa contenente l'indirizzo da verificare.</param>
        /// <param name="ipAddress">Oggetto IPAddress risultante dalla verifica.</param>
        /// <returns></returns>
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => OSKIPAddress.TryParse(OSKIPAddress.Match(address).Value, out ipAddress);

    }
}
