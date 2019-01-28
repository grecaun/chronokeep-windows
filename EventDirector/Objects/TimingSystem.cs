using EventDirector.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Objects
{
    public class TimingSystem
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int LocationID { get; set; } = -1;
        public string LocationName { get; set; } = "Unknown";
        public SYSTEM_STATUS Status { get; set; } = SYSTEM_STATUS.DISCONNECTED;
        public Socket Socket { get; private set; }
        public ITimingSystemInterface SystemInterface;
        private DateTime ConnectedAt;

        public TimingSystem(string ip, string type, IDBInterface database)
        {
            this.IPAddress = ip;
            this.Status = SYSTEM_STATUS.DISCONNECTED;
            UpdateSystemType(type, database);
        }

        public TimingSystem(string ip, int locId, string locName, SYSTEM_STATUS status, string type, IDBInterface database)
        {
            this.IPAddress = ip;
            this.LocationID = locId;
            this.LocationName = locName;
            this.Status = status;
            UpdateSystemType(type, database);
        }

        public void UpdateSystemType(string type, IDBInterface database)
        {
            if (type == Constants.Settings.TIMING_RFID)
            {
                this.Port = 23;
                SystemInterface = new RFIDUltraInterface(database, Socket, LocationID);
            }
            else if (type == Constants.Settings.TIMING_IPICO)
            {
                // Ipico interface.
                this.Port = 10000;
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
    }

    public enum SYSTEM_STATUS { CONNECTED, DISCONNECTED, WORKING }
}
