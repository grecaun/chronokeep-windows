using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class APIs
    {
        internal static int AddAPI(APIObject anAPI, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO results_api (api_type, api_url, api_auth_token, api_nickname, api_web_url)" +
                " VALUES (@type, @url, @token, @nickname, @weburl);";
            command.Parameters.AddRange(
            [
                new("@type", anAPI.Type),
                new("@url", anAPI.URL),
                new("@token", anAPI.AuthToken),
                new("@nickname", anAPI.Nickname),
                new("@weburl", anAPI.WebURL)
            ]);
            command.ExecuteNonQuery();
            long outVal = connection.LastInsertRowId;
            return (int)outVal;
        }

        internal static void UpdateAPI(APIObject anAPI, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE results_api SET api_type=@type, api_url=@url, api_auth_token=@token, api_nickname=@nickname, api_web_url=@weburl WHERE api_id=@id;";
            command.Parameters.AddRange(
            [
                new("@type", anAPI.Type),
                new("@url", anAPI.URL),
                new("@token", anAPI.AuthToken),
                new("@nickname", anAPI.Nickname),
                new("@id", anAPI.Identifier),
                new("@weburl", anAPI.WebURL)
            ]);
            command.ExecuteNonQuery();
        }

        internal static void RemoveAPI(int identifier, SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE events SET api_id=-1, api_event_id='' WHERE api_id=@id; DELETE FROM results_api WHERE api_id=@id;";
                command.Parameters.Add(("@id", identifier));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static APIObject GetAPI(int identifier, SQLiteConnection connection)
        {
            if (identifier < 0)
            {
                return null;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM results_api WHERE api_id=@id";
            command.Parameters.Add(new("@id", identifier));
            SQLiteDataReader reader = command.ExecuteReader();
            APIObject output = null;
            if (reader.Read())
            {
                output = new APIObject(
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_type"].ToString(),
                    reader["api_url"].ToString(),
                    reader["api_nickname"].ToString(),
                    reader["api_auth_token"].ToString(),
                    reader["api_web_url"].ToString()
                    );
            }
            reader.Close();
            return output;
        }

        internal static List<APIObject> GetAllAPI(SQLiteConnection connection)
        {
            List<APIObject> output = [];
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM results_api;";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new APIObject(
                    Convert.ToInt32(reader["api_id"]),
                    reader["api_type"].ToString(),
                    reader["api_url"].ToString(),
                    reader["api_nickname"].ToString(),
                    reader["api_auth_token"].ToString(),
                    reader["api_web_url"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }
    }
}
