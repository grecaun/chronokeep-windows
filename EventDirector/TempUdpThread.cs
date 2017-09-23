using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.EnableBroadcast = true;
            socket.DontFragment = true;

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, NetCore.UDPPort);
            byte[] sendBuffer = Encoding.ASCII.GetBytes("This is test number "+num);
            Log.D("Temp thread number " + num + " blasting off.");
            socket.SendTo(sendBuffer, groupEP);
        }
    }
}
