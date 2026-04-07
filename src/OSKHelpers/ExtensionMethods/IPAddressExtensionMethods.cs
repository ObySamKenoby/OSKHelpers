using OSKHelpers.Common;
using System.Net;
using System.Text.RegularExpressions;

namespace OSKHelpers.ExtensionMethods
{
    public static class IPAddressExtensionMethods
    {
        /// <summary>
        /// Returns, if present, the x.x.x.x quartet within the string.
        /// The result is not validated; it is the caller's responsibility to do so.
        /// </summary>
        /// <param name="address">String from which to extract the address.</param>
        public static Match Match(this IPAddress ipAddress, string address) => OSKIPAddress.Match(address);

        /// <summary>
        /// Retrieves, if present, the x.x.x.x quartet within the string and returns the resulting IPAddress object.
        /// </summary>
        /// <param name="address">String containing the address to validate.</param>
        /// <param name="ipAddress">Oggetto IPAddress risultante dalla verifica.</param>
        /// <returns></returns>
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => OSKIPAddress.TryParse(OSKIPAddress.Match(address).Value, out ipAddress);

    }
}
