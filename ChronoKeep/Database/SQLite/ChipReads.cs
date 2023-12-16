using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    class ChipReads
    {
        internal static void AddChipRead(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO chipreads (event_id, read_status, location_id, read_chipnumber, read_seconds," +
                "read_milliseconds, read_antenna, read_reader, read_box, read_logindex, read_rssi, read_isrewind, read_readertime," +
                "read_starttime, read_time_seconds, read_time_milliseconds, read_bib, read_type)" +
                " VALUES (@event, @status, @loc, @chip, @sec, @milli, @ant, @reader, @box, @logix, @rssi, @rewind, @readertime, " +
                "@starttime, @timesec, @timemill, @bib, @type);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", read.EventId),
                new SQLiteParameter("@status", read.Status),
                new SQLiteParameter("@loc", read.LocationID),
                new SQLiteParameter("@chip", read.ChipNumber),
                new SQLiteParameter("@sec", read.Seconds),
                new SQLiteParameter("@milli", read.Milliseconds),
                new SQLiteParameter("@ant", read.Antenna),
                new SQLiteParameter("@reader", read.Reader),
                new SQLiteParameter("@box", read.Box),
                new SQLiteParameter("@logix", read.LogId),
                new SQLiteParameter("@rssi", read.RSSI),
                new SQLiteParameter("@rewind", read.IsRewind),
                new SQLiteParameter("@readertime", read.ReaderTime),
                new SQLiteParameter("@starttime", read.StartTime),
                new SQLiteParameter("@timesec", read.TimeSeconds),
                new SQLiteParameter("@timemill", read.TimeMilliseconds),
                new SQLiteParameter("@bib", read.ReadBib),
                new SQLiteParameter("@type", read.Type)
            });
            command.ExecuteNonQuery();
        }

        internal static void UpdateChipRead(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE chipreads SET read_status=@status, read_time_seconds=@time, read_time_milliseconds=@mill WHERE read_id=@id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@status", read.Status),
                    new SQLiteParameter("@id", read.ReadId),
                    new SQLiteParameter("@time", read.TimeSeconds),
                    new SQLiteParameter("@mill", read.TimeMilliseconds)
            });
            command.ExecuteNonQuery();

        }

        internal static void SetChipReadStatus(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE chipreads SET read_status=@status WHERE read_id=@id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@status", read.Status),
                    new SQLiteParameter("@id", read.ReadId)
            });
            command.ExecuteNonQuery();
        }

        internal static void DeleteChipReads(List<ChipRead> reads, SQLiteConnection connection)
        {
            if (reads.Count < 1) return;
            using (var transaction = connection.BeginTransaction())
            {
                foreach (ChipRead read in reads)
                {
                    SQLiteCommand command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM chipreads WHERE read_id=@read;";
                    command.Parameters.Add(new SQLiteParameter("@read", read.ReadId));
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        internal static List<ChipRead> GetChipReads(Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b ON (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id;";
            command.Parameters.Add(new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB));
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetChipReads(int eventId, Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b ON (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id " +
                "WHERE c.event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            command.Parameters.Add(new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB));
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetUsefulChipReads(int eventId, Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b on (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id WHERE c.event_id=@event AND " +
                "(read_status=@status OR read_status=@used OR read_status=@start OR read_status=@dnf) AND c.location_id!=@announcer;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@used", Constants.Timing.CHIPREAD_STATUS_USED),
                new SQLiteParameter("@start", Constants.Timing.CHIPREAD_STATUS_STARTTIME),
                new SQLiteParameter("@dnf", Constants.Timing.CHIPREAD_STATUS_DNF),
                new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB),
                new SQLiteParameter("@announcer", Constants.Timing.LOCATION_ANNOUNCER)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetAnnouncerChipReads(int eventId, Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b on (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id WHERE c.event_id=@event AND " +
                "c.location_id=@announcer AND read_status=@none;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@none", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB),
                new SQLiteParameter("@announcer", Constants.Timing.LOCATION_ANNOUNCER)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetAnnouncerUsedChipReads(int eventId, Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b on (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id WHERE c.event_id=@event AND " +
                "c.location_id=@announcer AND read_status=@used;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB),
                new SQLiteParameter("@announcer", Constants.Timing.LOCATION_ANNOUNCER),
                new SQLiteParameter("@used", Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetDNSChipReads(int eventId, Event theEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b on (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id WHERE c.event_id=@event AND " +
                " read_status=@dns;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB),
                new SQLiteParameter("@announcer", Constants.Timing.LOCATION_ANNOUNCER),
                new SQLiteParameter("@dns", Constants.Timing.CHIPREAD_STATUS_DNS)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader, theEvent, connection);
            return output;
        }

        internal static List<ChipRead> GetChipReadsWorker(SQLiteDataReader reader, Event theEvent, SQLiteConnection connection)
        {
            DateTime start = DateTime.Now;
            if (theEvent != null)
            {
                start = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
            }
            List<TimingLocation> locations = new List<TimingLocation>();
            if (theEvent != null) locations.AddRange(TimingLocations.GetTimingLocations(theEvent.Identifier, connection));
            if (theEvent != null && !theEvent.CommonStartFinish)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else if (theEvent != null)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent == null ? -1 : theEvent.Identifier, "Start/Finish", theEvent == null ? 1 : theEvent.FinishMaxOccurrences, theEvent == null ? 0 : theEvent.FinishIgnoreWithin));
            }
            Dictionary<int, string> locDict = new Dictionary<int, string>();
            foreach (TimingLocation loc in locations)
            {
                locDict[loc.Identifier] = loc.Name;
            }
            List<ChipRead> output = new List<ChipRead>();
            while (reader.Read())
            {
                int locationId = Convert.ToInt32(reader["location_id"]);
                string locationName = locDict.ContainsKey(locationId) ? locDict[locationId] : "";
                output.Add(new ChipRead(
                    Convert.ToInt32(reader["read_id"]),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["read_status"]),
                    locationId,
                    reader["read_chipnumber"].ToString(),
                    Convert.ToInt64(reader["read_seconds"]),
                    Convert.ToInt32(reader["read_milliseconds"]),
                    Convert.ToInt32(reader["read_antenna"]),
                    reader["read_rssi"].ToString(),
                    Convert.ToInt32(reader["read_isrewind"]),
                    reader["read_reader"].ToString(),
                    reader["read_box"].ToString(),
                    reader["read_readertime"].ToString(),
                    Convert.ToInt32(reader["read_starttime"]),
                    Convert.ToInt32(reader["read_logindex"]),
                    Convert.ToInt64(reader["read_time_seconds"]),
                    Convert.ToInt32(reader["read_time_milliseconds"]),
                    Convert.ToInt32(reader["read_bib"]),
                    Convert.ToInt32(reader["read_type"]),
                    reader["bib"] == DBNull.Value ? Constants.Timing.CHIPREAD_DUMMYBIB : Convert.ToInt32(reader["bib"]),
                    reader["participant_first"] == DBNull.Value ? "" : reader["participant_first"].ToString(),
                    reader["participant_last"] == DBNull.Value ? "" : reader["participant_last"].ToString(),
                    start,
                    locationName
                    ));
            }
            reader.Close();
            return output;
        }
    }
}
