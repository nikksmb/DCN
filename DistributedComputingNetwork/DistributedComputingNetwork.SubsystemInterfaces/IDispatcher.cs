using DistributedComputingNetwork.MessageInfo;

namespace DistributedComputingNetwork.SubsystemInterfaces
{
    public interface IDispatcher
    {
        /// <summary>
        /// Will be called from ISubsystem class in order to send some information
        /// </summary>
        /// <param name="subsystem"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        void SendRequest(ISubsystem subsystem, InformationType type, object data);

        void SendAnswer(InformationType type, object data);
    }
}
