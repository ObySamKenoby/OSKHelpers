using System.Net;
using System.Text.RegularExpressions;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Extends <see cref="IPAddress"/> with regex-based IPv4 extraction and combined parse-and-validate helpers.
    /// </summary>
    public class OSKIPAddress : IPAddress
    {
        #region Constants

        private const string IPMASK = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="OSKIPAddress"/> from a byte array.
        /// </summary>
        /// <param name="address">Byte array representing the IP address.</param>
        public OSKIPAddress(byte[] address) : base(address)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OSKIPAddress"/> from a long value.
        /// </summary>
        /// <param name="newAddress">Long value representing the IP address.</param>
        public OSKIPAddress(long newAddress) : base(newAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OSKIPAddress"/> from a byte array and a scope identifier.
        /// </summary>
        /// <param name="address">Byte array representing the IP address.</param>
        /// <param name="scopeid">Scope identifier for the IPv6 address.</param>
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
        /// <returns>True if a valid IP address was found and parsed.</returns>
        public static bool TryParseMatch(string address, out IPAddress ipAddress) => TryParse(Match(address).Value, out ipAddress);

        #endregion
    }
}