using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private static readonly Regex ZERO_CONF = new Regex(@"^\[(?'PORTAL_NAME'[^|]*)\|(?'PORTAL_ID'[^|]*)\|(?'PORTAL_PORT'\d{1,5})\]");

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
                    Match match = ZERO_CONF.Match(response);
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

        public void GetStatus()
        {
            throw new NotImplementedException();
        }

        public void GetTime()
        {
            throw new NotImplementedException();
        }

        public Dictionary<MessageType, List<string>> ParseMessages(string message, Socket sock)
        {
            throw new NotImplementedException();
        }

        public void Rewind(DateTime start, DateTime end, int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void Rewind(int from, int to, int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void Rewind(int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void SetMainSocket(Socket sock)
        {
            throw new NotImplementedException();
        }

        public void SetSettingsSocket(Socket sock)
        {
            throw new NotImplementedException();
        }

        public void SetTime(DateTime date)
        {
            throw new NotImplementedException();
        }

        public void StartReading()
        {
            throw new NotImplementedException();
        }

        public void StartSending()
        {
            throw new NotImplementedException();
        }

        public void StopReading()
        {
            throw new NotImplementedException();
        }

        public void StopSending()
        {
            throw new NotImplementedException();
        }
    }
}
