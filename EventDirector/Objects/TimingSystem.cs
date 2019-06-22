using ChronoKeep.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class TimingSystem : IEquatable<TimingSystem>
    {
        public int SystemIdentifier { get; set; } = -1;
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int LocationID { get; set; } = Constants.Timing.LOCATION_FINISH;
        public string LocationName { get; set; } = "Unknown";
        public string Type { get; set; } = Constants.Settings.TIMING_RFID;
        public SYSTEM_STATUS Status { get; set; } = SYSTEM_STATUS.DISCONNECTED;
        public Socket Socket { get; private set; }
        public ITimingSystemInterface SystemInterface;
        private DateTime ConnectedAt;

        public string SystemTime { get; set; } = "";
        public string SystemStatus { get; set; } = "";

        public TimingSystem(string ip, string type)
        {
            this.IPAddress = ip;
            this.Status = SYSTEM_STATUS.DISCONNECTED;
            this.Type = type;
            if (type == Constants.Settings.TIMING_RFID)
            {
                this.Port = 23;
            }
            else if (type == Constants.Settings.TIMING_IPICO)
            {
                this.Port = 10000;
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

        public void UpdateSystemType(string type)
        {
            if (type == Constants.Settings.TIMING_RFID)
            {
                this.Port = 23;
            }
            else if (type == Constants.Settings.TIMING_IPICO)
            {
                this.Port = 10000;
            }
        }

        public void CreateTimingSystemInterface(IDBInterface database, Socket sock)
        {
            this.Socket = sock;
            if (this.Type == Constants.Settings.TIMING_RFID)
            {
                Log.D("System interface is RFID.");
                SystemInterface = new RFIDUltraInterface(database, sock, LocationID);
            }
            else if (this.Type == Constants.Settings.TIMING_IPICO)
            {
                SystemInterface = null;
            }
        }

        public void SetSocket(Socket sock)
        {
            this.Socket = sock;
            SystemInterface.SetMainSocket(sock);
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
    }

    public enum SYSTEM_STATUS { CONNECTED, DISCONNECTED, WORKING }
}
