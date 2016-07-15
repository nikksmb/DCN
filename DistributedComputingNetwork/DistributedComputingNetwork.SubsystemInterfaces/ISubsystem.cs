using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedComputingNetwork.MessageInfo;

namespace DistributedComputingNetwork.SubsystemInterfaces
{
    public interface ISubsystem
    {
        void PutAnswer(InformationType type, object data);
        object GetAnswer(InformationType requestType, object requestData);
    }
}
