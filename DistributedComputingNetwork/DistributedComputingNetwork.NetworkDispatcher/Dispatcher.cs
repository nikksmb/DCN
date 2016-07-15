using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.NetworkDispatcher
{
    public static class Dispatcher
    {
        public static List<ConnectionDispatcher> Dispatchers { get; }
        public static List<ISubsystem> Subsystems { get; } = new List<ISubsystem>();
        public static Dictionary<InformationType, List<ISubsystem>> NotificationLists { get; }
        public static Dictionary<ConnectionDispatcher,int> Workload;
        public static ISubsystem Logger;

        private static List<byte[]> assemblies; 

        static Dispatcher()
        {
            Dispatchers = new List<ConnectionDispatcher>();
            NotificationLists = new Dictionary<InformationType, List<ISubsystem>>();
            Workload = new Dictionary<ConnectionDispatcher, int>();
            assemblies = new List<byte[]>();
            foreach (InformationType type in Enum.GetValues(typeof(InformationType)))
            {
                List<ISubsystem> list = new List<ISubsystem>();
                NotificationLists.Add(type, list);
            }
        }

        public static void AddDispatcher(ConnectionDispatcher dispatcher)
        {
            Dispatchers.Add(dispatcher);
            Workload.Add(dispatcher,0);
            foreach(byte[] assm in assemblies)
            {
                dispatcher.WriteData(InformationType.Assembly, assm);
            }
        }

        public static void RemoveDispatcher(ConnectionDispatcher dispatcher)
        {
            Dispatchers.Remove(dispatcher);
            Workload.Remove(dispatcher);
        }

        public static void SendRequest(ISubsystem subsystem, InformationType type, object data)
        {
            if (!Dispatchers.Any())
            {
                if (DataInfo.Response(type) != null)
                {
                    foreach (ISubsystem subss in NotificationLists[type])
                    {
                        object answer = subss.GetAnswer(type, data);
                        if (answer != null)
                        {
                            subsystem.PutAnswer(DataInfo.Response(type).Value, answer);
                        }
                    }
                }
                return;
            }
            ConnectionDispatcher dispatcher = Workload.Keys.OrderBy(connectionDispatcher => Workload[connectionDispatcher]).First();
            Workload[dispatcher]++;
            dispatcher.SendRequest(subsystem, type, data);
        }

        public static void AddAssembly(byte[] assembly)
        {
            if (assemblies.Any(assm => assm.SequenceEqual(assembly)))
            {
                return;
            }
            assemblies.Add(assembly);
            AppDomain.CurrentDomain.Load(assembly);
            foreach (ConnectionDispatcher dispatcher in Dispatchers)
            {
                dispatcher.WriteData(InformationType.Assembly, assembly);
            }
        }

        public static void AddSubsystem(ISubsystem subsystem)
        {
            if (!Subsystems.Contains(subsystem))
            {
                Subsystems.Add(subsystem);
            }
        }

        public static void AddNotification(ISubsystem subsystem, InformationType type)
        {
            if (!NotificationLists[type].Contains(subsystem))
            {
                NotificationLists[type].Add(subsystem);
            }
        }

        public static void RemoveNotification(ISubsystem subsystem, InformationType type)
        {
            if (NotificationLists[type].Contains(subsystem))
            {
                NotificationLists[type].Remove(subsystem);
            }
        }
    }
}
