using System.Net;
using System.Threading.Tasks;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilità varie legate alla connettività di rete.
    /// </summary>
    public class NetUtils
    {
        /// <summary>
        /// Verifica che <paramref name="host"/> sia raggiungibile.
        /// </summary>
        /// <param name="host">Host per cui verificare la risoluzione DNS.</param>
        /// <returns>
        /// Una tupla composta da:<br/>
        /// <b>Rachable</b>: True se l'host è stato risolto;<br/>
        /// <b>Addresses</b>: indirizzi con cui l'host è stato risolto.
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
