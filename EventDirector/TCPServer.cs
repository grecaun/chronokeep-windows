using Newtonsoft.Json.Linq;
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
        JsonHandler jsonHandler;

        public TCPServer(IDBInterface database)
        {
            jsonHandler = new JsonHandler(database);
        }

        public void Run()
        {
            Log.D("Creating TCP Server using port " + NetCore.TCPPort());
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            server.Bind(new IPEndPoint(IPAddress.Any, NetCore.TCPPort()));
            server.Listen(10);
            clients.Add(server);
            int counter = 0;
            while (keepalive)
            {
                Log.D("TCP Server is on loop " + counter++ + ".");
                readList.Clear();
                readList.AddRange(clients);
                Socket.Select(readList, null, null, 60 * 1000 * 1000);
                foreach (Socket sock in readList)
                {
                    if (sock == server)
                    {
                        Log.D("TCP Server - New incoming connection.");
                        Socket newSock = sock.Accept();
                        clients.Add(newSock);
                    }
                    else
                    {
                        byte[] recvd = new byte[2056];
                        try
                        {
                            int num_recvd = sock.Receive(recvd);
                            if (num_recvd == 0)
                            {
                                Log.D("Client disconnected.");
                                clients.Remove(sock);
                            }
                            else
                            {
                                String msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                                Log.D("TCP Server - Client sent message '" + msg + "'");
                                ProcessMessage(msg, sock);
                            }
                        }
                        catch
                        {
                            Log.D("Appears the client has disconnected.");
                            clients.Remove(sock);
                        }
                    }
                }
            }
        }

        private void ProcessMessage(String msg, Socket sock)
        {
            List<JObject> possibleMatches = jsonHandler.ParseJsonMessage(msg);
            if (possibleMatches == null) { return; }
            foreach (JObject jsonObject in possibleMatches)
            {
                switch (jsonObject["Command"].ToString())
                {
                    case "client_authenticate":
                        Log.D("Request to authenticate client.");
                        break;
                    case "client_list":
                        Log.D("Request for event list.");
                        SendJson(jsonHandler.GetJsonServerEventList(), sock);
                        break;
                    case "client_event":
                        Log.D("Client event received.");
                        JsonClientEvent clientEvent = jsonObject.ToObject<JsonClientEvent>();
                        SendJson(jsonHandler.GetJsonServerEvent(clientEvent.EventId), sock);
                        break;
                    case "client_participants":
                        Log.D("Client participants received.");
                        JsonClientParticipants clientParts = jsonObject.ToObject<JsonClientParticipants>();
                        SendJson(jsonHandler.GetJsonServerParticipants(clientParts.EventId), sock);
                        break;
                    case "client_results":
                        Log.D("Client results received.");
                        JsonClientResults clientResults = jsonObject.ToObject<JsonClientResults>();
                        SendJson(jsonHandler.GetJsonServerResults(clientResults.EventId), sock);
                        break;
                    case "client_participant_update":
                        Log.D("Client participant update received.");
                        JsonClientParticipantUpdate clientPartUpd = jsonObject.ToObject<JsonClientParticipantUpdate>();
                        // UPDATE PARTICIPANT IN DATABASE
                        // BROADCAST PARTICIPANT UPDATE
                        break;
                    case "client_participant_add":
                        Log.D("Client participant add received.");
                        JsonClientParticipantAdd clientPartAdd = jsonObject.ToObject<JsonClientParticipantAdd>();
                        // ADD NEW PARTICIPANT
                        // BROADCAST NEW PARTICIPANT
                        break;
                    case "client_participant_set":
                        Log.D("Client participant set received.");
                        JsonClientParticipantSet clientPartSet = jsonObject.ToObject<JsonClientParticipantSet>();
                        jsonHandler.HandleJsonClientParticipantSet(clientPartSet);
                        BroadcastJson(jsonHandler.GetJsonServerSetParticipant(clientPartSet.EventId, clientPartSet.ParticipantId, clientPartSet.Value));
                        break;
                }
            }
        }

        internal void UpdateEvent(int eventId)
        {
            BroadcastJson(jsonHandler.GetJsonServerEventUpdate(eventId));
        }

        private void SendJson(String json, Socket sock)
        {
            Log.D("Message length is " + json.Length + " Content is '" + json + "'");
            sock.Send(Encoding.UTF8.GetBytes(json + "\n"));
        }

        private void BroadcastJson(String json)
        {
            Log.D("Broadcasting '" + json + "'");
            foreach (Socket s in clients)
            {
                if (s != server)
                {
                    Log.D("Sending message.");
                    s.Send(Encoding.UTF8.GetBytes(json + "\n"));
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
