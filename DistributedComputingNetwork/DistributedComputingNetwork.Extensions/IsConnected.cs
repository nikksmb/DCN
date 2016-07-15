using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DistributedComputingNetwork.Extensions
{
    public static class TcpClientExtension
    {
        public static bool IsConnected(this TcpClient clientSocket)
        {
            if (clientSocket == null)
                return false;
            if (!clientSocket.Connected)
                return false;
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = null;
            try
            {
                tcpConnections = ipProperties.
                    GetActiveTcpConnections();
            }
            catch(NetworkInformationException e)
            {
                return false;
            }
            tcpConnections = tcpConnections.
                Where(x => x.LocalEndPoint.
                    Equals(clientSocket.Client.LocalEndPoint)
                            && x.RemoteEndPoint.
                                Equals(clientSocket.Client.RemoteEndPoint)).ToArray();
            if (tcpConnections != null && tcpConnections.Length > 0)
            {
                TcpState stateOfConnection = tcpConnections.First().State;
                if (stateOfConnection == TcpState.Established)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
    }
}
