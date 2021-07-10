using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class DatabaseHelpers
    {
        internal static void HardResetDatabase(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DROP TABLE timing_systems; DROP TABLE age_groups; DROP TABLE available_bibs;" +
                    "DROP TABLE bib_group; DROP TABLE settings; DROP TABLE changes; DROP TABLE chipreads;" +
                    "DROP TABLE time_results; DROP TABLE segments; DROP TABLE eventspecific_apparel; DROP TABLE eventspecific;" +
                    "DROP TABLE participants; DROP TABLE timing_locations; DROP TABLE divisions; DROP TABLE kiosk; DROP TABLE dayof_participant;" +
                    "DROP TABLE events; DROP TABLE bib_chip_assoc;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        internal static void ResetDatabase(SQLiteConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems; DELETE FROM age_groups; DELETE FROM available_bibs;" +
                    "DELETE FROM bib_group; DELETE FROM settings; DELETE FROM changes; DELETE FROM chipreads;" +
                    "DELETE FROM time_results; DELETE FROM segments; DELETE FROM eventspecific_apparel; DELETE FROM eventspecific;" +
                    "DELETE FROM participants; DELETE FROM timing_locations; DELETE FROM divisions; DELETE FROM kiosk; DELETE FROM dayof_participant;" +
                    "DELETE FROM events; DELETE FROM bib_chip_assoc;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }
    }
}
