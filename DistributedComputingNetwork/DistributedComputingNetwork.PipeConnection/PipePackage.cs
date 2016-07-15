using System;

namespace DistributedComputingNetwork.PipeConnection
{
    [Serializable]
    public class PipePackage
    {
        public int ThreadId { get; set; }
        public object Data { get; set; }
    }
}
