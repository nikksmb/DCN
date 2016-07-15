using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DistributedComputingNetwork.Extensions;
using DistributedComputingNetwork.TaskStateMonitor;

namespace DistributedComputingNetwork.NetworkMonitor
{
    public class Monitor
    {
        public int Port { get; set; } = 17180;

        public int NewScanPeriod { get; set; } = 30000;
        public int ConnectionStateScanPeriod { get; set; } = 1500;
        public int ListenScanPeriod { get; set; } = 500;
        public int Timeout { get; set; } = 100;

        public TaskState ListenState { get; }
        public TaskState ConnectionState { get; }
        public TaskState ScanState { get; }

        private Thread listenThread;
        private Thread connectionStateThread;
        private Thread scanThread;

        public event EventHandler<ConnectionArgs> LostConnection = delegate { }; // if someone loses connection, send event
        public event EventHandler<ConnectionArgs> NewConnection = delegate { }; // on new connections

        private TcpListener listener; // listen to new connections

        public List<TcpClient> Sockets { get; } // all active connections
        public List<IPAddress> Addresses { get; } // IP addresses of active connections

        private object locking = new object(); // lock for lists
        
        /// <summary>
        /// Will program accept new connections
        /// </summary>
        private bool listen = false;
        /// <summary>
        /// will program scan for new network cells
        /// </summary>
        private bool networkScan = false;
        /// <summary>
        /// will program scan for network state
        /// </summary>
        private bool stateScan = false;

        public Monitor()
        {
            Sockets = new List<TcpClient>();
            Addresses = new List<IPAddress>();
            ListenState = new TaskState();
            ListenState.StateMessage = "Adding new connection";
            ScanState = new TaskState();
            ScanState.StateMessage = "Scanning";
            ConnectionState = new TaskState();
            ConnectionState.StateMessage = "Checking connection state";
        }

        public void Start()
        {
            StartListen();
            StartNetworkScan();
            StartStateScan();
        }

        public void Stop()
        {
            foreach (TcpClient client in Sockets)
            {
                client.Close();
                ConnectionArgs args = new ConnectionArgs(Addresses[Sockets.IndexOf(client)]);
                EventHandler<ConnectionArgs> handler = LostConnection;
                handler(this, args);
            }
            StopListen();
            StopNetworkScan();
            StopStateScan();
        }

        /// <summary>
        /// Listen all new connections, until calling StopListen.
        /// Ineffective when port is already listened.
        /// </summary>
        public void StartListen()
        {
            if (!listen)
            {
                listen = true;
                listenThread = new Thread(ConnectionAwait);
                listenThread.Start();
            }
        }

        public void StopListen()
        {
            if (listen)
            {
                listen = false;
                ListenState.Reset();
                listenThread.Abort();
            }
        }

        public void StartNetworkScan()
        {
            if (!networkScan)
            {
                networkScan = true;
                scanThread = new Thread(Scanner);
                scanThread.Start();
            }
        }

        public void StopNetworkScan()
        {
            if (networkScan)
            {
                networkScan = false;
                ScanState.Reset();
                scanThread.Abort();
            }
        }

        public void StartStateScan()
        {
            if (!stateScan)
            {
                stateScan = true;
                connectionStateThread = new Thread(ConnectionStateUpdater);
                connectionStateThread.Start();
            }
        }

        public void StopStateScan()
        {
            if (stateScan)
            {
                stateScan = false;
                ConnectionState.Reset();
                connectionStateThread.Abort();
            }
        }


        private IPAddress[] allAddressCache = new IPAddress[1];
        private IPAddress[] networkAddressesCache = new IPAddress[1]; 
        private IPAddress[] GetAvailableIpAddresses()
        {
            List<IPAddress> addresses = IPInfo.GetLocalIPAddresses();
            if (addresses.SequenceEqual(networkAddressesCache))
            {
                return allAddressCache;
            }
            networkAddressesCache = addresses.ToArray();
            List<IPAddress> allValidIpAddresses = new List<IPAddress>();
            foreach(IPAddress address in addresses)
            {
                allValidIpAddresses.AddRange(address.GetAllSubnetValidAddresses());
            }
            allAddressCache = allValidIpAddresses.ToArray();
            return allAddressCache;
        }


        private void ConnectionAwait(object o)
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            ListenState.MaxCount = 1;
            try
            {
                while (listen)
                {
                    ListenState.CurrentState = 0;
                    if (!listener.Pending())
                    {
                        ListenState.IsActive = false;
                        Thread.Sleep(ListenScanPeriod);
                        ListenState.IsActive = true;
                        continue;
                    }
                    TcpClient clientSocket = listener.AcceptTcpClient();
                    IPEndPoint ipep = (IPEndPoint)clientSocket.Client.RemoteEndPoint;
                    IPAddress ipa = ipep.Address; // getting IP address
                    lock (locking)
                    {
                        if (Addresses.Contains(ipa))
                            continue;
                        ListenState.CurrentState++;
                        Sockets.Add(clientSocket);
                        Addresses.Add(ipa);
                        ConnectionArgs args = new ConnectionArgs(ipa);
                        EventHandler<ConnectionArgs> handler = NewConnection;
                        handler(this, args);
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                listener.Stop();
            }
        }

        private void ConnectionStateUpdater(object o)
        {
            while (stateScan)
            {
                int i = 0;
                lock (locking)
                {
                    int[] numbers = new int[Sockets.Count];
                    ConnectionState.IsActive = true;
                    ConnectionState.MaxCount = Sockets.Count;
                    ConnectionState.CurrentState = 0;
                    foreach (TcpClient client in Sockets)
                    {
                        if (!client.IsConnected())
                        {
                            numbers[i] = Sockets.IndexOf(client);
                            ConnectionArgs args = new ConnectionArgs(Addresses[numbers[i]]);
                            EventHandler<ConnectionArgs> handler = LostConnection;
                            handler(this, args);
                            i++;
                        }
                        else
                            ConnectionState.CurrentState++;
                    }
                    if (i != 0)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            Sockets.RemoveAt(j);
                            Addresses.RemoveAt(j);
                            ConnectionState.CurrentState++;
                        }
                    }
                }
                ConnectionState.IsActive = false;
                Thread.Sleep(ConnectionStateScanPeriod);
            }
        }

        private void Scanner(object o)
        {
            List<IAsyncResult> results = new List<IAsyncResult>();
            while (networkScan)
            {
                ScanState.IsActive = true;
                results.Clear();
                IPAddress[] addresses = GetAvailableIpAddresses();
                //without existing
                IPAddress[] newAddresses = addresses.Except(Addresses).ToArray();
                ScanState.MaxCount = newAddresses.Length;
                ScanState.CurrentState = 0;
                foreach (IPAddress addr in newAddresses)
                {
                    TcpClient networkMember = new TcpClient();
                    try
                    {
                        IAsyncResult result = networkMember.Client.BeginConnect(addr, Port, ConnectResult, networkMember);
                        //networkMember.Connect(addresses[i], ListenerPort);
                        results.Add(result);
                    }
                    catch (SocketException e)
                    {
                        ScanState.CurrentState++;
                        //no active network cell
                    }
                }
                Parallel.ForEach(results, 
                    delegate(IAsyncResult res)
                    {
                        var success = res.AsyncWaitHandle.WaitOne(Timeout);
                        if (!success)
                            ScanState.CurrentState++;
                    });
                ScanState.IsActive = false;
                Thread.Sleep(NewScanPeriod);
            }
        }

        private void ConnectResult(IAsyncResult ar)
        {
            TcpClient client = ar.AsyncState as TcpClient;
            try
            {
                client.EndConnect(ar);
            }
            catch(SocketException)
            {
                ScanState.CurrentState++;
                return;
            }
            ScanState.CurrentState++;
            IPEndPoint ipep = (IPEndPoint)client.Client.RemoteEndPoint;
            IPAddress ipa = ipep.Address; // getting IP address
            lock (locking)
            {
                if (Addresses.Contains(ipa))
                    return;
                Sockets.Add(client); // putting TcpClient in list
                Addresses.Add(ipa);
            }
            ConnectionArgs args = new ConnectionArgs(ipa);
            EventHandler<ConnectionArgs> handler = NewConnection;
            handler(this, args);
        }

    }
}
