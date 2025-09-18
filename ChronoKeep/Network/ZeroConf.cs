using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chronokeep.Network
{
    class ZeroConf
    {
        bool keepAlive = true;
        UdpClient udpClient;
        string servername;
        public static string serverid;

        private static bool running = false;

        public ZeroConf(string name)
        {
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            char[] serverid_chars = new char[10];
            Random rng = new Random();
            for (int i = 0; i < serverid_chars.Length; i++)
            {
                serverid_chars[i] = chars[rng.Next(0, chars.Length)];
            }
            servername = name != null ? name : Constants.Network.DEFAULT_CHRONOKEEP_SERVER_NAME;
            serverid = new string(serverid_chars);
            Log.D("Network.ZeroConf", "Server name is " + servername + " and has an id of " + serverid + ".");
        }

        public void Run()
        {
            running = true;
            udpClient = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Constants.Network.CHRONOKEEP_ZCONF_PORT);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPAddress multicastAddr = IPAddress.Parse(Constants.Network.CHRONOKEEP_ZCONF_MULTICAST_IP);
            udpClient.JoinMulticastGroup(multicastAddr);
            udpClient.Client.Bind(endPoint);

            string received_data;
            byte[] receive_byte_array;

            int counter = 0;
            while (keepAlive == true)
            {
                Log.D("Network.ZeroConf", counter++ + " clients have contacted me.");
                try
                {
                    receive_byte_array = udpClient.Receive(ref endPoint);
                    received_data = Encoding.UTF8.GetString(receive_byte_array, 0, receive_byte_array.Length).Trim();
                    Log.D("Network.ZeroConf", string.Format("Received broadcast from '{0}' with data '{1}'", endPoint.ToString(), received_data));
                    if (received_data.Equals(Constants.Network.CHRONOKEEP_ZCONF_CONNECT_MSG, StringComparison.OrdinalIgnoreCase))
                    {
                        string outString = string.Format("[{0}|{1}|{2}]", servername, serverid, NetCore.TCPPort());
                        byte[] out_data = Encoding.UTF8.GetBytes(outString);
                        Log.D("Network.ZeroConf", string.Format("Sending '{0}'", outString));
                        udpClient.Send(out_data, out_data.Length, endPoint);
                    }
                }
                catch
                {
                    Log.E("Network.ZeroConf", "Exception thrown - Shutting down.");
                }
            }
            running = false;
        }

        public void Stop()
        {
            Log.D("Network.ZeroConf", "Zero Conf is instructed to stop.");
            keepAlive = false;
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        public bool IsRunning()
        {
            return running;
        }

        public void SetName(string name)
        {
            servername = name;
        }
    }
}