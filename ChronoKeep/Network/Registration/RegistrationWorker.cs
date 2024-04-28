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
    internal class RegistrationWorker
    {
        private static RegistrationWorker instance;

        private bool running = false;
        private bool keepalive = false;

        private readonly Mutex threadMutex = new Mutex();

        private Socket server;
        private List<Socket> clients = new List<Socket>();
        private List<Socket> readList = new List<Socket>();
        private Dictionary<Socket, StringBuilder> bufferDictionary = new Dictionary<Socket, StringBuilder>();
        private IDBInterface database;

        private bool updateDistanceDictionary = true;
        private Dictionary<string, Distance> distanceDictionary = new Dictionary<string, Distance>();

        private static readonly Regex msgRegex = new Regex(@"^[^\n]*\n");

        private RegistrationWorker(IDBInterface database)
        {
            this.database = database;
        }

        public static RegistrationWorker NewWorker(IDBInterface database)
        {
            if (instance == null)
            {
                instance = new RegistrationWorker(database);
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
            Log.D("Network.Registration.RegistrationWorker", "Instructed to stop registration.");
            if (threadMutex.WaitOne(3000))
            {
                keepalive = false;
                threadMutex.ReleaseMutex();
            }
            try
            {
                server.Close();
            }
            catch { }
        }

        public void UpdateDistances()
        {
            if (threadMutex.WaitOne(3000))
            {
                updateDistanceDictionary = true;
                threadMutex.ReleaseMutex();
            }
        }

        public void Run()
        {
            Log.D("Network.Registration.RegistrationWorker", "Starting Registration thread.");
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
                try
                {
                    Socket.Select(readList, null, null, 60000000);
                }
                catch { }
                foreach (Socket sock in readList)
                {
                    if (sock == server && sock.Connected)
                    {
                        bool update = false;
                        if (threadMutex.WaitOne(3000))
                        {
                            update = updateDistanceDictionary;
                            updateDistanceDictionary = false;
                            threadMutex.ReleaseMutex();
                        }
                        if (update)
                        {
                            foreach (Distance d in database.GetDistances(theEvent.Identifier))
                            {
                                distanceDictionary[d.Name] = d;
                            }
                            SendParticipants(theEvent);
                        }
                        Log.D("Network.Registration.RegistrationWorker", "New incoming connection to registration.");
                        Socket newSock = sock.Accept();
                        clients.Add(newSock);
                        bufferDictionary[sock] = new StringBuilder();
                    }
                    else
                    {
                        byte[] recvd = new byte[4096];
                        try
                        {
                            int num_recvd = sock.Receive(recvd);
                            if (num_recvd == 0)
                            {
                                Log.D("Network.Registration.RegistrationWorker", "Client disconnected.");
                                clients.Remove(sock);
                                bufferDictionary.Remove(sock);
                                sock.Close();
                            }
                            else
                            {
                                string msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                                StringBuilder buffer = bufferDictionary[sock];
                                buffer.Append(msg);
                                Log.D("Network.Registration.RegistrationWorker", string.Format("Message received: {0}", msg.Trim()));
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
                                            case Request.CONNECT:
                                                Log.D("Network.Registration.RegistrationWorker", "Received connect message.");
                                                AppSetting nameSetting = database.GetAppSetting(Constants.Settings.SERVER_NAME);
                                                string nameString = nameSetting != null && nameSetting.Value != null ? nameSetting.Value : Constants.Network.DEFAULT_CHRONOKEEP_SERVER_NAME;
                                                SendMessage(sock, JsonSerializer.Serialize(new ConnectionSuccessfulResponse
                                                {
                                                    Name = nameString,
                                                    Type = Constants.Network.CHRONOKEEP_REGISTRATION_TYPE,
                                                    Version = Constants.Network.CHRONOKEEP_REGISTRATION_VERS
                                                }));
                                                break;
                                            case Request.GET_PARTICIPANTS:
                                                Log.D("Network.Registration.RegistrationWorker","Received get participant message.");
                                                SendMessage(sock, JsonSerializer.Serialize(new ParticipantsResponse
                                                {
                                                    Participants = GetParticipants(theEvent),
                                                    Distances = GetDistances(theEvent),
                                                }));
                                                break;
                                            case Request.ADD_PARTICIPANT:
                                                Log.D("Network.Registration.RegistrationWorker", "Received add participant message.");
                                                try
                                                {
                                                    ModifyParticipant addReq = JsonSerializer.Deserialize<ModifyParticipant>(message);
                                                    if (distanceDictionary.ContainsKey(addReq.Participant.Distance))
                                                    {
                                                        Objects.Participant newPart = new Objects.Participant(
                                                            addReq.Participant.FirstName,
                                                            addReq.Participant.LastName,
                                                            "", // street
                                                            "", // city
                                                            "", // state
                                                            "", // zip
                                                            addReq.Participant.Birthdate,
                                                            new EventSpecific(
                                                                theEvent.Identifier,
                                                                distanceDictionary[addReq.Participant.Distance].Identifier,
                                                                addReq.Participant.Distance,
                                                                addReq.Participant.Bib,
                                                                0,  // checked-in
                                                                "", // comments
                                                                "", // owes
                                                                "", // other
                                                                false,
                                                                addReq.Participant.SMSEnabled
                                                                ),
                                                            "", // email
                                                            "", // phone
                                                            addReq.Participant.Mobile,
                                                            "", // parent
                                                            "", // country
                                                            "", // street2
                                                            addReq.Participant.Gender,
                                                            "", // emergency name
                                                            ""  // emergency phone
                                                            );
                                                        newPart.Trim();
                                                        newPart.FormatData();
                                                        database.AddParticipant(newPart);
                                                        SendParticipants(theEvent);
                                                    }
                                                    else
                                                    {
                                                        SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                        {
                                                            Error = RegistrationError.DISTANCE_NOT_FOUND
                                                        }));
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.E("Network.Registration.RegistrationWorker", string.Format("Error deserializing json for add participant. {0}", e.Message));
                                                    SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                    {
                                                        Error = RegistrationError.PARTICIPANT_NOT_FOUND
                                                    }));
                                                }
                                                break;
                                            case Request.UPDATE_PARTICIPANT:
                                                Log.D("Network.Registration.RegistrationWorker", "Received update participant message.");
                                                try
                                                {
                                                    ModifyParticipant addReq = JsonSerializer.Deserialize<ModifyParticipant>(message);
                                                    Objects.Participant updatedPart = database.GetParticipantEventSpecific(theEvent.Identifier, addReq.Participant.Id);
                                                    if (updatedPart == null)
                                                    {
                                                        SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                        {
                                                            Error = RegistrationError.PARTICIPANT_NOT_FOUND
                                                        }));
                                                    }
                                                    else if (!distanceDictionary.ContainsKey(addReq.Participant.Distance))
                                                    {
                                                        SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                        {
                                                            Error = RegistrationError.DISTANCE_NOT_FOUND
                                                        }));
                                                    }
                                                    else
                                                    {
                                                        updatedPart.Update(
                                                            addReq.Participant.FirstName,
                                                            addReq.Participant.LastName,
                                                            addReq.Participant.Gender,
                                                            addReq.Participant.Birthdate,
                                                            distanceDictionary[addReq.Participant.Distance],
                                                            addReq.Participant.Bib,
                                                            addReq.Participant.SMSEnabled,
                                                            addReq.Participant.Mobile
                                                            );
                                                        database.UpdateParticipant(updatedPart);
                                                        SendParticipants(theEvent);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.E("Network.Registration.RegistrationWorker", string.Format("Error deserializing json for add participant. {0}", e.Message));
                                                    SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                    {
                                                        Error = RegistrationError.PARTICIPANT_NOT_FOUND
                                                    }));
                                                }
                                                break;
                                            case Request.DISCONNECT:
                                                Log.D("Network.Registration.RegistrationWorker", "Received disconnect message.");
                                                clients.Remove(sock);
                                                bufferDictionary.Remove(sock);
                                                sock.Close();
                                                break;
                                            default:
                                                Log.D("Network.Registration.RegistrationWorker", "Unknown message received.");
                                                SendMessage(sock, JsonSerializer.Serialize(new ErrorResponse
                                                {
                                                    Error = RegistrationError.UNKNOWN_MESSAGE
                                                }));
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Log.E("Network.Registration.RegistrationWorker", string.Format("Error deserializing json. {0}", e.Message));
                                    }
                                    m = msgRegex.Match(buffer.ToString());
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.D("Network.Registration.RegistrationWorker", string.Format("Error communicating with socket. {0}", e.Message));
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
            Log.D("Network.Registration.RegistrationWorker", "Thread exiting.");
        }

        public void SendParticipants(Event theEvent)
        {
            foreach (Socket sock in clients)
            {
                if (sock != server)
                {
                    SendMessage(sock, JsonSerializer.Serialize(new ParticipantsResponse
                    {
                        Participants = GetParticipants(theEvent),
                        Distances = GetDistances(theEvent),
                    }));
                }
            }
        }

        public List<Participant> GetParticipants(Event theEvent)
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
                    SMSEnabled = participant.EventSpecific.SMSEnabled
                });
            }
            return output;
        }

        public List<string> GetDistances(Event theEvent)
        {
            return new List<string>(distanceDictionary.Keys);
        }

        public void SendMessage(Socket sock, string msg)
        {
            Log.D("Network.Registration.RegistrationWorker", string.Format("Sending message '{0}'", msg));
            sock.Send(Encoding.Default.GetBytes(msg + "\n"));
        }
    }
}
