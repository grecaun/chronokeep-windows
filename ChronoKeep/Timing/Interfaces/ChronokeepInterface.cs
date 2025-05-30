﻿using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Objects.ChronokeepPortal.Requests;
using Chronokeep.Objects.ChronokeepPortal.Responses;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.UI.Timing.ReaderSettings;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace Chronokeep.Timing.Interfaces
{
    internal class ChronokeepInterface : ITimingSystemInterface
    {
        private const int PARTICIPANTS_COUNT = 50;

        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        StringBuilder buffer = new StringBuilder();
        Socket sock;
        IMainWindow window = null;

        private bool wasShutdown = false;

        private ChronokeepSettings settingsWindow = null;
        private string reader_ip = "";
        private string reader_name = "";

        private static readonly Regex zeroconf = new Regex(@"^\[(?'PORTAL_NAME'[^|]*)\|(?'PORTAL_ID'[^|]*)\|(?'PORTAL_PORT'\d{1,5})\]");
        private static readonly Regex msg = new Regex(@"^[^\n]*\n");

        public ChronokeepInterface(IDBInterface database, int locationId, IMainWindow window)
        {
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            this.locationId = locationId;
            this.window = window;
        }

        public List<Socket> Connect(string IP_Address, int Port)
        {
            reader_ip = IP_Address;
            List<Socket> output = [];
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Log.D("Timing.Interfaces.ChronokeepInterface", "Attempting to get port from server.");
                using (UdpClient client = new UdpClient(AddressFamily.InterNetwork))
                {
                    byte[] msg = Encoding.Default.GetBytes(Constants.Network.CHRONOKEEP_ZCONF_CONNECT_MSG);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP_Address), Constants.Network.CHRONOKEEP_ZCONF_PORT);
                    client.Send(msg, msg.Length, endPoint);
                    client.Client.ReceiveTimeout = Constants.Readers.TIMEOUT;
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
                        int port = Constants.Network.CHRONOKEEP_ZCONF_PORT;
                        reader_name = match.Groups["PORTAL_NAME"].Value;
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
            List<ChipRead> chipReads = [];
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
                            try
                            {
                                ReadersResponse readRes = JsonSerializer.Deserialize<ReadersResponse>(message);
                                if (settingsWindow != null)
                                {
                                    settingsWindow.UpdateView(new PortalSettingsHolder
                                        {
                                            Readers = readRes.List,
                                            Changes = { PortalSettingsHolder.ChangeType.READERS }
                                        }
                                        );
                                }
                                int oneReadingCount = 0;
                                foreach (PortalReader reader in readRes.List)
                                {
                                    if (reader.Reading)
                                    {
                                        oneReadingCount++;
                                    }
                                }
                                if (!output.TryGetValue(MessageType.STATUS, out List<string> readersStatusList))
                                {
                                    readersStatusList = [];
                                    output[MessageType.STATUS] = readersStatusList;
                                }
                                if (oneReadingCount == 0)
                                {
                                    readersStatusList.Add(TimingSystem.READING_STATUS_STOPPED);
                                }
                                else if (oneReadingCount == readRes.List.Count)
                                {
                                    output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_READING);
                                }
                                else
                                {
                                    output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_PARTIAL);
                                }
                                if (!output.TryGetValue(MessageType.SETTINGVALUE, out List<string> settingList))
                                {
                                    settingList = [];
                                    output[MessageType.SETTINGVALUE] = settingList;
                                }

                                settingList.Add(message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing readers. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }

                                errorList.Add("Error processing readers.");
                            }
                            break;
                        case Response.READER_ANTENNAS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent reader antennas message.");
                            try
                            {
                                ReaderAntennasResponse antRes = JsonSerializer.Deserialize<ReaderAntennasResponse>(message);
                                if (settingsWindow != null)
                                {
                                    settingsWindow.UpdateView(new PortalSettingsHolder
                                    {
                                        Antennas = new PortalSettingsHolder.ReaderAntennas()
                                        {
                                            ReaderName = antRes.ReaderName,
                                            Antennas = antRes.Antennas,
                                        },
                                        Changes = { PortalSettingsHolder.ChangeType.ANTENNAS }
                                    }
                                    );
                                }
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing reader antennas. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }

                                errorList.Add("Error processing reader antennas.");
                            }
                            break;
                        case Response.ERROR:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent error message.");
                            try
                            {
                                ErrorResponse err = JsonSerializer.Deserialize<ErrorResponse>(message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error sent to us is of type '" + err.Value.Type + "' and has message '" + err.Value.Message + "'.");
                                window.ShowNotificationDialog(reader_name, reader_ip, new RemoteNotification
                                {
                                    Type = err.Value.Type,
                                    When = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    Message = err.Value.Message
                                });
                                errorList.Add(err.Value.Message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to process chip read. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing chip read.");
                            }
                            break;
                        case Response.SETTINGS:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent settings message.");
                            try
                            {
                                SettingsResponse settingsList = JsonSerializer.Deserialize<SettingsResponse>(message);
                                if (settingsWindow != null)
                                {
                                    PortalSettingsHolder updSettings = new PortalSettingsHolder();
                                    foreach (PortalSetting set in settingsList.List)
                                    {
                                        switch (set.Name)
                                        {
                                            case PortalSetting.SETTING_PORTAL_NAME:
                                                updSettings.Name = set.Value;
                                                break;
                                            case PortalSetting.SETTING_SIGHTING_PERIOD:
                                                updSettings.SightingPeriod = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_READ_WINDOW:
                                                updSettings.ReadWindow = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_CHIP_TYPE:
                                                updSettings.ChipType = set.Value == PortalSetting.TYPE_CHIP_DEC ? PortalSettingsHolder.ChipTypeEnum.DEC : PortalSettingsHolder.ChipTypeEnum.HEX;
                                                break;
                                            case PortalSetting.SETTING_PLAY_SOUND:
                                                updSettings.PlaySound = bool.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_VOLUME:
                                                updSettings.Volume = double.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_VOICE:
                                                if (set.Value == PortalSetting.VOICE_EMILY)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.EMILY;
                                                }
                                                else if (set.Value == PortalSetting.VOICE_MICHAEL)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.MICHAEL;
                                                }
                                                else if (set.Value == PortalSetting.VOICE_CUSTOM)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.CUSTOM;
                                                }
                                                else
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.EMILY;
                                                }
                                                break;
                                            case PortalSetting.SETTING_UPLOAD_INTERVAL:
                                                updSettings.UploadInterval = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_NTFY_URL:
                                                updSettings.NtfyURL = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_TOPIC:
                                                updSettings.NtfyTopic = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_USER:
                                                updSettings.NtfyUser = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_PASS:
                                                updSettings.NtfyPass = set.Value;
                                                break;
                                        }
                                        updSettings.Changes.Add(PortalSettingsHolder.ChangeType.SETTINGS);
                                    }
                                    settingsWindow.UpdateView(updSettings);
                                }
                                if (!output.TryGetValue(MessageType.SETTINGVALUE, out List<string> settingList))
                                {
                                    settingList = [];
                                    output[MessageType.SETTINGVALUE] = settingList;
                                }

                                settingList.Add(message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing settings. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing settings.");
                            }
                            break;
                        case Response.API_LIST:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent api list message.");
                            try
                            {
                                ApiListResponse apiList = JsonSerializer.Deserialize<ApiListResponse>(message);
                                if (settingsWindow != null)
                                {
                                    settingsWindow.UpdateView(new PortalSettingsHolder
                                        {
                                            APIs = apiList.List,
                                            Changes = { PortalSettingsHolder.ChangeType.APIS }
                                        }
                                        );
                                }
                                if (!output.TryGetValue(MessageType.SETTINGVALUE, out List<string> settingList))
                                {
                                    settingList = [];
                                    output[MessageType.SETTINGVALUE] = settingList;
                                }

                                settingList.Add(message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing api list. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing api list.");
                            }
                            break;
                        case Response.SETTINGS_ALL:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent all settings message.");
                            try
                            {
                                SettingsAllResponse allSettings = JsonSerializer.Deserialize<SettingsAllResponse>(message);
                                if (settingsWindow != null)
                                {
                                    PortalSettingsHolder updSettings = new PortalSettingsHolder
                                    {
                                        Readers = allSettings.Readers,
                                        APIs = allSettings.APIs,
                                        AutoUpload = allSettings.AutoUpload,
                                        PortalVersion = allSettings.PortalVersion,
                                    };
                                    foreach (PortalSetting set in allSettings.Settings)
                                    {
                                        switch (set.Name)
                                        {
                                            case PortalSetting.SETTING_PORTAL_NAME:
                                                updSettings.Name = set.Value;
                                                break;
                                            case PortalSetting.SETTING_SIGHTING_PERIOD:
                                                updSettings.SightingPeriod = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_READ_WINDOW:
                                                updSettings.ReadWindow = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_CHIP_TYPE:
                                                updSettings.ChipType = set.Value == PortalSetting.TYPE_CHIP_DEC ? PortalSettingsHolder.ChipTypeEnum.DEC : PortalSettingsHolder.ChipTypeEnum.HEX;
                                                break;
                                            case PortalSetting.SETTING_PLAY_SOUND:
                                                updSettings.PlaySound = bool.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_VOLUME:
                                                updSettings.Volume = double.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_VOICE:
                                                if (set.Value == PortalSetting.VOICE_EMILY)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.EMILY;
                                                }
                                                else if (set.Value == PortalSetting.VOICE_MICHAEL)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.MICHAEL;
                                                }
                                                else if (set.Value == PortalSetting.VOICE_CUSTOM)
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.CUSTOM;
                                                }
                                                else
                                                {
                                                    updSettings.Voice = PortalSettingsHolder.VoiceType.EMILY;
                                                }
                                                break;
                                            case PortalSetting.SETTING_UPLOAD_INTERVAL:
                                                updSettings.UploadInterval = int.Parse(set.Value);
                                                break;
                                            case PortalSetting.SETTING_NTFY_URL:
                                                updSettings.NtfyURL = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_TOPIC:
                                                updSettings.NtfyTopic = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_USER:
                                                updSettings.NtfyUser = set.Value;
                                                break;
                                            case PortalSetting.SETTING_NTFY_PASS:
                                                updSettings.NtfyPass = set.Value;
                                                break;
                                            case PortalSetting.SETTING_ENABLE_NTFY:
                                                updSettings.EnableNTFY = set.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                                                break;
                                        }
                                    }
                                    int settingsReadingCount = 0;
                                    foreach (PortalReader reader in allSettings.Readers)
                                    {
                                        if (reader.Reading)
                                        {
                                            settingsReadingCount++;
                                        }
                                    }
                                    if (!output.TryGetValue(MessageType.STATUS, out List<string> settingsStatusList))
                                    {
                                        settingsStatusList = [];
                                        output[MessageType.STATUS] = settingsStatusList;
                                    }
                                    if (settingsReadingCount == 0)
                                    {
                                        settingsStatusList.Add(TimingSystem.READING_STATUS_STOPPED);
                                    }
                                    else if (settingsReadingCount == allSettings.Readers.Count)
                                    {
                                        output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_READING);
                                    }
                                    else
                                    {
                                        output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_PARTIAL);
                                    }
                                    updSettings.Changes.Add(PortalSettingsHolder.ChangeType.SETTINGS);
                                    updSettings.Changes.Add(PortalSettingsHolder.ChangeType.READERS);
                                    updSettings.Changes.Add(PortalSettingsHolder.ChangeType.APIS);
                                    settingsWindow?.UpdateView(
                                        updSettings
                                        );
                                }
                                if (!output.TryGetValue(MessageType.SETTINGVALUE, out List<string> settingList))
                                {
                                    settingList = [];
                                    output[MessageType.SETTINGVALUE] = settingList;
                                }

                                settingList.Add(message);
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing all settings. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing settings.");
                            }
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
                                            pRead.Identifier,
                                            Constants.Timing.UTCSecondsToRFIDSeconds(pRead.Seconds),
                                            pRead.Milliseconds,
                                            pRead.Antenna,
                                            pRead.RSSI,
                                            pRead.Reader,
                                            pRead.Type == PortalRead.READ_KIND_CHIP ? Constants.Timing.CHIPREAD_TYPE_CHIP : Constants.Timing.CHIPREAD_TYPE_MANUAL,
                                            Constants.Timing.UTCToLocalDate(pRead.ReaderSeconds, pRead.ReaderMilliseconds).ToString("yyyy/MM/dd HH:mm:ss.fff")
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
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing chip read.");
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
                                if (!output.TryGetValue(MessageType.TIME, out List<string> timeList))
                                {
                                    timeList = [];
                                    output[MessageType.TIME] = timeList;
                                }

                                timeList.Clear();
                                DateTime timeDT = DateTime.ParseExact(t.Local, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                                timeList.Add(timeDT.ToString("dd MMM yyyy  HH:mm:ss"));
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Unable to process time message. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing time message.");
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
                            try
                            {
                                ReadAutoUploadResponse autoUploadResponse = JsonSerializer.Deserialize<ReadAutoUploadResponse>(message);
                                settingsWindow.UpdateView(new PortalSettingsHolder()
                                    {
                                        AutoUpload = autoUploadResponse.Status,
                                    }
                                    );
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error auto upload message. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing auto upload message.");
                            }
                            break;
                        case Response.CONNECTION_SUCCESSFUL:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent connection successful message.");
                            ConnectionSuccessfulResponse connectionResponse = JsonSerializer.Deserialize<ConnectionSuccessfulResponse>(message);
                            int readingCount = 0;
                            foreach (PortalReader reader in connectionResponse.Readers)
                            {
                                if (reader.Reading)
                                {
                                    readingCount++;
                                }
                            }
                            if (!output.TryGetValue(MessageType.STATUS, out List<string> statusList))
                            {
                                statusList = [];
                                output[MessageType.STATUS] = statusList;
                            }
                            if (readingCount == 0)
                            {
                                statusList.Add(TimingSystem.READING_STATUS_STOPPED);
                            }
                            else if (readingCount == connectionResponse.Readers.Count)
                            {
                                output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_READING);
                            }
                            else
                            {
                                output[MessageType.STATUS].Add(TimingSystem.READING_STATUS_PARTIAL);
                            }
                            if (!output.ContainsKey(MessageType.CONNECTED))
                            {
                                output[MessageType.CONNECTED] = null;
                            }
                            break;
                        case Response.DISCONNECT:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent disconnect message.");
                            if (!output.ContainsKey(MessageType.DISCONNECT))
                            {
                                output[MessageType.DISCONNECT] = null;
                            }
                            break;
                        case Response.NOTIFICATION:
                            Log.D("Timing.Interfaces.ChronokeepInterface", "Reader sent notification message.");
                            try
                            {
                                NotificationResponse notRes = JsonSerializer.Deserialize<NotificationResponse>(message);
                                string msg = "";
                                switch (notRes.Type)
                                {
                                    case PortalNotification.UPS_DISCONNECTED:
                                        msg = "Portal at " + reader_ip + " UPS has been disconnected.";
                                        break;
                                    case PortalNotification.UPS_CONNECTED:
                                        msg = "Portal at " + reader_ip + " UPS connection has been re-established.";
                                        break;
                                    case PortalNotification.UPS_ON_BATTERY:
                                        msg = "Portal at " + reader_ip + " UPS is working from battery power.";
                                        break;
                                    case PortalNotification.UPS_LOW_BATTERY:
                                        msg = "Portal at " + reader_ip + " UPS battery is low. Shutdown imminent.";
                                        break;
                                    case PortalNotification.UPS_ONLINE:
                                        msg = "Portal at " + reader_ip + " UPS is back on line power.";
                                        break;
                                    case PortalNotification.SHUTTING_DOWN:
                                        msg = "Portal at " + reader_ip + " is shutting down.";
                                        break;
                                    case PortalNotification.RESTARTING:
                                        msg = "Portal at " + reader_ip + " is restarting.";
                                        break;
                                    case PortalNotification.HIGH_TEMP:
                                        msg = "Portal at " + reader_ip + " temperature is high.";
                                        break;
                                    case PortalNotification.MAX_TEMP:
                                        msg = "Portal at " + reader_ip + " temperature is very high. Throttling will most likely occur.";
                                        break;
                                    case PortalNotification.BATTERY_LOW:
                                        msg = "Portal at " + reader_ip + " is indicating the battery is low.";
                                        break;
                                    case PortalNotification.BATTERY_CRITICAL:
                                        msg = "Portal at " + reader_ip + " is indicating the battery is critical.";
                                        break;
                                }
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
                                {
                                    DialogBox.Show(msg);
                                });
                            }
                            catch (Exception e)
                            {
                                Log.E("Timing.Interfaces.ChronokeepInterface", "Error processing reader antennas. " + e.Message);
                                if (!output.TryGetValue(MessageType.ERROR, out List<string> errorList))
                                {
                                    errorList = [];
                                    output[MessageType.ERROR] = errorList;
                                }
                                errorList.Add("Error processing reader antennas.");
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
                Time = date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffzzz")
            }));
        }

        public void StartReading()
        {
            SendMessage(JsonSerializer.Serialize(new ReaderStartAllRequest()));
        }

        public void StartSending() { }

        public void StopReading()
        {
            SendMessage(JsonSerializer.Serialize(new ReaderStopAllRequest()));
        }

        public void StopSending() { }

        public void SendQuit()
        {
            SendMessage(JsonSerializer.Serialize(new QuitRequest { }));
            wasShutdown = true;
        }

        public void SendRestart()
        {
            SendMessage(JsonSerializer.Serialize(new RestartRequest { }));
            wasShutdown = true;
        }

        public void SendShutdown()
        {
            SendMessage(JsonSerializer.Serialize(new ShutdownRequest { }));
            wasShutdown = true;
        }

        public void SendGetSettings()
        {
            SendMessage(JsonSerializer.Serialize(new SettingsGetAllRequest { }));
        }

        public void SendSetSettings(PortalSettingsHolder settings)
        {
            SettingsSetRequest settingsReq = new SettingsSetRequest
            {
                Settings = []
            };
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_PORTAL_NAME,
                Value = settings.Name
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_SIGHTING_PERIOD,
                Value = settings.SightingPeriod.ToString()
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_READ_WINDOW,
                Value = settings.ReadWindow.ToString()
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_CHIP_TYPE,
                Value = settings.ChipType == PortalSettingsHolder.ChipTypeEnum.DEC ? PortalSetting.TYPE_CHIP_DEC
                    : PortalSetting.TYPE_CHIP_HEX
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_VOLUME,
                Value = settings.Volume.ToString()
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_PLAY_SOUND,
                Value = settings.PlaySound == true ? "true" : "false"
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_UPLOAD_INTERVAL,
                Value = settings.UploadInterval.ToString()
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_VOICE,
                Value = settings.Voice == PortalSettingsHolder.VoiceType.EMILY ? PortalSetting.VOICE_EMILY
                    : settings.Voice == PortalSettingsHolder.VoiceType.MICHAEL ? PortalSetting.VOICE_MICHAEL
                    : PortalSetting.VOICE_CUSTOM
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_NTFY_URL,
                Value = settings.NtfyURL
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_NTFY_TOPIC,
                Value = settings.NtfyTopic
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_NTFY_USER,
                Value = settings.NtfyUser
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_NTFY_PASS,
                Value = settings.NtfyPass
            });
            settingsReq.Settings.Add(new PortalSetting
            {
                Name = PortalSetting.SETTING_ENABLE_NTFY,
                Value = settings.EnableNTFY == true ? "true" : "false",
            });
            SendMessage(JsonSerializer.Serialize(settingsReq));
        }

        public void SendUploadParticipants(List<PortalParticipant> participants)
        {
            if (participants.Count > PARTICIPANTS_COUNT)
            {
                int loopCounter = participants.Count / PARTICIPANTS_COUNT;
                int leftOver = participants.Count % PARTICIPANTS_COUNT;
                for (int ix = 0; ix < loopCounter; ix++)
                {
                    Log.D("Timing.Interfaces.ChronokeepInterface", string.Format("Sending {0} participants starting at {1}", PARTICIPANTS_COUNT, ix * PARTICIPANTS_COUNT));
                    SendMessage(JsonSerializer.Serialize(new ParticipantsAddRequest
                    {
                        Participants = participants.GetRange(ix * PARTICIPANTS_COUNT, PARTICIPANTS_COUNT)
                    }));
                }
                if (leftOver > 0)
                {
                    Log.D("Timing.Interfaces.ChronokeepInterface", string.Format("Sending {0} participants starting at {1}", leftOver, loopCounter * PARTICIPANTS_COUNT));
                    SendMessage(JsonSerializer.Serialize(new ParticipantsAddRequest
                    {
                        Participants = participants.GetRange(loopCounter * PARTICIPANTS_COUNT, leftOver)
                    }));
                }
            }
            else
            {
                SendMessage(JsonSerializer.Serialize(new ParticipantsAddRequest
                {
                    Participants = participants
                }));
            }
        }

        public void SendUploadBibChips(List<BibChip> bibChips)
        {
            if (bibChips.Count > PARTICIPANTS_COUNT)
            {
                int loopCounter = bibChips.Count / PARTICIPANTS_COUNT;
                int leftover = bibChips.Count % PARTICIPANTS_COUNT;
                for (int ix = 0; ix < loopCounter; ix++)
                {
                    Log.D("Timing.Interfaces.ChronokeepInterface", string.Format("Sending {0} bibchips starting at {1}", PARTICIPANTS_COUNT, ix * PARTICIPANTS_COUNT));
                    SendMessage(JsonSerializer.Serialize(new BibChipsAddRequest
                    {
                        BibChips = bibChips.GetRange(ix * PARTICIPANTS_COUNT, PARTICIPANTS_COUNT)
                    }));
                }
                if (leftover > 0)
                {
                    Log.D("Timing.Interfaces.ChronokeepInterface", string.Format("Sending {0} bibchips starting at {1}", leftover, loopCounter * PARTICIPANTS_COUNT));
                    SendMessage(JsonSerializer.Serialize(new BibChipsAddRequest
                    {
                        BibChips = bibChips.GetRange(loopCounter * PARTICIPANTS_COUNT, leftover)
                    }));
                }
            }
            else
            {
                SendMessage(JsonSerializer.Serialize(new BibChipsAddRequest
                {
                    BibChips = bibChips
                }));
            }
        }

        public void SendRemoveParticipants()
        {
            SendMessage(JsonSerializer.Serialize(new ParticipantsRemoveRequest()));
        }

        public void SendRemoveBibChips()
        {
            SendMessage(JsonSerializer.Serialize(new BibChipsRemoveRequest()));
        }

        public void SendSaveApi(PortalAPI api)
        {
            SendMessage(JsonSerializer.Serialize(new ApiSaveRequest
            {
                ID = api.Id,
                Name = api.Nickname,
                Type = api.Kind,
                URI = api.Uri,
                Token = api.Token,
            }));
        }

        public void SendDeleteApi(PortalAPI api)
        {
            SendMessage(JsonSerializer.Serialize(new ApiRemoveRequest
            {
                ID = api.Id
            }));
        }

        public void SendSaveReader(PortalReader reader)
        {
            SendMessage(JsonSerializer.Serialize(new ReaderAddRequest
            {
                Id = reader.Id,
                Name = reader.Name,
                Type = reader.Kind,
                IPAddress = reader.IPAddress,
                Port = reader.Port,
                AutoConnect = reader.AutoConnect,
            }));
        }

        public void SendStartReader(PortalReader reader)
        {
            SendMessage(JsonSerializer.Serialize(new ReaderStartRequest
            {
                Id = reader.Id,
            }));
        }

        public void SendStopReader(PortalReader reader)
        {
            SendMessage(JsonSerializer.Serialize(new ReaderStopRequest
            {
                Id = reader.Id,
            }));
        }

        public void SendRemoveReader(PortalReader reader)
        {
            SendMessage(JsonSerializer.Serialize(new ReaderRemoveRequest
            {
                Id = reader.Id,
            }));
        }

        public void SendManualResultsUpload()
        {
            SendMessage(JsonSerializer.Serialize(new ApiRemoteManualUploadRequest()));
        }

        public void SendAutoUploadResults(AutoUploadQuery query)
        {
            string q_string = "";
            switch (query)
            {
                case AutoUploadQuery.STOP:
                    q_string = Request.AUTO_UPLOAD_QUERY_STOP;
                    break;
                case AutoUploadQuery.START:
                    q_string = Request.AUTO_UPLOAD_QUERY_START;
                    break;
                case AutoUploadQuery.STATUS:
                    q_string = Request.AUTO_UPLOAD_QUERY_STATUS;
                    break;
            }
            SendMessage(JsonSerializer.Serialize(new ApiRemoteAutoUploadRequest
            {
                Query = q_string
            }));
        }

        public void SendDeleteAllReads()
        {
            SendMessage(JsonSerializer.Serialize(new ReadsDeleteAllRequest()));
        }

        public void Disconnect()
        {
            SendMessage(JsonSerializer.Serialize(new DisconnectRequest { }));
        }

        private void SendMessage(string msg)
        {
            Log.D("Timing.Interfaces.ChronokeepInterface", "Sending message '" + msg + "'");
            sock.Send(Encoding.Default.GetBytes(msg + "\n"));
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
            settingsWindow = new ChronokeepSettings(this, database);
            window.AddWindow(settingsWindow);
            settingsWindow.Show();
        }

        public void CloseSettings()
        {
            if (settingsWindow != null)
            {
                settingsWindow.CloseWindow();
            }
        }

        public void SettingsWindowFinalize()
        {
            window.WindowFinalize(settingsWindow);
            settingsWindow = null;
        }

        public bool WasShutdown()
        {
            return wasShutdown;
        }
    }
}
