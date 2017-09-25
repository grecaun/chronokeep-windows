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
            int numbytes = socket.Receive(recvBuffer);
            String received = Encoding.ASCII.GetString(recvBuffer, 0, numbytes);
            Log.D("Received this from the server: '" + received + "'");
        }
    }
}
