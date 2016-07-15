using System;
using System.Collections.Generic;

namespace DistributedComputingNetwork.NetworkInformationContainer
{
    [Serializable]
    public class NetworkInformation
    {
        public List<NetworkCellInformation> NetworkInfo { get; } = new List<NetworkCellInformation>();
    }
}
