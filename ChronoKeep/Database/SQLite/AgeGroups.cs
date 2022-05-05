using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class AgeGroups
    {
        internal static void AddAgeGroup(AgeGroup group, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO age_groups (event_id, distance_id, start_age, end_age)" +
                " VALUES (@event, @distance, @start, @end);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", group.EventId),
                    new SQLiteParameter("@distance", group.DistanceId),
                    new SQLiteParameter("@start", group.StartAge),
                    new SQLiteParameter("@end", group.EndAge)
            });
            command.ExecuteNonQuery();
        }

        internal static void UpdateAgeGroup(AgeGroup group, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE age_groups SET event_id=@event, distance_id=@distance, " +
                    "start_age=@start, end_age=@end WHERE group_id=@group;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", group.EventId),
                    new SQLiteParameter("@distance", group.DistanceId),
                    new SQLiteParameter("@start", group.StartAge),
                    new SQLiteParameter("@end", group.EndAge),
                    new SQLiteParameter("@group", group.GroupId)
                });
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
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@group", group.GroupId)
                });
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
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@distance", distanceId),
                });
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
                    command.Parameters.AddRange(new SQLiteParameter[]
                    {
                        new SQLiteParameter("@group", ag.GroupId),
                    });
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
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static List<AgeGroup> GetAgeGroups(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", eventId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = new List<AgeGroup>();
            while (reader.Read())
            {
                output.Add(new AgeGroup(Convert.ToInt32(reader["group_id"]), Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["distance_id"]), Convert.ToInt32(reader["start_age"]), Convert.ToInt32(reader["end_age"]), Convert.ToInt32(reader["last_group"])));
            }
            reader.Close();
            return output;
        }

        internal static List<AgeGroup> GetAgeGroups(int eventId, int distanceId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event AND distance_id=@distance;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@distance", distanceId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = new List<AgeGroup>();
            while (reader.Read())
            {
                output.Add(new AgeGroup(Convert.ToInt32(reader["group_id"]), Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["distance_id"]), Convert.ToInt32(reader["start_age"]), Convert.ToInt32(reader["end_age"]),
                    Convert.ToInt32(reader["last_group"])));
            }
            reader.Close();
            return output;
        }
    }
}
