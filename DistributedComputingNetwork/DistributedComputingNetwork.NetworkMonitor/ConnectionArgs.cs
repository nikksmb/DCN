using System;
using System.Net;

namespace DistributedComputingNetwork.NetworkMonitor
{
    public sealed class ConnectionArgs : EventArgs
    {
        public IPAddress Address { get; private set; }

        public ConnectionArgs(IPAddress address)
        {
            Address = address;
        }
    }
}
