using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.NetworkDispatcher;
using DistributedComputingNetwork.SubsystemInterfaces;

namespace DistributedComputingNetwork.PipeConnection
{
    public class ApplicationConnector:IDisposable, ISubsystem
    {
        public static int LibCount { get; set; } = 10;
        public static int QueueLength { get; set; } = 100;
        public int Timeout { get; set; } = 100;
        private bool started;
        private bool active;

        private string inputPipeName;

        private NamedPipeServerStream[] streams;

        private Thread[] servers;
        private Queue<object>[] writeQueues; 

        /// <summary>
        /// Default name - DistributedComputingNetwork
        /// </summary>
        public ApplicationConnector():this("DistributedComputingNetwork")
        {
        }

        public ApplicationConnector(string name)
        {
            inputPipeName = name;
            Host();
        }

        private void Host()
        {
            started = true;
            servers = new Thread[LibCount];
            writeQueues = new Queue<object>[LibCount];
            for (int i = 0; i < LibCount; i++)
            {
                servers[i] = new Thread(ServerInputThread);
                servers[i].Start();
                writeQueues[i] = new Queue<object>(QueueLength);
            }
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    while (started)
                    {
                        for (int i = 0; i < LibCount; i++)
                        {
                            if (!servers[i].IsAlive)
                            {
                                servers[i] = new Thread(ServerInputThread);
                            }
                        }
                        Thread.Sleep(Timeout);
                    }
                }
                catch
                {

                }
            });
        }

        private void ServerInputThread()
        {
            //semaphore
            NamedPipeServerStream pipeServer =
                    new NamedPipeServerStream(inputPipeName, PipeDirection.InOut, LibCount, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            int threadIndex = Array.IndexOf(servers, Thread.CurrentThread);
            try
            {
                pipeServer.WaitForConnectionAsync();
                while (!pipeServer.IsConnected)
                {
                    Thread.Sleep(Timeout);
                }
                BinaryFormatter formatter = new BinaryFormatter();
                active = true;
                while (active)
                {
                    if (!pipeServer.IsConnected)
                    {
                        break;
                    }
                    try
                    {
                        int size = Marshal.SizeOf(typeof(DataInfo));
                        byte[] buffer = new byte[size];
                    
                        //read requests from library
                        AsyncCallback callBack = SendEvent;
                        IAsyncResult result;
                        try
                        {
                            result = pipeServer.BeginRead(buffer, 0, size, callBack, pipeServer);
                            result.AsyncWaitHandle.WaitOne();
                        }
                        catch (IOException)
                        {
                            break;
                        }
                        //pipeServer.EndRead(result);
                        DataInfo info = new DataInfo(buffer);
                        object data;
                        if (info.TypeOfMessage == InformationType.QueueFlush)
                        {
                            //first - send count of items in queue
                            info.TypeOfMessage = InformationType.QueueLength;
                            info.Size = writeQueues[threadIndex].Count;
                            pipeServer.Write(info.ToBytes(), 0, size);
                            int queueCount = info.Size;
                            while (true)
                            {
                                //then dequeue all answers
                                if (queueCount <= 0)
                                {
                                    break;
                                }
                                data = writeQueues[threadIndex].Dequeue();
                                formatter.Serialize(pipeServer, data);
                                queueCount--;
                            }
                            continue;
                        }
                        if (info.TypeOfMessage == InformationType.Assembly)
                        {
                            data = formatter.Deserialize(pipeServer);
                            Dispatcher.AddAssembly((byte[])data);
                            continue;
                        }
                        if (info.TypeOfMessage != InformationType.NullInformation)
                        {
                            try
                            {
                                data = formatter.Deserialize(pipeServer);
                            }
                            catch(SerializationException)
                            {
                                continue;
                            }
                            PipePackage package = new PipePackage
                            {
                                ThreadId = threadIndex,
                                Data = data
                            };
                            Dispatcher.SendRequest(this, info.TypeOfMessage, package);
                        }
                        else
                        {
                            pipeServer.Flush();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                Console.WriteLine("Thread was terminated: {0}", e.Message);
            }
            finally
            {
                pipeServer.Dispose();
            }
        }
        
        private void SendEvent(IAsyncResult result)
        {
            //Console.WriteLine("SendEvent");
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            active = false;
            for (int i = 0; i < LibCount; i++)
            {
                servers[i].Abort();
            }
            servers = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            started = false;
            disposed = true;
        }

        public void Write(InformationType type, object data)
        {
            PipePackage package = data as PipePackage;
            if (package != null)
            {
                lock (writeQueues[package.ThreadId])
                {
                    writeQueues[package.ThreadId].Enqueue(package.Data);
                }
                return;
            }
            throw new InvalidOperationException("Package does not belong to this subsystem");
        }

        ~ApplicationConnector()
        {
            Dispose();
        }

        /// <summary>
        /// Returns answer to request
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void PutAnswer(InformationType type, object data)
        {
            Write(type, data);
        }

        /// <summary>
        /// Will return empty package
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="requestData"></param>
        /// <returns></returns>
        public object GetAnswer(InformationType requestType, object requestData)
        {
            return new PipePackage();
        }
    }
}

