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

        #region Methods

        /// <summary>
        /// Returns, if present, the x.x.x.x quad contained in the string.<br/>
        /// The result is not validated; it is the caller's responsibility to do so.<br/>
        /// To obtain the resulting IPAddress object with validation, use <see cref="TryParseMatch"/>.
        /// </summary>
        /// <param name="address">String from which to extract the address.</param>
        public static Match Match(string address) => Regex.Match(address ?? string.Empty, IPMASK);

        /// <summary>
        /// Extracts, if present, the x.x.x.x quad from the string and returns the resulting IPAddress object.
        /// </summary>
        /// <param name="address">String containing the address to validate.</param>
        /// <param name="ipAddress">Resulting IPAddress object.</param>
        /// <returns></returns>
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => TryParse(Match(address).Value, out ipAddress);

        #endregion
    }
}