using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.NetworkDispatcher;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.NetworkMonitorApplication.ApplicationSubsystems
{
    public class TextMessageSubsystem : ISubsystem
    {
        private Form1 form;

        public TextMessageSubsystem(Form1 form)
        {
            Dispatcher.AddNotification(this, InformationType.TextMessage);
            this.form = form;
        }

        public void SendMessage(IDispatcher dispatcher, string message)
        {
            dispatcher.SendRequest(this, InformationType.TextMessage, message);
        }

        public object GetAnswer(InformationType requestType, object requestData)
        {
            return "GetAnswer was called";
        }

        public void PutAnswer(InformationType type, object data)
        {
            form.ShowTextMessage($@"Received message: ""{data}""");
        }
    }
}
