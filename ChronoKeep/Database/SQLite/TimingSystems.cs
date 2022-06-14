using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    class TimingSystems
    {
        internal static void AddTimingSystem(TimingSystem system, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO timing_systems (ts_ip, ts_port, ts_location, ts_type)" +
                " VALUES (@ip, @port, @location, @type);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@ip", system.IPAddress),
                new SQLiteParameter("@port", system.Port),
                new SQLiteParameter("@location", system.LocationID),
                new SQLiteParameter("@type", system.Type)
            });
            command.ExecuteNonQuery();
        }

        internal static void UpdateTimingSystem(TimingSystem system, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE timing_systems SET ts_ip=@ip, ts_port=@port, ts_location=@location, ts_type=@type WHERE ts_identifier=@id;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@ip", system.IPAddress),
                    new SQLiteParameter("@port", system.Port),
                    new SQLiteParameter("@location", system.LocationID),
                    new SQLiteParameter("@type", system.Type),
                    new SQLiteParameter("@id", system.SystemIdentifier)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void SetTimingSystems(List<TimingSystem> systems, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems;";
                command.ExecuteNonQuery();
                foreach (TimingSystem sys in systems)
                {
                    AddTimingSystem(sys, connection);
                }
                transaction.Commit();
            }
        }

        internal static void RemoveTimingSystem(int systemId, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems WHERE ts_identifier=@id;";
                command.Parameters.Add(new SQLiteParameter("@id", systemId));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static List<TimingSystem> GetTimingSystems(SQLiteConnection connection)
        {
            List<TimingSystem> output = new List<TimingSystem>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM timing_systems;";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingSystem(Convert.ToInt32(reader["ts_identifier"]), reader["ts_ip"].ToString(),
                    Convert.ToInt32(reader["ts_port"]), Convert.ToInt32(reader["ts_location"]), reader["ts_type"].ToString()));
            }
            reader.Close();
            return output;
        }
    }
}
