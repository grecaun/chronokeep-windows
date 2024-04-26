using Chronokeep.Objects.Registration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Chronokeep.Network.Registration
{
    internal class Worker
    {
        private static Worker instance;

        private bool running = false;
        private bool keepalive = false;

        private readonly Mutex threadMutex = new Mutex();

        private Socket server;
        private List<Socket> clients = new List<Socket>();
        private List<Socket> readList = new List<Socket>();
        private Dictionary<Socket, StringBuilder> bufferDictionary = new Dictionary<Socket, StringBuilder>();
        private IDBInterface database;

        private static readonly Regex msgRegex = new Regex(@"^[^\n]*\n");

        private Worker(IDBInterface database)
        {
            this.database = database;
        }

        public static Worker NewWorker(IDBInterface database)
        {
            if (instance == null)
            {
                instance = new Worker(database);
            }
            return instance;
        }

        public bool IsRunning()
        {
            bool output = false;
            if (threadMutex.WaitOne(3000))
            {
                output = running;
                threadMutex.ReleaseMutex();
            }
            return output;
        }

        public void Stop()
        {
            if (threadMutex.WaitOne(3000))
            {
                keepalive = false;
                threadMutex.ReleaseMutex();
            }
        }

        public void Run()
        {
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 1)
            {
                return;
            }
            if (threadMutex.WaitOne(3000))
            {
                keepalive = true;
                running = true;
                threadMutex.ReleaseMutex();
            }
            else
            {
                return;
            }
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            server.Bind(new IPEndPoint(IPAddress.Any, NetCore.TCPPort()));
            server.Listen(10);
            clients.Add(server);
            while (true)
            {
                readList.Clear();
                readList.AddRange(clients);
                Socket.Select(readList, null, null, 60000000);
                foreach (Socket sock in readList)
                {
                    if (sock == server)
                    {
                        Log.D("Network.Registration.Worker", "New incoming connection to registration.");
                        Socket newSock = sock.Accept();
                        clients.Add(newSock);
                        bufferDictionary[sock] = new StringBuilder();
                        AppSetting nameSetting = database.GetAppSetting(Constants.Settings.SERVER_NAME);
                        string nameString = nameSetting != null && nameSetting.Value != null ? nameSetting.Value : Constants.Network.DEFAULT_CHRONOKEEP_SERVER_NAME;
                        SendMessage(newSock, JsonSerializer.Serialize(new ConnectionSuccessfulResponse
                        {
                            Name = nameString,
                            Type = Constants.Network.CHRONOKEEP_REGISTRATION_TYPE,
                            Version = Constants.Network.CHRONOKEEP_REGISTRATION_VERS
                        }));
                    }
                    else
                    {
                        byte[] recvd = new byte[4096];
                        try
                        {
                            int num_recvd = sock.Receive(recvd);
                            if (num_recvd == 0)
                            {
                                Log.D("Network.Registration.Worker", "Client disconnected.");
                                clients.Remove(sock);
                                bufferDictionary.Remove(sock);
                                sock.Close();
                            }
                            else
                            {
                                string msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                                StringBuilder buffer = bufferDictionary[sock];
                                buffer.Append(msg);
                                Log.D("Network.Registration.Worker", string.Format("Message received: {0}", msg.Trim()));
                                Match m = msgRegex.Match(buffer.ToString());
                                while (m.Success)
                                {
                                    buffer.Remove(m.Index, m.Length);
                                    string message = m.Value;
                                    try
                                    {
                                        Request res = JsonSerializer.Deserialize<Request>(message);
                                        switch (res.Command)
                                        {
                                            case Request.GET_PARTICIPANTS:
                                                Log.D("Network.Registration.Worker","Received get participant message.");
                                                SendMessage(sock, JsonSerializer.Serialize(new ParticipantsResponse
                                                {
                                                    Participants = GetParticipants(database, theEvent),
                                                    Distances = GetDistances(database, theEvent),
                                                }));
                                                break;
                                            case Request.ADD_PARTICIPANT:
                                                Log.D("Network.Registration.Worker", "Received add participant message.");
                                                try
                                                {
                                                    ModifyParticipant addReq = JsonSerializer.Deserialize<ModifyParticipant>(message);
                                                    // deal with request
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.E("Network.Registration.Worker", string.Format("Error deserializing json for add participant. {0}", e.Message));
                                                }
                                                break;
                                            case Request.UPDATE_PARTICIPANT:
                                                Log.D("Network.Registration.Worker", "Received update participant message.");
                                                try
                                                {
                                                    ModifyParticipant addReq = JsonSerializer.Deserialize<ModifyParticipant>(message);
                                                    // deal with request
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.E("Network.Registration.Worker", string.Format("Error deserializing json for add participant. {0}", e.Message));
                                                }
                                                break;
                                            case Request.DISCONNECT:
                                                Log.D("Network.Registration.Worker", "Received disconnect message.");
                                                clients.Remove(sock);
                                                bufferDictionary.Remove(sock);
                                                sock.Close();
                                                break;
                                            default:
                                                Log.D("Network.Registration.Worker", "Unknown message received.");
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Log.E("Network.Registration.Worker", string.Format("Error deserializing json. {0}", e.Message));
                                    }
                                    m = msgRegex.Match(buffer.ToString());
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.D("Network.Registration.Worker", string.Format("Error communicating with socket. {0}", e.Message));
                            clients.Remove(sock);
                            bufferDictionary.Remove(sock);
                            sock.Close();
                        }
                    }
                }
                if (threadMutex.WaitOne(3000))
                {
                    if (!keepalive)
                    {
                        running = false;
                        threadMutex.ReleaseMutex();
                        break;
                    }
                    threadMutex.ReleaseMutex();
                }
            }
        }

        public List<Participant> GetParticipants(IDBInterface database, Event theEvent)
        {
            List<Participant> output = new List<Participant>();
            List<Objects.Participant> participants = database.GetParticipants(theEvent.Identifier);
            foreach (Objects.Participant participant in participants)
            {
                output.Add(new Participant
                {
                    Bib = participant.Bib,
                    FirstName = participant.FirstName,
                    LastName = participant.LastName,
                    Gender = participant.Gender,
                    Birthdate = participant.Birthdate,
                    Distance = participant.Distance,
                    Mobile = participant.Mobile,
                    TextEnabled = false // TODO FIX
                });
            }
            return output;
        }

        public List<string> GetDistances(IDBInterface database, Event theEvent)
        {
            List<string> output = new List<string>();
            List<Distance> distances = database.GetDistances(theEvent.Identifier);
            foreach (Distance distance in distances)
            {
                output.Add(distance.Name);
            }
            return output;
        }

        public void SendMessage(Socket sock, string msg)
        {
            Log.D("Network.Registration.Worker", string.Format("Sending message '{0}'", msg));
            sock.Send(Encoding.Default.GetBytes(msg + "\n"));
        }
    }
}
