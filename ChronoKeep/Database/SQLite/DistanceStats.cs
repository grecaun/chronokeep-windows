using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class DistanceStats
    {
        internal static List<DistanceStat> GetDistanceStats(int eventId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT d.division_id AS id, d.division_name AS name, e.eventspecific_status AS status, COUNT(e.eventspecific_status) AS count " +
                "FROM divisions d JOIN eventspecific e ON d.division_id=e.division_id " +
                "WHERE e.event_id=@event " +
                "GROUP BY d.division_name, e.eventspecific_status;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            DistanceStat allstats = new DistanceStat
            {
                DivisionName = "All",
                DivisionID = -1,
                Active = 0,
                DNF = 0,
                DNS = 0,
                Finished = 0
            };
            Dictionary<int, DistanceStat> statsDictionary = new Dictionary<int, DistanceStat>();
            while (reader.Read())
            {
                int divId = Convert.ToInt32(reader["id"].ToString());
                if (!statsDictionary.ContainsKey(divId))
                {
                    statsDictionary[divId] = new DistanceStat()
                    {
                        DivisionName = reader["name"].ToString(),
                        DivisionID = divId
                    };
                }
                if (int.TryParse(reader["status"].ToString(), out int status))
                {
                    if (Constants.Timing.EVENTSPECIFIC_NOSHOW == status)
                    {
                        statsDictionary[divId].DNS = Convert.ToInt32(reader["count"]);
                        allstats.DNS += statsDictionary[divId].DNS;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_FINISHED == status)
                    {
                        statsDictionary[divId].Finished = Convert.ToInt32(reader["count"]);
                        allstats.Finished += statsDictionary[divId].Finished;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_STARTED == status)
                    {
                        statsDictionary[divId].Active = Convert.ToInt32(reader["count"]);
                        allstats.Active += statsDictionary[divId].Active;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_NOFINISH == status)
                    {
                        statsDictionary[divId].DNF = Convert.ToInt32(reader["count"]);
                        allstats.DNF += statsDictionary[divId].DNF;
                    }
                }
            }
            reader.Close();
            List<DistanceStat> output = new List<DistanceStat>
            {
                allstats
            };
            foreach (DistanceStat stats in statsDictionary.Values)
            {
                output.Add(stats);
            }
            return output;
        }
    }
}
