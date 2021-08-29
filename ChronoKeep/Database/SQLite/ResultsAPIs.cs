using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class ResultsAPIs
    {
        internal static int AddResultsAPI(ResultsAPI anAPI, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO results_api (api_type, api_url, api_auth_token, api_nickname)" +
                " VALUES (@type, @url, @token, @nickname);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@type", anAPI.Type),
                new SQLiteParameter("@url", anAPI.URL),
                new SQLiteParameter("@token", anAPI.AuthToken),
                new SQLiteParameter("@nickname", anAPI.Nickname)
            });
            command.ExecuteNonQuery();
            long outVal = connection.LastInsertRowId;
            return (int)outVal;
        }

        internal static void UpdateResultsAPI(ResultsAPI anAPI, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE results_api SET api_type=@type, api_url=@url, api_auth_token=@token, api_nickname=@nickname WHERE api_id=@id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@type", anAPI.Type),
                new SQLiteParameter("@url", anAPI.URL),
                new SQLiteParameter("@token", anAPI.AuthToken),
                new SQLiteParameter("@nickname", anAPI.Nickname),
                new SQLiteParameter("@id", anAPI.Identifier)
            });
            command.ExecuteNonQuery();
        }

        internal static void RemoveResultsAPI(int identifier, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE events SET api_id=-1, api_event_id='' WHERE api_id=@id; DELETE FROM results_api WHERE api_id=@id;";
                command.Parameters.Add(new SQLiteParameter("@id", identifier));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static ResultsAPI GetResultsAPI(int identifier, SQLiteConnection connection)
        {
            if (identifier < 0)
            {
                return null;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM results_api WHERE api_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id", identifier));
            SQLiteDataReader reader = command.ExecuteReader();
            ResultsAPI output = null;
            if (reader.Read())
            {
                output = new ResultsAPI(
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_type"].ToString(),
                    reader["api_url"].ToString(),
                    reader["api_nickname"].ToString(),
                    reader["api_auth_token"].ToString()
                    );
            }
            reader.Close();
            return output;
        }

        internal static List<ResultsAPI> GetAllResultsAPI(SQLiteConnection connection)
        {
            List<ResultsAPI> output = new List<ResultsAPI>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM results_api;";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new ResultsAPI(
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_type"].ToString(),
                    reader["api_url"].ToString(),
                    reader["api_nickname"].ToString(),
                    reader["api_auth_token"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }
    }
}
