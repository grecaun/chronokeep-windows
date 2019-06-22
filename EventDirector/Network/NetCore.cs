using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    class NetCore
    {
        private static readonly int udpPort = 4488;
        private static readonly int tcpPort = GetAvailableTCPPort(4488, 5588);

        public static int UDPPort()
        {
            return udpPort;
        }

        public static int TCPPort()
        {
            return tcpPort;
        }

        public static int GetAvailableTCPPort(int start, int end)
        {
            Log.D("Getting TCP Port.");
            List<int> portArray = new List<int>();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                               where n.LocalEndPoint.Port >= start && n.LocalEndPoint.Port <= end
                               select n.LocalEndPoint.Port);
            System.Net.IPEndPoint[] endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= start && n.Port <= end
                               select n.Port);
            portArray.Sort();
            for (int i = start; i <= end; i++)
            {
                if (!portArray.Contains(i))
                {
                    Log.D("TCP Port is: " + i);
                    return i;
                }
            }
            return 0;
        }
    }
}
