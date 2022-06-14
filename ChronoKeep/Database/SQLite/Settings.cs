using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    class Settings
    {
        internal static AppSetting GetAppSetting(string name, SQLiteConnection connection)
        {
            AppSetting output = null;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM settings WHERE setting=@name";
            command.Parameters.Add(new SQLiteParameter("@name", name));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output = new AppSetting
                {
                    name = Convert.ToString(reader["setting"]),
                    value = Convert.ToString(reader["value"])
                };
            }
            reader.Close();
            return output;
        }

        internal static void SetAppSetting(AppSetting setting, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO settings (setting, value)" +
                    " VALUES (@name,@value)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", setting.name),
                new SQLiteParameter("@value", setting.value) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }
    }
}
