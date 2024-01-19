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
    class RFIDUltraInterface : ITimingSystemInterface
    {
        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        StringBuilder buffer = new StringBuilder();
        Socket sock;
        IMainWindow window = null;

        private static readonly Regex voltage = new Regex(@"^V=.*");
        private static readonly Regex connected = new Regex(@"^Connected,.*");
        private static readonly Regex chipread = new Regex(@"^0,.*");
        private static readonly Regex settinginfo = new Regex(@"^U.*");
        private static readonly Regex settingconfirmation = new Regex(@"^u.*");
        private static readonly Regex time = new Regex(@"^(\d{1,2}:\d{1,2}:\d{1,2} \d{1,2}-\d{1,2}-\d{4}) \(\d*\)");
        private static readonly Regex status = new Regex(@"^S=.*");
        private static readonly Regex msg = new Regex(@"^[^\n]*\n");

        public RFIDUltraInterface(IDBInterface database, int locationId, IMainWindow window)
        {
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            this.locationId = locationId;
            this.window = window;
        }

        public List<Socket> Connect(string IpAddress, int Port)
        {
            List<Socket> output = new List<Socket>();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Log.D("Timing.Interfaces.RFIDUltraInterface", "Attempting to connect to " + IpAddress + ":" + Port.ToString());
            try
            {
                sock.Connect(IpAddress, Port);
                output.Add(sock);
            }
            catch
            {
                Log.D("Timing.Interfaces.RFIDUltraInterface", "Unable to connect.");
                return null;
            }
            Log.D("Timing.Interfaces.RFIDUltraInterface", "Connected. Returning socket.");
            return output;
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
                // all incoming messages are terminated by a linefeed character (0x0A)
                // If "0,[...]" Chip read
                if (chipread.IsMatch(message))
                {
                    string[] chipVals = message.Split(',');
                    ChipRead chipRead = new ChipRead(
                        theEvent.Identifier,
                        locationId,
                        chipVals[1],
                        long.Parse(chipVals[2]),
                        int.Parse(chipVals[3]),
                        int.Parse(chipVals[4]),
                        chipVals[5],
                        int.Parse(chipVals[6]),
                        chipVals[7],
                        chipVals[8],
                        chipVals[9],
                        long.Parse(chipVals[10]),
                        int.Parse(chipVals[11])
                    );
                    if (window != null && window.InDidNotStartMode())
                    {
                        chipRead.Status = Constants.Timing.CHIPREAD_STATUS_DNS;
                    }
                    chipReads.Add(chipRead);
                    // we don't need to do anything other than notify of a chipread
                    if (!output.ContainsKey(MessageType.CHIPREAD))
                    {
                        output[MessageType.CHIPREAD] = null;
                    }
                }
                // If "V=" then it's a voltage status.
                else if (voltage.IsMatch(message))
                {
                    double voltVal = 0;
                    try
                    {
                        voltVal = Double.Parse(message.Substring(2));
                    }
                    catch
                    {
                        if (!output.ContainsKey(MessageType.ERROR))
                        {
                            output[MessageType.ERROR] = new List<string>();
                        }
                        output[MessageType.ERROR].Add("Invalid voltage value given.");
                    }
                    if (voltVal != 0 && voltVal < 23)
                    {
                        // Voltage low and normal don't require anything else.
                        if (!output.ContainsKey(MessageType.VOLTAGELOW))
                        {
                            output[MessageType.VOLTAGELOW] = null;
                        }
                    }
                    else
                    {
                        // Voltage low and normal don't require anything else.
                        if (!output.ContainsKey(MessageType.VOLTAGENORMAL))
                        {
                            output[MessageType.VOLTAGENORMAL] = null;
                        }
                    }
                }
                // If "U[...]" Setting information
                else if (settinginfo.IsMatch(message))
                {
                    char settingID = message[1];
                    switch (settingID)
                    {
                        default:
                            break;
                    }
                    if (!output.ContainsKey(MessageType.SETTINGVALUE))
                    {
                        output[MessageType.SETTINGVALUE] = new List<string>();
                    }
                    // Add information to the settings values
                    //output[MessageType.SETTINGVALUE].Add("");
                }
                // If "u[...]" setting changed
                else if (settingconfirmation.IsMatch(message))
                {
                    char settingID = message[1];
                    switch (settingID)
                    {
                        default:
                            break;
                    }
                    if (!output.ContainsKey(MessageType.SETTINGCHANGE))
                    {
                        output[MessageType.SETTINGCHANGE] = new List<string>();
                    }
                    // Add information about the setting that changed.
                    //output[MessageType.SETTINGCHANGE].Add("");
                }
                // If "HH:MM:SS DD-MM-YYYY" then it's a time message
                else if (time.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.RFIDUltraInterface", "It's a time message.");
                    Match match = time.Match(message);
                    if (!output.ContainsKey(MessageType.TIME))
                    {
                        output[MessageType.TIME] = new List<string>();
                    }
                    output[MessageType.TIME].Clear();
                    DateTime timeDT = DateTime.ParseExact(match.Groups[1].Value, "H:m:s d-M-yyyy", CultureInfo.CurrentCulture);
                    output[MessageType.TIME].Add(timeDT.ToString("dd MMM yyyy  HH:mm:ss"));
                }
                // If "S=[...]" then status
                else if (status.IsMatch(message))
                {
                    if (!output.ContainsKey(MessageType.STATUS))
                    {
                        output[MessageType.STATUS] = new List<string>();
                    }
                    output[MessageType.STATUS].Add(message);
                }
                // If "Connected,[LastTimeSent]" that's a connection successful message.
                else if (connected.IsMatch(message))
                {
                    // Nothing other than connected care about for right now.
                    if (!output.ContainsKey(MessageType.CONNECTED))
                    {
                        output[MessageType.CONNECTED] = null;
                    }
                }
                else
                {
                    if (!output.ContainsKey(MessageType.UNKNOWN))
                    {
                        output[MessageType.UNKNOWN] = null;
                    }
                }
                m = msg.Match(buffer.ToString());
            }
            if (chipReads.Count > 0)
            {
                database.AddChipReads(chipReads);
            }
            return output;
        }

        public void StartReading()
        {
            SendMessage("R");
        }

        public void StopReading()
        {
            SendMessage("S");
        }

        public void Rewind(DateTime start, DateTime end, int reader = 1)
        {
            SendMessage("800" + Constants.Timing.RFIDDateToEpoch(start).ToString() + RFIDUltraCodes.RewindDelimiter + Constants.Timing.RFIDDateToEpoch(end).ToString());
        }

        public void Rewind(int reader = 1)
        {
            SendMessage("8000" + RFIDUltraCodes.RewindDelimiter + "0");
        }

        public void Rewind(int start, int end, int reader = 1)
        {
            if (start < 1)
            {
                start = 1;
            }
            SendMessage("600" + start.ToString() + RFIDUltraCodes.RewindDelimiter + end.ToString());
        }

        public void StopRewind()
        {
            SendMessage("9");
        }

        public void SetTime(DateTime date)
        {
            SendMessage("t" + RFIDUltraCodes.SetTime + date.ToString("HH:mm:ss dd-MM-yyyy"));
        }

        public void SetTime()
        {
            SendMessage("t" + RFIDUltraCodes.SetTime + DateTime.Now.ToString("HH:mm:ss dd-MM-yyyy"));
        }

        public void GetTime()
        {
            SendMessage("r");
        }

        public void GetStatus()
        {
            SendMessage("?");
        }

        public void StartSending()
        {
            SendMessage("700");
        }

        public void StartSending(DateTime date)
        {
            SendMessage("700" + Constants.Timing.RFIDDateToEpoch(date));
        }

        public void StopSending()
        {
            SendMessage("s");
        }

        public void Disconnect() { }

        /**
         * Changing settings on the Ultra 
         */
        public void SetGPRS(bool turnOn)
        {
            SendMessage("u" + RFIDUltraCodes.GPRS + (turnOn ? "1" : "0") + RFIDUltraCodes.SettingsTerm);
        }

        public void SetGPRSIp(string address)
        {
            string[] nums = address.Split('.');
            if (nums.Length != 4)
            {
                return;
            }
            byte[] vals = new byte[4];
            for (int i = 0; i<4; i++)
            {
                vals[i] = byte.Parse(nums[i]);
            }
            SendMessage("u" + RFIDUltraCodes.GPRSIp + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm);
        }

        public void SetGPRSPort(int port)
        {
            SendMessage("u" + RFIDUltraCodes.GPRSPort + port.ToString() + RFIDUltraCodes.SettingsTerm);
        }

        public void SetAPNName(string name)
        {
            SendMessage("u" + RFIDUltraCodes.APNName + name + RFIDUltraCodes.SettingsTerm);
        }

        public void SetAPNUserName(string name)
        {
            SendMessage("u" + RFIDUltraCodes.APNUser + name + RFIDUltraCodes.SettingsTerm);
        }

        public void SetAPNPassword(string name)
        {
            SendMessage("u" + RFIDUltraCodes.APNPass + name + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - FCC
         * 0x01 - ETSI EN 300-220
         * 0x02 - ETSI EN 302-208
         * 0x03 - Australia, New Zealand, Hong Kong
         * 0x04 - Taiwan
         * 0x05 - Japan
         * 0x06 - Japan (Max 10mW power)
         * 0x07 - ETSI EN 302-208
         * 0x08 - Korea
         * 0x09 - Malaysia
         * 0x0A - China
         */
        public void SetRegion(byte regionCode)
        {
            SendMessage("u" + RFIDUltraCodes.Region + "" + regionCode + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - MACH1
         * 0x01 - LLRP
         */
        public void SetComProtocol(byte protocol)
        {
            SendMessage("u" + RFIDUltraCodes.ComProto + "" + protocol + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Decimal
         * 0x01 - Hexadecimal
         */
        public void SetChipOutputType(byte type)
        {
            SendMessage("u" + RFIDUltraCodes.ChipOutType + "" + type + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Off
         * 0x01 - On
         */
        public void SetAntennaStatus(int readerNo, int antennaNo, byte status)
        {
            byte code = 0x00;
            if (readerNo == 1)
            {
                switch (antennaNo)
                {
                    case 1:
                        code = RFIDUltraCodes.Read1Ant1;
                        break;
                    case 2:
                        code = RFIDUltraCodes.Read1Ant2;
                        break;
                    case 3:
                        code = RFIDUltraCodes.Read1Ant3;
                        break;
                    case 4:
                        code = RFIDUltraCodes.Read1Ant4;
                        break;
                    default:
                        return;
                }
            }
            else if (readerNo == 2)
            {
                switch (antennaNo)
                {
                    case 1:
                        code = RFIDUltraCodes.Read2Ant1;
                        break;
                    case 2:
                        code = RFIDUltraCodes.Read2Ant2;
                        break;
                    case 3:
                        code = RFIDUltraCodes.Read2Ant3;
                        break;
                    case 4:
                        code = RFIDUltraCodes.Read2Ant4;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                return;
            }
            SendMessage("u" + code + status + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Start 
         * 0x01 - Desktop
         * 0x02 - Raw
         * 0x03 - Finish
         * 0x04 - MTB Downhill
         */
        public void SetReaderMode(int readerNo, byte mode)
        {
            byte code;
            if (readerNo == 1)
            {
                code = RFIDUltraCodes.Read1Mode;
            }
            else if (readerNo == 2)
            {
                code = RFIDUltraCodes.Read2Mode;
            }
            else
            {
                return;
            }
            SendMessage("u" + code + mode + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Session 0
         * 0x01 - Session 1
         * 0x02 - Session 2
         * 0x03 - Session 3
         */
        public void SetReaderSession(int readerNo, byte session)
        {
            byte code;
            if (readerNo == 1)
            {
                code = RFIDUltraCodes.Read1Session;
            }
            else if (readerNo == 2)
            {
                code = RFIDUltraCodes.Read2Session;
            }
            else
            {
                return;
            }
            SendMessage("u" + code + session + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * Max Power of ...
         */
        public void SetReaderPower(int readerNo, int power)
        {
            byte code;
            if (readerNo == 1)
            {
                code = RFIDUltraCodes.Read1Power;
            }
            else if (readerNo == 2)
            {
                code = RFIDUltraCodes.Read2Power;
            }
            else
            {
                return;
            }
            SendMessage("u" + code + power.ToString() + RFIDUltraCodes.SettingsTerm);
        }

        public void SetReaderIp(int readerNo, string address)
        {
            string[] nums = address.Split('.');
            if (nums.Length != 4)
            {
                return;
            }
            byte[] vals = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                vals[i] = byte.Parse(nums[i]);
            }
            byte code;
            if (readerNo == 1)
            {
                code = RFIDUltraCodes.Read1Ip;
            }
            else if (readerNo == 2)
            {
                code = RFIDUltraCodes.Read2Ip;
            }
            else
            {
                return;
            }
            SendMessage("u" + code + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Per reader
         * 0x01 - Per box
         * 0x02 - First time seen
         */
        public void SetGatingMode(byte mode)
        {
            SendMessage("u" + RFIDUltraCodes.GatingMode + mode + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * Largest value accepted is 20 seconds.
         */
        public void SetGatingInterval(int seconds)
        {
            SendMessage("u" + RFIDUltraCodes.GatingInterval + seconds.ToString() + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Channel A
         * 0x01 - Channel B
         * 0x02 - Auto
         */
        public void SetChannelNumber(byte number)
        {
            SendMessage("u" + RFIDUltraCodes.GatingInterval + number + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Off
         * 0x01 - Soft
         * 0x02 - Loud
         */
        public void SetBeeperVolume(byte vol)
        {
            SendMessage("u" + RFIDUltraCodes.BeeperVolume + vol + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Don't set using GPS
         * 0x01 - Set using GPS
         * 0x02 - Loud ? (Probably an error in documentation...)
         */
        public void SetAutoGPSTime(byte gps)
        {
            SendMessage("u" + RFIDUltraCodes.AutoSetGPS + gps + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * Valid values are -23 to 23
         */
        public void SetTimeZone(int zone)
        {
            if (zone > 23 || zone < -23)
            {
                return;
            }
            SendMessage("u" + RFIDUltraCodes.TimeZone + zone + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Always send
         * 0x01 - Send only when requested
         */
        public void SetDataSending(byte value)
        {
            SendMessage("u" + RFIDUltraCodes.DataSending + value + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * Id can be any value from 1 to 255.
         */
        public void SetUltraId(int id)
        {
            SendMessage("u" + RFIDUltraCodes.UltraId + id + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Off
         * 0x01 - On
         */
        public void SetAntenna4Backup(int readerNo, bool value)
        {
            byte code;
            if (readerNo == 1)
            {
                code = RFIDUltraCodes.Read1Antenna4Backup;
            }
            else if (readerNo == 2)
            {
                code = RFIDUltraCodes.Read2Antenna4Backup;
            }
            else
            {
                return;
            }
            SendMessage("u" + code + (value ? 0x01 : 0x00) + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - beep always
         * 0x01 - beep when first seen
         */
        public void SetWhenToBeep(char value)
        {
            SendMessage("u" + RFIDUltraCodes.WhenBeep + value + RFIDUltraCodes.SettingsTerm);
        }
        
        public void SetUploadURL(string url)
        {
            SendMessage("u" + RFIDUltraCodes.UploadURL + url + RFIDUltraCodes.SettingsTerm);
        }

        public void SetGateway(string gateway)
        {
            string[] nums = gateway.Split('.');
            if (nums.Length != 4)
            {
                return;
            }
            char[] vals = new char[4];
            for (int i = 0; i < 4; i++)
            {
                vals[i] = (char)int.Parse(nums[i]);
            }
            SendMessage("u" + RFIDUltraCodes.Gateway + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm);
        }

        public void SetDNSServer(string server)
        {
            string[] nums = server.Split('.');
            if (nums.Length != 4)
            {
                return;
            }
            char[] vals = new char[4];
            for (int i = 0; i < 4; i++)
            {
                vals[i] = (char)int.Parse(nums[i]);
            }
            SendMessage("u" + RFIDUltraCodes.DNSServer + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm);
        }

        public void SaveSettings()
        {
            SendMessage("u" + RFIDUltraCodes.SettingsTerm);
        }

        public void QuerySettings()
        {
            SendMessage("U");
        }

        private void SendMessage(string msg)
        {
            Log.D("Timing.Interfaces.RFIDUltraInterface", "Sending message '" + msg + "'");
            sock.Send(Encoding.ASCII.GetBytes(msg + "\n"));
        }

        public void SetMainSocket(Socket sock)
        {
            this.sock = sock;
        }

        public void SetSettingsSocket(Socket sock)
        {
            return;
        }

        public bool SettingsEditable()
        {
            return false;
        }

        public void OpenSettings()
        {
            // TODO: Implement settings for RFID
        }

        public void CloseSettings()
        {
            // TODO: Implement settings for RFID
        }

        public enum RFIDMessage
        {
            CONNECTED, VOLTAGENORMAL, VOLTAGELOW, CHIPREAD, TIME, SETTINGVALUE, SETTINGCHANGE, STATUS, UNKNOWN, ERROR
        }
    }

    public class RFIDUltraCodes
    {
        public const byte SettingsTerm = 0xFF;
        public const byte GPRS = 0x01;
        public const byte GPRSIp = 0x02;
        public const byte GPRSPort = 0x03;
        public const byte APNName = 0x04;
        public const byte APNUser = 0x05;
        public const byte APNPass = 0x06;
        public const byte Region = 0x07;
        public const byte ComProto = 0x08;
        public const byte ChipOutType = 0x09;
        public const byte Read1Ant1 = 0x0C;
        public const byte Read1Ant2 = 0x0D;
        public const byte Read1Ant3 = 0x0E;
        public const byte Read1Ant4 = 0x0F;
        public const byte Read2Ant1 = 0x10;
        public const byte Read2Ant2 = 0x11;
        public const byte Read2Ant3 = 0x12;
        public const byte Read2Ant4 = 0x13;
        public const byte Read1Mode = 0x14;
        public const byte Read2Mode = 0x15;
        public const byte Read1Session = 0x16;
        public const byte Read2Session = 0x17;
        public const byte Read1Power = 0x18;
        public const byte Read2Power = 0x19;
        public const byte Read1Ip = 0x1A;
        public const byte Read2Ip = 0x1B;
        public const byte GatingMode = 0x1D;
        public const byte GatingInterval = 0x1E;
        public const byte ChannelNumber = 0x1F;
        public const byte BeeperVolume = 0x21;
        public const byte AutoSetGPS = 0x22;
        public const byte TimeZone = 0x23;
        public const byte DataSending = 0x24;
        public const byte UltraId = 0x25;
        public const byte Read1Antenna4Backup = 0x26;
        public const byte Read2Antenna4Backup = 0x27;
        public const byte WhenBeep = 0x28;
        public const byte UploadURL = 0x29;
        public const byte Gateway = 0x2A;
        public const byte DNSServer = 0x2B;
        public const byte SetTime = 0x20;
        public const byte RewindDelimiter = 0x0D;
        public const byte LogSize = 0x1C;
        public const byte LineFeed = 0x0A;
    }
}
