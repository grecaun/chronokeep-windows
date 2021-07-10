using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class Distances
    {

        internal static void AddDistance(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (division_name, event_id, division_cost, division_distance, division_distance_unit," +
                "division_start_location, division_start_within, division_finish_location, division_finish_occurance, division_wave, bib_group_number," +
                "division_start_offset_seconds, division_start_offset_milliseconds, division_end_offset_seconds, division_early_start_offset_seconds, " +
                "division_linked_id, division_type, division_ranking_order) " +
                "values (@name,@event_id,@cost,@distance,@unit,@startloc,@startwithin,@finishloc,@finishocc,@wave,@bgn,@soffsec,@soffmill,@endSec,@esdiff,@linked,@type,@rank)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@event_id", d.EventIdentifier),
                new SQLiteParameter("@cost", d.Cost),
                new SQLiteParameter("@distance", d.DistanceValue),
                new SQLiteParameter("@unit", d.DistanceUnit),
                new SQLiteParameter("@startloc", d.StartLocation),
                new SQLiteParameter("@startwithin", d.StartWithin),
                new SQLiteParameter("@finishloc", d.FinishLocation),
                new SQLiteParameter("@finishocc", d.FinishOccurrence),
                new SQLiteParameter("@wave", d.Wave),
                new SQLiteParameter("@bgn", d.BibGroupNumber),
                new SQLiteParameter("@soffsec", d.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", d.StartOffsetMilliseconds),
                new SQLiteParameter("@endSec", d.EndSeconds),
                new SQLiteParameter("@esdiff", d.EarlyStartOffsetSeconds),
                new SQLiteParameter("@linked", d.LinkedDivision),
                new SQLiteParameter("@type", d.Type),
                new SQLiteParameter("@rank", d.Ranking)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        internal static void RemoveDistance(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = " DELETE FROM segments WHERE division_id=@id; DELETE FROM divisions WHERE division_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
        }

        internal static void UpdateDistance(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE divisions SET division_name=@name, event_id=@event, division_cost=@cost, division_distance=@distance," +
                "division_distance_unit=@unit, division_start_location=@startloc, division_start_within=@within, division_finish_location=@finishloc," +
                "division_finish_occurance=@occurance, division_wave=@wave, bib_group_number=@bgn, division_start_offset_seconds=@soffsec, " +
                "division_start_offset_milliseconds=@soffmill, division_end_offset_seconds=@endSec, division_early_start_offset_seconds=@esdiff," +
                "division_linked_id=@linked, division_type=@type, division_ranking_order=@rank " +
                "WHERE division_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@event", d.EventIdentifier),
                new SQLiteParameter("@cost", d.Cost),
                new SQLiteParameter("@distance", d.DistanceValue),
                new SQLiteParameter("@unit", d.DistanceUnit),
                new SQLiteParameter("@startloc", d.StartLocation),
                new SQLiteParameter("@within", d.StartWithin),
                new SQLiteParameter("@finishloc", d.FinishLocation),
                new SQLiteParameter("@occurance", d.FinishOccurrence),
                new SQLiteParameter("@wave", d.Wave),
                new SQLiteParameter("@bgn", d.BibGroupNumber),
                new SQLiteParameter("@soffsec", d.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", d.StartOffsetMilliseconds),
                new SQLiteParameter("@id", d.Identifier),
                new SQLiteParameter("@endSec", d.EndSeconds),
                new SQLiteParameter("@esdiff", d.EarlyStartOffsetSeconds),
                new SQLiteParameter("@linked", d.LinkedDivision),
                new SQLiteParameter("@type", d.Type),
                new SQLiteParameter("@rank", d.Ranking)
            });
            command.ExecuteNonQuery();
        }

        internal static List<Distance> GetDistances(SQLiteConnection connection)
        {
            List<Distance> output = new List<Distance>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM divisions";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Distance(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"]),
                    Convert.ToInt32(reader["division_early_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_linked_id"]),
                    Convert.ToInt32(reader["division_type"]),
                    Convert.ToInt32(reader["division_ranking_order"])
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
            command.CommandText = "SELECT * FROM divisions WHERE event_id = " + eventId;
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Distance(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"]),
                    Convert.ToInt32(reader["division_early_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_linked_id"]),
                    Convert.ToInt32(reader["division_type"]),
                    Convert.ToInt32(reader["division_ranking_order"])
                    ));
            }
            reader.Close();
            return output;
        }

        internal static int GetDistanceID(Distance d, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT division_id FROM divisions WHERE division_name=@name AND event_id=@eventid";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", d.Name),
                new SQLiteParameter("@eventid", d.EventIdentifier)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["division_id"]);
            }
            reader.Close();
            return output;
        }

        internal static Distance GetDistance(int distanceId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM divisions WHERE division_id=@div";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@div", distanceId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            Distance output = null;
            if (reader.Read())
            {
                output = new Distance(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"]),
                    Convert.ToInt32(reader["division_early_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_linked_id"]),
                    Convert.ToInt32(reader["division_type"]),
                    Convert.ToInt32(reader["division_ranking_order"])
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
                command.CommandText = "UPDATE divisions SET division_start_offset_seconds=@seconds," +
                    " division_start_offset_milliseconds=@milli WHERE event_id=@event AND division_wave=@wave;";
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
