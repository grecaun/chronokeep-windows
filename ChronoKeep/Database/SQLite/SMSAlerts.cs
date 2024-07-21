using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    internal class SMSAlerts
    {
        public static List<(int, int)> GetSMSAlerts(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM sms_alert WHERE event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<(int, int)> output = new List<(int, int)>();
            int id;
            int seg;
            while (reader.Read())
            {
                if (int.TryParse(reader["eventspecific_id"].ToString(), out id) && int.TryParse(reader["segment_id"].ToString(), out seg))
                {
                    output.Add((id, seg));
                }
            }
            reader.Close();
            return output;
        }

        public static void AddSMSAlert(int eventId, int eventspecific_id, int segment_id, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO sms_alert (event_id, eventspecific_id, segment_id) VALUES (@event, @eventspec, @segment);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@eventspec", eventspecific_id),
                new SQLiteParameter("@segment", segment_id)
            });
            command.ExecuteNonQuery();
        }
    }
}
