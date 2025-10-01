﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class TimingLocations
    {
        internal static int AddTimingLocation(TimingLocation tl, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timing_locations (event_id, location_name, location_max_occurances, location_ignore_within) " +
                "VALUES (@event,@name,@max,@ignore)";
            command.Parameters.AddRange([
                new("@event", tl.EventIdentifier),
                new("@name", tl.Name),
                new("@max", tl.MaxOccurrences),
                new("@ignore", tl.IgnoreWithin) ]);
            command.ExecuteNonQuery();
            long outVal = connection.LastInsertRowId;
            return (int)outVal;
        }

        internal static void RemoveTimingLocation(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM timing_locations WHERE location_id=@id";
            command.Parameters.AddRange([
                    new("@id", identifier) ]);
            command.ExecuteNonQuery();
        }

        internal static void UpdateTimingLocation(TimingLocation tl, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE timing_locations SET event_id=@event, location_name=@name, location_max_occurances=@max, " +
                "location_ignore_within=@ignore WHERE location_id=@id";
            command.Parameters.AddRange([
                new("@event", tl.EventIdentifier),
                new("@name", tl.Name),
                new("@max", tl.MaxOccurrences),
                new("@ignore", tl.IgnoreWithin),
                new("@id", tl.Identifier) ]);
            command.ExecuteNonQuery();
        }

        internal static List<TimingLocation> GetTimingLocations(int eventId, SQLiteConnection connection)
        {
            List<TimingLocation> output = [];
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM timing_locations WHERE event_id=@event;";
            command.Parameters.Add(new("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new(Convert.ToInt32(reader["location_id"]), Convert.ToInt32(reader["event_id"]),
                    reader["location_name"].ToString(), Convert.ToInt32(reader["location_max_occurances"]), Convert.ToInt32(reader["location_ignore_within"])));
            }
            reader.Close();
            return output;
        }

        internal static int GetTimingLocationID(TimingLocation tl, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT timingpoint_id FROM timing_locations WHERE event_id=@eventid, location_name=@name";
            command.Parameters.AddRange(
            [
                new("@name", tl.Name),
                new("@eventid", tl.EventIdentifier)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["location_id"]);
            }
            reader.Close();
            return output;
        }
    }
}
