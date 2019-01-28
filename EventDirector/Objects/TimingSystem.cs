using EventDirector.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Objects
{
    public class TimingSystem : IEquatable<TimingSystem>
    {
        public int SystemIdentifier { get; set; } = -1;
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int LocationID { get; set; } = -1;
        public string LocationName { get; set; } = "Unknown";
        public string Type { get; set; } = Constants.Settings.TIMING_RFID;
        public SYSTEM_STATUS Status { get; set; } = SYSTEM_STATUS.DISCONNECTED;
        public Socket Socket { get; private set; }
        public ITimingSystemInterface SystemInterface;
        private DateTime ConnectedAt;

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

        public TimingSystem(int sysId, string ip, int port, string type)
        {
            this.SystemIdentifier = sysId;
            this.IPAddress = ip;
            this.Port = port;
            this.Type = type;
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

        public void CreateTimingSystemInterface(IDBInterface database)
        {
            if (this.Type == Constants.Settings.TIMING_RFID)
            {
                SystemInterface = new RFIDUltraInterface(database, Socket, LocationID);
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

        public void SetTime()
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
