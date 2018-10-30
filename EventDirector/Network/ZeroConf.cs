using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class ZeroConf
    {
        bool keepAlive = true;
        UdpClient udpClient;
        String servername = "Northwest Endurance Events";
        public static String serverid;

        public ZeroConf(string name)
        {
            Char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            Char[] serverid_chars = new Char[10];
            Random rng = new Random();
            for (int i=0; i<serverid_chars.Length; i++)
            {
                serverid_chars[i] = chars[rng.Next(0, chars.Length)];
            }
            servername = name;
            serverid = new String(serverid_chars);
            Log.D("Server name is " + servername + " and has an id of " + serverid + ".");
        }

        public void Run()
        {
            udpClient = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, NetCore.UDPPort());
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(endPoint);

            string received_data;
            byte[] receive_byte_array;

            int counter = 0;
            while (keepAlive == true)
            {
                Log.D(counter++ + " clients have contacted me.");
                try
                {
                    receive_byte_array = udpClient.Receive(ref endPoint);
                    received_data = Encoding.UTF8.GetString(receive_byte_array, 0, receive_byte_array.Length);
                    Log.D(String.Format("Received broadcast from '{0}' with data '{1}'", endPoint.ToString(), received_data));
                    if (received_data == "[DISCOVER_EDSERVER_REQUEST]")
                    {
                        byte[] out_data = Encoding.UTF8.GetBytes("[" + servername + "|" + serverid + "|" + NetCore.TCPPort() + "]");
                        udpClient.Send(out_data, out_data.Length, endPoint);
                    }
                }
                catch
                {
                    Log.E("Shutting down.");
                }
            }
        }

        public void Stop()
        {
            Log.D("I've been shot! I repeat, ZeroConf has been shot! Those bastards!");
            keepAlive = false;
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        public void SetName(String name)
        {
            servername = name;
        }
    }
}
