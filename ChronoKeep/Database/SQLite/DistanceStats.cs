﻿using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class DistanceStats
    {
        internal static List<DistanceStat> GetDistanceStats(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT d.distance_id AS id, d.distance_name AS name, e.eventspecific_status AS status, COUNT(e.eventspecific_status) AS count " +
                "FROM distances d JOIN eventspecific e ON d.distance_id=e.distance_id " +
                "WHERE e.event_id=@event " +
                "GROUP BY d.distance_name, e.eventspecific_status;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            DistanceStat allstats = new DistanceStat
            {
                DistanceName = "All",
                DistanceID = -1,
                Active = 0,
                DNF = 0,
                DNS = 0,
                Finished = 0
            };
            Dictionary<int, DistanceStat> statsDictionary = new Dictionary<int, DistanceStat>();
            while (reader.Read())
            {
                int distanceId = Convert.ToInt32(reader["id"].ToString());
                if (!statsDictionary.ContainsKey(distanceId))
                {
                    statsDictionary[distanceId] = new DistanceStat()
                    {
                        DistanceName = reader["name"].ToString(),
                        DistanceID = distanceId
                    };
                }
                if (int.TryParse(reader["status"].ToString(), out int status))
                {
                    if (Constants.Timing.EVENTSPECIFIC_DNS == status || Constants.Timing.EVENTSPECIFIC_UNKNOWN == status)
                    {
                        statsDictionary[distanceId].DNS = Convert.ToInt32(reader["count"]);
                        allstats.DNS += statsDictionary[distanceId].DNS;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_FINISHED == status)
                    {
                        statsDictionary[distanceId].Finished = Convert.ToInt32(reader["count"]);
                        allstats.Finished += statsDictionary[distanceId].Finished;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_STARTED == status)
                    {
                        statsDictionary[distanceId].Active = Convert.ToInt32(reader["count"]);
                        allstats.Active += statsDictionary[distanceId].Active;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_DNF == status)
                    {
                        statsDictionary[distanceId].DNF = Convert.ToInt32(reader["count"]);
                        allstats.DNF += statsDictionary[distanceId].DNF;
                    }
                }
            }
            reader.Close();
            List<DistanceStat> output =
            [
                .. statsDictionary.Values
            ];
            output.Sort((x1, x2) => x1.Active != x2.Active ? x2.Active.CompareTo(x1.Active) : x1.DistanceName.CompareTo(x2.DistanceName));
            if (output.Count > 1)
            {
                output.Insert(0, allstats);
            }
            return output;
        }
    }
}
