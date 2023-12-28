using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Objects.ChronokeepPortal.Requests;
using Chronokeep.Objects.ChronokeepPortal.Responses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Chronokeep.Timing.Interfaces
{
    internal class ChronokeepInerface : ITimingSystemInterface
    {
        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        StringBuilder buffer = new StringBuilder();
        Socket sock;
        IMainWindow window = null;

        private static readonly Regex zeroconf = new Regex(@"^\[(?'PORTAL_NAME'[^|]*)\|(?'PORTAL_ID'[^|]*)\|(?'PORTAL_PORT'\d{1,5})\]");
        private static readonly Regex msg = new Regex(@"^[^\n]*\n");

        public ChronokeepInerface(IDBInterface database, int locationId, IMainWindow window)
        {
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            this.locationId = locationId;
            this.window = window;
        }

        public List<Socket> Connect(string IP_Address, int Port)
        {
            List<Socket> output = new List<Socket>();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                using (UdpClient client = new UdpClient(AddressFamily.InterNetwork))
                {
                    byte[] msg = Encoding.Default.GetBytes(Constants.Readers.CHRONO_PORTAL_CONNECT_MSG);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Constants.Readers.CHRONO_PORTAL_ZCONF_IP), Constants.Readers.CHRONO_PORTAL_ZCONF_PORT);
                    client.Send(msg, msg.Length, endPoint);
                    byte[] data = client.Receive(ref endPoint);
                    string response = Encoding.Default.GetString(data);
                    Match match = zeroconf.Match(response);
                    if (match.Success)
                    {
                        Log.D("Timing.Interfaces.ChronokeepInterface", "Successfully received message from reader. Name is "
                            + match.Groups["PORTAL_NAME"].Value
                            + ". Id is "
                            + match.Groups["PORTAL_ID"].Value
                            + ". Port is "
                            + match.Groups["PORTAL_PORT"].Value
                            );
                        int port = Constants.Readers.CHRONO_PORTAL_ZCONF_PORT;
                        if (!int.TryParse(match.Groups["PORTAL_PORT"].Value, out port))
                        {
                            Log.E("Timing.Interfaces.ChronokeepInterface", "Error parsing port.");
                            return null;
                        }
                        sock.Connect(IP_Address, port);
                        SendMessage(JsonSerializer.Serialize(new ConnectRequest
                        {
                            Reads = true,
                            Sightings = false,
                        }));
                        output.Add(sock);
                    }
                    else
                    {
                        Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to parse message from server. Unknown value. '" + response + "'");
                        return null;
                    }
                }
            }
            catch
            {
                Log.E("Timing.Interfaces.ChronokeepInterface", "Error connecting to reader.");
                return null;
            }
            Log.D("Timing.Interfaces.ChronokeepInterface", "Connected. Returning socket.");
            return output;
        }

        public void GetStatus() { }

        public void GetTime()
        {
            Log.D("Timing.Interfaces.ChronokeepInterface", "Requesting time.");
            SendMessage(JsonSerializer.Serialize(new TimeGetRequest { }));
        }

        public Dictionary<MessageType, List<string>> ParseMessages(string inMessage, Socket sock)
        {
            Dictionary<MessageType, List<string>> output = new Dictionary<MessageType, List<string>>();
            buffer.Append(inMessage);
            Match m = msg.Match(buffer.ToString());
            List<ChipRead> chipReads = new List<ChipRead>();
            while (m.Success)
            {
                buffer.Remove(m.Index, m.Length);
                string message = m.Value;
                try
                {
                    Response res = JsonSerializer.Deserialize<Response>(message);
                    switch (res.Command)
                    {
                        case Response.KEEPALIVE:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent keepalive message.");
                            SendMessage(JsonSerializer.Serialize(new KeepaliveAckRequest { }));
                            break;
                        case Response.READERS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent readers message.");
                            //if (!output.ContainsKey(MessageType.SETTINGVALUE))
                            //{
                            //    output[MessageType.SETTINGVALUE] = new List<string>();
                            //}
                            //output[MessageType.SETTINGVALUE].Add(message);
                            break;
                        case Response.ERROR:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent error message.");
                            try
                            {
                                ErrorResponse err = JsonSerializer.Deserialize<ErrorResponse>(message);
                                if (!output.ContainsKey(MessageType.ERROR))
                                {
                                    output[MessageType.ERROR] = new List<string>();
                                }
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error sent to us is of type '" + err.Value.Type + "' and has message '" + err.Value.Message + "'.");
                                output[MessageType.ERROR].Add(err.Value.Message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to process chip read. " + e.Message);
                                if (!output.ContainsKey(MessageType.ERROR))
                                {
                                    output[MessageType.ERROR] = new List<string>();
                                }
                                output[MessageType.ERROR].Add("Error processing chip read.");
                            }
                            break;
                        case Response.SETTINGS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent settings message.");
                            break;
                        case Response.API_LIST:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent api list message.");
                            break;
                        case Response.READS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent reads message.");
                            try
                            {
                                ReadsResponse reads = JsonSerializer.Deserialize<ReadsResponse>(message);
                                if (reads.List.Count > 0)
                                {
                                    foreach (PortalRead pRead in reads.List)
                                    {
                                        ChipRead newRead = new ChipRead(
                                            theEvent.Identifier,
                                            locationId,
                                            pRead.IdentType == PortalRead.READ_IDENT_TYPE_CHIP,
                                            pRead.Chip,
                                            Constants.Timing.UTCSecondsToRFIDSeconds(pRead.Seconds),
                                            pRead.Milliseconds,
                                            pRead.Antenna,
                                            pRead.RSSI,
                                            pRead.Reader,
                                            pRead.Type == PortalRead.READ_KIND_CHIP ? Constants.Timing.CHIPREAD_TYPE_CHIP : Constants.Timing.CHIPREAD_TYPE_MANUAL
                                            );
                                        chipReads.Add(newRead);
                                    }
                                    if (!output.ContainsKey(MessageType.CHIPREAD))
                                    {
                                        output[MessageType.CHIPREAD] = null;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to process chip read. " + e.Message);
                                if (!output.ContainsKey(MessageType.ERROR))
                                {
                                    output[MessageType.ERROR] = new List<string>();
                                }
                                output[MessageType.ERROR].Add("Error processing chip read.");
                            }
                            break;
                        case Response.SUCCESS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent success message.");
                            if (!output.ContainsKey(MessageType.SUCCESS))
                            {
                                output[MessageType.SUCCESS] = null;
                            }
                            break;
                        case Response.TIME:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent time message.");
                            try
                            {
                                TimeResponse t = JsonSerializer.Deserialize<TimeResponse>(message);
                                if (!output.ContainsKey(MessageType.TIME))
                                {
                                    output[MessageType.TIME] = new List<string>();
                                }
                                output[MessageType.TIME].Clear();
                                DateTime timeDT = DateTime.ParseExact(t.Local, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                                output[MessageType.TIME].Add(timeDT.ToString("dd MMM yyyy  HH:mm:ss"));
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to process time message. " + e.Message);
                                if (!output.ContainsKey(MessageType.ERROR))
                                {
                                    output[MessageType.ERROR] = new List<string>();
                                }
                                output[MessageType.ERROR].Add("Error processing time message.");
                            }
                            break;
                        case Response.PARTICIPANTS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent participants message.");
                            break;
                        case Response.SIGHTINGS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent sightings message.");
                            break;
                        case Response.EVENTS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent events message.");
                            break;
                        case Response.EVENT_YEARS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent event years message.");
                            break;
                        case Response.READ_AUTO_UPLOAD:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent read auto upload message.");
                            break;
                        case Response.CONNECTION_SUCCESSFUL:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent connection successful message.");
                            if (!output.ContainsKey(MessageType.CONNECTED))
                            {
                                output[MessageType.CONNECTED] = null;
                            }
                            break;
                        case Response.DISCONNECT:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent disconnect message.");
                            if (!output.ContainsKey(MessageType.DISCONNECT))
                            {
                                output[MessageType.DISCONNECT] = new List<string>();
                            }
                            break;
                        default:
                            Log.E("Timing.Interfaces.ChronokeepInterface", "Unknown message received: " + res.Command);
                            if (!output.ContainsKey(MessageType.UNKNOWN))
                            {
                                output[MessageType.UNKNOWN] = null;
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.E("Timing.Interfaces.ChronokeepInterface", "Error deserializing json. " + e.Message);
                }
                m = msg.Match(buffer.ToString());
            }
            if (chipReads.Count > 0)
            {
                database.AddChipReads(chipReads);
            }
            return output;
        }

        public void Rewind(DateTime start, DateTime end, int reader = 1)
        {
            SendMessage(JsonSerializer.Serialize(new ReadsGetRequest
            {
                StartSeconds = Constants.Timing.UnixDateToEpoch(start.ToUniversalTime()),
                EndSeconds = Constants.Timing.UnixDateToEpoch(end.ToUniversalTime())
            }));
        }

        public void Rewind(int from, int to, int reader = 1) { }

        public void Rewind(int reader = 1)
        {
            SendMessage(JsonSerializer.Serialize(new ReadsGetAllRequest { }));
        }

        public void SetMainSocket(Socket sock) { }

        public void SetSettingsSocket(Socket sock) { }

        public void SetTime(DateTime date)
        {
            SendMessage(JsonSerializer.Serialize(new TimeSetRequest
            {
                Time = date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.f")
            }));
        }

        public void StartReading() { }

        public void StartSending() { }

        public void StopReading() { }

        public void StopSending() { }

        public void Disconnect()
        {
            SendMessage(JsonSerializer.Serialize(new DisconnectRequest { }));
        }

        private void SendMessage(string msg)
        {
            Log.D("Timing.Interfaces.ChronokeepInterface", "Sending message '" + msg + "'");
            sock.Send(Encoding.Default.GetBytes(msg + "\n"));
        }
    }
}
