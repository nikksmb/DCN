using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Configuration;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DistributedComputingNetwork.Extensions;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.NetworkDispatcher
{
    public class ConnectionDispatcher:IDispatcher, IDisposable
    {
        public TcpClient Connection { get; }
        
        private NetworkStream stream;
        private bool waitForInfo = true;

        public ConnectionDispatcher(TcpClient connection)
        {
            Connection = connection;
            stream = connection.GetStream();
            ThreadPool.QueueUserWorkItem(WaitForInfo);
            Dispatcher.AddDispatcher(this);
        }

        private bool disposed;
        ~ConnectionDispatcher()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                waitForInfo = false;
                stream.Dispose();
                Dispatcher.RemoveDispatcher(this);
                disposed = true;
            }
        }

        private void WaitForInfo(object o)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            AsyncCallback callBack = SendEvent;
            while(waitForInfo)
            {
                int size = Marshal.SizeOf(typeof(DataInfo));
                byte[] buffer = new byte[size];
                try
                {
                    IAsyncResult result = stream.BeginRead(buffer, 0, size, callBack, stream);
                    result.AsyncWaitHandle.WaitOne();
                }
                catch (IOException)
                {
                    break;
                }
                catch(ObjectDisposedException)
                {
                    Dispose();
                    return;
                }
                DataInfo info = new DataInfo(buffer);
                object data;
                if (info.TypeOfMessage != InformationType.NullInformation)
                {
                    data = formatter.Deserialize(stream);
                    SendToSubscribersAsync(info.TypeOfMessage, data);
                }
                else
                {
                 /*   byte[] clearBuf = new byte[4096];
                    while (stream.DataAvailable)
                    {
                        stream.Read(clearBuf, 0, clearBuf.Length);
                    }*/
                }
            }
        }

        public void WriteDataAsync(InformationType type, object data)
        {
            ThreadPool.QueueUserWorkItem(o => WriteData(type, data));
        }

        public void WriteData(InformationType type, object data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            if (Connection.IsConnected())
                lock(stream)
                {
                    NetworkPackage package = new NetworkPackage
                    {
                        Data = data,
                        SubsystemNumber = Dispatcher.Dispatchers.IndexOf(this)
                    };
                    DataInfo info = new DataInfo();
                    info.TypeOfMessage = type;
                    info.Size = 0; //!, will be 0 for now due using serialisation
                    byte[] bytes = info.ToBytes();
                    stream.Write(bytes, 0, bytes.Length);
                    formatter.Serialize(stream, package);
                }
            else
            {
                waitForInfo = false;
            }
        }

        private void SendEvent(IAsyncResult result)
        {
            //Console.WriteLine("SendEvent");
        }

        /// <summary>
        /// Async method to get request and send answer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        private void SendToSubscribersAsync(InformationType type, object data)
        {
            NetworkPackage package = data as NetworkPackage;
            if (package == null)
            {
                return;
            }
            List<ISubsystem> items = Dispatcher.NotificationLists[type];
            foreach (ISubsystem subsystem in items)
            {
                if (DataInfo.Response(type) != null)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        package.Data = subsystem.GetAnswer(type, package.Data);
                        if (package.Data != null)
                        {
                            SendAnswer(DataInfo.Response(type).Value, package);
                        }
                    });
                }
                else
                {
                    subsystem.PutAnswer(type, package.Data);
                    Dispatcher.Workload[this]--;
                }
            }
        }

        /// <summary>
        /// Serializes data into NetworkPackage and send it via network connection
        /// </summary>
        /// <param name="subsystem"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void SendRequest(ISubsystem subsystem, InformationType type, object data)
        {
            Dispatcher.AddSubsystem(subsystem);
            BinaryFormatter formatter = new BinaryFormatter();
            if (Connection.IsConnected())
                lock (stream)
                {
                    DataInfo info = new DataInfo();
                    info.TypeOfMessage = type;
                    info.Size = 0; //!, will be 0 for now due using serialisation
                    byte[] bytes = info.ToBytes();
                    stream.Write(bytes, 0, bytes.Length);
                    NetworkPackage package = new NetworkPackage
                    {
                        Data = data,
                        SubsystemNumber = Dispatcher.Subsystems.IndexOf(subsystem)
                    };
                    formatter.Serialize(stream, package);
                }
            else
            {
                waitForInfo = false;
                subsystem.PutAnswer(InformationType.LostConnection, data);
            }
        }

        /// <summary>
        /// Put NetworkPackage to corresponding stream
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void SendAnswer(InformationType type, object data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            if (Connection.IsConnected())
                lock (stream)
                {
                    DataInfo info = new DataInfo();
                    info.TypeOfMessage = type;
                    info.Size = 0; //!, will be 0 for now due using serialisation
                    byte[] bytes = info.ToBytes();
                    stream.Write(bytes, 0, bytes.Length);
                    formatter.Serialize(stream, data);
                }
            else
            {
                waitForInfo = false;
            }
        }
    }
}
