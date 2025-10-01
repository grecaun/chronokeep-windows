using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class AgeGroups
    {
        internal static int AddAgeGroup(AgeGroup group, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO age_groups (event_id, distance_id, start_age, end_age, custom_name)" +
                " VALUES (@event, @distance, @start, @end, @custom);";
            command.Parameters.AddRange(
            [
                    new("@event", group.EventId),
                    new("@distance", group.DistanceId),
                    new("@start", group.StartAge),
                    new("@end", group.EndAge),
                    new("@custom", group.CustomName)
            ]);
            command.ExecuteNonQuery();
            long outVal = connection.LastInsertRowId;
            return (int)outVal;
        }

        internal static void UpdateAgeGroup(AgeGroup group, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE age_groups SET event_id=@event, distance_id=@distance, " +
                    "start_age=@start, end_age=@end, custom_name=@custom WHERE group_id=@group;";
                command.Parameters.AddRange(
                [
                    new("@event", group.EventId),
                    new("@distance", group.DistanceId),
                    new("@start", group.StartAge),
                    new("@end", group.EndAge),
                    new("@group", group.GroupId),
                    new("@custom", group.CustomName)
                ]);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void RemoveAgeGroup(AgeGroup group, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM age_groups WHERE group_id=@group;";
                command.Parameters.AddRange(
                [
                    new("@group", group.GroupId)
                ]);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void RemoveAgeGroups(int eventId, int distanceId, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM age_groups WHERE event_id=@event AND distance_id=@distance;";
                command.Parameters.AddRange(
                [
                    new("@event", eventId),
                    new("@distance", distanceId),
                ]);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void RemoveAgeGroups(List<AgeGroup> groups, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                foreach (AgeGroup ag in groups)
                {
                    command.CommandText = "DELETE FROM age_groups WHERE group_id=@group;";
                    command.Parameters.AddRange(
                    [
                        new("@group", ag.GroupId),
                    ]);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        internal static void ResetAgeGroups(int eventId, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM age_groups WHERE event_id=@event;";
                command.Parameters.AddRange(
                [
                    new("@event", eventId),
                ]);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static List<AgeGroup> GetAgeGroups(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event;";
            command.Parameters.AddRange(
            [
                    new("@event", eventId)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = new List<AgeGroup>();
            while (reader.Read())
            {
                output.Add(
                    new AgeGroup(
                        Convert.ToInt32(reader["group_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["distance_id"]),
                        Convert.ToInt32(reader["start_age"]),
                        Convert.ToInt32(reader["end_age"]),
                        Convert.ToInt32(reader["last_group"]),
                        reader["custom_name"].ToString()
                        ));
            }
            reader.Close();
            return output;
        }

        internal static List<AgeGroup> GetAgeGroups(int eventId, int distanceId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event AND distance_id=@distance;";
            command.Parameters.AddRange(
            [
                    new("@event", eventId),
                    new("@distance", distanceId)
            ]);
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = [];
            while (reader.Read())
            {
                output.Add(
                    new AgeGroup(
                        Convert.ToInt32(reader["group_id"]), 
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["distance_id"]), 
                        Convert.ToInt32(reader["start_age"]),
                        Convert.ToInt32(reader["end_age"]),
                        Convert.ToInt32(reader["last_group"]),
                        reader["custom_name"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }
    }
}
