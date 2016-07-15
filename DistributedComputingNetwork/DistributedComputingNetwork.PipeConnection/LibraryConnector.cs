using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using DistributedComputingNetwork.MessageInfo;
using DistributedComputingNetwork.SubsystemInterfaces;
using Serialize.Linq.Serializers;

namespace DistributedComputingNetwork.PipeConnection
{
    public class LibraryConnector:IDisposable, ISubsystem
    {
        public int ReconnectTimeout { get; set; } = 100;
        public int AnswerAwaitTimeout { get; set; } = 60000;
        public int QueueCheckTimeout { get; set; } = 50;

        public bool ConnectionState { get; private set; }

        private string pipeName;
        private NamedPipeClientStream pipeClient;

        private Queue<RequestPackage> queue;

        private volatile int RequestIndex;
        private object inclock = new object();

        private Dictionary<int, AnswerItem> answers;
        private Thread queueThread;


        /// <summary>
        /// Default name - DistributedComputingNetwork
        /// </summary>
        public LibraryConnector():this("DistributedComputingNetwork")
        {
        }

        public LibraryConnector(string name)
        {
            RequestIndex = 0;
            pipeName = name;
            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            queueThread = new Thread(() => {
                try
                {
                    while (true)
                    {
                        Thread.Sleep(QueueCheckTimeout);
                        lock (answers)
                        {
                            if (ConnectionState && answers.Count != 0)
                            {
                                CheckForAnswers();
                            }
                        }
                    }
                }
                catch(ThreadAbortException)
                {

                }
            });
            queueThread.IsBackground = true;
            answers = new Dictionary<int, AnswerItem>();
            queue = new Queue<RequestPackage>();
            ConnectionState = false;
        }

        static LibraryConnector()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
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

        public bool Connect()
        {
            if (ConnectionState)
            {
                if (!pipeClient.IsConnected)
                {
                    ConnectionState = false;
                    return false;
                }
                return true;
            }
            try
            {
                pipeClient.Connect(ReconnectTimeout);

                Assembly assm = Assembly.GetEntryAssembly();
                //Assembly.GetExecutingAssembly();
                //Assembly.GetCallingAssembly();
                //Assembly.GetEntryAssembly();
                string pathToAssm = assm.Location;
                byte[] assmBytes = File.ReadAllBytes(pathToAssm);
                AppDomain.CurrentDomain.Load(assmBytes);
                
                WriteData(InformationType.Assembly, assmBytes);

                assm = Assembly.GetCallingAssembly();
                pathToAssm = assm.Location;
                assmBytes = File.ReadAllBytes(pathToAssm);
                AppDomain.CurrentDomain.Load(assmBytes);
                ConnectionState = true;
                queueThread.Start();
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        private Thread connectThread;

        public void ConnectAsync()
        {
            connectThread = new Thread(delegate()
            {
                while (!ConnectionState)
                {
                    Console.WriteLine("Pipe connection check");
                    if (!Connect())
                    {
                        Thread.Sleep(ReconnectTimeout*10);
                    }
                }
                Console.WriteLine("End pipe connection check");
            });
            connectThread.IsBackground = true;
            connectThread.Start();
        }

        private void WriteData(InformationType type, object data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            lock (pipeClient)
            {
                DataInfo info = new DataInfo();
                info.TypeOfMessage = type;
                info.Size = 0; //!, will be 0 for now due using serialisation
                byte[] bytes = info.ToBytes();
                try
                {
                    pipeClient.Write(bytes, 0, bytes.Length);
                }
                catch (IOException)
                {
                    ConnectionState = false;
                    return;
                }

                if (type != InformationType.QueueFlush)
                {
                    formatter.Serialize(pipeClient, data);
                }
            }
        }

        private void ReadQueue()
        {
            Console.WriteLine($"{DateTime.Now}: ReadQueue executing");
            WriteData(InformationType.QueueFlush, null);
            //now in read mode
            //need to read all objects from queue
            //until QueueFlush received
            BinaryFormatter formatter = new BinaryFormatter();
            int size = Marshal.SizeOf(typeof(DataInfo));
            byte[] buffer = new byte[size];
            IAsyncResult result;
            try
            {
                result = pipeClient.BeginRead(buffer, 0, size, null, pipeClient);
                result.AsyncWaitHandle.WaitOne();
                pipeClient.EndRead(result);
                DataInfo info = new DataInfo(buffer);
                if (info.TypeOfMessage != InformationType.QueueLength)
                {
                    return;
                }
                //read queue
                for (int i = 0; i<info.Size; i++)
                {
                    queue.Enqueue(formatter.Deserialize(pipeClient) as RequestPackage);
                }
            }
            catch (IOException)
            {
                ConnectionState = false;
            }
        }

        private void DispatchAnswers()
        {
            Console.WriteLine($"{DateTime.Now}: DispatchAnswers executing");
            foreach (RequestPackage package in queue)
            {
                answers[package.RequestId].Package = package;
                answers[package.RequestId].Semaphore.Release();
            }
            queue.Clear();
        }

        private bool disposed = false;
        public void Dispose()
        {
            Console.WriteLine("Dispose called");
            if (disposed)
            {
                return;
            }
            pipeClient?.Close();
            queueThread?.Abort();
            if (connectThread != null)
            {
                if (connectThread.IsAlive)
                {
                    connectThread.Abort();
                }
            }
            disposed = true;
            Console.WriteLine("Dispose finished");
        }

        /// <summary>
        /// This subsystem is not going to get any request; always throws InvalidOperationException
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void PutAnswer(InformationType type, object data)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Call from calculation dispatcher to get answer on request
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="requestData">Serialized request</param>
        /// <returns></returns>
        public object GetAnswer(InformationType requestType, object requestData)
        {
            if (requestData is Expression)
            {
                BinarySerializer binarySerializer = new BinarySerializer();
                ExpressionSerializer serializer = new ExpressionSerializer(binarySerializer);
                byte[] bytes = serializer.SerializeBinary((Expression)requestData);
                requestData = bytes;
            }
            RequestPackage package = new RequestPackage();
            AnswerItem item;
            Semaphore answer;
            lock (inclock)
            {
                package.RequestId = RequestIndex;
                RequestIndex++;
                package.Data = requestData;
                item = new AnswerItem();
                answer = new Semaphore(0, 1);
                item.Semaphore = answer;
                item.Package = null;
                answers.Add(package.RequestId, item);
            }
            Console.WriteLine($"{DateTime.Now} request {package.RequestId}");
            WriteData(requestType, package);
            //timeouts?
            bool awaitResult = answer.WaitOne(AnswerAwaitTimeout);
            if (!awaitResult)
            {
                //work with timeout
                Console.WriteLine($"{DateTime.Now}: request {package.RequestId} is timeout");
                lock (answers)
                {
                    answers.Remove(package.RequestId);
                }
                return null;
            }
            //got answer
            Console.WriteLine($"{DateTime.Now}: request {package.RequestId} got answer");
            if (package.Equals(answers[package.RequestId].Package))
            {
                lock (answers)
                {
                    answers.Remove(package.RequestId);
                }
                return null;
            }
            package = answers[package.RequestId].Package;
            lock (answers)
            {
                answers.Remove(package.RequestId);
            }
            return package.Data;
        }

        public bool CheckForAnswers()
        {
            if (!ConnectionState)
            {
                return false;
            }
            ReadQueue();
            if (queue.Any())
            {
                DispatchAnswers();
                return true;
            }
            return false;
        }

        ~LibraryConnector()
        {
            Console.WriteLine("Destructor called");
            if (!disposed)
            {
                Dispose();
            }
        }
    }

    internal class AnswerItem
    {
        public RequestPackage Package;
        public Semaphore Semaphore;
    }
}
