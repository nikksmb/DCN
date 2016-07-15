using System;

namespace DistributedComputingNetwork.MessageInfo
{
    public class DispatchArgs:EventArgs
    {
        public DataInfo Info { get; }
        public object Data { get; } 

        public DispatchArgs(DataInfo info, object data)
        {
            Info = info;
            Data = data;
        }
    }
}
