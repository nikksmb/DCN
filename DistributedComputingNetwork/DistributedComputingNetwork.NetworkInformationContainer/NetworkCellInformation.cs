using System.Net;

namespace DistributedComputingNetwork.NetworkInformationContainer
{
    public struct NetworkCellInformation
    {
        public IPAddress Address;
        public int Delay;
        public int Port;
        public int Speed;
    }
}
