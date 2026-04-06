using System.Net;
using System.Text.RegularExpressions;

namespace OSKHelpers.Common
{
    public class OSKIPAddress : IPAddress
    {
        #region Costanti

        private const string IPMASK = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";

        #endregion

        #region Costruttori

        public OSKIPAddress(byte[] address) : base(address)
        {
        }

        public OSKIPAddress(long newAddress) : base(newAddress)
        {
        }

        public OSKIPAddress(byte[] address, long scopeid) : base(address, scopeid)
        {
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Restituisce, se presente, la quartina x.x.x.x all'interno della stringa.<br/>
        /// Il risultato non viene verificato, sarà responsabilità del chiamante procedere in tal senso.<br/>
        /// Se si desidera l'oggetto IPAddress risultante dalla verifica utilizzare il metodo TryParseMatch.
        /// </summary>
        /// <param name="address">Stringa da cui estrarre l'indirizzo</param>
        public static Match Match(string address) => Regex.Match(address ?? string.Empty, IPMASK);

        /// <summary>
        /// Recupera, se presente, la quartina x.x.x.x all'interno della stringa e restituisce l'oggetto IPAddress risultante
        /// </summary>
        /// <param name="address">Stringa contenente l'indirizzo da verificare.</param>
        /// <param name="ipAddress">Oggetto IPAddress risultante dalla verifica.</param>
        /// <returns></returns>
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => TryParse(Match(address).Value, out ipAddress);

        #endregion
    }
}