using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chronokeep.Timing.Interfaces
{
    class IpicoInterface : ITimingSystemInterface
    {
        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        Dictionary<Socket, StringBuilder> bufferDict = new Dictionary<Socket, StringBuilder>();
        Socket controlSocket;
        Socket streamSocket;
        Socket rewindSocket;
        string ipadd;
        string Type;

        IMainWindow window = null;


        // private static readonly Regex voltage/connected/chipread/settinginfo/settingconfirmation/time/status/msg
        private static readonly Regex chipread = new Regex(@"aa[0-9a-fA-F]{34,36}");
        private static readonly Regex time = new Regex(@"date\.\w{3} \w{3} {1,2}\d{1,2} \d{2}:\d{2}:\d{2} \w{3} \d{4} *");
        private static readonly Regex msg = new Regex(@"^[^\n]+\n");

        public IpicoInterface(IDBInterface database, int locationId, string type, IMainWindow window)
        {
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            this.locationId = locationId;
            this.Type = type;
            this.window = window;
        }

        public List<Socket> Connect(string IpAddress, int Port)
        {
            this.ipadd = IpAddress;
            List<Socket> output = new List<Socket>();
            controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            streamSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (Type != Constants.Readers.SYSTEM_IPICO_LITE)
            {
                try
                {
                    Log.D("Timing.Interfaces.IpicoInterface", "Attempting to connect to " + IpAddress + ":" + Constants.Readers.IPICO_CONTROL_PORT);
                    controlSocket.Connect(IpAddress, Constants.Readers.IPICO_CONTROL_PORT);
                    output.Add(controlSocket);
                }
                catch
                {
                    Log.D("Timing.Interfaces.IpicoInterface", "Unable to connect to control socket...");
                    controlSocket = null;
                }
            }
            else
            {
                Log.D("Timing.Interfaces.IpicoInterface", "IPICO Lite Reader found.");
                controlSocket = null;
            }
            try
            {
                Log.D("Timing.Interfaces.IpicoInterface", "Attempting to connect to " + IpAddress + ":" + Constants.Readers.IPICO_DEFAULT_PORT);
                streamSocket.Connect(IpAddress, Constants.Readers.IPICO_DEFAULT_PORT);
                output.Add(streamSocket);
            }
            catch
            {
                Log.D("Timing.Interfaces.IpicoInterface", "Unable to connect to stream socket...");
                streamSocket = null;
            }
            if ((controlSocket == null && Type != Constants.Readers.SYSTEM_IPICO_LITE) || streamSocket == null)
            {
                return null;
            }
            return output;
        }

        public Dictionary<MessageType, List<string>> ParseMessages(string inMessage, Socket sock)
        {
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- parsing message.");
            Dictionary<MessageType, List<string>> output = new Dictionary<MessageType, List<string>>();
            if (sock == null)
            {
                return output;
            }
            if (!bufferDict.ContainsKey(sock))
            {
                bufferDict[sock] = new StringBuilder();
            }
            bufferDict[sock].Append(inMessage);
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- new message is '" + inMessage + "' with a length of " + inMessage.Length);
            Match m = msg.Match(bufferDict[sock].ToString());
            List<ChipRead> chipReads = new List<ChipRead>();
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- matching all lines for messages");
            int count = 1;
            while (m.Success)
            {
                Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- message " + count++);
                bufferDict[sock].Remove(m.Index, m.Length);
                string message = m.Value;
                Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- message is : " + message);
                // a chipread is as follows: (note that milliseconds don't appear to be an actual millisecond but a hundredth of a second)
                // aa[ReaderId{2}][TagID{12}(Starts with 058)][ICount?{2}][QCount{2}][Date{yyMMdd}][Time{HHmmss}][Milliseconds{2}(Hex)][Checksum{2}][FS|LS]
                if (chipread.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- chipread found");
                    DateTime time = DateTime.ParseExact(message.Substring(20, 12), "yyMMddHHmmss", CultureInfo.InvariantCulture);
                    int.TryParse(message.Substring(32, 2), NumberStyles.HexNumber, null, out int milliseconds);
                    milliseconds *= 10;
                    time = time.AddMilliseconds(milliseconds);
                    ChipRead chipRead = new ChipRead(
                        theEvent.Identifier,
                        locationId,
                        message.Substring(4, 12),
                        time,
                        Convert.ToInt32(message.Substring(2, 2)),
                        message.Length == 36 ? 0 : 1
                        );
                    if (window != null && window.InDidNotStartMode())
                    {
                        chipRead.Status = Constants.Timing.CHIPREAD_STATUS_DNS;
                    }
                    chipReads.Add(chipRead);
                    if (!output.ContainsKey(MessageType.CHIPREAD))
                    {
                        output[MessageType.CHIPREAD] = null;
                    }
                }
                // Time is as follows:
                // date.Wed Nov 12 15:30:30 CST 2008
                else if (time.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- It's a time message.");
                    string month = message.Substring(9, 3);
                    string day = message.Substring(13, 2).Trim();
                    int dayVal = int.Parse(day);
                    string time = message.Substring(16, 8);
                    string year = message.Substring(29, 4);
                    string dateStr = string.Format("{0:D2} {1} {2}  {3}", dayVal, month, year, time);
                    Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- date string is " + dateStr);
                    output[MessageType.TIME] = new List<string>();
                    output[MessageType.TIME].Add(dateStr);
                }
                m = msg.Match(bufferDict[sock].ToString());
            }
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- messages parsed successfully adding chipreads");
            database.AddChipReads(chipReads);
            return output;
        }

        public void StartReading() { }

        public void StopReading() { }

        public void SetTime(DateTime date)
        {
            // setdate.YYMMDDHH:MM:SS
            SendMessage("setdate." + DateTime.Now.ToString("yyMMddHH:mm:ss"));
        }

        public void GetTime()
        {
            SendMessage("getdate");
        }

        public void GetStatus() { }

        public void StartSending() { }

        public void StopSending() { }

        public void GetRewind()
        {
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- connecting to rewind socket");
            rewindSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                rewindSocket.Connect(ipadd, 10300);
            }
            catch
            {
                Log.D("Timing.Interfaces.IpicoInterface", "Upable to connect to rewind socket...");
                rewindSocket = null;
                return;
            }
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- looking for messages");
            byte[] recvd = new byte[4112];
            try
            {
                rewindSocket.Send(Encoding.ASCII.GetBytes("\n"));
                int num_recvd = rewindSocket.Receive(recvd);
                while (num_recvd > 0)
                {
                    string msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                    Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- Rewind -- message :" + msg);
                    Log.D("Timing.Interfaces.IpicoInterface", "Timing System - Message is :" + msg.Trim());
                    Dictionary<MessageType, List<string>> messageTypes = ParseMessages(msg, rewindSocket);
                    num_recvd = rewindSocket.Receive(recvd);
                }
            }
            catch
            {
                Log.E("Timing.Interfaces.IpicoInterface", "Something went wrong with the rewind.");
            }
            finally
            {
                if (rewindSocket != null && bufferDict.ContainsKey(rewindSocket))
                {
                    bufferDict[rewindSocket].Clear();
                }
                rewindSocket?.Close();
                rewindSocket = null;
            }
            Log.D("Timing.Interfaces.IpicoInterface", "IpicoInterface -- finished rewind");
        }

        public void Rewind(DateTime start, DateTime end, int reader = 1)
        {
            // yyMMddHHmmss
            if (reader == 1)
            {
                SendMessage("replay_start file.ttyS0 port.10300 datetime." + start.ToString("yyMMddHHmmss"));
            }
            else
            {
                SendMessage("replay_start file.ttyS1 port.10300 datetime." + start.ToString("yyMMddHHmmss"));
            }
        }

        public void Rewind(int from, int to, int reader = 1) { }

        public void Rewind(int reader = 1)
        {
            if (reader == 1)
            {
                SendMessage("replay_start file.ttyS0 port.10300");
            }
            else
            {
                SendMessage("replay_start file.ttyS1 port.10300");
            }
        }

        public void SetMainSocket(Socket sock)
        {
            streamSocket = sock;
        }

        public void SetSettingsSocket(Socket sock)
        {
            controlSocket = sock;
        }

        public void Disconnect() { }

        public void SendMessage(string msg)
        {
            Log.D("Timing.Interfaces.IpicoInterface", "Sending message '" + msg + "'");
            controlSocket.Send(Encoding.ASCII.GetBytes(msg + "\n"));
        }
    }
}
