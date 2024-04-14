using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    internal class SMSAlerts
    {
        public static List<string> GetSMSAlerts(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM sms_alert WHERE event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> output = new List<string>();
            while (reader.Read())
            {
                output.Add(reader["sms_bib"].ToString());
            }
            reader.Close();
            return output;
        }

        public static void AddSMSAlert(int eventId, string bib, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO sms_alert (event_id, sms_bib) VALUES (@event, @bib);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@bib", bib)
            });
            command.ExecuteNonQuery();
        }
    }
}
