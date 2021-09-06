﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class Events
    {
        internal static void AddEvent(Event anEvent, SQLiteConnection connection)
        {
            Log.D("Attempting to grab Mutex: ID 9");
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO events(event_name, event_date," +
                "event_common_age_groups, event_common_start_finish, event_rank_by_gun, event_distance_specific_segments, event_yearcode, " +
                "event_start_time_seconds, " +
                "event_start_time_milliseconds, event_timing_system, event_type)" +
                " values(@name,@date,@age,@start,@gun,@sepseg,@yearcode,@startsec,@startmill,@system," +
                "@type)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date),
                new SQLiteParameter("@age", anEvent.CommonAgeGroups),
                new SQLiteParameter("@start", anEvent.CommonStartFinish),
                new SQLiteParameter("@gun", anEvent.RankByGun),
                new SQLiteParameter("@sepseg", anEvent.DistanceSpecificSegments),
                new SQLiteParameter("@yearcode", anEvent.YearCode),
                new SQLiteParameter("@startsec", anEvent.StartSeconds),
                new SQLiteParameter("@startmill", anEvent.StartMilliseconds),
                new SQLiteParameter("@system", anEvent.TimingSystem),
                new SQLiteParameter("@type", anEvent.EventType)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        internal static void RemoveEvent(int identifier, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM time_results WHERE event_id=@event;" +
                    "DELETE FROM bib_chip_assoc WHERE event_id=@event;" +
                    "DELETE FROM segments WHERE event_id=@event; DELETE FROM chipreads WHERE event_id=@event;" +
                    "DELETE FROM age_groups WHERE event_id=@event;" +
                    "DELETE FROM distances WHERE event_id=@event; DELETE FROM timing_locations WHERE event_id=@event;" +
                    "DELETE FROM eventspecific WHERE event_id=@event; DELETE FROM events WHERE event_id=@event;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void UpdateEvent(Event anEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE events SET event_name=@name, event_date=@date, event_yearcode=@yearcode," +
                "event_common_age_groups=@age, event_common_start_finish=@start, event_rank_by_gun=@gun, " +
                "event_distance_specific_segments=@seg, " +
                "event_start_time_seconds=@startsec, event_start_time_milliseconds=@startmill, " +
                "event_timing_system=@system, event_type=@type," +
                "event_finish_max_occurances=@maxocc, event_finish_ignore_within=@ignore," +
                "event_start_window=@startWindow, api_id=@apiid, api_event_id=@apieventid WHERE event_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", anEvent.Identifier),
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date),
                new SQLiteParameter("@yearcode", anEvent.YearCode),
                new SQLiteParameter("@age", anEvent.CommonAgeGroups),
                new SQLiteParameter("@start", anEvent.CommonStartFinish),
                new SQLiteParameter("@gun", anEvent.RankByGun),
                new SQLiteParameter("@seg", anEvent.DistanceSpecificSegments),
                new SQLiteParameter("@startsec", anEvent.StartSeconds),
                new SQLiteParameter("@startmill", anEvent.StartMilliseconds),
                new SQLiteParameter("@system", anEvent.TimingSystem),
                new SQLiteParameter("@type", anEvent.EventType),
                new SQLiteParameter("@maxocc", anEvent.FinishMaxOccurrences),
                new SQLiteParameter("@ignore", anEvent.FinishIgnoreWithin),
                new SQLiteParameter("@startWindow", anEvent.StartWindow),
                new SQLiteParameter("@apiid", anEvent.API_ID),
                new SQLiteParameter("@apieventid", anEvent.API_Event_ID),
            });
            command.ExecuteNonQuery();
        }

        internal static List<Event> GetEvents(SQLiteConnection connection)
        {

            List<Event> output = new List<Event>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Event(Convert.ToInt32(reader["event_id"]),
                    reader["event_name"].ToString(),
                    reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]),
                    Convert.ToInt32(reader["event_distance_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]),
                    reader["event_yearcode"].ToString(),
                    Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]),
                    Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]),
                    Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString(),
                    Convert.ToInt32(reader["event_type"]),
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_event_id"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }

        internal static int GetEventID(Event anEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT event_id FROM events WHERE event_name=@name AND event_date=@date";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["event_id"]);
            }
            reader.Close();
            return output;
        }

        internal static Event GetEvent(int id, SQLiteConnection connection)
        {
            if (id < 0)
            {
                return null;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id", id));
            SQLiteDataReader reader = command.ExecuteReader();
            Event output = null;
            if (reader.Read())
            {
                output = new Event(Convert.ToInt32(reader["event_id"]),
                    reader["event_name"].ToString(),
                    reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]),
                    Convert.ToInt32(reader["event_distance_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]),
                    reader["event_yearcode"].ToString(),
                    Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]),
                    Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]),
                    Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString(),
                    Convert.ToInt32(reader["event_type"]),
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_event_id"].ToString()
                    );
            }
            reader.Close();
            return output;
        }

        internal static void SetStartWindow(Event anEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_start_window=@window WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@window", anEvent.StartWindow),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
        }

        internal static void SetFinishOptions(Event anEvent, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_finish_max_occurances=@occ, event_finish_ignore_within=@ignore WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@occ", anEvent.FinishMaxOccurrences),
                new SQLiteParameter("@ignore", anEvent.FinishIgnoreWithin),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
        }
    }
}