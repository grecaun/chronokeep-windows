using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Objects;
using Chronokeep.Objects.RFID;
using Chronokeep.UI.Timing.ReaderSettings;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Chronokeep.Timing.Interfaces
{
    public class RFIDUltraInterface : ITimingSystemInterface
    {
        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        StringBuilder buffer = new StringBuilder();
        Socket sock;
        IMainWindow window = null;

        private RFIDSettings settingsWindow = null;

        private static readonly Regex voltage = new Regex(@"^V=.*");
        private static readonly Regex connected = new Regex(@"^Connected,.*");
        private static readonly Regex chipread = new Regex(@"^0,.*");
        private static readonly Regex settinginfo = new Regex(@"^U.*");
        private static readonly Regex settingconfirmation = new Regex(@"^u.*");
        private static readonly Regex time = new Regex(@"^(\d{1,2}:\d{1,2}:\d{1,2} \d{1,2}-\d{1,2}-\d{4}) \(\d*\)");
        private static readonly Regex status = new Regex(@"^S=(\d)(\d)");
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
            List<Socket> output = [];
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Log.D("Timing.Interfaces.RFIDUltraInterface", "Attempting to connect to " + IpAddress + ":" + Port.ToString());
            try
            {
                IAsyncResult result = sock.BeginConnect(IpAddress, Port, null, null);
                result.AsyncWaitHandle.WaitOne(Constants.Readers.TIMEOUT, true);
                if (sock.Connected)
                {
                    sock.EndConnect(result);
                }
                else
                {
                    sock.Close();
                    throw new ApplicationException("Failed to connect to reader.");
                }
                output.Add(sock);
            }
            catch
            {
                Log.D("Timing.Interfaces.RFIDUltraInterface", "Unable to connect.");
                return null;
            }
            Log.D("Timing.Interfaces.RFIDUltraInterface", "Connected. Returning socket.");
            // Query current status of the reader
            GetStatus();
            return output;
        }

        public Dictionary<MessageType, List<string>> ParseMessages(string inMessage, Socket sock)
        {
            Dictionary<MessageType, List<string>> output = new Dictionary<MessageType, List<string>>();
            buffer.Append(inMessage);
            Match m = msg.Match(buffer.ToString());
            HashSet<string> ignoredChips = [];
            foreach (BibChipAssociation ignore in database.GetBibChips(-1))
            {
                ignoredChips.Add(ignore.Chip);
            }
            List<ChipRead> chipReads = [];
            RFIDSettingsHolder settingsHolder = null;
            while (m.Success)
            {
                buffer.Remove(m.Index, m.Length);
                string message = m.Value;
                // all incoming messages are terminated by a linefeed character (0x0A)
                // If "0,[...]" Chip read
                if (chipread.IsMatch(message))
                {
                    // Only add a chip read if it isn't on the ignore list.
                    string[] chipVals = message.Split(',');
                    string chip = chipVals[1].Trim();
                    if (!ignoredChips.Contains(chip))
                    {
                        ChipRead chipRead = new(
                            theEvent.Identifier,
                            locationId,
                            chip,
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
                        if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                        {
                            errorList = ([]);
                            output[MessageType.ERROR] = errorList;
                        }
                        errorList.Add("Invalid voltage value given.");
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
                    Log.D("Timing.Interfaces.RFIDUltraInterface", "It's a setting information message. " + message);
                    if (settingsHolder == null)
                    {
                        settingsHolder = new RFIDSettingsHolder();
                    }
                    char settingID = message[1];
                    string subMsg = message.Substring(2, message.Length - 3);
                    int tmp = -1;
                    switch (settingID)
                    {
                        case RFIDUltraCodes.UltraId:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Ultra ID: " + subMsg);
                            if (int.TryParse(subMsg, out tmp))
                            {
                                settingsHolder.UltraID = tmp;
                            }
                            break;
                        case RFIDUltraCodes.ChipOutType:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Chip out type: " + message[2]);
                            switch (message[2])
                            {
                                case '0':
                                    settingsHolder.ChipType = RFIDSettingsHolder.ChipTypeEnum.DEC;
                                    break;
                                case '1':
                                    settingsHolder.ChipType = RFIDSettingsHolder.ChipTypeEnum.HEX;
                                    break;
                                default:
                                    settingsHolder.ChipType = RFIDSettingsHolder.ChipTypeEnum.UNKNOWN;
                                    break;
                            }
                            break;
                        case RFIDUltraCodes.GatingMode:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Gating Mode: " + message[2]);
                            switch (message[2])
                            {
                                case '0':
                                    settingsHolder.GatingMode = RFIDSettingsHolder.GatingModeEnum.PER_READER;
                                    break;
                                case '1':
                                    settingsHolder.GatingMode = RFIDSettingsHolder.GatingModeEnum.PER_BOX;
                                    break;
                                case '2':
                                    settingsHolder.GatingMode = RFIDSettingsHolder.GatingModeEnum.FIRST_TIME_SEEN;
                                    break;
                                default:
                                    settingsHolder.GatingMode = RFIDSettingsHolder.GatingModeEnum.UNKNOWN;
                                    break;
                            }
                            break;
                        case RFIDUltraCodes.GatingInterval:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Gating Interval: " + subMsg);
                            if (int.TryParse(subMsg, out tmp))
                            {
                                settingsHolder.GatingInterval = tmp;
                            }
                            break;
                        case RFIDUltraCodes.WhenBeep:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "When beep: " + message[2]);
                            switch (message[2])
                            {
                                case '0':
                                    settingsHolder.Beep = RFIDSettingsHolder.BeepEnum.ALWAYS;
                                    break;
                                case '1':
                                    settingsHolder.Beep = RFIDSettingsHolder.BeepEnum.ONLY_FIRST_SEEN;
                                    break;
                                default:
                                    settingsHolder.Beep = RFIDSettingsHolder.BeepEnum.UNKNOWN;
                                    break;
                            }
                            break;
                        case RFIDUltraCodes.BeeperVolume:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Beeper volume: " + message[2]);
                            switch (message[2])
                            {
                                case '0':
                                    settingsHolder.BeepVolume = RFIDSettingsHolder.BeepVolumeEnum.OFF;
                                    break;
                                case '1':
                                    settingsHolder.BeepVolume = RFIDSettingsHolder.BeepVolumeEnum.SOFT;
                                    break;
                                case '2':
                                    settingsHolder.BeepVolume = RFIDSettingsHolder.BeepVolumeEnum.LOUD;
                                    break;
                                default:
                                    settingsHolder.BeepVolume = RFIDSettingsHolder.BeepVolumeEnum.UNKNOWN;
                                    break;
                            }
                            break;
                        case RFIDUltraCodes.AutoSetGPS:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Auto set gps: " + message[2]);
                            switch (message[2])
                            {
                                case '0':
                                    settingsHolder.SetFromGPS = RFIDSettingsHolder.GPSEnum.DONT_SET;
                                    break;
                                case '1':
                                    settingsHolder.SetFromGPS = RFIDSettingsHolder.GPSEnum.SET;
                                    break;
                                default:
                                    settingsHolder.SetFromGPS = RFIDSettingsHolder.GPSEnum.UNKNOWN;
                                    break;
                            }
                            break;
                        case RFIDUltraCodes.TimeZone:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Timezone: " + subMsg);
                            if (int.TryParse(subMsg, out tmp))
                            {
                                settingsHolder.TimeZone = tmp;
                            }
                            break;
                        default:
                            break;
                    }
                    if (!output.ContainsKey(MessageType.SETTINGVALUE))
                    {
                        output[MessageType.SETTINGVALUE] = null;
                    }
                }
                // If "u[...]" setting changed
                else if (settingconfirmation.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.RFIDUltraInterface", "It's a settings confirmation message. " + message + BitConverter.ToString(message.Select(c => (byte)c).ToArray()));
                    if (settingsHolder == null)
                    {
                        settingsHolder = new RFIDSettingsHolder();
                    }
                    char settingID = message[1];
                    switch (settingID)
                    {
                        case RFIDUltraCodes.UltraId:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Ultra ID set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.ChipOutType:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Chip out type set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.GatingMode:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Gating mode set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.GatingInterval:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Gating Interval set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.WhenBeep:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "When to beep set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.BeeperVolume:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Beeper volume set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.AutoSetGPS:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Set time via gps set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        case RFIDUltraCodes.TimeZone:
                            Log.D("Timing.Interfaces.RFIDUltraInterface", "Timezone set");
                            if (message[2] != (char)0x02)
                            {
                                Log.E("Timing.Interfaces.RFIDUltraInterface", "Setting not saved.");
                            }
                            break;
                        default:
                            break;
                    }
                    if (!output.ContainsKey(MessageType.SETTINGCHANGE))
                    {
                        output[MessageType.SETTINGCHANGE] = null;
                    }
                }
                // If "HH:MM:SS DD-MM-YYYY" then it's a time message
                else if (time.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.RFIDUltraInterface", "It's a time message.");
                    Match match = time.Match(message);
                    if (!output.TryGetValue(MessageType.TIME, out List<string> timeList))
                    {
                        timeList = ([]);
                        output[MessageType.TIME] = timeList;
                    }

                    timeList.Clear();
                    DateTime timeDT = DateTime.ParseExact(match.Groups[1].Value, "H:m:s d-M-yyyy", CultureInfo.CurrentCulture);
                    timeList.Add(timeDT.ToString("dd MMM yyyy  HH:mm:ss"));
                }
                // If "S=[...]" then status
                else if (status.IsMatch(message))
                {
                    Log.D("Timing.Interfaces.RFIDUltraInterface", "It's a status message.");
                    if (settingsHolder == null)
                    {
                        settingsHolder = new RFIDSettingsHolder();
                    }
                    Match match = status.Match(message);
                    if (!output.TryGetValue(MessageType.STATUS, out List<string> statusList))
                    {
                        statusList = ([]);
                        output[MessageType.STATUS] = statusList;
                    }
                    switch (Convert.ToInt32(match.Groups[1].Value))
                    {
                        case 0:
                            statusList.Add(TimingSystem.READING_STATUS_STOPPED);
                            settingsHolder.Status = RFIDSettingsHolder.StatusEnum.STOPPED;
                            break;
                        case 1:
                            statusList.Add(TimingSystem.READING_STATUS_READING);
                            settingsHolder.Status = RFIDSettingsHolder.StatusEnum.STARTED;
                            break;
                        default:
                            statusList.Add(TimingSystem.READING_STATUS_UNKNOWN);
                            settingsHolder.Status = RFIDSettingsHolder.StatusEnum.UNKNOWN;
                            break;
                    }
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
            if (settingsHolder != null && settingsWindow != null)
            {
                settingsWindow.UpdateView(settingsHolder);
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
            char[] vals = new char[4];
            for (int i = 0; i<4; i++)
            {
                vals[i] = (char)Convert.ToByte(nums[i]);
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
            SendMessage("u" + RFIDUltraCodes.Region + regionCode + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - MACH1
         * 0x01 - LLRP
         */
        public void SetComProtocol(char protocol)
        {
            SendMessage("u" + RFIDUltraCodes.ComProto + protocol + RFIDUltraCodes.SettingsTerm);
        }

        /**
         * 0x00 - Decimal
         * 0x01 - Hexadecimal
         */
        public void SetChipOutputType(char type)
        {
            SendMessage("u" + RFIDUltraCodes.ChipOutType + type + RFIDUltraCodes.SettingsTerm);
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
         * Max Power of 30?
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
            if (power > 30)
            {
                power = 30;
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
                vals[i] = (char)Convert.ToByte(nums[i]);
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
            if (id > 255 || id < 1)
            {
                return;
            }
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
            return true;
        }

        public void OpenSettings()
        {
            if (settingsWindow != null)
            {
                DialogBox.Show("Settings window already open.");
                return;
            }
            settingsWindow = new RFIDSettings(this);
            window.AddWindow(settingsWindow);
            settingsWindow.Show();
        }

        public void SettingsWindowFinalize()
        {
            window.WindowFinalize(settingsWindow);
            settingsWindow = null;
        }

        public void CloseSettings()
        {
            if (settingsWindow != null)
            {
                settingsWindow.CloseWindow();
            }
        }

        public bool WasShutdown()
        {
            return false;
        }

        public enum RFIDMessage
        {
            CONNECTED, VOLTAGENORMAL, VOLTAGELOW, CHIPREAD, TIME, SETTINGVALUE, SETTINGCHANGE, STATUS, UNKNOWN, ERROR
        }
    }

    public class RFIDUltraCodes
    {
        public const char SettingsTerm = (char)0xFF;
        public const char GPRS = (char)0x01;
        public const char GPRSIp = (char)0x02;
        public const char GPRSPort = (char)0x03;
        public const char APNName = (char)0x04;
        public const char APNUser = (char)0x05;
        public const char APNPass = (char)0x06;
        public const char Region = (char)0x07;
        public const char ComProto = (char)0x08;
        public const char ChipOutType = (char)0x09;
        public const char Read1Ant1 = (char)0x0C;
        public const char Read1Ant2 = (char)0x0D;
        public const char Read1Ant3 = (char)0x0E;
        public const char Read1Ant4 = (char)0x0F;
        public const char Read2Ant1 = (char)0x10;
        public const char Read2Ant2 = (char)0x11;
        public const char Read2Ant3 = (char)0x12;
        public const char Read2Ant4 = (char)0x13;
        public const char Read1Mode = (char)0x14;
        public const char Read2Mode = (char)0x15;
        public const char Read1Session = (char)0x16;
        public const char Read2Session = (char)0x17;
        public const char Read1Power = (char)0x18;
        public const char Read2Power = (char)0x19;
        public const char Read1Ip = (char)0x1A;
        public const char Read2Ip = (char)0x1B;
        public const char GatingMode = (char)0x1D;
        public const char GatingInterval = (char)0x1E;
        public const char ChannelNumber = (char)0x1F;
        public const char BeeperVolume = (char)0x21;
        public const char AutoSetGPS = (char)0x22;
        public const char TimeZone = (char)0x23;
        public const char DataSending = (char)0x24;
        public const char UltraId = (char)0x25;
        public const char Read1Antenna4Backup = (char)0x26;
        public const char Read2Antenna4Backup = (char)0x27;
        public const char WhenBeep = (char)0x28;
        public const char UploadURL = (char)0x29;
        public const char Gateway = (char)0x2A;
        public const char DNSServer = (char)0x2B;
        public const char SetTime = (char)0x20;
        public const char RewindDelimiter = (char)0x0D;
        public const char LogSize = (char)0x1C;
        public const char LineFeed = (char)0x0A;
    }
}
