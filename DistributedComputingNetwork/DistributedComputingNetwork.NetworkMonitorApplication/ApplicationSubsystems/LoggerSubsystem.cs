using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.NetworkMonitorApplication.ApplicationSubsystems
{
    public class LoggerSubsystem:ISubsystem
    {
        private Form1 form;

        public LoggerSubsystem(Form1 form)
        {
            this.form = form;
        }

        public void PutAnswer(InformationType type, object data)
        {
            string result;
            if (type == InformationType.LogInfo)
            {
                result = $"{DateTime.Now}: {data}";
            }
            else
            {
                result = $"{DateTime.Now}: received {type}";
            }
            form.AddLog(result);
        }

        public object GetAnswer(InformationType requestType, object requestData)
        {
            string result = $"{DateTime.Now}: received {requestType}";
            form.AddLog(result);
            return null;
        }
    }
}
