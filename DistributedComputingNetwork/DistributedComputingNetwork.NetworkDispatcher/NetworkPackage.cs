using System;

namespace DistributedComputingNetwork.NetworkDispatcher
{
    [Serializable]
    public class NetworkPackage
    {
        public int SubsystemNumber { get; set; }
        public object Data { get; set; }
    }
}
