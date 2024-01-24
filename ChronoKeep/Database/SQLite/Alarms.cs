﻿using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    internal class Alarms
    {
        internal static void SaveAlarms(int eventId, List<Alarm> alarms, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO alarms (event_id, alarm_bib, alarm_chip, alarm_enabled, alarm_sound) VALUES (@eventId, @bib, @chip, @enabled, @sound);";
                foreach (Alarm item in alarms)
                {
                    command.Parameters.AddRange(new SQLiteParameter[]
                    {
                        new SQLiteParameter("@eventId", eventId),
                        new SQLiteParameter("@bib", item.Bib),
                        new SQLiteParameter("@chip", item.Chip),
                        new SQLiteParameter("@enabled", item.Enabled ? 1 : 0),
                        new SQLiteParameter("@sound", item.AlarmSound),
                    });
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        internal static void SaveAlarm(int eventId, Alarm alarm, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO alarms (event_id, alarm_bib, alarm_chip, alarm_enabled, alarm_sound) VALUES (@eventId, @bib, @chip, @enabled, @sound);";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@eventId", eventId),
                    new SQLiteParameter("@bib", alarm.Bib),
                    new SQLiteParameter("@chip", alarm.Chip),
                    new SQLiteParameter("@enabled", alarm.Enabled ? 1 : 0),
                    new SQLiteParameter("@sound", alarm.AlarmSound),
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static List<Alarm> GetAlarms(int eventId,  SQLiteConnection connection)
        {
            List<Alarm> output = new List<Alarm>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM alarms WHERE event_id=@eventId;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@eventId", eventId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Alarm(
                    Convert.ToInt32(reader["alarm_id"]),
                    reader["alarm_bib"].ToString(),
                    reader["alarm_chip"].ToString(),
                    Convert.ToInt32(reader["alarm_enabled"]) == 1,
                    Convert.ToInt32(reader["alarm_sound"])
                    ));
            }
            reader.Close();
            return output;
        }

        internal static void DeleteAlarms(int eventId, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM alarms WHERE event_id=@eventId;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventId", eventId)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void DeleteAlarm(Alarm alarm, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM alarms WHERE alarm_id=@alarmId;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@alarmId", alarm.Identifier)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }
    }
}
