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

        public void Run()
        {
            udpClient = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, NetCore.GetUDPPort());
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(endPoint);

            string received_data;
            byte[] receive_byte_array;

            int counter = 0;
            while (keepAlive == true)
            {
                Log.D("Loop number " + counter++ + ". WEEEEEEEEE.");
                try
                {
                    receive_byte_array = udpClient.Receive(ref endPoint);
                    received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                    Log.D(String.Format("Received broadcast from '{0}' with data '{1}'", endPoint.ToString(), received_data));
                }
                catch
                {
                    Log.E("Unable to read incoming.");
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
    }
}
