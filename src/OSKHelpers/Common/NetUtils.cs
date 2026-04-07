using System.Net;
using System.Threading.Tasks;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Miscellaneous network connectivity utilities.
    /// </summary>
    public class NetUtils
    {
        /// <summary>
        /// Checks whether <paramref name="host"/> is reachable.
        /// </summary>
        /// <param name="host">Host for which to perform a DNS resolution.</param>
        /// <returns>
        /// A tuple containing:<br/>
        /// <b>Reachable</b>: true if the host was resolved;<br/>
        /// <b>Addresses</b>: the IP addresses the host was resolved to.
        /// </returns>
        public static (bool Reachable, IPAddress[] Addresses) CheckDNS(string host)
        {
            return CheckDNSAsync(host).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="CheckDNS(string)"/>
        public static async Task<(bool Reachable, IPAddress[] Addresses)> CheckDNSAsync(string host)
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            var reachable = addresses.Length > 0;
            return (reachable, addresses);
        }


    }
}
