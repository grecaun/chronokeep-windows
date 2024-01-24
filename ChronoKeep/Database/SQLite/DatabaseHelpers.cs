﻿using System.Data.SQLite;

namespace Chronokeep.Database.SQLite
{
    class DatabaseHelpers
    {
        internal static void HardResetDatabase(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DROP TABLE remote_readers; DROP TABLE alarms; DROP TABLE timing_systems; DROP TABLE age_groups;" +
                    "DROP TABLE settings; DROP TABLE chipreads;" +
                    "DROP TABLE time_results; DROP TABLE segments; DROP TABLE eventspecific;" +
                    "DROP TABLE participants; DROP TABLE timing_locations; DROP TABLE distances;" +
                    "DROP TABLE events; DROP TABLE bib_chip_assoc; DROP TABLE results_api;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void ResetDatabase(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM remote_readers; DELETE FROM alarms; DELETE FROM timing_systems; DELETE FROM age_groups;" +
                    "DELETE FROM settings; DELETE FROM chipreads;" +
                    "DELETE FROM time_results; DELETE FROM segments; DELETE FROM eventspecific;" +
                    "DELETE FROM participants; DELETE FROM timing_locations; DELETE FROM distances;" +
                    "DELETE FROM events; DELETE FROM bib_chip_assoc; DELETE FROM results_api;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }
    }
}
