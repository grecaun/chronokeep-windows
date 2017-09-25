using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EventDirector
{
    class TempUdpThread
    {
        int num;

        public TempUdpThread(int num)
        {
            this.num = num;
        }

        public void Run()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true,
                DontFragment = true
            };

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, NetCore.UDPPort());
            byte[] sendBuffer = Encoding.ASCII.GetBytes("This is test number "+num);
            Log.D("Temp thread number " + num + " blasting off.");
            socket.SendTo(sendBuffer, groupEP);
            byte[] recvBuffer = new byte[256];
            EndPoint server = new IPEndPoint(IPAddress.Any, 0);
            int numbytes = socket.ReceiveFrom(recvBuffer, ref server);
            String received = Encoding.ASCII.GetString(recvBuffer, 0, numbytes);

            string[] msgs = received.Trim('[').Trim(']').Split('|');
            Log.D("Server name is '" + msgs[0] + "' it's identifier is '" + msgs[1] + "' and port number is '" + msgs[2] + "'");
            int.TryParse(msgs[2], out int tcpPort);
            EndPoint serverCon = new IPEndPoint(((IPEndPoint)server).Address, tcpPort);
            Log.D("This is the server we're trying to connect to: " + serverCon.ToString());
            Socket tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcp.Connect(serverCon);
            Thread.Sleep(5000);
            tcp.Send(Encoding.ASCII.GetBytes("This is a tcp test from tester number " + num));
            Thread.Sleep(5000);
            tcp.Close();
        }
    }
}
