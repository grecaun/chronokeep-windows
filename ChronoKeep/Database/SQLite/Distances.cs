using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class Distances
    {

        internal static int AddDistance(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO distances (distance_name, event_id, distance_distance, distance_distance_unit," +
                "distance_start_location, distance_start_within, distance_finish_location, distance_finish_occurance, distance_wave, " +
                "distance_start_offset_seconds, distance_start_offset_milliseconds, distance_end_offset_seconds, " +
                "distance_linked_id, distance_type, distance_ranking_order, distance_sms_enabled, distance_upload_results) " +
                "values (@name,@event_id,@distance,@unit,@startloc,@startwithin,@finishloc,@finishocc,@wave,@soffsec,@soffmill,@endSec,@linked,@type,@rank,@sms,@upload)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@event_id", d.EventIdentifier),
                new SQLiteParameter("@distance", d.DistanceValue),
                new SQLiteParameter("@unit", d.DistanceUnit),
                new SQLiteParameter("@startloc", d.StartLocation),
                new SQLiteParameter("@startwithin", d.StartWithin),
                new SQLiteParameter("@finishloc", d.FinishLocation),
                new SQLiteParameter("@finishocc", d.FinishOccurrence),
                new SQLiteParameter("@wave", d.Wave),
                new SQLiteParameter("@soffsec", d.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", d.StartOffsetMilliseconds),
                new SQLiteParameter("@endSec", d.EndSeconds),
                new SQLiteParameter("@linked", d.LinkedDistance),
                new SQLiteParameter("@type", d.Type),
                new SQLiteParameter("@rank", d.Ranking),
                new SQLiteParameter("@sms", d.SMSEnabled ? 1 : 0),
                new SQLiteParameter("@upload", d.Upload ? 1 : 0),
            });
            Log.D("Database.SQLite.Distances", "SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
            long outVal = connection.LastInsertRowId;
            return (int)outVal;
        }

        internal static void RemoveDistance(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM segments WHERE distance_id=@id; DELETE FROM eventspecific WHERE distance_id=@id; DELETE FROM age_groups WHERE distance_id=@id; DELETE FROM distances WHERE distance_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
        }

        internal static void UpdateDistance(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE distances SET distance_name=@name, event_id=@event, distance_distance=@distance," +
                "distance_distance_unit=@unit, distance_start_location=@startloc, distance_start_within=@within, distance_finish_location=@finishloc," +
                "distance_finish_occurance=@occurance, distance_wave=@wave, distance_start_offset_seconds=@soffsec, " +
                "distance_start_offset_milliseconds=@soffmill, distance_end_offset_seconds=@endSec, " +
                "distance_linked_id=@linked, distance_type=@type, distance_ranking_order=@rank, distance_sms_enabled=@sms, distance_upload_results=@upload " +
                "WHERE distance_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@event", d.EventIdentifier),
                new SQLiteParameter("@distance", d.DistanceValue),
                new SQLiteParameter("@unit", d.DistanceUnit),
                new SQLiteParameter("@startloc", d.StartLocation),
                new SQLiteParameter("@within", d.StartWithin),
                new SQLiteParameter("@finishloc", d.FinishLocation),
                new SQLiteParameter("@occurance", d.FinishOccurrence),
                new SQLiteParameter("@wave", d.Wave),
                new SQLiteParameter("@soffsec", d.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", d.StartOffsetMilliseconds),
                new SQLiteParameter("@id", d.Identifier),
                new SQLiteParameter("@endSec", d.EndSeconds),
                new SQLiteParameter("@linked", d.LinkedDistance),
                new SQLiteParameter("@type", d.Type),
                new SQLiteParameter("@rank", d.Ranking),
                new SQLiteParameter("@sms", d.SMSEnabled ? 1 : 0),
                new SQLiteParameter("@upload", d.Upload ? 1 : 0),
            });
            command.ExecuteNonQuery();
        }

        internal static List<Distance> GetDistances(SQLiteConnection connection)
        {
            List<Distance> output = new List<Distance>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM distances";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Distance(Convert.ToInt32(reader["distance_id"]),
                    reader["distance_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToDouble(reader["distance_distance"]),
                    Convert.ToInt32(reader["distance_distance_unit"]),
                    Convert.ToInt32(reader["distance_finish_location"]),
                    Convert.ToInt32(reader["distance_finish_occurance"]),
                    Convert.ToInt32(reader["distance_start_location"]),
                    Convert.ToInt32(reader["distance_start_within"]),
                    Convert.ToInt32(reader["distance_wave"]),
                    Convert.ToInt32(reader["distance_start_offset_seconds"]),
                    Convert.ToInt32(reader["distance_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["distance_end_offset_seconds"]),
                    Convert.ToInt32(reader["distance_linked_id"]),
                    Convert.ToInt32(reader["distance_type"]),
                    Convert.ToInt32(reader["distance_ranking_order"]),
                    Convert.ToInt32(reader["distance_sms_enabled"]) == 0 ? false : true,
                    Convert.ToInt32(reader["distance_upload_results"]) == 0 ? false : true
                    ));
            }
            reader.Close();
            return output;
        }

        internal static List<Distance> GetDistances(int eventId, SQLiteConnection connection)
        {
            List<Distance> output = new List<Distance>();
            if (eventId <= 0)
            {
                return output;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM distances WHERE event_id = " + eventId;
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Distance(Convert.ToInt32(reader["distance_id"]),
                    reader["distance_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToDouble(reader["distance_distance"]),
                    Convert.ToInt32(reader["distance_distance_unit"]),
                    Convert.ToInt32(reader["distance_finish_location"]),
                    Convert.ToInt32(reader["distance_finish_occurance"]),
                    Convert.ToInt32(reader["distance_start_location"]),
                    Convert.ToInt32(reader["distance_start_within"]),
                    Convert.ToInt32(reader["distance_wave"]),
                    Convert.ToInt32(reader["distance_start_offset_seconds"]),
                    Convert.ToInt32(reader["distance_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["distance_end_offset_seconds"]),
                    Convert.ToInt32(reader["distance_linked_id"]),
                    Convert.ToInt32(reader["distance_type"]),
                    Convert.ToInt32(reader["distance_ranking_order"]),
                    Convert.ToInt32(reader["distance_sms_enabled"]) == 0 ? false : true,
                    Convert.ToInt32(reader["distance_upload_results"]) == 0 ? false : true
                    ));
            }
            reader.Close();
            return output;
        }

        internal static int GetDistanceID(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT distance_id FROM distances WHERE distance_name=@name AND event_id=@eventid";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@eventid", d.EventIdentifier)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["distance_id"]);
            }
            reader.Close();
            return output;
        }

        internal static Distance GetDistance(int distanceId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM distances WHERE distance_id=@div";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@div", distanceId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            Distance output = null;
            if (reader.Read())
            {
                output = new Distance(Convert.ToInt32(reader["distance_id"]),
                    reader["distance_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToDouble(reader["distance_distance"]),
                    Convert.ToInt32(reader["distance_distance_unit"]),
                    Convert.ToInt32(reader["distance_finish_location"]),
                    Convert.ToInt32(reader["distance_finish_occurance"]),
                    Convert.ToInt32(reader["distance_start_location"]),
                    Convert.ToInt32(reader["distance_start_within"]),
                    Convert.ToInt32(reader["distance_wave"]),
                    Convert.ToInt32(reader["distance_start_offset_seconds"]),
                    Convert.ToInt32(reader["distance_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["distance_end_offset_seconds"]),
                    Convert.ToInt32(reader["distance_linked_id"]),
                    Convert.ToInt32(reader["distance_type"]),
                    Convert.ToInt32(reader["distance_ranking_order"]),
                    Convert.ToInt32(reader["distance_sms_enabled"]) == 0 ? false : true,
                    Convert.ToInt32(reader["distance_upload_results"]) == 0 ? false : true
                    );
            }
            reader.Close();
            return output;
        }

        internal static void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE distances SET distance_start_offset_seconds=@seconds," +
                    " distance_start_offset_milliseconds=@milli WHERE event_id=@event AND distance_wave=@wave;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@wave", wave),
                    new SQLiteParameter("@seconds", seconds),
                    new SQLiteParameter("@milli", milliseconds)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }
    }
}
