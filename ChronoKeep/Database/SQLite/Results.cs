﻿using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class Results
    {
        internal static void AddTimingResult(TimeResult tr, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO time_results (event_id, eventspecific_id, location_id, segment_id, " +
                "timeresult_occurance, timeresult_time, timeresult_unknown_id, read_id, timeresult_chiptime," +
                "timeresult_place, timeresult_age_place, timeresult_gender_place," +
                "timeresult_status, timeresult_splittime, timeresult_uploaded)" +
                " VALUES (@event,@specific,@location,@segment,@occ,@time,@unknown,@read,@chip,@place,@agplace," +
                "@gendplace,@status,@split,@uploaded)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventIdentifier),
                new SQLiteParameter("@specific", tr.EventSpecificId),
                new SQLiteParameter("@location", tr.LocationId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@occ", tr.Occurrence),
                new SQLiteParameter("@time", tr.Time),
                new SQLiteParameter("@unknown", tr.UnknownId),
                new SQLiteParameter("@read", tr.ReadId),
                new SQLiteParameter("@chip", tr.ChipTime),
                new SQLiteParameter("@place", tr.Place),
                new SQLiteParameter("@agplace", tr.AgePlace),
                new SQLiteParameter("@gendplace", tr.GenderPlace),
                new SQLiteParameter("@status", tr.Status),
                new SQLiteParameter("@split", tr.LapTime),
                new SQLiteParameter("@uploaded", tr.Uploaded)
            });
            command.ExecuteNonQuery();
        }

        internal static void RemoveTimingResult(TimeResult tr, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM time_results WHERE eventspecific_id=@event AND location_id=@location AND " +
                "segment_id=@segment AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventSpecificId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@occurance", tr.Occurrence),
                new SQLiteParameter("@location", tr.LocationId) });
            command.ExecuteNonQuery();
        }

        private static List<TimeResult> GetResultsInternal(SQLiteDataReader reader)
        {
            List<TimeResult> output = new List<TimeResult>();
            while (reader.Read())
            {
                string bib = "";
                if (reader["bib"] != DBNull.Value)
                {
                    bib = reader["bib"].ToString();
                }
                else if (reader["eventspecific_bib"] != DBNull.Value)
                {
                    bib = reader["eventspecific_bib"].ToString();
                }
                output.Add(new TimeResult(
                    reader["event_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["event_id"]),
                    reader["eventspecific_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_id"]),
                    Convert.ToInt32(reader["location_id"]),
                    Convert.ToInt32(reader["segment_id"]),
                    reader["timeresult_time"].ToString(),
                    Convert.ToInt32(reader["timeresult_occurance"]),
                    reader["participant_first"] == DBNull.Value ? "" : reader["participant_first"].ToString(),
                    reader["participant_last"] == DBNull.Value ? "" : reader["participant_last"].ToString(),
                    reader["distance_name"] == DBNull.Value ? "" : reader["distance_name"].ToString(),
                    bib,
                    Convert.ToInt32(reader["read_id"]),
                    reader["timeresult_unknown_id"].ToString(),
                    Convert.ToInt64(reader["read_time_seconds"]),
                    Convert.ToInt32(reader["read_time_milliseconds"]),
                    reader["timeresult_chiptime"].ToString(),
                    Convert.ToInt32(reader["timeresult_place"]),
                    Convert.ToInt32(reader["timeresult_age_place"]),
                    Convert.ToInt32(reader["timeresult_gender_place"]),
                    reader["participant_gender"] == DBNull.Value ? "" : reader["participant_gender"].ToString(),
                    Convert.ToInt32(reader["timeresult_status"]),
                    reader["timeresult_splittime"].ToString(),
                    reader["eventspecific_age_group_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_age_group_id"]),
                    reader["eventspecific_age_group_name"] == DBNull.Value ? "" : reader["eventspecific_age_group_name"].ToString(),
                    Convert.ToInt32(reader["timeresult_uploaded"]),
                    reader["participant_birthday"] == DBNull.Value ? "" : reader["participant_birthday"].ToString(),
                    reader["distance_type"] == DBNull.Value ? Constants.Timing.DISTANCE_TYPE_NORMAL : Convert.ToInt32(reader["distance_type"]),
                    reader["linked_distance_name"] == DBNull.Value ? "" : reader["linked_distance_name"].ToString(),
                    reader["chip"] == DBNull.Value ? "" : reader["chip"].ToString(),
                    reader["eventspecific_anonymous"] != DBNull.Value && Convert.ToInt16(reader["eventspecific_anonymous"]) != 0,
                    reader["part_id"] != DBNull.Value ? reader["part_id"].ToString() : ""
                    ));
            }
            reader.Close();
            return output;
        }

        internal static List<TimeResult> GetTimingResults(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static List<TimeResult> GetLastSeenResults(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "MAX(read_time_seconds) AS read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid " +
                "GROUP BY e.eventspecific_id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static List<TimeResult> GetFinishTimes(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", Constants.Timing.SEGMENT_FINISH)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static List<TimeResult> GetStartTimes(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", Constants.Timing.SEGMENT_START)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static List<TimeResult> GetSegmentTimes(int eventId, int segmentId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", segmentId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static void UpdateTimingResult(TimeResult oldResult, string newTime, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE time_results SET timeresult_time=@time WHERE event_id=@event AND eventspecific_id=@eventspecific AND location_id=@location AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@time", newTime),
                new SQLiteParameter("@event", oldResult.EventIdentifier),
                new SQLiteParameter("@eventspecific", oldResult.EventSpecificId),
                new SQLiteParameter("@location", oldResult.LocationId),
                new SQLiteParameter("@occurance", oldResult.Occurrence)});
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
                    command.Parameters.AddRange(new SQLiteParameter[] {
                        new SQLiteParameter("@uploaded", result.Uploaded),
                        new SQLiteParameter("@event", result.EventIdentifier),
                        new SQLiteParameter("@eventspecific", result.EventSpecificId),
                        new SQLiteParameter("@location", result.LocationId),
                        new SQLiteParameter("@occurance", result.Occurrence)}
                    );
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        internal static List<TimeResult> GetNonUploadedResults(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT " +
                    "e.event_id," +
                    "e.eventspecific_id," +
                    "r.location_id," +
                    "r.segment_id," +
                    "timeresult_time," +
                    "timeresult_occurance," +
                    "participant_first," +
                    "participant_last," +
                    "d.distance_name," +
                    "eventspecific_bib," +
                    "r.read_id," +
                    "timeresult_unknown_id," +
                    "read_time_seconds," +
                    "read_time_milliseconds," +
                    "timeresult_chiptime," +
                    "timeresult_place," +
                    "timeresult_age_place," +
                    "timeresult_gender_place," +
                    "participant_gender," +
                    "timeresult_status," +
                    "timeresult_splittime," +
                    "eventspecific_age_group_id," +
                    "eventspecific_age_group_name," +
                    "timeresult_uploaded," +
                    "participant_birthday," +
                    "d.distance_type," +
                    "y.distance_name AS linked_distance_name, " +
                    "b.bib," +
                    "b.chip," +
                    "eventspecific_anonymous," +
                    "p.participant_id AS part_id " +
                "FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN bib_chip_assoc b ON ( b.chip=c.read_chipnumber AND r.event_id=b.event_id )" +
                "JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN (distances d LEFT JOIN distances y ON d.distance_linked_id=y.distance_id) ON d.distance_id=e.distance_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.timeresult_uploaded=@uploaded;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@uploaded", Constants.Timing.TIMERESULT_UPLOADED_FALSE)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            return output;
        }

        internal static long UnprocessedReadsExist(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM chipreads WHERE event_id=@event AND read_status=@status;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            long output = reader.GetInt64(0);
            reader.Close();
            return output;
        }

        internal static long UnprocessedResultsExist(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM time_results " +
                "WHERE event_id=@event AND timeresult_status=@status " +
                "AND segment_id<>@start AND segment_id<>@none " +
                "AND eventspecific_id<>@dummy;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.TIMERESULT_STATUS_NONE),
                new SQLiteParameter("@start", Constants.Timing.SEGMENT_START),
                new SQLiteParameter("@dummy", Constants.Timing.TIMERESULT_DUMMYPERSON),
                new SQLiteParameter("@none", Constants.Timing.SEGMENT_NONE)
            });
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
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@ignore", Constants.Timing.CHIPREAD_STATUS_IGNORE),
                new SQLiteParameter("@dnf", Constants.Timing.CHIPREAD_STATUS_DNF),
                new SQLiteParameter("@dnf_ignore", Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE),
                new SQLiteParameter("@dns", Constants.Timing.CHIPREAD_STATUS_DNS),
                new SQLiteParameter("@dns_ignore", Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE),
                new SQLiteParameter("@estatus", Constants.Timing.EVENTSPECIFIC_UNKNOWN)
            });
            command.ExecuteNonQuery();
        }

        internal static void ResetTimingResultsPlacements(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE time_results SET timeresult_status=@status WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE)
            });
            command.ExecuteNonQuery();
        }
    }
}