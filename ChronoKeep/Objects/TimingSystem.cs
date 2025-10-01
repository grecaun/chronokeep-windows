using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Interfaces.UI;
using Chronokeep.Timing.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Chronokeep.Objects
{
    public class TimingSystem : IEquatable<TimingSystem>
    {
        public const string READING_STATUS_STOPPED = "STOPPED";
        public const string READING_STATUS_READING = "READING";
        public const string READING_STATUS_PARTIAL = "PARTIAL";
        public const string READING_STATUS_UNKNOWN = "UNKNOWN";

        public int SystemIdentifier { get; set; } = Constants.Timing.TIMINGSYSTEM_UNKNOWN;
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int LocationID { get; set; } = Constants.Timing.LOCATION_FINISH;
        public string LocationName { get; set; } = "Unknown";
        public string Type { get; set; } = Constants.Readers.SYSTEM_RFID;
        public SYSTEM_STATUS Status { get; set; } = SYSTEM_STATUS.DISCONNECTED;
        public List<Socket> Sockets { get; private set; }
        public ITimingSystemInterface SystemInterface;
        private DateTime ConnectedAt;

        public string SystemTime { get; set; } = "";
        public string SystemStatus { get; set; } = "";

        public TimingSystem(string ip, string type)
        {
            this.IPAddress = ip;
            this.Status = SYSTEM_STATUS.DISCONNECTED;
            this.Type = type;
            if (type == Constants.Readers.SYSTEM_RFID)
            {
                this.Port = Constants.Readers.RFID_DEFAULT_PORT;
            }
            else if (type == Constants.Readers.SYSTEM_IPICO || type == Constants.Readers.SYSTEM_IPICO_LITE)
            {
                this.Port = Constants.Readers.IPICO_DEFAULT_PORT;
            }
            else if (type == Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL)
            {
                this.Port = Constants.Network.CHRONOKEEP_ZCONF_PORT;
            }
        }

        public TimingSystem(string ip, int locId, string locName, SYSTEM_STATUS status, string type)
        {
            this.IPAddress = ip;
            this.LocationID = locId;
            this.LocationName = locName;
            this.Status = status;
            this.Type = type;
        }

        public TimingSystem(int sysId, string ip, int port, int location, string type)
        {
            this.SystemIdentifier = sysId;
            this.IPAddress = ip;
            this.Port = port;
            this.Type = type;
            this.LocationID = location;
            this.Status = SYSTEM_STATUS.DISCONNECTED;
        }

        public List<Socket> Connect()
        {
            if (SystemInterface == null)
            {
                return null;
            }
            Log.D("Objects.TimingSystem", "TimingSystem class calling connect on interface.");
            Sockets = SystemInterface.Connect(IPAddress, Port);
            Log.D("Objects.TimingSystem", "TimingSystem class returning output from Connect.");
            return Sockets;
        }

        public void Disconnect()
        {
            SystemInterface.Disconnect();
            foreach (Socket sock in Sockets)
            {
                sock.Disconnect(false);
            }
        }

        public void UpdateSystemType(string type)
        {
            this.Type = type;
            if (type == Constants.Readers.SYSTEM_RFID)
            {
                this.Port = Constants.Readers.RFID_DEFAULT_PORT;
            }
            else if (type == Constants.Readers.SYSTEM_IPICO || type == Constants.Readers.SYSTEM_IPICO_LITE)
            {
                this.Port = Constants.Readers.IPICO_DEFAULT_PORT;
            }
            else if (type == Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL)
            {
                this.Port = Constants.Network.CHRONOKEEP_ZCONF_PORT;
            }
        }

        public void CopyFrom(TimingSystem other)
        {
            this.IPAddress = other.IPAddress;
            this.LocationID = other.LocationID;
            this.LocationName = other.LocationName;
            this.Port = other.Port;
            this.Type = other.Type;
        }

        public void CreateTimingSystemInterface(IDBInterface database, IMainWindow window)
        {
            if (this.Type == Constants.Readers.SYSTEM_RFID)
            {
                Log.D("Objects.TimingSystem", "System interface is RFID.");
                SystemInterface = new RFIDUltraInterface(database, LocationID, window);
            }
            else if (this.Type == Constants.Readers.SYSTEM_IPICO || this.Type == Constants.Readers.SYSTEM_IPICO_LITE)
            {
                Log.D("Objects.TimingSystem", "System interface is IPICO.");
                SystemInterface = new IpicoInterface(database, LocationID, this.Type, window);
            }
            else if (this.Type == Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL)
            {
                Log.D("Objects.TimingSystem", "System interface is CHRONOKEEP_PORTAL.");
                SystemInterface = new ChronokeepInterface(database, LocationID, window);
            }
            else
            {
                Log.E("Objects.TimingSystem", "Unknown interface selected.");
                SystemInterface = null;
            }
        }

        public void SetLastCommunicationTime()
        {
            this.ConnectedAt = DateTime.Now;
        }

        public bool TimedOut()
        {
            TimeSpan ellapsed = DateTime.Now - ConnectedAt;
            return ellapsed.Seconds > 5;
        }

        public bool Equals(TimingSystem other)
        {
            return other != null && this.IPAddress.Trim().Equals(other.IPAddress.Trim()) && this.Port == other.Port
                && this.LocationID == other.LocationID && this.Type.Equals(other.Type);
        }

        public bool Saved()
        {
            return this.SystemIdentifier != Constants.Timing.TIMINGSYSTEM_UNKNOWN;
        }
    }

    public enum SYSTEM_STATUS { CONNECTED, WORKING, DISCONNECTED }
}
