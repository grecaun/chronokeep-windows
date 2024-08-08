﻿using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    internal class EmailAlerts
    {
        public static List<int> GetEmailAlerts(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM email_alert WHERE event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<int> output = new List<int>();
            int id;
            while (reader.Read())
            {
                if (int.TryParse(reader["eventspecific_id"].ToString(), out id))
                {
                    output.Add(id);
                }
            }
            reader.Close();
            return output;
        }

        public static void AddEmailAlert(int eventId, int eventspecific_id, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO email_alert (event_id, eventspecific_id) VALUES (@event, @eventspec);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@eventspec", eventspecific_id)
            });
            command.ExecuteNonQuery();
        }

        public static void RemoveEmailAlert(int eventId, int eventspecific_id, SQLiteConnection connection)
        {
            // TODO
        }
    }
}
