using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.NetworkMonitorApplication.ApplicationSubsystems
{
    public class AssemblySubsystem:ISubsystem
    {
        public AssemblySubsystem()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
        }

        ~AssemblySubsystem()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveEventHandler;
        }

        public void PutAnswer(InformationType type, object data)
        {
            if (type != InformationType.Assembly)
            {
                return;
            }
            byte[] assembly = (byte[]) data;
            AppDomain.CurrentDomain.Load(assembly);
        }

        public object GetAnswer(InformationType requestType, object requestData)
        {
            return null;
        }

        private static Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly result = 
                AppDomain.CurrentDomain
                .GetAssemblies()
                .First(assembly => 
                assembly.FullName.Contains(args.Name));
            return result;
        }

    }
}
