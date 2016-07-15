using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DistributedComputingNetwork.Extensions
{
    public static class IPInfo
    {
        public static List<IPAddress> GetLocalIPAddresses()
        {
            List<IPAddress> result = new List<IPAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    result.Add(ip);
                }
            }
            return result;
        }


    }
}
