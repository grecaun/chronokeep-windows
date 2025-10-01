using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class Results
    {
        public static Dictionary<int, TimingLocation> locations = [];
        public static Dictionary<int, Segment> segments = [];
        public static Dictionary<string, Distance> distances = [];
        public static Event theEvent = null;

        public static void GetStaticVariables(IDBInterface database)
        {
            locations.Clear();
            segments.Clear();
            distances.Clear();
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            if (!theEvent.CommonStartFinish)
            {
                locations[Constants.Timing.LOCATION_FINISH] = new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
                locations[Constants.Timing.LOCATION_START] = new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow);
            }
            else
            {
                locations[Constants.Timing.LOCATION_FINISH] = new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
            }
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                locations[loc.Identifier] = loc;
            }
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                segments[seg.Identifier] = seg;
            }
            foreach (Distance dist in database.GetDistances(theEvent.Identifier))
            {
                distances[dist.Name] = dist;
            }
        }

        internal static void AddTimingResult(TimeResult tr, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO time_results (event_id, eventspecific_id, location_id, segment_id, " +
                "timeresult_occurance, timeresult_time, timeresult_unknown_id, read_id, timeresult_chiptime," +
                "timeresult_place, timeresult_age_place, timeresult_gender_place," +
                "timeresult_status, timeresult_splittime, timeresult_uploaded, timeresult_division_place)" +
                " VALUES (@event,@specific,@location,@segment,@occ,@time,@unknown,@read,@chip,@place,@agplace," +
                "@gendplace,@status,@split,@uploaded,@divPlace)";
            command.Parameters.AddRange([
                new("@event", tr.EventIdentifier),
                new("@specific", tr.EventSpecificId),
                new("@location", tr.LocationId),
                new("@segment", tr.SegmentId),
                new("@occ", tr.Occurrence),
                new("@time", tr.Time),
                new("@unknown", tr.UnknownId),
                new("@read", tr.ReadId),
                new("@chip", tr.ChipTime),
                new("@place", tr.Place),
                new("@agplace", tr.AgePlace),
                new("@gendplace", tr.GenderPlace),
                new("@status", tr.Status),
                new("@split", tr.LapTime),
                new("@uploaded", tr.Uploaded),
                new("@divPlace", tr.DivisionPlace)
            ]);
            command.ExecuteNonQuery();
        }

        internal static void RemoveTimingResult(TimeResult tr, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM time_results WHERE eventspecific_id=@event AND location_id=@location AND " +
                "segment_id=@segment AND timeresult_occurance=@occurance";
            command.Parameters.AddRange([
                new("@event", tr.EventSpecificId),
                new("@segment", tr.SegmentId),
                new("@occurance", tr.Occurrence),
                new("@location", tr.LocationId) ]);
            command.ExecuteNonQuery();
        }

        private static List<TimeResult> GetResultsInternal(
            SQLiteDataReader reader,
            Dictionary<int, ChipRead> chipReadDict,
            Dictionary<int, Participant> partDict,
            Dictionary<int, Distance> distanceDict
            )
        {
            List<TimeResult> output = [];
            while (reader.Read())
            {
                int eventSpecificId = reader["eventspecific_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_id"]);
                int readId = Convert.ToInt32(reader["read_id"]);
                int distanceId = -1;
                string bib = "";
                Participant part = null;
                ChipRead chipRead = null;
                chipReadDict.TryGetValue(readId, out chipRead);
                if (eventSpecificId > -1 && partDict.TryGetValue(eventSpecificId, out part))
                {
                    bib = part.Bib;
                    distanceId = part.EventSpecific.DistanceIdentifier;
                }
                else if (chipRead != null)
                {
                    bib = chipRead.Bib;
                }
                bool knownDist = distanceDict.TryGetValue(distanceId, out Distance di);
                output.Add(new(
                    reader["event_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["event_id"]),
                    reader["eventspecific_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_id"]),
                    Convert.ToInt32(reader["location_id"]),
                    Convert.ToInt32(reader["segment_id"]),
                    reader["timeresult_time"].ToString(),
                    Convert.ToInt32(reader["timeresult_occurance"]),
                    part != null ? part.FirstName : "",
                    part != null ? part.LastName : "",
                    knownDist ? di.Name : "",
                    bib,
                    readId,
                    reader["timeresult_unknown_id"].ToString(),
                    chipRead != null ? chipRead.TimeSeconds : 0,
                    chipRead != null ? chipRead.TimeMilliseconds : 0,
                    reader["timeresult_chiptime"].ToString(),
                    Convert.ToInt32(reader["timeresult_place"]),
                    Convert.ToInt32(reader["timeresult_age_place"]),
                    Convert.ToInt32(reader["timeresult_gender_place"]),
                    part != null ? part.Gender : "",
                    Convert.ToInt32(reader["timeresult_status"]),
                    reader["timeresult_splittime"].ToString(),
                    part != null ? part.EventSpecific.AgeGroupId : -1,
                    part != null ? part.EventSpecific.AgeGroupName : "",
                    Convert.ToInt32(reader["timeresult_uploaded"]),
                    part != null ? part.Birthdate : "",
                    knownDist ? di.Type : Constants.Timing.DISTANCE_TYPE_NORMAL,
                    knownDist && distanceDict.TryGetValue(di.LinkedDistance, out Distance linked) ? linked.Name : "",
                    chipRead != null ? chipRead.ChipNumber : "",
                    part != null ? part.EventSpecific.Anonymous : false,
                    part != null ? part.Identifier.ToString() : "",
                    locations,
                    segments,
                    distances,
                    theEvent,
                    part != null ? part.EventSpecific.Division : "",
                    Convert.ToInt32(reader["timeresult_division_place"])
                    ));
            }
            reader.Close();
            return output;
        }

        internal static List<TimeResult> GetTimingResults(int eventId, SQLiteConnection connection)
        {
            Event theEvent = Events.GetEvent(eventId, connection);
            Dictionary<int, ChipRead> chipReadDict = [];
            foreach (ChipRead cr in ChipReads.GetChipReads(eventId, theEvent, connection))
            {
                chipReadDict.Add(cr.ReadId, cr);
            }
            Dictionary<int, Participant> partDict = [];
            foreach (Participant p in Participants.GetParticipants(eventId, connection)) {
                partDict.Add(p.EventSpecific.Identifier, p);
            }
            Dictionary<int, Distance> distanceDict = [];
            foreach (Distance d in Distances.GetDistances(eventId, connection)) {
                distanceDict.Add(d.Identifier, d);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * " +
                "FROM time_results r " +
                "WHERE r.event_id=@eventid";
            command.Parameters.Add(new("@eventid", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader, chipReadDict, partDict, distanceDict);
            return output;
        }

        internal static List<TimeResult> GetLastSeenResults(int eventId, SQLiteConnection connection)
        {
            Event theEvent = Events.GetEvent(eventId, connection);
            Dictionary<int, ChipRead> chipReadDict = [];
            foreach (ChipRead cr in ChipReads.GetChipReads(eventId, theEvent, connection))
            {
                chipReadDict.Add(cr.ReadId, cr);
            }
            Dictionary<int, Participant> partDict = [];
            foreach (Participant p in Participants.GetParticipants(eventId, connection))
            {
                partDict.Add(p.EventSpecific.Identifier, p);
            }
            Dictionary<int, Distance> distanceDict = [];
            foreach (Distance d in Distances.GetDistances(eventId, connection))
            {
                distanceDict.Add(d.Identifier, d);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * " +
                "FROM time_results r " +
                "WHERE r.event_id=@eventid " +
                "GROUP BY eventspecific_id;";
            command.Parameters.AddRange(
            [
                new("@eventid", eventId),
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader, chipReadDict, partDict, distanceDict);
            return output;
        }

        internal static List<TimeResult> GetFinishTimes(int eventId, SQLiteConnection connection)
        {
            return GetSegmentTimes(eventId, Constants.Timing.SEGMENT_FINISH, connection);
        }

        internal static List<TimeResult> GetStartTimes(int eventId, SQLiteConnection connection)
        {
            return GetSegmentTimes(eventId, Constants.Timing.SEGMENT_START, connection);
        }

        internal static List<TimeResult> GetSegmentTimes(int eventId, int segmentId, SQLiteConnection connection)
        {
            Event theEvent = Events.GetEvent(eventId, connection);
            Dictionary<int, ChipRead> chipReadDict = [];
            foreach (ChipRead cr in ChipReads.GetChipReads(eventId, theEvent, connection))
            {
                chipReadDict.Add(cr.ReadId, cr);
            }
            Dictionary<int, Participant> partDict = [];
            foreach (Participant p in Participants.GetParticipants(eventId, connection))
            {
                partDict.Add(p.EventSpecific.Identifier, p);
            }
            Dictionary<int, Distance> distanceDict = [];
            foreach (Distance d in Distances.GetDistances(eventId, connection))
            {
                distanceDict.Add(d.Identifier, d);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * " +
                "FROM time_results r " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(
            [
                new("@eventid", eventId),
                new("@segment", segmentId)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader, chipReadDict, partDict, distanceDict);
            return output;
        }

        internal static void UpdateTimingResult(TimeResult oldResult, string newTime, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE time_results SET timeresult_time=@time WHERE event_id=@event AND eventspecific_id=@eventspecific AND location_id=@location AND timeresult_occurance=@occurance";
            command.Parameters.AddRange([
                new("@time", newTime),
                new("@event", oldResult.EventIdentifier),
                new("@eventspecific", oldResult.EventSpecificId),
                new("@location", oldResult.LocationId),
                new("@occurance", oldResult.Occurrence)]);
            command.ExecuteNonQuery();
        }

        internal static void SetUploadedTimingResults(List<TimeResult> results, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (TimeResult result in results)
                {
                    SQLiteCommand command = connection.CreateCommand();
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "UPDATE time_results SET timeresult_uploaded=@uploaded WHERE event_id=@event AND eventspecific_id=@eventspecific AND location_id=@location AND timeresult_occurance=@occurance";
                    command.Parameters.AddRange([
                        new("@uploaded", result.Uploaded),
                        new("@event", result.EventIdentifier),
                        new("@eventspecific", result.EventSpecificId),
                        new("@location", result.LocationId),
                        new("@occurance", result.Occurrence)]
                    );
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        internal static List<TimeResult> GetNonUploadedResults(int eventId, SQLiteConnection connection)
        {
            Event theEvent = Events.GetEvent(eventId, connection);
            Dictionary<int, ChipRead> chipReadDict = [];
            foreach (ChipRead cr in ChipReads.GetChipReads(eventId, theEvent, connection))
            {
                chipReadDict.Add(cr.ReadId, cr);
            }
            Dictionary<int, Participant> partDict = [];
            foreach (Participant p in Participants.GetParticipants(eventId, connection))
            {
                partDict.Add(p.EventSpecific.Identifier, p);
            }
            Dictionary<int, Distance> distanceDict = [];
            foreach (Distance d in Distances.GetDistances(eventId, connection))
            {
                distanceDict.Add(d.Identifier, d);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * " +
                "FROM time_results r " +
                "WHERE r.event_id=@eventid AND r.timeresult_uploaded=@uploaded;";
            command.Parameters.AddRange(
            [
                new("@eventid", eventId),
                new("@uploaded", Constants.Timing.TIMERESULT_UPLOADED_FALSE)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader, chipReadDict, partDict, distanceDict);
            return output;
        }

        internal static long UnprocessedResultsExist(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM time_results " +
                "WHERE event_id=@event AND timeresult_status=@status " +
                "AND segment_id<>@start AND segment_id<>@none " +
                "AND eventspecific_id<>@dummy;";
            command.Parameters.AddRange(
            [
                new("@event", eventId),
                new("@status", Constants.Timing.TIMERESULT_STATUS_NONE),
                new("@start", Constants.Timing.SEGMENT_START),
                new("@dummy", Constants.Timing.TIMERESULT_DUMMYPERSON),
                new("@none", Constants.Timing.SEGMENT_NONE)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            long output = reader.GetInt64(0);
            reader.Close();
            return output;
        }

        /*
         * Reset options for time_results and chipreads
         */

        internal static void ResetTimingResultsEvent(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM time_results WHERE event_id=@event;" +
                "UPDATE chipreads SET read_status=@status WHERE event_id=@event AND read_status!=@ignore " +
                "AND read_status!=@dnf AND read_status!=@dnf_ignore AND read_status!=@dns AND read_status!=@dns_ignore;" +
                "UPDATE eventspecific SET eventspecific_status=@estatus WHERE event_id=@event;";
            command.Parameters.AddRange(
            [
                new("@event", eventId),
                new("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new("@ignore", Constants.Timing.CHIPREAD_STATUS_IGNORE),
                new("@dnf", Constants.Timing.CHIPREAD_STATUS_DNF),
                new("@dnf_ignore", Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE),
                new("@dns", Constants.Timing.CHIPREAD_STATUS_DNS),
                new("@dns_ignore", Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE),
                new("@estatus", Constants.Timing.EVENTSPECIFIC_UNKNOWN)
            ]);
            command.ExecuteNonQuery();
        }

        internal static void ResetTimingResultsPlacements(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE time_results SET timeresult_status=@status WHERE event_id=@event;";
            command.Parameters.AddRange(
            [
                new("@event", eventId),
                new("@status", Constants.Timing.CHIPREAD_STATUS_NONE)
            ]);
            command.ExecuteNonQuery();
        }
    }
}