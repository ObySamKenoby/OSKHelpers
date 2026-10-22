using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Miscellaneous network connectivity utilities.
    /// </summary>
    public class NetUtils
    {
        /// <summary>
        /// Checks whether <paramref name="host"/> is reachable.<br/>
        /// A host that cannot be resolved is reported as not reachable, without exceptions.
        /// </summary>
        /// <param name="host">Host for which to perform a DNS resolution.</param>
        /// <returns>
        /// A tuple containing:<br/>
        /// <b>Reachable</b>: true if the host was resolved;<br/>
        /// <b>Addresses</b>: the IP addresses the host was resolved to (empty if not resolved).
        /// </returns>
        public static (bool Reachable, IPAddress[] Addresses) CheckDNS(string host)
            => CheckDNSAsync(host).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc cref="CheckDNS(string)"/>
        public static async Task<(bool Reachable, IPAddress[] Addresses)> CheckDNSAsync(string host)
        {
            IPAddress[] addresses;

            try
            {
                addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                // The resolution failed (e.g. unknown host): the host is not reachable
                addresses = Array.Empty<IPAddress>();
            }

            return (addresses.Length > 0, addresses);
        }
    }
}
