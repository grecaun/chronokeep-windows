﻿using Newtonsoft.Json.Linq;
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
        IDBInterface database;
        IChangeUpdater changeUpdater;

        public TCPServer(IDBInterface database, IChangeUpdater changeUpdater)
        {
            jsonHandler = new JsonHandler(database);
            this.database = database;
            this.changeUpdater = changeUpdater;
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
                Socket.Select(readList, null, null, 60000000);
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
                        catch (Exception e)
                        {
                            Log.D("Appears the client has disconnected.");
                            clients.Remove(sock);
                            Log.D(e.StackTrace);
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
                        JsonClientParticipantUpdate partUpd = jsonObject.ToObject<JsonClientParticipantUpdate>();
                        Participant oldPart = database.GetParticipant(partUpd.EventId, partUpd.Participant.Id);
                        Participant newPart = new Participant(partUpd.Participant.Id, partUpd.Participant.First, partUpd.Participant.Last,
                            partUpd.Participant.Street, partUpd.Participant.City, partUpd.Participant.State, partUpd.Participant.Zip,
                            partUpd.Participant.Birthday, partUpd.Participant.Specific, partUpd.Participant.Email,
                            partUpd.Participant.Mobile, partUpd.Participant.Parent, partUpd.Participant.Country, partUpd.Participant.Street2,
                            partUpd.Participant.Gender, partUpd.Participant.EmergencyContactName, partUpd.Participant.EmergencyContactPhone);
                        database.UpdateParticipant(newPart);
                        newPart = database.GetParticipant(partUpd.EventId, partUpd.Participant.Id);
                        database.AddChange(newPart, oldPart);
                        if (changeUpdater != null) changeUpdater.UpdateChangesBox();
                        BroadcastJson(jsonHandler.GetJsonServerUpdateParticipant(partUpd.EventId, newPart));
                        break;
                    case "client_participant_add":
                        Log.D("Client participant add received.");
                        JsonClientParticipantAdd clientPartAdd = jsonObject.ToObject<JsonClientParticipantAdd>();
                        Participant addPart = new Participant(clientPartAdd.Participant.First, clientPartAdd.Participant.Last,
                            clientPartAdd.Participant.Street, clientPartAdd.Participant.City, clientPartAdd.Participant.State,
                            clientPartAdd.Participant.Zip, clientPartAdd.Participant.Birthday, clientPartAdd.Participant.Specific,
                            clientPartAdd.Participant.Email, clientPartAdd.Participant.Mobile, clientPartAdd.Participant.Parent,
                            clientPartAdd.Participant.Country, clientPartAdd.Participant.Street2, clientPartAdd.Participant.Gender,
                            clientPartAdd.Participant.EmergencyContactName, clientPartAdd.Participant.EmergencyContactPhone);
                        database.AddParticipant(addPart);
                        addPart = database.GetParticipant(clientPartAdd.EventId, addPart);
                        database.AddChange(addPart, null);
                        if (changeUpdater != null) changeUpdater.UpdateChangesBox();
                        BroadcastJson(jsonHandler.GetJsonServerAddParticipant(clientPartAdd.EventId, addPart));
                        break;
                    case "client_participant_set":
                        Log.D("Client participant set received.");
                        JsonClientParticipantSet clientPartSet = jsonObject.ToObject<JsonClientParticipantSet>();
                        if (clientPartSet.Value.Name == "checked_in")
                        {
                            Log.D("Checked in set value found.");
                            database.CheckInParticipant(clientPartSet.EventId, clientPartSet.ParticipantId, Convert.ToInt32(clientPartSet.Value.Value));
                        }
                        else if (clientPartSet.Value.Name == "early_start")
                        {
                            Log.D("Early start set value found.");
                            database.SetEarlyStartParticipant(clientPartSet.EventId, clientPartSet.ParticipantId, Convert.ToInt32(clientPartSet.Value.Value));
                        }
                        BroadcastJson(jsonHandler.GetJsonServerSetParticipant(clientPartSet.EventId, clientPartSet.ParticipantId, clientPartSet.Value));
                        break;
                    case "client_kiosk_dayof_add":
                        Log.D("Client day of participant add received.");
                        JsonClientDayOfAdd dayofPartAdd = jsonObject.ToObject<JsonClientDayOfAdd>();
                        Log.D("First '" + dayofPartAdd.Participant.First + "' Last '" + dayofPartAdd.Participant.Last + "'");
                        database.AddDayOfParticipant(dayofPartAdd.Participant);
                        if (database.GetPrintOption(dayofPartAdd.EventId) == 1)
                        {
                            Log.D("Print option is set to 1.");
                            List<Division> divs = database.GetDivisions(dayofPartAdd.EventId);
                            foreach (Division d in divs)
                            {
                                if (d.Identifier == dayofPartAdd.Participant.DivisionIdentifier)
                                {
                                    Printerface.PrintDayOf(dayofPartAdd.Participant, d);
                                    break;
                                }
                            }
                        }
                        BroadcastJson(jsonHandler.GetJsonServerKioskDayOfAdd(dayofPartAdd.EventId, dayofPartAdd.Participant));
                        break;
                    case "client_kiosk_dayof_approve":
                        Log.D("Client day of participant approve received.");
                        JsonClientDayOfApprove dayofPartApprove = jsonObject.ToObject<JsonClientDayOfApprove>();
                        if (database.ApproveDayOfParticipant(dayofPartApprove.EventId, dayofPartApprove.DayOfId, dayofPartApprove.Bib, dayofPartApprove.EarlyStart))
                        {
                            Log.D("Approved.");
                            BroadcastJson(jsonHandler.GetJsonServerKioskDayOfRemove(dayofPartApprove.EventId, dayofPartApprove.DayOfId));
                        }
                        else
                        {
                            Log.D("Denied.");
                        }
                        break;
                    case "client_kiosk_dayof":
                        Log.D("Client request for day of registrants list received.");
                        JsonClientDayOfParticipants clientDayOfList = jsonObject.ToObject<JsonClientDayOfParticipants>();
                        SendJson(jsonHandler.GetJsonServerKioskDayOfParticipants(clientDayOfList.EventId), sock);
                        break;
                    case "client_kiosk":
                        Log.D("Client kiosk received.");
                        JsonClientKiosk clientKiosk = jsonObject.ToObject<JsonClientKiosk>();
                        SendJson(jsonHandler.GetJsonServerKioskWaiver(clientKiosk.EventId), sock);
                        break;
                    case "client_register_next_year":
                        Log.D("Client register next year received.");
                        JsonClientRegisterNextYear regNextYear = jsonObject.ToObject<JsonClientRegisterNextYear>();
                        Event thisYear = database.GetEvent(regNextYear.EventId);
                        List<Division> nextYearDivs = database.GetDivisions(thisYear.NextYear);
                        Division nextYearDiv = nextYearDivs[0];
                        Log.D("Getting division");
                        foreach (Division d in nextYearDivs)
                        {
                            if (d.Identifier == regNextYear.Participant.Specific.DivisionIdentifier || d.Name.Equals(regNextYear.Participant.Specific.DivisionName))
                            {
                                Log.D("Division found. Id is " + d.Identifier + " Name is " + d.Name);
                                nextYearDiv = d;
                                break;
                            }
                        }
                        Log.D("Getting participant information from this year.");
                        Participant thisYearEntry = database.GetParticipant(thisYear.Identifier, regNextYear.Participant.Id);
                        if (thisYearEntry.EventSpecific.NextYear  == 1)
                        {
                            break;
                        }
                        Participant nextYearEntry = new Participant(regNextYear.Participant.First, regNextYear.Participant.Last,
                            regNextYear.Participant.Street, regNextYear.Participant.City, regNextYear.Participant.State, regNextYear.Participant.Zip,
                            regNextYear.Participant.Birthday, regNextYear.Participant.Specific, regNextYear.Participant.Email,
                            regNextYear.Participant.Mobile, regNextYear.Participant.Parent, regNextYear.Participant.Country,
                            regNextYear.Participant.Street2, regNextYear.Participant.Gender, regNextYear.Participant.EmergencyContactName,
                            regNextYear.Participant.EmergencyContactPhone);
                        nextYearEntry.EventSpecific.DivisionIdentifier = nextYearDiv.Identifier;
                        nextYearEntry.EventSpecific.EventIdentifier = thisYear.NextYear;
                        Log.D("Adding next year entry.");
                        database.AddParticipant(nextYearEntry);
                        nextYearEntry = database.GetParticipant(thisYear.NextYear, nextYearEntry);
                        if (nextYearEntry != null)
                        {
                            Log.D("Participant successfully added to database.");
                            thisYearEntry.EventSpecific.NextYear = 1;
                            database.UpdateParticipant(thisYearEntry);
                            database.AddChange(nextYearEntry, null);
                            BroadcastJson(jsonHandler.GetJsonServerAddParticipant(thisYear.NextYear, nextYearEntry));
                            BroadcastJson(jsonHandler.GetJsonServerUpdateParticipant(thisYear.Identifier, thisYearEntry));
                            if (changeUpdater != null) changeUpdater.UpdateChangesBox();
                        }
                        else
                        {
                            Log.D("Hmm, unable to add participant for some reason.");
                            nextYearEntry = new Participant(regNextYear.Participant.First, regNextYear.Participant.Last, regNextYear.Participant.Street,
                                regNextYear.Participant.City, regNextYear.Participant.State, regNextYear.Participant.Zip, regNextYear.Participant.Birthday,
                                regNextYear.Participant.Specific, regNextYear.Participant.Email, regNextYear.Participant.Mobile,
                                regNextYear.Participant.Parent, regNextYear.Participant.Country, regNextYear.Participant.Street2,
                                regNextYear.Participant.Gender, regNextYear.Participant.EmergencyContactName, regNextYear.Participant.EmergencyContactPhone);
                            nextYearEntry.EventSpecific.DivisionIdentifier = nextYearDiv.Identifier;
                            nextYearEntry.EventSpecific.EventIdentifier = thisYear.NextYear;
                            nextYearEntry.EventSpecific.Comments = "Error adding this person to the entrants for next year.";
                            database.AddChange(nextYearEntry, null);
                            if (changeUpdater != null) changeUpdater.UpdateChangesBox();
                        }
                        break;
                }
            }
        }

        internal void UpdateEvent(int eventId)
        {
            BroadcastJson(jsonHandler.GetJsonServerEventUpdate(eventId));
        }

        internal void UpdateEventList()
        {
            BroadcastJson(jsonHandler.GetJsonServerEventList());
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

        public void UpdateEventKiosk(int eventId)
        {
            BroadcastJson(jsonHandler.GetJsonServerKioskWaiver(eventId));
        }

    }
}
