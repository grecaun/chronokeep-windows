using EventDirector.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    class RFIDUltraInterface : ITimingSystemInterface
    {
        IDBInterface database;
        StringBuilder buffer = new StringBuilder();
        Socket sock;

        private static readonly Regex voltage = new Regex(@"^V=.*");
        private static readonly Regex connected = new Regex(@"^Connected,.*");
        private static readonly Regex chipread = new Regex(@"^0,.*");
        private static readonly Regex settinginfo = new Regex(@"^U.*");
        private static readonly Regex settingconfirmation = new Regex(@"^u.*");
        private static readonly Regex time = new Regex(@"^\d{2}:\d{2}:\d{2} \d{2}-\d{2}-\d{4} \(\d*\)");
        private static readonly Regex status = new Regex(@"^S=.*");
        private static readonly Regex msg = new Regex(@"^[^\n]*\n");

        public RFIDUltraInterface(IDBInterface database)
        {
            this.database = database;
        }

        public RFIDUltraInterface(IDBInterface database, Socket sock)
        {
            this.database = database;
            this.sock = sock;
        }

        public HashSet<MessageType> ParseMessages(string inMessage)
        {
            HashSet<MessageType> output = new HashSet<MessageType>();
            buffer.Append(inMessage);
            Match m = msg.Match(buffer.ToString());
            while (m.Success)
            {
                buffer.Remove(m.Index, m.Length);
                string message = m.Value;
                // all incoming messages are terminated by a linefeed character (0x0A)
                // If "0,[...]" Chip read
                if (chipread.IsMatch(message))
                {
                    string[] chipVals = message.Split(',');
                    ChipRead chipRead = new ChipRead
                    {
                        EventId = 0, // UPDATE LATER
                        Status = 0,
                        LocationID = 0,
                        ChipNumber = long.Parse(chipVals[1]),
                        Seconds = long.Parse(chipVals[2]),
                        Milliseconds = int.Parse(chipVals[3]),
                        Antenna = int.Parse(chipVals[4]),
                        RSSI = chipVals[5],
                        IsRewind = int.Parse(chipVals[6]),
                        Reader = chipVals[7],
                        Box = chipVals[8],
                        ReaderTime = chipVals[9],
                        StartTime = long.Parse(chipVals[10]),
                        LogId = int.Parse(chipVals[11])
                    };
                    chipRead.SetTime();
                    database.AddChipRead(chipRead);
                    output.Add(MessageType.CHIPREAD);
                }
                // If "V=" then it's a voltage status.
                else if (voltage.IsMatch(message))
                {
                    try
                    {
                        double voltVal = Double.Parse(message.Substring(2));
                    }
                    catch
                    {
                        output.Add(MessageType.ERROR);
                    }
                    output.Add(MessageType.VOLTAGENORMAL);
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
                    output.Add(MessageType.SETTINGVALUE);
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
                    output.Add(MessageType.SETTINGCHANGE);
                }
                // If "HH:MM:SS DD-MM-YYYY" then it's a time message
                else if (time.IsMatch(message))
                {
                    string time = message.Substring(0, 19);
                    string seconds = message.Substring(19);
                    seconds = seconds.Trim().Replace("(", "").Replace(")","");
                    try
                    {
                        DateTime now = DateTime.Now;
                        DateTime ultra = DateTime.ParseExact(time, "HH:mm:ss dd-MM-yyyy", null);
                        DateTime epochUlt = EpochToDate(Convert.ToInt64(seconds));
                        Log.D("Now " + now.ToLongTimeString() + " " + now.ToLongDateString() + 
                            " Ultra " + ultra.ToLongTimeString() + " " + ultra.ToLongDateString());
                        Log.D("We believe this to be equal to " + epochUlt.ToLongTimeString() + " " + epochUlt.ToLongDateString());
                    }
                    catch
                    {
                        output.Add(MessageType.ERROR);
                    }
                    output.Add(MessageType.TIME);
                }
                // If "S=[...]" then status
                else if (status.IsMatch(message))
                {
                    output.Add(MessageType.STATUS);
                }
                // If "Connected,[LastTimeSent]" that's a connection successful message.
                else if (connected.IsMatch(message))
                {
                    output.Add(MessageType.CONNECTED);
                }
                else
                {
                    output.Add(MessageType.UNKNOWN);
                }
                m = msg.Match(buffer.ToString());
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

        public void Rewind(DateTime start, DateTime end)
        {
            SendMessage("800" + DateToEpoch(start).ToString() + RFIDUltraCodes.RewindDelimiter + DateToEpoch(end).ToString());
        }

        public void Rewind()
        {
            SendMessage("8000" + RFIDUltraCodes.RewindDelimiter + "0");
        }

        public void Rewind(int start, int end)
        {
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
            SendMessage("700" + DateToEpoch(date));
        }

        public void StopSending()
        {
            SendMessage("s");
        }

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
            char[] vals = new char[4];
            for (int i = 0; i<4; i++)
            {
                vals[i] = (char) int.Parse(nums[i]);
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
        public void SetRegion(char regionCode)
        {
            SendMessage("u" + RFIDUltraCodes.Region + "" + regionCode + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - MACH1
         * 0x01 - LLRP
         */
        public void SetComProtocol(char protocol)
        {
            SendMessage("u" + RFIDUltraCodes.ComProto + "" + protocol + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Decimal
         * 0x01 - Hexadecimal
         */
        public void SetChipOutputType(char type)
        {
            SendMessage("u" + RFIDUltraCodes.ChipOutType + "" + type + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Off
         * 0x01 - On
         */
        public void SetAntennaStatus(int readerNo, int antennaNo, char status)
        {
            char code = (char)0x00;
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
        public void SetReaderMode(int readerNo, char mode)
        {
            char code;
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
        public void SetReaderSession(int readerNo, char session)
        {
            char code;
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
            char code;
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
            char[] vals = new char[4];
            for (int i = 0; i < 4; i++)
            {
                vals[i] = (char)int.Parse(nums[i]);
            }
            char code;
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
        public void SetGatingMode(char mode)
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
        public void SetChannelNumber(char number)
        {
            SendMessage("u" + RFIDUltraCodes.GatingInterval + number + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Off
         * 0x01 - Soft
         * 0x02 - Loud
         */
        public void SetBeeperVolume(char vol)
        {
            SendMessage("u" + RFIDUltraCodes.BeeperVolume + vol + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Don't set using GPS
         * 0x01 - Set using GPS
         * 0x02 - Loud ? (Probably an error in documentation...)
         */
        public void SetAutoGPSTime(char gps)
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
        public void SetDataSending(char value)
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
            char code;
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
            Log.D("Sending message '" + msg + "'");
            sock.Send(Encoding.ASCII.GetBytes(msg + "\n"));
        }

        public static long DateToEpoch(DateTime date)
        {
            var ticks = date.Ticks - new DateTime(1980,1,1,0,0,0,DateTimeKind.Utc).Ticks;
            return ticks / TimeSpan.TicksPerSecond;
        }

        public static DateTime EpochToDate(long date)
        {
            return new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(date * TimeSpan.TicksPerSecond);
        }

        public void SettingsWindow()
        {
            throw new NotImplementedException();
        }

        public void ClockWindow()
        {
            throw new NotImplementedException();
        }

        public void SetMainSocket(Socket sock)
        {
            this.sock = sock;
        }

        public void SetSettingsSocket(Socket sock)
        {
            return;
        }

        public enum RFIDMessage
        {
            CONNECTED, VOLTAGENORMAL, VOLTAGELOW, CHIPREAD, TIME, SETTINGVALUE, SETTINGCHANGE, STATUS, UNKNOWN, ERROR
        }
    }

    public class RFIDUltraCodes
    {
        public static readonly char SettingsTerm = (char) 0xFF;
        public static readonly char GPRS = (char)0x01;
        public static readonly char GPRSIp = (char)0x02;
        public static readonly char GPRSPort = (char)0x03;
        public static readonly char APNName = (char)0x04;
        public static readonly char APNUser = (char)0x05;
        public static readonly char APNPass = (char)0x06;
        public static readonly char Region = (char)0x07;
        public static readonly char ComProto = (char)0x08;
        public static readonly char ChipOutType = (char)0x09;
        public static readonly char Read1Ant1 = (char)0x0C;
        public static readonly char Read1Ant2 = (char)0x0D;
        public static readonly char Read1Ant3 = (char)0x0E;
        public static readonly char Read1Ant4 = (char)0x0F;
        public static readonly char Read2Ant1 = (char)0x10;
        public static readonly char Read2Ant2 = (char)0x11;
        public static readonly char Read2Ant3 = (char)0x12;
        public static readonly char Read2Ant4 = (char)0x13;
        public static readonly char Read1Mode = (char)0x14;
        public static readonly char Read2Mode = (char)0x15;
        public static readonly char Read1Session = (char)0x16;
        public static readonly char Read2Session = (char)0x17;
        public static readonly char Read1Power = (char)0x18;
        public static readonly char Read2Power = (char)0x19;
        public static readonly char Read1Ip = (char)0x1A;
        public static readonly char Read2Ip = (char)0x1B;
        public static readonly char GatingMode = (char)0x1D;
        public static readonly char GatingInterval = (char)0x1E;
        public static readonly char ChannelNumber = (char)0x1F;
        public static readonly char BeeperVolume = (char)0x21;
        public static readonly char AutoSetGPS = (char)0x22;
        public static readonly char TimeZone = (char)0x23;
        public static readonly char DataSending = (char)0x24;
        public static readonly char UltraId = (char)0x25;
        public static readonly char Read1Antenna4Backup = (char)0x26;
        public static readonly char Read2Antenna4Backup = (char)0x27;
        public static readonly char WhenBeep = (char)0x28;
        public static readonly char UploadURL = (char)0x29;
        public static readonly char Gateway = (char)0x2A;
        public static readonly char DNSServer = (char)0x2B;
        public static readonly char SetTime = (char)0x20;
        public static readonly char RewindDelimiter = (char)0x0D;
        public static readonly char LogSize = (char)0x1C;
        public static readonly char LineFeed = (char)0x0A;
    }
}
