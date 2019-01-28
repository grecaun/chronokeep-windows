using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class ChipRead
    {
        public int Identifier { get; set; }
        public int EventId { get; set; }
        public int Status { get; set; }
        public int LocationID { get; set; }
        public long ChipNumber { get; set; }
        public long Seconds { get; set; }
        public int Milliseconds { get; set; }
        public DateTime Time { get; set; }
        public int Antenna { get; set; }
        public string Reader { get; set; }
        public string Box { get; set; }
        public int LogId { get; set; }
        public string RSSI { get; set; }
        public int IsRewind { get; set; }
        public string ReaderTime { get; set; }
        public long StartTime { get; set; }

        public ChipRead(int eventId, int status, int locationId, long chipNumber, long seconds, int millisec,
            int antenna, string rssi, int isRewind, string reader, string box, string readertime, long starttime,
            int logid)
        {
            this.EventId = eventId;
            this.Status = status;
            this.LocationID = locationId;
            this.ChipNumber = chipNumber;
            this.Seconds = seconds;
            this.Milliseconds = millisec;
            this.Antenna = antenna;
            this.RSSI = rssi;
            this.IsRewind = IsRewind;
            this.Reader = reader;
            this.Box = box;
            this.ReaderTime = readertime;
            this.StartTime = starttime;
            this.LogId = logid;
            Time = RFIDUltraInterface.EpochToDate(Seconds);
            Time.AddMilliseconds(Milliseconds);
        }
    }
}
