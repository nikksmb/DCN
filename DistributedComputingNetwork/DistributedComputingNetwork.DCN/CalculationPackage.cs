using System;

namespace DistributedComputingNetwork.DCN
{
    [Serializable]
    public class CalculationPackage
    {
        public object Func;
        public object Argument;
    }
}
