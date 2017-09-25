using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class TCPServer
    {
        bool keepalive = true;
        Socket server;
        List<Socket> clients = new List<Socket>(), readList = new List<Socket>();

        public TCPServer()
        {
            Log.D("Creating TCP Server using port " + NetCore.TCPPort());
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            server.Bind(new IPEndPoint(IPAddress.Any, NetCore.TCPPort()));
            server.Listen(10);
            clients.Add(server);
        }

        public void Run()
        {
            int counter = 0;
            while (keepalive)
            {
                Log.D("TCP Server is on loop " + counter++ + ".");
                readList.Clear();
                readList.AddRange(clients);
                Socket.Select(readList, null, null, 10 * 1000 * 1000);
                foreach (Socket s in readList)
                {
                    if (s == server)
                    {
                        Log.D("TCP Server - New incoming connection.");
                        clients.Add(s.Accept());
                    }
                    else
                    {
                        byte[] recvd = new byte[512];
                        try
                        {
                            int num_recvd = s.Receive(recvd);
                            if (num_recvd == 0)
                            {
                                Log.D("Client disconnected.");
                                clients.Remove(s);
                            }
                            else
                            {
                                Log.D("TCP Server - Client sent message '" + Encoding.ASCII.GetString(recvd, 0, num_recvd) + "'");
                            }
                        }
                        catch
                        {
                            Log.D("Error.");
                            clients.Remove(s);
                        }
                    }
                }
                if (counter == 10) return;
            }
        }

        public void Stop()
        {
            server.Close();
            keepalive = false;
        }

    }
}
