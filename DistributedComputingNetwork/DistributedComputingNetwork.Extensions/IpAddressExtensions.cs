using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedComputingNetwork.Extensions
{
    public static class IpAddressExtensions
    {
        public static IPAddress GetSubnetMask(this IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public static bool PingHost(this IPAddress address, int timeout)
        {
            bool pingable = false;
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(address, timeout);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            return pingable;
        }

        public static List<IPAddress> GetAllSubnetValidAddresses(this IPAddress subnetLocalAddress)
        {
            IPAddress subnetMask = subnetLocalAddress.GetSubnetMask();
            byte[] address = subnetLocalAddress.GetAddressBytes();
            byte[] mask = subnetMask.GetAddressBytes();
            int maskLength = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] == 255)
                    continue;
                if (mask[i] == 0)
                {
                    maskLength += 8;
                    continue;
                }
                for (int j = 0; j < 8; j++)
                {
                    if ((mask[i] & (1 << j)) == 0)
                    {
                        maskLength++;
                    }
                }
            }
            //zeroing address
            for (int i = 0; i < address.Length; i++)
                address[i] = (byte)(address[i] & mask[i]);
            int addressCount = 1 << maskLength;
            List<IPAddress> allAddresses = new List<IPAddress>();
            for (int i = 0; i < addressCount; i++)
            {
                address.Increment();
                if (subnetLocalAddress.GetAddressBytes().SequenceEqual(address)) continue;
                if (address[address.Length - 1] == 0 || address[address.Length - 1] == 255) continue;
                allAddresses.Add(new IPAddress(address));
            }
            return allAddresses;
        }

    }
}
