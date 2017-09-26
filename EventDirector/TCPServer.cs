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
                        Socket newSock = s.Accept();
                        clients.Add(newSock);
                    }
                    else
                    {
                        byte[] recvd = new byte[2056];
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
                                String msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                                Log.D("TCP Server - Client sent message '" + msg + "'");
                                try
                                {
                                    Log.D("Sending message to client.");
                                    s.Send(Encoding.UTF8.GetBytes("[Hello friend, just letting you know that I received this '" + msg + "'.]"));
                                }
                                catch
                                {
                                    Log.D("Client disconnected.");
                                    clients.Remove(s);
                                }
                            }
                        }
                        catch (SocketException se)
                        {
                            Log.D("Appears the client has disconnected.");
                            clients.Remove(s);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            server.Close();
            keepalive = false;
        }

    }
}
