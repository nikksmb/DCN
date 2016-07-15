using System;

namespace DistributedComputingNetwork.PipeConnection
{
    [Serializable]
    public class RequestPackage
    {
        public int RequestId { get; set; }
        public object Data { get; set; }
    }
}
