using System;

namespace Chronokeep
{
    public class ChipRead : IComparable<ChipRead>
    {
        public int ReadId { get; set; }
        public int EventId { get; set; }
        public int Status { get; set; }
        public int LocationID { get; set; }
        public string ChipNumber { get; set; }
        public long Seconds { get; set; }
        public int Milliseconds { get; set; }
        public long TimeSeconds { get; set; }
        public int TimeMilliseconds { get; set; }
        public int Antenna { get; set; }
        public string Reader { get; set; }
        public string Box { get; set; }
        public int LogId { get; set; }
        public string RSSI { get; set; }
        public int IsRewind { get; set; }
        public string ReaderTime { get; set; }
        public long StartTime { get; set; }
        public string ReadBib { get; set; }
        public string ChipBib { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }

        // RawReads window functions
        private DateTime Start { get; set; }
        public string LocationName { get; set; }
        public string Bib {
            get
            {
                return Constants.Timing.CHIPREAD_TYPE_CHIP == Type ? ChipBib : ReadBib;
            }
        }

        // This constructor is used when receiving a read from a RFID Ultra 8
        public ChipRead(
            int eventId, 
            int locationId, 
            string chipNumber, 
            long seconds, 
            int millisec,
            int antenna, 
            string rssi, 
            int isRewind, 
            string reader, 
            string box, 
            string readertime,
            long starttime, 
            int logid
            )
        {
            this.EventId = eventId;
            this.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
            this.LocationID = locationId;
            this.ChipNumber = chipNumber;
            this.Seconds = seconds;
            this.Milliseconds = millisec;
            this.Antenna = antenna;
            this.RSSI = rssi;
            this.IsRewind = isRewind;
            this.Reader = reader;
            this.Box = box;
            this.ReaderTime = readertime;
            this.StartTime = starttime;
            this.LogId = logid;
            this.TimeSeconds = seconds;
            this.TimeMilliseconds = millisec;
            this.Type = Constants.Timing.CHIPREAD_TYPE_CHIP;
            this.ReadBib = Constants.Timing.CHIPREAD_DUMMYBIB;
        }

        // This constructor is used when receiving a read from an Ipico system
        public ChipRead(
            int eventId, 
            int locationId, 
            string chipNumber, 
            DateTime time, 
            int antenna, 
            int isRewind
            )
        {
            this.ReadBib = Constants.Timing.CHIPREAD_DUMMYBIB;
            this.TimeSeconds = Constants.Timing.RFIDDateToEpoch(time);
            this.TimeMilliseconds = time.Millisecond;
            this.Type = Constants.Timing.CHIPREAD_TYPE_CHIP;
            this.EventId = eventId;
            this.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
            this.LocationID = locationId;
            this.ChipNumber = chipNumber.Trim();
            this.Seconds = Constants.Timing.RFIDDateToEpoch(time);
            this.Milliseconds = time.Millisecond;
            this.Antenna = antenna;
            this.RSSI = "";
            this.IsRewind = isRewind;
            this.Reader = "I";
            this.Box = "Ipico";
            this.ReaderTime = "";
            this.StartTime = 0;
            this.LogId = 0;
        }

        // This constructor is used when receiving a read from a Chronokeep Portal system
        public ChipRead(
            int eventId, 
            int locationId, 
            bool chipIsChip, 
            string chipNumber,
            long seconds, 
            int millisec, 
            int antenna, 
            string rssi, 
            string reader, 
            int readType,
            string readertime
            )
        {
            this.EventId = eventId;
            this.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
            this.LocationID = locationId;
            this.Seconds = seconds;
            this.Milliseconds = millisec;
            this.Antenna = antenna;
            this.RSSI = rssi;
            this.IsRewind = 0;
            this.Reader = reader;
            this.Box = "Chronokeep Portal";
            this.StartTime = 0;
            this.LogId = 0;
            this.TimeSeconds = seconds;
            this.TimeMilliseconds = millisec;
            this.Type = readType;
            this.ReaderTime = readertime;
            if (chipIsChip)
            {
                this.ChipNumber = chipNumber;
                this.ReadBib = Constants.Timing.CHIPREAD_DUMMYBIB;
            }
            else // chip is a bib
            {
                this.ChipNumber = Constants.Timing.CHIPREAD_DUMMYCHIP;
                this.ReadBib = chipNumber;
            }
        }

        // This is the OLD database' constructor.
        public ChipRead(int readId, 
            int eventId, 
            int status, 
            int locationId, 
            long chipNumber, 
            long seconds,
            int millisec, 
            int antenna, 
            string rssi, 
            int isRewind, 
            string reader, 
            string box,
            string readertime,
            long starttime, 
            int logid, 
            DateTime time, 
            int readbib,
            int type
            )
        {
            this.ReadId = readId;
            this.EventId = eventId;
            this.Status = status;
            this.LocationID = locationId;
            this.ChipNumber = chipNumber.ToString();
            this.Seconds = seconds;
            this.Milliseconds = millisec;
            this.Antenna = antenna;
            this.RSSI = rssi;
            this.IsRewind = isRewind;
            this.Reader = reader;
            this.Box = box;
            this.ReaderTime = readertime;
            this.StartTime = starttime;
            this.LogId = logid;
            this.TimeSeconds = Constants.Timing.RFIDDateToEpoch(time);
            this.TimeMilliseconds = time.Millisecond;
            this.ReadBib = readbib.ToString();
            this.Type = type;
        }

        // new database constructor
        public ChipRead(
            int readId, 
            int eventId,
            int status, 
            int locationId, 
            string chipNumber, 
            long seconds,
            int millisec, 
            int antenna, 
            string rssi, 
            int isRewind, 
            string reader, 
            string box, 
            string readertime,
            long starttime, 
            int logid, 
            long time_seconds, 
            int time_millisec, 
            string readbib, 
            int type, 
            string chipbib,
            string first,
            string last, 
            DateTime start,
            string locationName
            )
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
            this.IsRewind = isRewind;
            this.Reader = reader;
            this.Box = box;
            this.ReaderTime = readertime;
            this.StartTime = starttime;
            this.LogId = logid;
            this.TimeSeconds = time_seconds;
            this.TimeMilliseconds = time_millisec;
            this.ReadBib = readbib;
            this.Type = type;
            this.ChipBib = chipbib;
            this.Name = string.Format("{0} {1}", first, last).Trim();
            this.Start = start;
            this.LocationName = locationName;
        }

        // Constructor used in manual entry.
        public ChipRead(
            int eventId,
            int locationId,
            string bib,
            DateTime time,
            int status
            )
        {
            this.ReadBib = bib;
            this.TimeSeconds = Constants.Timing.RFIDDateToEpoch(time);
            this.TimeMilliseconds = time.Millisecond;
            this.Type = Constants.Timing.CHIPREAD_TYPE_MANUAL;
            this.EventId = eventId;
            this.Status = status;
            this.LocationID = locationId;
            this.ChipNumber = Constants.Timing.CHIPREAD_DUMMYCHIP;
            this.Seconds = Constants.Timing.RFIDDateToEpoch(time);
            this.Milliseconds = time.Millisecond;
            this.Antenna = 0;
            this.RSSI = "";
            this.IsRewind = 0;
            this.Reader = "M";
            this.Box = "Man";
            this.ReaderTime = "";
            this.StartTime = 0;
            this.LogId = 0;
        }

        // Constructor used when loading from a Log
        public ChipRead(
            int eventId,
            int locationId,
            string chip,
            DateTime time
            )
        {
            this.ReadBib = Constants.Timing.CHIPREAD_DUMMYBIB;
            this.TimeSeconds = Constants.Timing.RFIDDateToEpoch(time);
            this.TimeMilliseconds = time.Millisecond;
            this.Type = Constants.Timing.CHIPREAD_TYPE_CHIP;
            this.EventId = eventId;
            this.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
            this.LocationID = locationId;
            this.ChipNumber = chip.Trim();
            this.Seconds = Constants.Timing.RFIDDateToEpoch(time);
            this.Milliseconds = time.Millisecond;
            this.Antenna = 0;
            this.RSSI = "";
            this.IsRewind = 0;
            this.Reader = "L";
            this.Box = "Log";
            this.ReaderTime = "";
            this.StartTime = 0;
            this.LogId = 0;
        }

        // Constructor used for loading data from a chronokeep log, fake variable used to differentiate between this and the old db constructor.
        public ChipRead(
            int eventId,
            int locationId,
            int status,
            string chipNumber, 
            long seconds, 
            int milliseconds,
            long time_seconds,
            int time_milliseconds,
            int antenna,
            string reader,
            string box,
            int log_index,
            string rssi,
            int is_rewind,
            string reader_time,
            long start_time,
            string read_bib,
            int type,
            bool _fake)
        {
            this.EventId = eventId;
            this.LocationID = locationId;
            this.Status = status;
            this.ChipNumber = chipNumber;
            this.Seconds = seconds;
            this.Milliseconds = milliseconds;
            this.TimeSeconds = time_seconds;
            this.TimeMilliseconds = time_milliseconds;
            this.Antenna = antenna;
            this.Reader = reader;
            this.Box = box;
            this.LogId = log_index;
            this.RSSI = rssi;
            this.IsRewind = is_rewind;
            this.ReaderTime = reader_time;
            this.StartTime = start_time;
            this.ReadBib = read_bib;
            this.Type = type;
        }

        // Constructor used for loading data from a remote reader.
        public ChipRead(
            int eventId,
            int locationId,
            string chip,
            string bib,
            long seconds,
            int milliseconds,
            int antenna,
            string reader,
            string rssi,
            int type
            )
        {
            this.EventId = eventId;
            this.LocationID = locationId;
            this.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
            this.ChipNumber = chip;
            this.ReadBib = bib;
            this.TimeSeconds = Constants.Timing.UTCSecondsToRFIDSeconds(seconds);
            this.TimeMilliseconds = milliseconds;
            this.Seconds = Constants.Timing.UTCSecondsToRFIDSeconds(seconds);
            this.Milliseconds = milliseconds;
            this.Antenna = antenna;
            this.Reader = reader;
            this.RSSI = rssi;
            this.Type = type;
            this.IsRewind = 0;
            this.Box = "Remote Reader";
            this.ReaderTime = "";
            this.StartTime = 0;
            this.LogId = 0;
        }

        public string TimeString
        {
            get
            {
                return Time.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
        }

        public string NetTime
        {
            get
            {
                TimeSpan ellapsed = Time - Start;
                return string.Format("{0}:{1:D2}:{2:D2}.{3:D3}",
                    ellapsed.Days * 24 + ellapsed.Hours,
                    Math.Abs(ellapsed.Minutes),
                    Math.Abs(ellapsed.Seconds),
                    Math.Abs(ellapsed.Milliseconds));
            }
        }

        public DateTime Time
        {
            get => Constants.Timing.RFIDEpochToDate(TimeSeconds).AddMilliseconds(TimeMilliseconds);
        }

        public string TypeName
        {
            get
            {
                return Constants.Timing.CHIPREAD_TYPE_MANUAL == Type ? "Manual" : "Chip";
            }
        }
        public string StatusName
        {
            get
            {
                if (Constants.Timing.CHIPREAD_STATUS_NONE == Status)
                {
                    return "Unprocessed";
                }
                if (Constants.Timing.CHIPREAD_STATUS_PRESTART == Status)
                {
                    return "Before Start";
                }
                if (Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART == Status)
                {
                    return "Unused Start";
                }
                if (Constants.Timing.CHIPREAD_STATUS_IGNORE == Status ||
                    Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE == Status ||
                    Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE == Status)
                {
                    return "Ignored";
                }
                if (Constants.Timing.CHIPREAD_STATUS_USED == Status)
                {
                    return "Used";
                }
                if (Constants.Timing.CHIPREAD_STATUS_WITHINIGN == Status)
                {
                    return "Too Soon";
                }
                if (Constants.Timing.CHIPREAD_STATUS_OVERMAX == Status)
                {
                    return "Extra";
                }
                if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == Status)
                {
                    return "Start";
                }
                if (Constants.Timing.CHIPREAD_STATUS_DNF == Status)
                {
                    return "DNF";
                }
                if (Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_SEEN == Status)
                {
                    return "A-Seen";
                }
                if (Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED == Status)
                {
                    return "A-Used";
                }
                if (Constants.Timing.CHIPREAD_STATUS_DNS == Status)
                {
                    return "DNS";
                }
                if (Constants.Timing.CHIPREAD_STATUS_AFTER_DNS == Status)
                {
                    return "After DNS";
                }
                return "Unknown";
            }
        }

        public int CompareTo(ChipRead other)
        {
            if (other == null) return this.CompareTo(other);
            return this.Time.CompareTo(other.Time);
        }

        public static int CompareByBib(ChipRead one, ChipRead two)
        {
            if (one == null || two == null) return 1;
            // Check if they're the same bib
            // Make sure they're not the dummy bib numer, then compare them.
            if ( (Constants.Timing.CHIPREAD_DUMMYBIB != one.ReadBib && (one.ReadBib == two.ReadBib || one.ReadBib == two.ChipBib)) ||
                 (Constants.Timing.CHIPREAD_DUMMYBIB != one.ChipBib && (one.ChipBib == two.ChipBib || one.ChipBib == two.ReadBib)) )
            {
                return one.Time.CompareTo(two.Time);
            }
            string stringBibOne = one.ReadBib == Constants.Timing.CHIPREAD_DUMMYBIB ? one.ChipBib : one.ReadBib;
            string stringBibTwo = two.ReadBib == Constants.Timing.CHIPREAD_DUMMYBIB ? two.ChipBib : two.ReadBib;
            int intBibOne, intBibTwo;
            if (int.TryParse(stringBibOne, out intBibOne) && int.TryParse(stringBibTwo, out intBibTwo))
            {
                return intBibOne.CompareTo(intBibTwo);
            }
            return stringBibOne.CompareTo(stringBibTwo);
        }

        public bool IsNotMatch(string value)
        {
            return this.Bib.ToString().IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1
                && this.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1
                && this.ChipNumber.ToString().IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1;
        }

        public bool IsIgnored()
        {
            return Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE == Status || Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE == Status || Constants.Timing.CHIPREAD_STATUS_IGNORE == Status;
        }

        public bool IsUseful()
        {
            return Constants.Timing.CHIPREAD_STATUS_NONE == Status
                || Constants.Timing.CHIPREAD_STATUS_USED == Status
                || Constants.Timing.CHIPREAD_STATUS_STARTTIME == Status
                || Constants.Timing.CHIPREAD_STATUS_DNF == Status
                || Constants.Timing.CHIPREAD_STATUS_DNS == Status;
        }

        public bool CanBeReset()
        {
            return Constants.Timing.CHIPREAD_STATUS_IGNORE != Status
                && Constants.Timing.CHIPREAD_STATUS_DNS != Status
                && Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE != Status
                && Constants.Timing.CHIPREAD_STATUS_DNF != Status
                && Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE != Status;
        }

        public bool Equals(ChipRead other)
        {
            if (other == null) return false;
            return this.ReadId == other.ReadId
                || (this.EventId == other.EventId
                && this.LocationID == other.LocationID
                && this.ChipNumber == other.ChipNumber
                && this.Seconds == other.Seconds
                && this.Milliseconds == other.Milliseconds
                && this.Antenna == other.Antenna
                && this.RSSI == other.RSSI
                && this.IsRewind == other.IsRewind
                && this.Reader == other.Reader
                && this.Box == other.Box
                && this.ReaderTime == other.ReaderTime
                && this.StartTime == other.StartTime
                && this.LogId == other.LogId
                && this.TimeSeconds == other.TimeSeconds
                && this.TimeMilliseconds == other.TimeMilliseconds
                && this.ReadBib == other.ReadBib
                && this.Type == other.Type
                && this.ChipBib == other.ChipBib
                && this.Name == other.Name
                && this.Start == other.Start
                && this.LocationName == other.LocationName);
        }
    }
}
