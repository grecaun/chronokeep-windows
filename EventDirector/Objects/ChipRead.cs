using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class ChipRead
    {
        public int ReadId { get; set; }
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
        public int Bib { get; set; }
        public int Type { get; set; }

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
            this.Type = Constants.Timing.CHIPREAD_TYPE_CHIP;
            this.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
        }

        public ChipRead(int readId, int eventId, int status, int locationId, long chipNumber, long seconds, int millisec,
           int antenna, string rssi, int isRewind, string reader, string box, string readertime, long starttime,
           int logid, DateTime time, int bib, int type)
        {
            this.ReadId = readId;
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
            this.Time = time;
            this.Bib = bib;
            this.Type = type;
        }

        public ChipRead(int eventId, int status, int locationId, int bib, DateTime time)
        {
            this.Bib = bib;
            this.Time = time;
            this.Type = Constants.Timing.CHIPREAD_TYPE_MANUAL;
            this.EventId = eventId;
            this.Status = status;
            this.LocationID = locationId;
            this.ChipNumber = Constants.Timing.CHIPREAD_DUMMYCHIP;
            this.Seconds = RFIDUltraInterface.DateToEpoch(time);
            this.Milliseconds = time.Millisecond;
            this.Antenna = 0;
            this.RSSI = "";
            this.IsRewind = 0;
            this.Reader = "Manual Entry";
            this.Box = "N/A";
            this.ReaderTime = "";
            this.StartTime = 0;
            this.LogId = 0;
        }

        public string TimeString()
        {
            return Time.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
