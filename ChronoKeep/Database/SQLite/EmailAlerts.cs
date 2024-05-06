using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    internal class EmailAlerts
    {
        public static List<string> GetEmailAlerts(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM email_alert WHERE event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> output = new List<string>();
            while (reader.Read())
            {
                output.Add(reader["email_bib"].ToString());
            }
            reader.Close();
            return output;
        }

        public static void AddEmailAlert(int eventId, string bib, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO email_alert (event_id, email_bib) VALUES (@event, @bib);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@bib", bib)
            });
            command.ExecuteNonQuery();
        }
    }
}
