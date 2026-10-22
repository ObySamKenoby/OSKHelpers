using OSKHelpers.Common;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace OSKHelpers.ExtensionMethods
{
    /// <summary>
    /// Extension methods that expose <see cref="OSKIPAddress"/> functionality directly on <see cref="IPAddress"/>.
    /// </summary>
    public static class IPAddressExtensionMethods
    {
        /// <summary>
        /// Returns, if present, the x.x.x.x quartet within the string.
        /// The result is not validated; it is the caller's responsibility to do so.
        /// </summary>
        /// <param name="ipAddress">Reference to object.</param>
        /// <param name="address">String from which to extract the address.</param>
        public static Match Match(this IPAddress ipAddress, string address) => OSKIPAddress.Match(address);

        /// <summary>
        /// Retrieves, if present, the x.x.x.x quartet within the string and returns the resulting IPAddress object.
        /// </summary>
        /// <param name="address">String containing the address to validate.</param>
        /// <param name="ipAddress">Resulting IPAddress object.</param>
        /// <returns>True if a valid IP address was found and parsed.</returns>
        [Obsolete("This is not an extension method: use OSKIPAddress.TryParseMatch or the TryParseMatch extension method on IPAddress.")]
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => OSKIPAddress.TryParseMatch(address, out ipAddress);

        /// <summary>
        /// Retrieves, if present, the x.x.x.x quartet within the string and returns the resulting IPAddress object.<br/>
        /// The instance the method is called on is not used, as in <see cref="Match(IPAddress, string)"/>.
        /// </summary>
        /// <param name="ipAddress">Reference to object.</param>
        /// <param name="address">String containing the address to validate.</param>
        /// <param name="result">Resulting IPAddress object.</param>
        /// <returns>True if a valid IP address was found and parsed.</returns>
        public static bool TryParseMatch(this IPAddress ipAddress, string address, out IPAddress result) => OSKIPAddress.TryParseMatch(address, out result);

    }
}
