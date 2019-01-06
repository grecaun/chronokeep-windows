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
        public Socket Socket { get; set; }
        public string Type { get; set; } = Constants.Settings.TIMING_RFID;
    }

    public enum SYSTEM_STATUS { CONNECTED, DISCONNECTED, WORKING }
}
