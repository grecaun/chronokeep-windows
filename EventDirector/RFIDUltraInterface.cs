using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    class RFIDUltraInterface
    {
        IDBInterface database;
        Socket sock;

        private static readonly Regex voltage = new Regex(@"^V=.*");
        private static readonly Regex connected = new Regex(@"^Connected,.*");
        private static readonly Regex chipread = new Regex(@"^0,.*");
        private static readonly Regex settinginfo = new Regex(@"^U.*");
        private static readonly Regex settingconfirmation = new Regex(@"^u.*");
        private static readonly Regex time = new Regex(@"^\d{2}:\d{2}:\d{2} \d{2}-\d{2}-\d{4}");
        private static readonly Regex status = new Regex(@"^S=.*");

        public RFIDUltraInterface(IDBInterface database)
        {
            this.database = database;
        }

        public RFIDUltraInterface(IDBInterface database, Socket sock)
        {
            this.database = database;
            this.sock = sock;
        }

        public RFIDMessage ParseMessage(String message)
        {
            // all incoming messages are terminated by a linefeed character (0x0A)
            // If "0,[...]" Chip read
            if (chipread.IsMatch(message))
            {
                string[] chipVals = message.Split(',');
                ChipRead chipRead = new ChipRead
                {
                    EventId = 0,
                    ChipNumber = long.Parse(chipVals[1]),
                    Seconds = long.Parse(chipVals[2]),
                    Milliseconds = int.Parse(chipVals[3]),
                    Antenna = int.Parse(chipVals[4]),
                    RSSI = int.Parse(chipVals[5]),
                    IsRewind = int.Parse(chipVals[6]),
                    ReaderNumber = int.Parse(chipVals[7]),
                    UltraId = int.Parse(chipVals[8]),
                    ReaderTime = chipVals[9],
                    StartTime = long.Parse(chipVals[10]),
                    LogId = int.Parse(chipVals[11])
                };
                chipRead.SetTime();
                database.AddChipRead(chipRead);
                return RFIDMessage.CHIPREAD;
            }
            // If "V=" then it's a voltage status.
            if (voltage.IsMatch(message))
            {
                try
                {
                    double voltVal = Double.Parse(message.Substring(2));
                }
                catch
                {
                    return RFIDMessage.ERROR;
                }
                return RFIDMessage.VOLTAGENORMAL;
            }
            // If "U[...]" Setting information
            if (settinginfo.IsMatch(message))
            {
                char settingID = message[1];
                switch (settingID)
                {
                    default:
                        break;
                }
                return RFIDMessage.SETTINGVALUE;
            }
            // If "u[...]" setting changed
            if (settingconfirmation.IsMatch(message))
            {
                char settingID = message[1];
                switch (settingID)
                {
                    default:
                        break;
                }
                return RFIDMessage.SETTINGCHANGE;
            }
            // If "HH:MM:SS DD-MM-YYYY" then it's a time message
            if (time.IsMatch(message))
            {
                try
                {
                    DateTime now = DateTime.Now;
                    DateTime ultra = DateTime.ParseExact(message, "HH:mm:ss dd-MM-yyyy", null);
                }
                catch
                {
                    return RFIDMessage.ERROR;
                }
                return RFIDMessage.TIME;
            }
            // If "S=[...]" then status
            if (status.IsMatch(message))
            {
                return RFIDMessage.STATUS;
            }
            // If "Connected,[LastTimeSent]" that's a connection successful message.
            if (connected.IsMatch(message))
            {
                return RFIDMessage.CONNECTED;
            }
            return RFIDMessage.UNKNOWN;
        }

        public void StartReading()
        {
            sock.Send(Encoding.ASCII.GetBytes("R"));
        }

        public void StopReading()
        {
            sock.Send(Encoding.ASCII.GetBytes("S"));
        }

        public void Rewind(DateTime start, DateTime end)
        {
            sock.Send(Encoding.ASCII.GetBytes("800" + DateToEpoch(start).ToString() + RFIDUltraCodes.RewindDelimiter + DateToEpoch(end).ToString()));
        }

        public void Rewind()
        {
            sock.Send(Encoding.ASCII.GetBytes("8000" + RFIDUltraCodes.RewindDelimiter + "0"));
        }

        public void Rewind(int start, int end)
        {
            sock.Send(Encoding.ASCII.GetBytes("600" + start.ToString() + RFIDUltraCodes.RewindDelimiter + end.ToString()));
        }

        public void StopRewind()
        {
            sock.Send(Encoding.ASCII.GetBytes("9"));
        }

        public void SetTime(DateTime date)
        {
            sock.Send(Encoding.ASCII.GetBytes("t" + RFIDUltraCodes.SetTime + date.ToString("HH:mm:ss dd-MM-yyyy")));
        }

        public void SetTime()
        {
            sock.Send(Encoding.ASCII.GetBytes("t" + RFIDUltraCodes.SetTime + DateTime.Now.ToString("HH:mm:ss dd-MM-yyyy")));
        }

        public void GetTime()
        {
            sock.Send(Encoding.ASCII.GetBytes("r"));
        }

        public void GetStatus()
        {
            sock.Send(Encoding.ASCII.GetBytes("?"));
        }

        public void StartSending()
        {
            sock.Send(Encoding.ASCII.GetBytes("700"));
        }

        public void StartSending(DateTime date)
        {
            sock.Send(Encoding.ASCII.GetBytes("700" + DateToEpoch(date)));
        }

        public void StopSending()
        {
            sock.Send(Encoding.ASCII.GetBytes("s"));
        }

        /**
         * Changing settings on the Ultra 
         */
        public void SetGPRS(bool turnOn)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GPRS + (turnOn ? "1" : "0") + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GPRSIp + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm));
        }

        public void SetGPRSPort(int port)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GPRSPort + port.ToString() + RFIDUltraCodes.SettingsTerm));
        }

        public void SetAPNName(string name)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.APNName + name + RFIDUltraCodes.SettingsTerm));
        }

        public void SetAPNUserName(string name)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.APNUser + name + RFIDUltraCodes.SettingsTerm));
        }

        public void SetAPNPassword(string name)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.APNPass + name + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.Region + "" + regionCode + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - MACH1
         * 0x01 - LLRP
         */
        public void SetComProtocol(char protocol)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.ComProto + "" + protocol + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Decimal
         * 0x01 - Hexadecimal
         */
        public void SetChipOutputType(char type)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.ChipOutType + "" + type + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + status + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + mode + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + session + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + power.ToString() + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Per reader
         * 0x01 - Per box
         * 0x02 - First time seen
         */
        public void SetGatingMode(char mode)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GatingMode + mode + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * Largest value accepted is 20 seconds.
         */
        public void SetGatingInterval(int seconds)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GatingInterval + seconds.ToString() + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Channel A
         * 0x01 - Channel B
         * 0x02 - Auto
         */
        public void SetChannelNumber(char number)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.GatingInterval + number + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Off
         * 0x01 - Soft
         * 0x02 - Loud
         */
        public void SetBeeperVolume(char vol)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.BeeperVolume + vol + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Don't set using GPS
         * 0x01 - Set using GPS
         * 0x02 - Loud ? (Probably an error in documentation...)
         */
        public void SetAutoGPSTime(char gps)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.AutoSetGPS + gps + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.TimeZone + zone + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - Always send
         * 0x01 - Send only when requested
         */
        public void SetDataSending(char value)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.DataSending + value + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * Id can be any value from 1 to 255.
         */
        public void SetUltraId(int id)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.UltraId + id + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + code + (value ? 0x01 : 0x00) + RFIDUltraCodes.SettingsTerm));
        }

        /**
         * 0x00 - beep always
         * 0x01 - beep when first seen
         */
        public void SetWhenToBeep(char value)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.WhenBeep + value + RFIDUltraCodes.SettingsTerm));
        }
        
        public void SetUploadURL(string url)
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.UploadURL + url + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.Gateway + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm));
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
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.DNSServer + vals[0] + vals[1] + vals[2] + vals[3] + RFIDUltraCodes.SettingsTerm));
        }

        public void SaveSettings()
        {
            sock.Send(Encoding.ASCII.GetBytes("u" + RFIDUltraCodes.SettingsTerm));
        }

        public void QuerySettings()
        {
            sock.Send(Encoding.ASCII.GetBytes("U"));
        }

        public static long DateToEpoch(DateTime date)
        {
            var ticks = date.Ticks - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).Ticks;
            return ticks / TimeSpan.TicksPerSecond;
        }

        public static DateTime EpochToDate(long date)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(date * TimeSpan.TicksPerSecond);
        }

        public enum RFIDMessage
        {
            CONNECTED, VOLTAGENORMAL, VOLTAGELOW, CHIPREAD, TIME, SETTINGVALUE, SETTINGCHANGE, STATUS, UNKNOWN, ERROR
        }
    }

    public class ChipRead
    {
        public int EventId { get; set; }
        public long ChipNumber { get; set; }
        public long Seconds { get; set; }
        public int Milliseconds { get; set; }
        public DateTime Time { get; set; }
        public int Antenna { get; set; }
        public int RSSI { get; set; }
        public int IsRewind { get; set; }
        public int ReaderNumber { get; set; }
        public int UltraId { get; set; }
        public string ReaderTime { get; set; }
        public long StartTime { get; set; }
        public int LogId { get; set; }

        public void SetTime()
        {
            Time = RFIDUltraInterface.EpochToDate(Seconds);
            Time.AddMilliseconds(Milliseconds);
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
