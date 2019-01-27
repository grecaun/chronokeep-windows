using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EventDirector.Objects;

namespace EventDirector
{
    class SQLiteInterface : IDBInterface
    {
        private readonly int version = 25;
        SQLiteConnection connection;
        readonly string connectionInfo;

        public SQLiteInterface(String info)
        {
            connectionInfo = info;
            connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
        }

        public void Initialize()
        {
            ArrayList queries = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='settings'", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            int oldVersion = -1;
            if (reader.Read())
            {
                Log.D("Tables do not need to be made.");
                command = new SQLiteCommand("SELECT version FROM settings;", connection);
                using (SQLiteDataReader versionChecker = command.ExecuteReader())
                {
                    if (versionChecker.Read())
                    {
                        oldVersion = Convert.ToInt32(versionChecker["version"]);
                    }
                    else
                    {
                        Log.D("Something went wrong when checking the version...");
                    }
                }
            }
            else
            {
                Log.D("Tables haven't been created. Doing so now.");
                command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection); // Ensure Foreign key constraints work.
                command.ExecuteNonQuery();
                queries.Add("CREATE TABLE IF NOT EXISTS bib_chip_assoc (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "bib INTEGER NOT NULL," +
                    "chip INTEGER NOT NULL," +
                    "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS events (" +
                    "event_id INTEGER PRIMARY KEY," +
                    "event_name VARCHAR(100) NOT NULL," +
                    "event_date VARCHAR(15) NOT NULL," +
                    "event_yearcode VARCHAR(10) NOT NULL DEFAULT ''," +
                    "event_registration_open INTEGER DEFAULT 0," +
                    "event_results_open INTEGER DEFAULT 0," +
                    "event_announce_available INTEGER DEFAULT 0," +
                    "event_allow_early_start INTEGER DEFAULT 0," +
                    "event_early_start_difference INTEGER NOT NULL DEFAULT 0," +
                    "event_kiosk INTEGER DEFAULT 0," +
                    "event_next_year_event_id INTEGER DEFAULT -1," +
                    "event_shirt_optional INTEGER DEFAULT 1," +
                    "event_shirt_price INTEGER DEFAULT 0," +
                    "event_rank_by_gun INTEGER DEFAULT 1," +
                    "event_common_age_groups INTEGER DEFAULT 1," +
                    "event_common_start_finish INTEGER DEFAULT 1," +
                    "event_division_specific_segments INTEGER DEFAULT 0," +
                    "event_start_time_seconds INTEGER NOT NULL DEFAULT -1," +
                    "event_start_time_milliseconds INTEGER NOT NULL DEFAULT 0," +
                    "event_finish_max_occurances INTEGER NOT NULL DEFAULT 1," +
                    "event_finish_ignore_within INTEGER NOT NULL DEFAULT 0," +
                    "event_start_window INTEGER NOT NULL DEFAULT -1," +
                    "event_timing_system VARCHAR NOT NULL DEFAULT 'RFID'," +
                    "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS dayof_participant (" +
                    "dop_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                    "dop_first VARCHAR NOT NULL," +
                    "dop_last VARCHAR NOT NULL," +
                    "dop_street VARCHAR," +
                    "dop_city VARCHAR," +
                    "dop_state VARCHAR," +
                    "dop_zip VARCHAR," +
                    "dop_birthday VARCHAR NOT NULL," +
                    "dop_email VARCHAR," +
                    "dop_mobile VARCHAR," +
                    "dop_parent VARCHAR," +
                    "dop_country VARCHAR," +
                    "dop_street2 VARCHAR," +
                    "dop_gender VARCHAR," +
                    "dop_comments VARCHAR," +
                    "dop_other VARCHAR," +
                    "dop_other2 VARCHAR," +
                    "dop_emergency_name VARCHAR NOT NULL," +
                    "dop_emergency_phone VARCHAR NOT NULL," +
                    "UNIQUE (event_id, dop_first, dop_last, dop_street, dop_zip, dop_birthday) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS kiosk (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "kiosk_waiver_text VARCHAR NOT NULL," +
                    "kiosk_print_new INTEGER DEFAULT 0," +
                    "UNIQUE (event_id) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS divisions (" +
                    "division_id INTEGER PRIMARY KEY," +
                    "division_name VARCHAR(100) NOT NULL," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "division_cost INTEGER DEFAULT 7000," +
                    "division_distance DECIMAL(10,2) DEFAULT 0.0," +
                    "division_distance_unit INTEGER DEFAULT 0," +
                    "division_start_location INTEGER DEFAULT -2," +
                    "division_start_within INTEGER DEFAULT -1," +
                    "division_finish_location INTEGER DEFAULT -1," +
                    "division_finish_occurance INTEGER DEFAULT 1," +
                    "division_wave INTEGER NOT NULL DEFAULT 1," +
                    "bib_group_number INTEGER NOT NULL DEFAULT -1," +
                    "division_start_offset_seconds INTEGER NOT NULL DEFAULT 0," +
                    "division_start_offset_milliseconds INTEGER NOT NULL DEFAULT 0," +
                    "UNIQUE (division_name, event_id) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS timing_locations (" +
                    "location_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "location_name VARCHAR(100) NOT NULL," +
                    "location_max_occurances INTEGER NOT NULL DEFAULT 1," +
                    "location_ignore_within INTEGER NOT NULL DEFAULT -1," +
                    "UNIQUE (event_id, location_name) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS participants (" +
                    "participant_id INTEGER PRIMARY KEY," +
                    "participant_first VARCHAR(50) NOT NULL," +
                    "participant_last VARCHAR(75) NOT NULL," +
                    "participant_street VARCHAR(150)," +
                    "participant_city VARCHAR(75)," +
                    "participant_state VARCHAR(25)," +
                    "participant_zip VARCHAR(10)," +
                    "participant_birthday VARCHAR(15) NOT NULL," +
                    "participant_email VARCHAR(150)," +
                    "participant_mobile VARCHAR(20)," +
                    "participant_parent VARCHAR(150)," +
                    "participant_country VARCHAR(50)," +
                    "participant_street2 VARCHAR(50)," +
                    "participant_gender VARCHAR(10)," +
                    "emergencycontact_name VARCHAR(150) NOT NULL DEFAULT '911'," +
                    "emergencycontact_phone VARCHAR(20)," +
                    "UNIQUE (participant_first, participant_last, participant_street, participant_zip, participant_birthday) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (" +
                    "eventspecific_id INTEGER PRIMARY KEY," +
                    "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                    "eventspecific_bib INTEGER," +
                    "eventspecific_checkedin INTEGER DEFAULT 0," +
                    "eventspecific_comments VARCHAR," +
                    "eventspecific_owes VARCHAR(50)," +
                    "eventspecific_other VARCHAR," +
                    "eventspecific_earlystart INTEGER DEFAULT 0," +
                    "eventspecific_next_year INTEGER DEFAULT 0," +
                    "eventspecific_registration_date VARCHAR NOT NULL DEFAULT ''," +
                    "UNIQUE (participant_id, event_id, division_id) ON CONFLICT REPLACE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS eventspecific_apparel (" +
                    "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                    "name VARCHAR NOT NULL," +
                    "value VARCHAR NOT NULL," +
                    "UNIQUE (eventspecific_id, name) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS segments (" +
                    "segment_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "division_id INTEGER DEFAULT -1," +
                    "location_id INTEGER DEFAULT -1," +
                    "location_occurance INTEGER DEFAULT 1," +
                    "name VARCHAR DEFAULT ''," +
                    "distance_segment DECIMAL (10,2) DEFAULT 0.0," +
                    "distance_cumulative DECIMAL (10,2) DEFAULT 0.0," +
                    "distance_unit INTEGER DEFAULT 0," +
                    "UNIQUE (event_id, division_id, location_id, location_occurance) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS time_results (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                    "location_id INTEGER NOT NULL," +
                    "timeresult_time INTEGER NOT NULL," +
                    "segment_id INTEGER NOT NULL DEFAULT -3," +
                    "timeresult_occurance INTEGER NOT NULL," +
                    "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance) ON CONFLICT IGNORE" +
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS chipreads (" +
                    "read_id INTEGER NOT NULL PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "read_status INTEGER NOT NULL DEFAULT 0," +
                    "location_id INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                    "read_chipnumber INTEGER NOT NULL," +
                    "read_seconds INTEGER NOT NULL," +
                    "read_milliseconds INTEGER NOT NULL," +
                    "read_antenna INTEGER NOT NULL," +
                    "read_reader INTEGER NOT NULL," +
                    "read_box INTEGER NOT NULL," +
                    "read_logindex INTEGER NOT NULL," +
                    "read_rssi INTEGER NOT NULL," +
                    "read_isrewind INTEGER NOT NULL," +
                    "read_readertime TEXT NOT NULL," +
                    "read_starttime INTEGER NOT NULL," +
                    "UNIQUE (read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS settings (version INTEGER NOT NULL, name VARCHAR NOT NULL," +
                    " identifier VARCHAR NOT NULL); INSERT INTO settings (version, name, identifier) VALUES " +
                    "(" + version + ", 'Northwest Endurance Events', '" + Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "") + "')");
                queries.Add("CREATE TABLE IF NOT EXISTS changes (" +
                    "change_id INTEGER PRIMARY KEY, " +
                    "old_participant_id INTEGER NOT NULL," +
                    "old_first VARCHAR(50) NOT NULL," +
                    "old_last VARCHAR(75) NOT NULL," +
                    "old_street VARCHAR(150)," +
                    "old_city VARCHAR(75)," +
                    "old_state VARCHAR(25)," +
                    "old_zip VARCHAR(10)," +
                    "old_birthday VARCHAR(15) NOT NULL," +
                    "old_email VARCHAR(150)," +
                    "old_emergency_id INTEGER DEFAULT -1," +
                    "old_emergency_name VARCHAR(150)," +
                    "old_emergency_email VARCHAR(150)," +
                    "old_event_spec_id INTEGER DEFAULT -1," +
                    "old_event_spec_event_id INTEGER DEFAULT -1," +
                    "old_event_spec_division_id INTEGER DEFAULT -1," +
                    "old_event_spec_bib INTEGER," +
                    "old_event_spec_checkedin INTEGER DEFAULT -1," +
                    "old_event_spec_comments VARCHAR," +
                    "old_mobile VARCHAR(20)," +
                    "old_parent VARCHAR(150)," +
                    "old_country VARCHAR(50)," +
                    "old_street2 VARCHAR(50)," +
                    "old_owes VARCHAR(50)," +
                    "old_other VARCHAR," +
                    "old_gender VARCHAR(10)," +
                    "old_earlystart INTEGER DEFAULT -1," +
                    "old_next_year INTEGER DEFAULT 0," +

                    "new_participant_id INTEGER NOT NULL," +
                    "new_first VARCHAR(50) NOT NULL," +
                    "new_last VARCHAR(75) NOT NULL," +
                    "new_street VARCHAR(150)," +
                    "new_city VARCHAR(75)," +
                    "new_state VARCHAR(25)," +
                    "new_zip VARCHAR(10)," +
                    "new_birthday VARCHAR(15) NOT NULL," +
                    "new_email VARCHAR(150)," +
                    "new_emergency_id INTEGER DEFAULT -1," +
                    "new_emergency_name VARCHAR(150)," +
                    "new_emergency_phone VARCHAR(20)," +
                    "new_emergency_email VARCHAR(150)," +
                    "new_event_spec_id INTEGER DEFAULT -1," +
                    "new_event_spec_event_id INTEGER DEFAULT -1," +
                    "new_event_spec_division_id INTEGER DEFAULT -1," +
                    "new_event_spec_bib INTEGER DEFAULT -1," +
                    "new_event_spec_checkedin INTEGER DEFAULT -1," +
                    "new_event_spec_comments VARCHAR," +
                    "new_mobile VARCHAR(20)," +
                    "new_parent VARCHAR(150)," +
                    "new_country VARCHAR(50)," +
                    "new_street2 VARCHAR(50)," +
                    "new_owes VARCHAR(50)," +
                    "new_other VARCHAR," +
                    "new_gender VARCHAR(10)," +
                    "new_earlystart INTEGER DEFAULT -1," +
                    "new_next_year INTEGER DEFAULT 0" +
                    ")");
                queries.Add("INSERT INTO participants (participant_id, participant_first, participant_last," +
                    " participant_birthday) VALUES (0, 'J', 'Doe', '01/01/1901')");
                queries.Add("CREATE TABLE IF NOT EXISTS app_settings (" +
                    "setting VARCHAR NOT NULL," +
                    "value VARCHAR NOT NULL," +
                    "UNIQUE (setting) ON CONFLICT REPLACE" +
                    ")");
                queries.Add("CREATE TABLE IF NOT EXISTS bib_group (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "bib_group_number INTEGER NOT NULL," +
                    "bib_group_name VARCHAR NOT NULL," +
                    "UNIQUE (event_id, bib_group_number) ON CONFLICT REPLACE," +
                    "UNIQUE (event_id, bib_group_name) ON CONFLICT REPLACE);");
                queries.Add("CREATE TABLE IF NOT EXISTS available_bibs (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "bib_group_number INTEGER NOT NULL DEFAULT -1," +
                    "bib INTEGER NOT NULL," +
                    "UNIQUE (event_id, bib) ON CONFLICT REPLACE);");
                queries.Add("CREATE TABLE IF NOT EXISTS age_groups (" +
                    "group_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "division_id INTEGER NOT NULL DEFAULT -1," +
                    "start_age INTEGER NOT NULL," +
                    "end_age INTEGER NOT NULL);");

                using (var transaction = connection.BeginTransaction())
                {
                    int counter = 1;
                    foreach (String q in queries)
                    {
                        Log.D("Table query number " + counter++ + " Query string is: " + q);
                        command = new SQLiteCommand(q, connection);
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }

                command = new SQLiteCommand("SELECT version FROM settings;", connection);
                using (SQLiteDataReader versionChecker = command.ExecuteReader())
                {
                    if (versionChecker.Read())
                    {
                        oldVersion = Convert.ToInt32(versionChecker["version"]);
                    }
                    else
                    {
                        Log.D("Something went wrong when checking the version...");
                    }
                }
            }
            reader.Close();
            if (oldVersion == -1) Log.D("Unable to get a version number. Something is terribly wrong.");
            else if (oldVersion < version) UpdateDatabase(oldVersion, version);
        }

        private void UpdateDatabase(int oldversion, int newversion)
        {
            Log.D("Database is version " + oldversion + " but it needs to be upgraded to version " + newversion);
            SQLiteCommand command = connection.CreateCommand();
            using (var transaction = connection.BeginTransaction())
            {
                switch (oldversion)
                {
                    case 1:
                        Log.D("Updating from version 1.");
                        command.CommandText = "ALTER TABLE divisions ADD division_cost INTEGER DEFAULT 7000; ALTER TABLE eventspecific ADD eventspecific_fleece VARCHAR DEFAULT '';" +
                                "ALTER TABLE changes ADD old_fleece VARCHAR DEFAULT ''; ALTER TABLE changes ADD new_fleece VARCHAR DEFAULT '';UPDATE settings SET version=2 WHERE version=1;";
                        command.ExecuteNonQuery();
                        goto case 2;
                    case 2:
                        Log.D("Updating from version 2.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE settings ADD name VARCHAR DEFAULT 'Northwest Endurance Events'; ALTER TABLE events ADD event_kiosk INTEGER DEFAULT 0; CREATE TABLE IF NOT EXISTS kiosk (event_id INTEGER NOT NULL, kiosk_waiver_text VARCHAR NOT NULL, UNIQUE (event_id) ON CONFLICT IGNORE);UPDATE settings SET version=3 WHERE version=2;";
                        command.ExecuteNonQuery();
                        goto case 3;
                    case 3:
                        Log.D("Updating from version 3");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS dayof_participant (" +
                            "dop_id INTEGER PRIMARY KEY," +
                            "dop_event_id INTEGER NOT NULL," +
                            "dop_first VARCHAR NOT NULL," +
                            "dop_last VARCHAR NOT NULL," +
                            "dop_street VARCHAR," +
                            "dop_city VARCHAR," +
                            "dop_state VARCHAR," +
                            "dop_zip VARCHAR," +
                            "dop_birthday VARCHAR NOT NULL," +
                            "dop_phone VARCHAR NOT NULL," +
                            "dop_email VARCHAR," +
                            "dop_mobile VARCHAR," +
                            "dop_parent VARCHAR," +
                            "dop_country VARCHAR," +
                            "dop_street2 VARCHAR," +
                            "dop_gender VARCHAR," +
                            "dop_comments VARCHAR," +
                            "dop_other VARCHAR," +
                            "dop_other2 VARCHAR," +
                            "dop_emergency_name VARCHAR NOT NULL," +
                            "dop_emergency_phone VARCHAR NOT NULL," +
                            "UNIQUE (dop_first, dop_last, dop_street, dop_city, dop_state, dop_zip, dop_birthday)" +
                            ");UPDATE settings SET version=4 WHERE version=3;";
                        command.ExecuteNonQuery();
                        goto case 4;
                    case 4:
                        Log.D("Updating from version 4.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE dayof_participant ADD dop_division_id INTEGER NOT NULL DEFAULT -1;UPDATE settings SET version=5 WHERE version=4;";
                        command.ExecuteNonQuery();
                        goto case 5;
                    case 5:
                        Log.D("Updating from version 5.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE kiosk ADD kiosk_print_new INTEGER DEFAULT 0; UPDATE settings SET version=6 WHERE version=5;";
                        command.ExecuteNonQuery();
                        goto case 6;
                    case 6:
                        Log.D("Updating from version 6.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_next_year_event_id INTEGER DEFAULT -1; ALTER TABLE events ADD event_shirt_optional INTEGER DEFAULT 1; ALTER TABLE eventspecific ADD eventspecific_next_year INTEGER DEFAULT 0; ALTER TABLE changes ADD old_next_year INTEGER DEFAULT 0; ALTER TABLE changes ADD new_next_year INTEGER DEFAULT 0; UPDATE settings SET version=7 WHERE version=6;";
                        command.ExecuteNonQuery();
                        goto case 7;
                    case 7:
                        Log.D("Updating from version 7.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_shirt_price INTEGER DEFAULT 0; UPDATE settings SET version=8 WHERE version=7;";
                        command.ExecuteNonQuery();
                        goto case 8;
                    case 8:
                        Log.D("Updating from version 8.");
                        command = connection.CreateCommand();
                        command.CommandText = "UPDATE settings SET version=9 WHERE version=8;";
                        command.ExecuteNonQuery();
                        goto case 9;
                    case 9:
                        Log.D("Updating from version 9.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE eventspecific RENAME TO old_eventspecific;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific(" +
                            "eventspecific_id INTEGER PRIMARY KEY," +
                            "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                            "eventspecific_bib INTEGER," +
                            "eventspecific_checkedin INTEGER DEFAULT 0," +
                            "eventspecific_shirtsize VARCHAR," +
                            "eventspecific_comments VARCHAR," +
                            "eventspecific_secondshirt VARCHAR," +
                            "eventspecific_owes VARCHAR(50)," +
                            "eventspecific_hat VARCHAR(20)," +
                            "eventspecific_other VARCHAR," +
                            "eventspecific_earlystart INTEGER DEFAULT 0," +
                            "eventspecific_fleece VARCHAR DEFAULT ''," +
                            "eventspecific_next_year INTEGER DEFAULT 0," +
                            "UNIQUE (participant_id, event_id, division_id) ON CONFLICT IGNORE" +
                            "); CREATE TABLE IF NOT EXISTS bib_chip_assoc (" +
                            "event_id INTEGER PRIMARY KEY," +
                            "bib INTEGER NOT NULL," +
                            "chip INTEGER NOT NULL" +
                            ");" +
                            "INSERT INTO eventspecific SELECT * FROM old_eventspecific; UPDATE settings SET version=10 WHERE version=9;";
                        command.ExecuteNonQuery();
                        goto case 10;
                    case 10:
                        Log.D("Updating from version 10.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE bib_chip_assoc RENAME TO old_bib_chip_assoc;" +
                            "CREATE TABLE IF NOT EXISTS bib_chip_assoc (" +
                            "event_id INTEGER PRIMARY KEY," +
                            "bib INTEGER NOT NULL," +
                            "chip INTEGER NOT NULL," +
                            "UNIQUE (event_id, bib) ON CONFLICT REPLACE," +
                            "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                            "); INSERT INTO bib_chip_assoc SELECT * FROM old_bib_chip_assoc; UPDATE settings SET version=11 WHERE version=10;";
                        command.ExecuteNonQuery();
                        goto case 11;
                    case 11:
                        Log.D("Updating from version 11.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE bib_chip_assoc RENAME TO old_old_bib_chip_assoc;" +
                            "CREATE TABLE IF NOT EXISTS bib_chip_assoc (" +
                            "event_id INTEGER," +
                            "bib INTEGER NOT NULL," +
                            "chip INTEGER NOT NULL," +
                            "UNIQUE (event_id, bib) ON CONFLICT REPLACE," +
                            "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                            "); INSERT INTO bib_chip_assoc SELECT * FROM old_bib_chip_assoc; UPDATE settings SET version=12 WHERE version=11;";
                        command.ExecuteNonQuery();
                        goto case 12;
                    case 12:
                        Log.D("Updating from version 12.");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS chipreads (" +
                            "read_id INTEGER NOT NULL PRIMARY KEY," +
                            "read_status INTEGER NOT NULL DEFAULT 0," +
                            "timingpoint_id INTEGER NOT NULL REFERENCES timingpoints(timingpoint_id)," +
                            "read_chipnumber INTEGER NOT NULL," +
                            "read_seconds INTEGER NOT NULL," +
                            "read_milliseconds INTEGER NOT NULL," +
                            "read_antenna INTEGER NOT NULL," +
                            "read_reader INTEGER NOT NULL," +
                            "read_box INTEGER NOT NULL," +
                            "read_logindex INTEGER NOT NULL," +
                            "read_rssi INTEGER NOT NULL," +
                            "read_isrewind INTEGER NOT NULL," +
                            "read_readertime TEXT NOT NULL," +
                            "read_starttime INTEGER NOT NULL," +
                            "UNIQUE (read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE);" +
                            "ALTER TABLE participants RENAME TO old_participants;" +
                            "CREATE TABLE IF NOT EXISTS participants (" +
                                "participant_id INTEGER PRIMARY KEY," +
                                "participant_first VARCHAR(50) NOT NULL," +
                                "participant_last VARCHAR(75) NOT NULL," +
                                "participant_street VARCHAR(150)," +
                                "participant_city VARCHAR(75)," +
                                "participant_state VARCHAR(25)," +
                                "participant_zip VARCHAR(10)," +
                                "participant_birthday VARCHAR(15) NOT NULL," +
                                "participant_phone VARCHAR(20)," +
                                "participant_email VARCHAR(150)," +
                                "participant_mobile VARCHAR(20)," +
                                "participant_parent VARCHAR(150)," +
                                "participant_country VARCHAR(50)," +
                                "participant_street2 VARCHAR(50)," +
                                "participant_gender VARCHAR(10)," +
                                "emergencycontact_name VARCHAR(150) NOT NULL DEFAULT '911'," +
                                "emergencycontact_phone VARCHAR(20)," +
                                "UNIQUE (participant_first, participant_last, participant_street, participant_zip, participant_birthday) ON CONFLICT IGNORE);" +
                            "ALTER TABLE eventspecific RENAME TO older_eventspecific;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific (" +
                                "eventspecific_id INTEGER PRIMARY KEY," +
                                "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                                "eventspecific_bib INTEGER," +
                                "eventspecific_checkedin INTEGER DEFAULT 0," +
                                "eventspecific_shirtsize VARCHAR," +
                                "eventspecific_comments VARCHAR," +
                                "eventspecific_secondshirt VARCHAR," +
                                "eventspecific_owes VARCHAR(50)," +
                                "eventspecific_hat VARCHAR(20)," +
                                "eventspecific_other VARCHAR," +
                                "eventspecific_earlystart INTEGER DEFAULT 0," +
                                "eventspecific_fleece VARCHAR DEFAULT ''," +
                                "eventspecific_next_year INTEGER DEFAULT 0," +
                                "UNIQUE (participant_id, event_id, division_id) ON CONFLICT IGNORE);" +
                            "ALTER TABLE settings ADD COLUMN identifier VARCHAR NOT NULL DEFAULT '';" +
                            "INSERT INTO participants SELECT participant_id, participant_first, participant_last, participant_street, participant_city," +
                                "participant_state, participant_zip, participant_birthday, participant_phone, participant_email, participant_mobile, participant_parent," +
                                "participant_country, participant_street2, participant_gender, emergencycontact_name, emergencycontact_phone FROM old_participants," +
                                "emergencycontacts WHERE old_participants.emergencycontact_id=emergencycontacts.emergencycontact_id;" +
                            "INSERT INTO eventspecific SELECT * FROM older_eventspecific;" +
                            "UPDATE settings SET version=13, identifier='" + Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "") + "' WHERE version=12;";
                        command.ExecuteNonQuery();
                        goto case 13;
                    case 13:
                        Log.D("Updating from version 13.");
                        command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM old_eventspecific; DROP TABLE old_eventspecific; DELETE FROM older_eventspecific; DROP TABLE older_eventspecific;" +
                            "DELETE FROM old_participants; DROP TABLE old_participants; DELETE FROM emergencycontacts; DROP TABLE emergencycontacts;" +
                            "UPDATE settings SET version=14 WHERE version=13;";
                        Log.D("Executing query.");
                        command.ExecuteNonQuery();
                        Log.D("Done deleting.");
                        goto case 14;
                    case 14:
                        Log.D("Updating from version 14.");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS app_settings (setting VARCHAR NOT NULL, value VARCHAR NOT NULL, UNIQUE (setting) ON CONFLICT REPLACE); ALTER TABLE events ADD " +
                        "event_rank_by_gun INTEGER DEFAULT 1;UPDATE settings SET version=15 WHERE version=14;";
                        command.ExecuteNonQuery();
                        goto case 15;
                    case 15:
                        Log.D("Updating from version 15.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_common_age_groups INTEGER DEFAULT 1;" +
                            "ALTER TABLE events ADD event_common_start_finish INTEGER DEFAULT 1;" +
                            "ALTER TABLE events ADD event_division_specific_segments INTEGER DEFAULT 0;" +
                            "DROP TABLE timingpoints;" +
                            "CREATE TABLE IF NOT EXISTS timing_locations(" +
                                "location_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "location_name VARCHAR(100) NOT NULL," +
                                "UNIQUE (event_id, location_name) ON CONFLICT IGNORE" +
                                ");" +
                            "ALTER TABLE divisions ADD division_distance DECIMAL(10,2) DEFAULT 0.0;" +
                            "ALTER TABLE divisions ADD division_distance_unit INTEGER DEFAULT 0;" +
                            "ALTER TABLE divisions ADD division_start_location INTEGER DEFAULT -2;" +
                            "ALTER TABLE divisions ADD division_start_within INTEGER DEFAULT -1;" +
                            "ALTER TABLE divisions ADD division_finish_location INTEGER DEFAULT -1;" +
                            "ALTER TABLE divisions ADD division_finish_occurance INTEGER DEFAULT 1;" +
                            "ALTER TABLE eventspecific RENAME TO old_eventspecific;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific (" +
                                "eventspecific_id INTEGER PRIMARY KEY," +
                                "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                                "eventspecific_bib INTEGER," +
                                "eventspecific_checkedin INTEGER DEFAULT 0," +
                                "eventspecific_comments VARCHAR," +
                                "eventspecific_owes VARCHAR(50)," +
                                "eventspecific_other VARCHAR," +
                                "eventspecific_earlystart INTEGER DEFAULT 0," +
                                "eventspecific_next_year INTEGER DEFAULT 0," +
                                "UNIQUE (participant_id, event_id, division_id) ON CONFLICT IGNORE" +
                                ");" +
                            "INSERT INTO eventspecific SELECT eventspecific_id, participant_id, event_id, division_id, eventspecific_bib, eventspecific_checkedin," +
                                "eventspecific_comments, eventspecific_owes, eventspecific_other, eventspecific_earlystart, eventspecific_next_year " +
                                "FROM old_eventspecific;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific_apparel (" +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "name VARCHAR NOT NULL," +
                                "value VARCHAR NOT NULL," +
                                "UNIQUE (eventspecific_id, name) ON CONFLICT IGNORE" +
                                ");" +
                            "ALTER TABLE participants RENAME TO old_participants;" +
                            "CREATE TABLE IF NOT EXISTS participants (" +
                                "participant_id INTEGER PRIMARY KEY," +
                                "participant_first VARCHAR(50) NOT NULL," +
                                "participant_last VARCHAR(75) NOT NULL," +
                                "participant_street VARCHAR(150)," +
                                "participant_city VARCHAR(75)," +
                                "participant_state VARCHAR(25)," +
                                "participant_zip VARCHAR(10)," +
                                "participant_birthday VARCHAR(15) NOT NULL," +
                                "participant_email VARCHAR(150)," +
                                "participant_mobile VARCHAR(20)," +
                                "participant_parent VARCHAR(150)," +
                                "participant_country VARCHAR(50)," +
                                "participant_street2 VARCHAR(50)," +
                                "participant_gender VARCHAR(10)," +
                                "emergencycontact_name VARCHAR(150) NOT NULL DEFAULT '911'," +
                                "emergencycontact_phone VARCHAR(20)," +
                                "UNIQUE (participant_first, participant_last, participant_street, participant_zip, participant_birthday) ON CONFLICT IGNORE" +
                                ");" +
                            "INSERT INTO participants SELECT participant_id, participant_first, participant_last, participant_street, participant_city," +
                                "participant_state, participant_zip, participant_birthday, participant_email, participant_phone, participant_parent," +
                                "participant_country, participant_street2, participant_gender, emergencycontact_name, emergencycontact_phone FROM old_participants;" +
                            "DROP TABLE timeresults;" +
                            "CREATE TABLE IF NOT EXISTS time_results (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "location_id INTEGER NOT NULL," +
                                "timeresult_time INTEGER NOT NULL," +
                                "segment_id INTEGER NOT NULL DEFAULT -3," +
                                "timeresult_occurance INTEGER NOT NULL," +
                                "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance) ON CONFLICT IGNORE" +
                                ");" +
                            "DROP TABLE old_bib_chip_assoc; DROP TABLE old_old_bib_chip_assoc;" +
                            "DROP TABLE chipreads;" +
                            "CREATE TABLE IF NOT EXISTS chipreads (" +
                                "read_id INTEGER NOT NULL PRIMARY KEY," +
                                "read_status INTEGER NOT NULL DEFAULT 0," +
                                "location_id INTEGER NOT NULL," +
                                "read_chipnumber INTEGER NOT NULL," +
                                "read_seconds INTEGER NOT NULL," +
                                "read_milliseconds INTEGER NOT NULL," +
                                "read_antenna INTEGER NOT NULL," +
                                "read_reader INTEGER NOT NULL," +
                                "read_box INTEGER NOT NULL," +
                                "read_logindex INTEGER NOT NULL," +
                                "read_rssi INTEGER NOT NULL," +
                                "read_isrewind INTEGER NOT NULL," +
                                "read_readertime TEXT NOT NULL," +
                                "read_starttime INTEGER NOT NULL," +
                                "UNIQUE (read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                                ");" +
                            "DROP TABLE dayof_participant;" +
                            "CREATE TABLE IF NOT EXISTS dayof_participant (" +
                                "dop_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "division_id INTEGER NOT NULL," +
                                "dop_first VARCHAR NOT NULL," +
                                "dop_last VARCHAR NOT NULL," +
                                "dop_street VARCHAR," +
                                "dop_city VARCHAR," +
                                "dop_state VARCHAR," +
                                "dop_zip VARCHAR," +
                                "dop_birthday VARCHAR NOT NULL," +
                                "dop_email VARCHAR," +
                                "dop_mobile VARCHAR," +
                                "dop_parent VARCHAR," +
                                "dop_country VARCHAR," +
                                "dop_street2 VARCHAR," +
                                "dop_gender VARCHAR," +
                                "dop_comments VARCHAR," +
                                "dop_other VARCHAR," +
                                "dop_other2 VARCHAR," +
                                "dop_emergency_name VARCHAR NOT NULL," +
                                "dop_emergency_phone VARCHAR NOT NULL," +
                                "UNIQUE (event_id, dop_first, dop_last, dop_street, dop_zip, dop_birthday) ON CONFLICT IGNORE" +
                                ");" +
                            "DROP TABLE changes;" +
                            "CREATE TABLE IF NOT EXISTS changes (" +
                                "change_id INTEGER PRIMARY KEY, " +
                                "old_participant_id INTEGER NOT NULL," +
                                "old_first VARCHAR(50) NOT NULL," +
                                "old_last VARCHAR(75) NOT NULL," +
                                "old_street VARCHAR(150)," +
                                "old_city VARCHAR(75)," +
                                "old_state VARCHAR(25)," +
                                "old_zip VARCHAR(10)," +
                                "old_birthday VARCHAR(15) NOT NULL," +
                                "old_email VARCHAR(150)," +
                                "old_emergency_id INTEGER DEFAULT -1," +
                                "old_emergency_name VARCHAR(150)," +
                                "old_emergency_email VARCHAR(150)," +
                                "old_event_spec_id INTEGER DEFAULT -1," +
                                "old_event_spec_event_id INTEGER DEFAULT -1," +
                                "old_event_spec_division_id INTEGER DEFAULT -1," +
                                "old_event_spec_bib INTEGER," +
                                "old_event_spec_checkedin INTEGER DEFAULT -1," +
                                "old_event_spec_comments VARCHAR," +
                                "old_mobile VARCHAR(20)," +
                                "old_parent VARCHAR(150)," +
                                "old_country VARCHAR(50)," +
                                "old_street2 VARCHAR(50)," +
                                "old_owes VARCHAR(50)," +
                                "old_other VARCHAR," +
                                "old_gender VARCHAR(10)," +
                                "old_earlystart INTEGER DEFAULT -1," +
                                "old_next_year INTEGER DEFAULT 0," +

                                "new_participant_id INTEGER NOT NULL," +
                                "new_first VARCHAR(50) NOT NULL," +
                                "new_last VARCHAR(75) NOT NULL," +
                                "new_street VARCHAR(150)," +
                                "new_city VARCHAR(75)," +
                                "new_state VARCHAR(25)," +
                                "new_zip VARCHAR(10)," +
                                "new_birthday VARCHAR(15) NOT NULL," +
                                "new_email VARCHAR(150)," +
                                "new_emergency_id INTEGER DEFAULT -1," +
                                "new_emergency_name VARCHAR(150)," +
                                "new_emergency_phone VARCHAR(20)," +
                                "new_emergency_email VARCHAR(150)," +
                                "new_event_spec_id INTEGER DEFAULT -1," +
                                "new_event_spec_event_id INTEGER DEFAULT -1," +
                                "new_event_spec_division_id INTEGER DEFAULT -1," +
                                "new_event_spec_bib INTEGER DEFAULT -1," +
                                "new_event_spec_checkedin INTEGER DEFAULT -1," +
                                "new_event_spec_comments VARCHAR," +
                                "new_mobile VARCHAR(20)," +
                                "new_parent VARCHAR(150)," +
                                "new_country VARCHAR(50)," +
                                "new_street2 VARCHAR(50)," +
                                "new_owes VARCHAR(50)," +
                                "new_other VARCHAR," +
                                "new_gender VARCHAR(10)," +
                                "new_earlystart INTEGER DEFAULT -1," +
                                "new_next_year INTEGER DEFAULT 0" +
                                ");" +
                            "CREATE TABLE IF NOT EXISTS segments (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "division_id INTEGER DEFAULT -1," +
                                "location_id INTEGER DEFAULT -1," +
                                "location_occurance INTEGER DEFAULT 1," +
                                "name VARCHAR DEFAULT ''," +
                                "distance_segment DECIMAL (10,2) DEFAULT 0.0," +
                                "distance_cumulative DECIMAL (10,2) DEFAULT 0.0," +
                                "distance_unit INTEGER DEFAULT 0," +
                                "UNIQUE (event_id, division_id, location_id, location_occurance) ON CONFLICT IGNORE" +
                                ");" +
                            "UPDATE settings SET version=16 WHERE version=15;";
                        command.ExecuteNonQuery();
                        goto case 16;
                    case 16:
                        Log.D("Upgrading from verison 16.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE timing_locations ADD location_max_occurances INTEGER NOT NULL DEFAULT 1;" +
                                "ALTER TABLE timing_locations ADD location_ignore_within INTEGER NOT NULL DEFAULT -1;" +
                                "ALTER TABLE events ADD event_yearcode VARCHAR(10) NOT NULL DEFAULT '';" +
                                "ALTER TABLE events ADD event_early_start_difference INTEGER NOT NULL DEFAULT 0;" +
                                "UPDATE settings SET version=17 WHERE version=16;";
                        Log.D(command.CommandText);
                        command.ExecuteNonQuery();
                        goto case 17;
                    case 17:
                        Log.D("Upgrading from version 17.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE divisions ADD bib_group_number INTEGER NOT NULL DEFAULT -1; " +
                            "ALTER TABLE divisions ADD division_wave INTEGER NOT NULL DEFAULT 1;" +
                            "CREATE TABLE IF NOT EXISTS bib_group (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "bib_group_number INTEGER NOT NULL," +
                                "bib_group_name VARCHAR NOT NULL," +
                                "UNIQUE (event_id, bib_group_number) ON CONFLICT REPLACE," +
                                "UNIQUE (event_id, bib_group_name) ON CONFLICT REPLACE);" +
                                "UPDATE settings SET version=18 WHERE version=17";
                        command.ExecuteNonQuery();
                        goto case 18;
                    case 18:
                        Log.D("Upgrading from version 18.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_start_time_seconds INTEGER NOT NULL DEFAULT -1;" +
                            "ALTER TABLE events ADD event_start_time_milliseconds INTEGER NOT NULL DEFAULT 0;" +
                            "ALTER TABLE divisions ADD division_start_offset_seconds INTEGER NOT NULL DEFAULT 0;" +
                            "ALTER TABLE divisions ADD division_start_offset_milliseconds INTEGER NOT NULL DEFAULT 0;" +
                            "ALTER TABLE eventspecific ADD eventspecific_registration_date VARCHAR NOT NULL DEFAULT '';" +
                            "UPDATE settings SET version=19 WHERE version=18;";
                        command.ExecuteNonQuery();
                        goto case 19;
                    case 19:
                        Log.D("Upgrading from version 19.");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS available_bibs (" +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "bib_group_number INTEGER NOT NULL DEFAULT -1," +
                            "bib INTEGER NOT NULL," +
                            "UNIQUE (event_id, bib) ON CONFLICT REPLACE);" +
                            "UPDATE settings SET version=20 WHERE version=19;";
                        command.ExecuteNonQuery();
                        goto case 20;
                    case 20:
                        Log.D("Upgrading from version 20.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE old_eventspecific; DROP TABLE old_participants;" +
                            "ALTER TABLE eventspecific RENAME TO old_eventspecific;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific(" +
                            "eventspecific_id INTEGER PRIMARY KEY," +
                            "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                            "eventspecific_bib INTEGER," +
                            "eventspecific_checkedin INTEGER DEFAULT 0," +
                            "eventspecific_comments VARCHAR," +
                            "eventspecific_owes VARCHAR(50)," +
                            "eventspecific_other VARCHAR," +
                            "eventspecific_earlystart INTEGER DEFAULT 0," +
                            "eventspecific_next_year INTEGER DEFAULT 0," +
                            "eventspecific_registration_date VARCHAR NOT NULL DEFAULT ''," +
                            "UNIQUE (participant_id, event_id, division_id) ON CONFLICT REPLACE);" +
                            "INSERT INTO eventspecific SELECT * FROM old_eventspecific;" +
                            "DROP TABLE old_eventspecific;" +
                            "UPDATE settings SET version=21 WHERE version=20;";
                        command.ExecuteNonQuery();
                        goto case 21;
                    case 21:
                        Log.D("Upgrading from version 21.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_finish_max_occurances INTEGER NOT NULL DEFAULT 1;" +
                            "ALTER TABLE events ADD event_finish_ignore_within INTEGER NOT NULL DEFAULT 0;" +
                            "ALTER TABLE events ADD event_start_window INTEGER NOT NULL DEFAULT -1;" +
                            "UPDATE settings SET version=22 WHERE version=21;";
                        command.ExecuteNonQuery();
                        goto case 22;
                    case 22:
                        Log.D("Upgrading from version 22.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE segments;" +
                            "CREATE TABLE IF NOT EXISTS segments(" +
                            "segment_id INTEGER PRIMARY KEY," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "division_id INTEGER DEFAULT -1," +
                            "location_id INTEGER DEFAULT -1," +
                            "location_occurance INTEGER DEFAULT 1," +
                            "name VARCHAR DEFAULT ''," +
                            "distance_segment DECIMAL (10,2) DEFAULT 0.0," +
                            "distance_cumulative DECIMAL (10,2) DEFAULT 0.0," +
                            "distance_unit INTEGER DEFAULT 0," +
                            "UNIQUE (event_id, division_id, location_id, location_occurance) ON CONFLICT IGNORE);" +
                            "UPDATE settings SET version=23 WHERE version=22;";
                        command.ExecuteNonQuery();
                        goto case 23;
                    case 23:
                        Log.D("Upgrading from version 23.");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS age_groups (" +
                            "group_id INTEGER PRIMARY KEY," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "division_id INTEGER NOT NULL DEFAULT -1," +
                            "start_age INTEGER NOT NULL," +
                            "end_age INTEGER NOT NULL);" +
                            "UPDATE settings SET version=24 WHERE version=23;";
                        command.ExecuteNonQuery();
                        goto case 24;
                    case 24:
                        Log.D("Upgrading from version 24.");
                        command = connection.CreateCommand();
                        command.CommandText = "UPDATE events SET event_start_time_seconds=-1 WHERE event_start_time_seconds=0;" +
                            "ALTER TABLE events ADD event_timing_system VARCHAR NOT NULL DEFAULT 'RFID';" +
                            "UPDATE settings SET version=25 WHERE version=24;";
                        command.ExecuteNonQuery();
                        break;
                }
                transaction.Commit();
            }
        }

        /*
         * Divisions
         */

        public void AddDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (division_name, event_id, division_cost, division_distance, division_distance_unit," +
                "division_start_location, division_start_within, division_finish_location, division_finish_occurance, division_wave, bib_group_number," +
                "division_start_offset_seconds, division_start_offset_milliseconds) " +
                "values (@name,@event_id,@cost,@distance,@unit,@startloc,@startwithin,@finishloc,@finishocc,@wave,@bgn,@soffsec,@soffmill)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", div.Name),
                new SQLiteParameter("@event_id", div.EventIdentifier),
                new SQLiteParameter("@cost", div.Cost),
                new SQLiteParameter("@distance", div.Distance),
                new SQLiteParameter("@unit", div.DistanceUnit),
                new SQLiteParameter("@startloc", div.StartLocation),
                new SQLiteParameter("@startwithin", div.StartWithin),
                new SQLiteParameter("@finishloc", div.FinishLocation),
                new SQLiteParameter("@finishocc", div.FinishOccurrence),
                new SQLiteParameter("@wave", div.Wave),
                new SQLiteParameter("@bgn", div.BibGroupNumber),
                new SQLiteParameter("@soffsec", div.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", div.StartOffsetMilliseconds),
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void RemoveDivision(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = " DELETE FROM segments WHERE division_id=@id; DELETE FROM divisions WHERE division_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveDivision(Division div)
        {
            RemoveDivision(div.Identifier);
        }

        public void UpdateDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE divisions SET division_name=@name, event_id=@event, division_cost=@cost, division_distance=@distance," +
                "division_distance_unit=@unit, division_start_location=@startloc, division_start_within=@within, division_finish_location=@finishloc," +
                "division_finish_occurance=@occurance, division_wave=@wave, bib_group_number=@bgn, division_start_offset_seconds=@soffsec, " +
                "division_start_offset_milliseconds=@soffmill WHERE division_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", div.Name),
                new SQLiteParameter("@event", div.EventIdentifier),
                new SQLiteParameter("@cost", div.Cost),
                new SQLiteParameter("@distance", div.Distance),
                new SQLiteParameter("@unit", div.DistanceUnit),
                new SQLiteParameter("@startloc", div.StartLocation),
                new SQLiteParameter("@within", div.StartWithin),
                new SQLiteParameter("@finishloc", div.FinishLocation),
                new SQLiteParameter("@occurance", div.FinishOccurrence),
                new SQLiteParameter("@wave", div.Wave),
                new SQLiteParameter("@bgn", div.BibGroupNumber),
                new SQLiteParameter("@soffsec", div.StartOffsetSeconds),
                new SQLiteParameter("@soffmill", div.StartOffsetMilliseconds),
                new SQLiteParameter("@id", div.Identifier) });
            command.ExecuteNonQuery();
        }

        public List<Division> GetDivisions()
        {
            List<Division> output = new List<Division>();
            String commandTxt = "SELECT * FROM divisions";
            SQLiteCommand command = new SQLiteCommand(commandTxt, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]), reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_cost"]), Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]), Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]), Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]), Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]), Convert.ToInt32(reader["division_start_offset_milliseconds"])));
            }
            return output;
        }

        public List<Division> GetDivisions(int eventId)
        {
            List<Division> output = new List<Division>();
            if (eventId < 0)
            {
                return output;
            }
            String commandTxt;
            if (eventId != -1)
            {
                commandTxt = "SELECT * FROM divisions WHERE event_id = " + eventId;
            }
            else
            {
                commandTxt = "SELECT * FROM divisions";
            }
            SQLiteCommand command = new SQLiteCommand(commandTxt, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]), reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]), Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]), Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]), Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]), Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]), Convert.ToInt32(reader["division_start_offset_milliseconds"])));
            }
            return output;
        }

        public int GetDivisionID(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT division_id FROM divisions WHERE division_name=@name AND event_id=@eventid";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", div.Name),
                new SQLiteParameter("@eventid", div.EventIdentifier)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["division_id"]);
            }
            return output;
        }

        public Division GetDivision(int divId)
        {
            SQLiteCommand command = new SQLiteCommand
            {
                CommandText = "SELECT * FROM divisions WHERE division_id=@div"
            };
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@div", divId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            Division output = null;
            if (reader.Read())
            {
                output = new Division(Convert.ToInt32(reader["division_id"]), reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]), Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]), Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]), Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]), Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]), Convert.ToInt32(reader["division_start_offset_milliseconds"]));
            }
            return output;
        }

        /*
         * Events
         */

        public void AddEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO events(event_name, event_date, event_shirt_optional, event_shirt_price," +
                "event_common_age_groups, event_common_start_finish, event_rank_by_gun, event_division_specific_segments, event_yearcode, " +
                "event_next_year_event_id, event_allow_early_start, event_early_start_difference, event_start_time_seconds, " +
                "event_start_time_milliseconds, event_timing_system)" +
                " values(@name,@date,@so,@price,@age,@start,@gun,@sepseg,@yearcode,@ny,@early,@diff,@startsec,@startmill,@system)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date),
                new SQLiteParameter("@so", anEvent.ShirtOptional),
                new SQLiteParameter("@price", anEvent.ShirtPrice),
                new SQLiteParameter("@age", anEvent.CommonAgeGroups),
                new SQLiteParameter("@start", anEvent.CommonStartFinish),
                new SQLiteParameter("@gun", anEvent.RankByGun),
                new SQLiteParameter("@sepseg", anEvent.DivisionSpecificSegments),
                new SQLiteParameter("@yearcode", anEvent.YearCode),
                new SQLiteParameter("@ny", anEvent.NextYear),
                new SQLiteParameter("@early", anEvent.AllowEarlyStart),
                new SQLiteParameter("@diff", anEvent.EarlyStartDifference),
                new SQLiteParameter("@startsec", anEvent.StartSeconds),
                new SQLiteParameter("@startmill", anEvent.StartMilliseconds),
                new SQLiteParameter("@system", anEvent.TimingSystem)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void RemoveEvent(Event anEvent)
        {
            RemoveEvent(anEvent.Identifier);
        }

        public void RemoveEvent(int identifier)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM segments WHERE event_id=@event; DELETE FROM kiosk WHERE event_id=@event;" +
                    "DELETE FROM bib_chip_assoc WHERE event_id=@event; DELETE FROM timing_locations WHERE event_id=@event;" +
                    "DELETE FROM divisions WHERE event_id=@event; DELETE FROM events WHERE event_id=@event;" +
                    "UPDATE events SET event_next_year_event_id='-1' WHERE event_next_year_event_id=@event";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void UpdateEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE events SET event_name=@name, event_date=@date, event_next_year_event_id=@ny, event_shirt_optional=@so," +
                "event_shirt_price=@price, event_common_age_groups=@age, event_common_start_finish=@start, event_rank_by_gun=@gun, " +
                "event_division_specific_segments=@seg, event_yearcode=@yearcode, event_allow_early_start=@early, " +
                "event_early_start_difference=@diff, event_start_time_seconds=@startsec, event_start_time_milliseconds=@startmill, " +
                "event_timing_system=@system WHERE event_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", anEvent.Identifier),
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date),
                new SQLiteParameter("@so", anEvent.ShirtOptional),
                new SQLiteParameter("@price", anEvent.ShirtPrice),
                new SQLiteParameter("@age", anEvent.CommonAgeGroups),
                new SQLiteParameter("@start", anEvent.CommonStartFinish),
                new SQLiteParameter("@gun", anEvent.RankByGun),
                new SQLiteParameter("@seg", anEvent.DivisionSpecificSegments),
                new SQLiteParameter("@yearcode", anEvent.YearCode),
                new SQLiteParameter("@ny", anEvent.NextYear),
                new SQLiteParameter("@early", anEvent.AllowEarlyStart),
                new SQLiteParameter("@diff", anEvent.EarlyStartDifference),
                new SQLiteParameter("@startsec", anEvent.StartSeconds),
                new SQLiteParameter("@startmill", anEvent.StartMilliseconds),
                new SQLiteParameter("@system", anEvent.TimingSystem)
            });
            command.ExecuteNonQuery();
        }

        public List<Event> GetEvents()
        {
            List<Event> output = new List<Event>();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM events", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_next_year_event_id"]), Convert.ToInt32(reader["event_shirt_optional"]),
                    Convert.ToInt32(reader["event_shirt_price"]), Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]), Convert.ToInt32(reader["event_division_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]), reader["event_yearcode"].ToString(), Convert.ToInt32(reader["event_allow_early_start"]),
                    Convert.ToInt32(reader["event_early_start_difference"]), Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]), Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]), Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString()
                    ));
            }
            return output;
        }

        public int GetEventID(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT event_id FROM events WHERE event_name=@name AND event_date=@date";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["event_id"]);
            }
            return output;
        }

        public Event GetCurrentEvent()
        {
            if (GetAppSetting(Constants.Settings.CURRENT_EVENT) == null)
            {
                return null;
            }
            return GetEvent(Convert.ToInt32(GetAppSetting(Constants.Settings.CURRENT_EVENT).value));
        }

        public Event GetEvent(int id)
        {
            if (id < 0)
            {
                return null;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id", id));
            SQLiteDataReader reader = command.ExecuteReader();
            Event output = null;
            if (reader.Read())
            {
                output = new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_next_year_event_id"]), Convert.ToInt32(reader["event_shirt_optional"]),
                    Convert.ToInt32(reader["event_shirt_price"]), Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]), Convert.ToInt32(reader["event_division_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]), reader["event_yearcode"].ToString(), Convert.ToInt32(reader["event_allow_early_start"]),
                    Convert.ToInt32(reader["event_early_start_difference"]), Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]), Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]), Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString()
                    );
            }
            return output;
        }

        public List<JsonOption> GetEventOptions(int eventId)
        {
            List<JsonOption> output = new List<JsonOption>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                Log.D("Event Registration Open is in the DB as " + reader["event_registration_open"].ToString());
                int.TryParse(reader["event_registration_open"].ToString(), out int val);
                output.Add(new JsonOption()
                {
                    Name = "registration_open",
                    Value = val == 0 ? "false" : "true"
                });
                Log.D("Event Results Open is in the DB as " + reader["event_results_open"].ToString());
                int.TryParse(reader["event_results_open"].ToString(), out val);
                output.Add(new JsonOption()
                {
                    Name = "results_open",
                    Value = val == 0 ? "false" : "true"
                });
                Log.D("Event Announce Available is in the DB as " + reader["event_announce_available"].ToString());
                int.TryParse(reader["event_announce_available"].ToString(), out val);
                output.Add(new JsonOption()
                {
                    Name = "announce_available",
                    Value = val == 0 ? "false" : "true"
                });
                Log.D("Event Allow Early Start is in the DB as " + reader["event_allow_early_start"].ToString());
                int.TryParse(reader["event_allow_early_start"].ToString(), out val);
                output.Add(new JsonOption()
                {
                    Name = "allow_early_start",
                    Value = val == 0 ? "false" : "true"
                });
                Log.D("Event Kiosk is in the DB as " + reader["event_kiosk"].ToString());
                int.TryParse(reader["event_kiosk"].ToString(), out val);
                output.Add(new JsonOption()
                {
                    Name = "kiosk",
                    Value = val == 0 ? "false" : "true"
                });
            }
            return output;
        }

        public void SetEventOptions(int eventId, List<JsonOption> options)
        {
            List<JsonOption> output = new List<JsonOption>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_allow_early_start=@es, event_announce_available=@announce, event_results_open=@results, event_registration_open=@registration, event_kiosk=@kiosk WHERE event_id=@id";
            int es = 0, results = 0, registration = 0, announce = 0, kiosk = 0;
            foreach (JsonOption opt in options)
            {
                int val = opt.Value == "true" ? 1 : 0;
                Log.D("Option name is " + opt.Name + " and Value is " + opt.Value + " integer we've got is " + val);
                switch (opt.Name)
                {
                    case "announce_available":
                        announce = val;
                        break;
                    case "registration_open":
                        registration = val;
                        break;
                    case "results_open":
                        results = val;
                        break;
                    case "allow_early_start":
                        es = val;
                        break;
                    case "kiosk":
                        kiosk = val;
                        break;
                }
            }
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@es", es),
                new SQLiteParameter("@announce", announce),
                new SQLiteParameter("@results", results),
                new SQLiteParameter("@registration", registration),
                new SQLiteParameter("@id", eventId),
                new SQLiteParameter("@kiosk", kiosk)
            });
            command.ExecuteNonQuery();
        }

        public void SetStartWindow(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_start_window=@window WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@window", anEvent.StartWindow),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
        }

        public void SetFinishOptions(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_finish_max_occurances=@occ, event_finish_ignore_within=@ignore WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@occ", anEvent.FinishMaxOccurrences),
                new SQLiteParameter("@ignore", anEvent.FinishIgnoreWithin),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
        }

        /*
         * Participants
         */

        public void AddParticipant(Participant person)
        {
            using (var transaction = connection.BeginTransaction())
            {
                AddParticipantNoTransaction(person);
                transaction.Commit();
            }
        }

        public void AddParticipants(List<Participant> people)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant person in people)
                {
                    AddParticipantNoTransaction(person);
                }
                transaction.Commit();
            }
        }

        private void AddParticipantNoTransaction(Participant person)
        {
            person.FormatData();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO participants (participant_first, participant_last, participant_street, " +
                "participant_city, participant_state, participant_zip, participant_birthday, participant_email, " +
                "participant_mobile, participant_parent, participant_country, participant_street2, participant_gender, " +
                "emergencycontact_name, emergencycontact_phone)" +
                " VALUES (@first,@last,@street,@city,@state,@zip,@birthdate,@email,@mobile,@parent,@country,@street2," +
                "@gender,@ecname,@ecphone); SELECT participant_id FROM participants WHERE participant_first=@first " +
                "AND participant_last=@last AND participant_street=@street AND participant_city=@city AND " +
                "participant_state=@state AND participant_zip=@zip;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@first", person.FirstName),
                new SQLiteParameter("@last", person.LastName),
                new SQLiteParameter("@street", person.Street),
                new SQLiteParameter("@city", person.City),
                new SQLiteParameter("@state", person.State),
                new SQLiteParameter("@zip", person.Zip),
                new SQLiteParameter("@birthdate", person.Birthdate),
                new SQLiteParameter("@email", person.Email),
                new SQLiteParameter("@mobile", person.Mobile),
                new SQLiteParameter("@parent", person.Parent),
                new SQLiteParameter("@country", person.Country),
                new SQLiteParameter("@street2", person.Street2),
                new SQLiteParameter("@ecname", person.ECName),
                new SQLiteParameter("@ecphone", person.ECPhone),
                new SQLiteParameter("@gender", person.Gender) } );
            Log.D("SQL query: '" + command.CommandText + "'");
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                person.Identifier = Convert.ToInt32(reader["participant_id"]);
            }
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO eventspecific (participant_id, event_id, division_id, eventspecific_bib, " +
                "eventspecific_checkedin, eventspecific_comments, eventspecific_owes, eventspecific_other, " +
                "eventspecific_earlystart, eventspecific_next_year) " +
                "VALUES (@participant,@event,@division,@bib,@checkedin,@comments,@owes,@other,@earlystart,@nextYear)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@participant", person.Identifier),
                new SQLiteParameter("@event", person.EventSpecific.EventIdentifier),
                new SQLiteParameter("@division", person.EventSpecific.DivisionIdentifier),
                new SQLiteParameter("@bib", person.EventSpecific.Bib),
                new SQLiteParameter("@checkedin", person.EventSpecific.CheckedIn),
                new SQLiteParameter("@comments", person.EventSpecific.Comments),
                new SQLiteParameter("@owes", person.EventSpecific.Owes),
                new SQLiteParameter("@other", person.EventSpecific.Other),
                new SQLiteParameter("@earlystart", person.EventSpecific.EarlyStart),
                new SQLiteParameter("@nextYear", person.EventSpecific.NextYear) } );
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void RemoveParticipant(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@0; DELETE FROM participant WHERE participant_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            RemoveParticipant(person.Identifier);
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant p in participants)
                {
                    RemoveParticipant(p.Identifier);
                }
                transaction.Commit();
            }
        }

        public void RemoveEntry(int eventId, int participantId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@participant AND event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@participant", participantId) });
            command.ExecuteNonQuery();
        }

        public void RemoveEntry(Participant person)
        {
            RemoveEntry(person.EventIdentifier, person.Identifier);
        }

        public void RemoveEntries(List<Participant> people)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant p in people)
                {
                    RemoveEntry(p.EventIdentifier, p.Identifier);
                }
                transaction.Commit();
            }
        }

        public void UpdateParticipant(Participant person)
        {
            person.FormatData();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                Log.D("Updating participant values.");
                command.CommandText = "UPDATE participants SET participant_first=@first, participant_last=@last, participant_street=@street," +
                    " participant_city=@city, participant_state=@state, participant_zip=@zip, participant_birthday=@birthdate," +
                    " emergencycontact_name=@ecname, emergencycontact_phone=@ecphone, participant_email=@email, participant_mobile=@mobile," +
                    " participant_parent=@parent, participant_country=@country, participant_street2=@street2, participant_gender=@gender WHERE participant_id=@participantid";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@first", person.FirstName),
                    new SQLiteParameter("@last", person.LastName),
                    new SQLiteParameter("@street", person.Street),
                    new SQLiteParameter("@city", person.City),
                    new SQLiteParameter("@state", person.State),
                    new SQLiteParameter("@zip", person.Zip),
                    new SQLiteParameter("@birthdate", person.Birthdate),
                    new SQLiteParameter("@ecname", person.ECName),
                    new SQLiteParameter("@ecphone", person.ECPhone),
                    new SQLiteParameter("@email", person.Email),
                    new SQLiteParameter("@participantid", person.Identifier),
                    new SQLiteParameter("@mobile", person.Mobile),
                    new SQLiteParameter("@parent", person.Parent),
                    new SQLiteParameter("@country", person.Country),
                    new SQLiteParameter("@street2", person.Street2),
                    new SQLiteParameter("@gender", person.Gender) });
                command.ExecuteNonQuery();
                command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                Log.D("Updating event specific.... bib is " + person.EventSpecific.Bib);
                command.CommandText = "UPDATE eventspecific SET division_id=@divid, eventspecific_bib=@bib, eventspecific_checkedin=@checkedin, " +
                    "eventspecific_owes=@owes, eventspecific_other=@other, eventspecific_earlystart=@earlystart, eventspecific_next_year=@nextYear," +
                    "eventspecific_comments=@comments " +
                    "WHERE eventspecific_id=@eventspecid";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@divid", person.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@bib", person.EventSpecific.Bib),
                    new SQLiteParameter("@checkedin", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@eventspecid", person.EventSpecific.Identifier),
                    new SQLiteParameter("@owes", person.EventSpecific.Owes),
                    new SQLiteParameter("@other", person.EventSpecific.Other),
                    new SQLiteParameter("@earlystart", person.EventSpecific.EarlyStart),
                    new SQLiteParameter("@nextYear", person.EventSpecific.NextYear),
                    new SQLiteParameter("@comments", person.EventSpecific.Comments)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void CheckInParticipant(int eventId, int identifier, int checkedIn)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE eventspecific SET eventspecific_checkedin=@0 WHERE participant_id=@id AND event_id=@eventId";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", checkedIn),
                new SQLiteParameter("@id", identifier),
                new SQLiteParameter("@eventId", eventId)
            });
            command.ExecuteNonQuery();
        }

        public void CheckInParticipant(Participant person)
        {
            CheckInParticipant((int)person.EventSpecific.EventIdentifier, person.Identifier, (int)person.EventSpecific.CheckedIn);
        }

        public void SetEarlyStartParticipant(int eventId, int identifier, int earlystart)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE eventspecific SET eventspecific_earlystart=@earlystart WHERE event_id=@eventid AND participant_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@earlystart", earlystart),
                new SQLiteParameter("@id", identifier),
                new SQLiteParameter("@eventid", eventId)
            });
            command.ExecuteNonQuery();
        }

        public void SetEarlyStartParticipant(Participant person)
        {
            SetEarlyStartParticipant((int)person.EventSpecific.EventIdentifier, person.Identifier, (int)person.EventSpecific.EarlyStart);
        }

        public List<Participant> GetParticipants()
        {
            Log.D("Getting all participants for all events.");
            return GetParticipantsWorker("SELECT * FROM participants AS p, eventspecific as s, divisions AS d WHERE " +
                "p.participant_id=s.participant_id AND d.division_id=s.division_id", -1, -1);
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants AS p, eventspecific AS s, divisions AS d WHERE " +
                "p.participant_id=s.participant_id AND s.event_id=@event AND d.division_id=s.division_id", eventId, -1);
        }


        public List<Participant> GetParticipants(int eventId, int divisionId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants AS p, eventspecific AS s, divisions AS d WHERE " +
                "p.participant_id=s.participant_id AND s.event_id=@event AND d.division_id=s.division_id AND" +
                " d.division_id=@division", eventId, divisionId);
        }

        public List<Participant> GetParticipantsWorker(string query, int eventId, int divisionId)
        {
            List<Participant> output = new List<Participant>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (eventId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@event", eventId));
            }
            if (divisionId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@division", divisionId));
            }
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Participant(
                    Convert.ToInt32(reader["participant_id"]),
                    reader["participant_first"].ToString(),
                    reader["participant_last"].ToString(),
                    reader["participant_street"].ToString(),
                    reader["participant_city"].ToString(),
                    reader["participant_state"].ToString(),
                    reader["participant_zip"].ToString(),
                    reader["participant_birthday"].ToString(),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString(),
                    reader["emergencycontact_name"].ToString(),
                    reader["emergencycontact_phone"].ToString()
                    ));
            }
            return output;
        }

        public Participant GetParticipant(int eventId, int identifier)
        {
            Participant output = null;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d WHERE p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND p.participant_id=@partId";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            command.Parameters.Add(new SQLiteParameter("@partId", identifier));
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = new Participant(
                    Convert.ToInt32(reader["participant_id"]),
                    reader["participant_first"].ToString(),
                    reader["participant_last"].ToString(),
                    reader["participant_street"].ToString(),
                    reader["participant_city"].ToString(),
                    reader["participant_state"].ToString(),
                    reader["participant_zip"].ToString(),
                    reader["participant_birthday"].ToString(),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString(),
                    reader["emergencycontact_name"].ToString(),
                    reader["emergencycontact_phone"].ToString()
                    );
            }
            return output;
        }

        public Participant GetParticipant(int eventId, Participant unknown)
        {
            Participant output = null;
            SQLiteCommand command = connection.CreateCommand();
            if (unknown.EventSpecific.Chip != -1)
            {
                command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d, bib_chip_assoc as b WHERE p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND " +
                    "s.eventspecific_bib=b.bib AND b.chip=@chip AND b.event_id=s.event_id;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@chip", unknown.EventSpecific.Chip),
                });
            }
            else
            {
                command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d WHERE p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND " +
                    "p.participant_first=@first AND p.participant_last=@last AND p.participant_street=@street AND p.participant_city=@city AND p.participant_state=@state AND p.participant_zip=@zip AND p.participant_birthday=@birthday";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@first", unknown.FirstName),
                    new SQLiteParameter("@last", unknown.LastName),
                    new SQLiteParameter("@street", unknown.Street),
                    new SQLiteParameter("@city", unknown.City),
                    new SQLiteParameter("@state", unknown.State),
                    new SQLiteParameter("@zip", unknown.Zip),
                    new SQLiteParameter("@birthday", unknown.Birthdate)
                });

            }
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = new Participant(
                    Convert.ToInt32(reader["participant_id"]),
                    reader["participant_first"].ToString(),
                    reader["participant_last"].ToString(),
                    reader["participant_street"].ToString(),
                    reader["participant_city"].ToString(),
                    reader["participant_state"].ToString(),
                    reader["participant_zip"].ToString(),
                    reader["participant_birthday"].ToString(),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["chip"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString(),
                    reader["emergencycontact_name"].ToString(),
                    reader["emergencycontact_phone"].ToString()
                    );
            }
            return output;
        }

        public int GetParticipantID(Participant person)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT participant_id FROM participants WHERE participant_first=@first AND participant_last=@last" +
                " AND participant_street=@street AND participant_city=@city AND participant_state=@state AND participant_zip=@zip AND participant_birthday=@birthday";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@first", person.FirstName),
                new SQLiteParameter("@last", person.LastName),
                new SQLiteParameter("@street", person.Street),
                new SQLiteParameter("@city", person.City),
                new SQLiteParameter("@state", person.State),
                new SQLiteParameter("@zip", person.Zip),
                new SQLiteParameter("@birthday", person.Birthdate)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["participant_id"]);
            }
            return output;
        }

        /*
         * Timing Locations
         */

        public void AddTimingLocation(TimingLocation tl)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timing_locations (event_id, location_name, location_max_occurances, location_ignore_within) " +
                "VALUES (@event,@name,@max,@ignore)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tl.EventIdentifier),
                new SQLiteParameter("@name", tl.Name),
                new SQLiteParameter("@max", tl.MaxOccurrences),
                new SQLiteParameter("@ignore", tl.IgnoreWithin) } );
            command.ExecuteNonQuery();
        }

        public void RemoveTimingLocation(TimingLocation tl)
        {
            RemoveTimingLocation(tl.Identifier);
        }

        public void RemoveTimingLocation(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM timing_locations WHERE location_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
        }

        public void UpdateTimingLocation(TimingLocation tl)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE timing_locations SET event_id=@event, location_name=@name, location_max_occurances=@max, " +
                "location_ignore_within=@ignore WHERE location_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tl.EventIdentifier),
                new SQLiteParameter("@name", tl.Name),
                new SQLiteParameter("@max", tl.MaxOccurrences),
                new SQLiteParameter("@ignore", tl.IgnoreWithin),
                new SQLiteParameter("@id", tl.Identifier) });
            command.ExecuteNonQuery();
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            List<TimingLocation> output = new List<TimingLocation>();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM timing_locations WHERE event_id=" + eventId, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingLocation(Convert.ToInt32(reader["location_id"]), Convert.ToInt32(reader["event_id"]),
                    reader["location_name"].ToString(), Convert.ToInt32(reader["location_max_occurances"]), Convert.ToInt32(reader["location_ignore_within"])));
            }
            return output;
        }

        public int GetTimingLocationID(TimingLocation tl)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT timingpoint_id FROM timing_locations WHERE event_id=@eventid, location_name=@name";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", tl.Name),
                new SQLiteParameter("@eventid", tl.EventIdentifier)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["location_id"]);
            }
            return output;
        }

        /*
         * Segment
         */

        public void AddSegment(Segment seg)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "INSERT INTO segments (event_id, division_id, location_id, location_occurance, name, distance_segment, " +
                    "distance_cumulative, distance_unit) " +
                    "VALUES (@event,@division,@location,@occurance,@name,@dseg,@dcum,@dunit)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event",seg.EventId),
                new SQLiteParameter("@division",seg.DivisionId),
                new SQLiteParameter("@location",seg.LocationId),
                new SQLiteParameter("@occurance",seg.Occurrence),
                new SQLiteParameter("@name",seg.Name),
                new SQLiteParameter("@dseg",seg.SegmentDistance),
                new SQLiteParameter("@dcum",seg.CumulativeDistance),
                new SQLiteParameter("@dunit",seg.DistanceUnit) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void RemoveSegment(Segment seg)
        {
            RemoveSegment(seg.Identifier);
        }

        public void RemoveSegment(int identifier)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM segments WHERE segment_id=@id";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@id", identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void UpdateSegment(Segment seg)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "UPDATE segments SET event_id=@event, division_id=@division, location_id=@location, " +
                    "location_occurance=@occurance, name=@name, distance_segment=@dseg, distance_cumulative=@dcum, distance_unit=@dunit " +
                    "WHERE segment_id=@id";
                command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event",seg.EventId),
                new SQLiteParameter("@division",seg.DivisionId),
                new SQLiteParameter("@location",seg.LocationId),
                new SQLiteParameter("@occurance",seg.Occurrence),
                new SQLiteParameter("@name",seg.Name),
                new SQLiteParameter("@dseg",seg.SegmentDistance),
                new SQLiteParameter("@dcum",seg.CumulativeDistance),
                new SQLiteParameter("@dunit",seg.DistanceUnit),
                new SQLiteParameter("@id",seg.Identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public int GetSegmentId(Segment seg)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM segments WHERE event_id=@event, division_id=@division, location_id=@location, occurance=@occurance;";
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                return Convert.ToInt32(reader["segment_id"]);
            }
            return -1;
        }

        public List<Segment> GetSegments(int eventId)
        {
            List<Segment> output = new List<Segment>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM segments WHERE event_id=@event";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event",eventId), });
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Segment(Convert.ToInt32(reader["segment_id"]), Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_id"]),
                    Convert.ToInt32(reader["location_id"]), Convert.ToInt32(reader["location_occurance"]), Convert.ToDouble(reader["distance_segment"]),
                    Convert.ToDouble(reader["distance_cumulative"]), Convert.ToInt32(reader["distance_unit"]), reader["name"].ToString()));
            }
            return output;
        }

        /*
         * Timing Results
         */

        public void AddTimingResult(TimeResult tr)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO time_results (event_id, eventspecific_id, location_id, segment_id, timeresult_time, timeresult_occurance)" +
                " VALUES (@event,@specific,@location,@segment,@time,@occ)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventIdentifier),
                new SQLiteParameter("@specific", tr.EventSpecificId),
                new SQLiteParameter("@location", tr.LocationId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@time", tr.Time),
                new SQLiteParameter("@occ", tr.Occurrence) } );
            command.ExecuteNonQuery();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM time_results WHERE eventspecific_id=@event AND location_id=@location AND " +
                "segment_id=@segment AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventSpecificId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@occurance", tr.Occurrence),
                new SQLiteParameter("@location", tr.LocationId) } );
            command.ExecuteNonQuery();
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            Log.D("Getting timing results for event id of " + eventId);
            List<TimeResult> output = new List<TimeResult>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM time_results WHERE event_id=@eventid";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimeResult(
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["eventspecific_id"]),
                    Convert.ToInt32(reader["location_id"]),
                    Convert.ToInt32(reader["segment_id"]),
                    reader["timeresult_time"].ToString(),
                    Convert.ToInt32(reader["timeresult_occurance"])
                    ));
            }
            return output;
        }

        public void UpdateTimingResult(TimeResult oldResult, String newTime)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE time_results SET timeresult_time=@time WHERE event_id=@event AND eventspecific_id=@eventspecific AND location_id=@location AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@time", newTime),
                new SQLiteParameter("@event", oldResult.EventIdentifier),
                new SQLiteParameter("@eventspecific", oldResult.EventSpecificId),
                new SQLiteParameter("@location", oldResult.LocationId),
                new SQLiteParameter("@occurance", oldResult.Occurrence)} );
            command.ExecuteNonQuery();
        }

        /*
         * Changes
         */

        public void AddChange(Participant newParticipant, Participant oldParticipant)
        {
            SQLiteCommand command = connection.CreateCommand();
            Log.D("Adding change - new participant id is " + newParticipant.Identifier + " old participant id is" + (oldParticipant == null ? -1 : oldParticipant.Identifier));
            if (oldParticipant == null)
            {
                Log.D("Found an add player change.");
                command.CommandText = "INSERT INTO changes (" +
                    "old_participant_id, old_first, old_last, old_street, old_city, old_state, old_zip, old_birthday, old_email," +
                    "old_emergency_name, old_emergency_phone, old_event_spec_id, old_event_spec_event_id," +
                    "old_event_spec_division_id, old_event_spec_bib, old_event_spec_checkedin," +
                    "old_event_spec_comments, old_mobile, old_parent, old_country, old_street2, old_owes, old_other," +
                    "old_gender, old_earlystart," +
                    "new_participant_id, new_first, new_last, new_street, new_city, new_state, new_zip, new_birthday, new_email," +
                    "new_emergency_name, new_emergency_phone, new_event_spec_id, new_event_spec_event_id," +
                    "new_event_spec_division_id, new_event_spec_bib, new_event_spec_checkedin," +
                    "new_event_spec_comments, new_mobile, new_parent, new_country, new_street2, new_owes, new_other," +
                    "new_gender, new_earlystart)" +
                    " VALUES" +
                    "(0, 'J', 'Doe', '', '', '', '', '01/01/1901', '', " +
                    "'911', '', 0, @newESEvId, " +
                    "-1, -1, 0," +
                    "'New Participant', '', '', '', '', '', ''," +
                    "'', 0," +
                    " @newPartId, @newFirst, @newLast, @newStreet, @newCity, @newState, @newZip, @newBirthday, @newEmail, " +
                    "@newEName, @newEPhone, @newESId, @newESEvId, " +
                    "@newESDId, @newESBib, @newESCheckedIn, " +
                    "@newESComments, @newMobile, @newParent, @newCountry, @newStreet2, @newOwes, @newOther, " +
                    "@newGender, @newEarlyStart)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@newPartId", newParticipant.Identifier),
                    new SQLiteParameter("@newFirst", newParticipant.FirstName),
                    new SQLiteParameter("@newLast", newParticipant.LastName),
                    new SQLiteParameter("@newStreet", newParticipant.Street),
                    new SQLiteParameter("@newCity", newParticipant.City),
                    new SQLiteParameter("@newState", newParticipant.State),
                    new SQLiteParameter("@newZip", newParticipant.Zip),
                    new SQLiteParameter("@newBirthday", newParticipant.Birthdate),
                    new SQLiteParameter("@newEmail", newParticipant.Email),
                    new SQLiteParameter("@newEName", newParticipant.ECName),
                    new SQLiteParameter("@newEPhone", newParticipant.ECPhone),
                    new SQLiteParameter("@newESId", newParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@newESEvId", newParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@newESDId", newParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@newESBib", newParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@newESCheckedIn", newParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@newESComments", newParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@newMobile", newParticipant.Mobile),
                    new SQLiteParameter("@newParent", newParticipant.Parent),
                    new SQLiteParameter("@newCountry", newParticipant.Country),
                    new SQLiteParameter("@newStreet2", newParticipant.Street2),
                    new SQLiteParameter("@newOwes", newParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@newOther", newParticipant.EventSpecific.Other),
                    new SQLiteParameter("@newGender", newParticipant.Gender),
                    new SQLiteParameter("@newEarlyStart", newParticipant.EventSpecific.EarlyStart),
                });
                command.ExecuteNonQuery();
            }
            else
            {
                Log.D("An update occured.");
                command.CommandText = "INSERT INTO changes (" +
                    "old_participant_id, old_first, old_last, old_street, old_city, old_state, old_zip, old_birthday, old_email," +
                    "old_emergency_name, old_emergency_phone, old_event_spec_id, old_event_spec_event_id," +
                    "old_event_spec_division_id, old_event_spec_bib, old_event_spec_checkedin, " +
                    "old_event_spec_comments, old_mobile, old_parent, old_country, old_street2, old_owes, old_other," +
                    "old_gender, old_earlystart, " +
                    "new_participant_id, new_first, new_last, new_street, new_city, new_state, new_zip, new_birthday, new_email," +
                    "new_emergency_name, new_emergency_phone, new_event_spec_id, new_event_spec_event_id," +
                    "new_event_spec_division_id, new_event_spec_bib, new_event_spec_checkedin," +
                    "new_event_spec_comments, new_mobile, new_parent, new_country, new_street2, new_owes, new_other," +
                    "new_gender, new_earlystart)" +
                    "VALUES" +
                    "(@oldPartId, @oldFirst, @oldLast, @oldStreet, @oldCity, @oldState, @oldZip, @oldBirthday, @oldEmail," +
                    "@oldEName, @oldEPhone,@oldESId, @oldESEvId," +
                    "@oldESDId, @oldESBib, @oldESCheckedIn," +
                    "@oldESComments, @oldMobile, @oldParent, @oldCountry, @oldStreet2, @oldOwes, @oldOther," +
                    "@oldGender, @oldEarlyStart," +
                    "@newPartId, @newFirst, @newLast, @newStreet, @newCity, @newState, @newZip, @newBirthday, @newEmail," +
                    "@newEName, @newEPhone,@newESId, @newESEvId," +
                    "@newESDId, @newESBib, @newESCheckedIn," +
                    "@newESComments, @newMobile, @newParent, @newCountry, @newStreet2, @newOwes, @newOther," +
                    "@newGender, @newEarlyStart)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@oldPartId", oldParticipant.Identifier),
                    new SQLiteParameter("@oldFirst", oldParticipant.FirstName),
                    new SQLiteParameter("@oldLast", oldParticipant.LastName),
                    new SQLiteParameter("@oldStreet", oldParticipant.Street),
                    new SQLiteParameter("@oldCity", oldParticipant.City),
                    new SQLiteParameter("@oldState", oldParticipant.State),
                    new SQLiteParameter("@oldZip", oldParticipant.Zip),
                    new SQLiteParameter("@oldBirthday", oldParticipant.Birthdate),
                    new SQLiteParameter("@oldEmail", oldParticipant.Email),
                    new SQLiteParameter("@oldEName", oldParticipant.ECName),
                    new SQLiteParameter("@oldEPhone", oldParticipant.ECPhone),
                    new SQLiteParameter("@oldESId", oldParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@oldESEvId", oldParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@oldESDId", oldParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@oldESBib", oldParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@oldESCheckedIn", oldParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@oldESComments", oldParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@oldMobile", oldParticipant.Mobile),
                    new SQLiteParameter("@oldParent", oldParticipant.Parent),
                    new SQLiteParameter("@oldCountry", oldParticipant.Country),
                    new SQLiteParameter("@oldStreet2", oldParticipant.Street2),
                    new SQLiteParameter("@oldOwes", oldParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@oldOther", oldParticipant.EventSpecific.Other),
                    new SQLiteParameter("@oldGender", oldParticipant.Gender),
                    new SQLiteParameter("@oldEarlyStart", oldParticipant.EventSpecific.EarlyStart),

                    new SQLiteParameter("@newPartId", newParticipant.Identifier),
                    new SQLiteParameter("@newFirst", newParticipant.FirstName),
                    new SQLiteParameter("@newLast", newParticipant.LastName),
                    new SQLiteParameter("@newStreet", newParticipant.Street),
                    new SQLiteParameter("@newCity", newParticipant.City),
                    new SQLiteParameter("@newState", newParticipant.State),
                    new SQLiteParameter("@newZip", newParticipant.Zip),
                    new SQLiteParameter("@newBirthday", newParticipant.Birthdate),
                    new SQLiteParameter("@newEmail", newParticipant.Email),
                    new SQLiteParameter("@newEName", newParticipant.ECName),
                    new SQLiteParameter("@newEPhone", newParticipant.ECPhone),
                    new SQLiteParameter("@newESId", newParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@newESEvId", newParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@newESDId", newParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@newESBib", newParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@newESCheckedIn", newParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@newESComments", newParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@newMobile", newParticipant.Mobile),
                    new SQLiteParameter("@newParent", newParticipant.Parent),
                    new SQLiteParameter("@newCountry", newParticipant.Country),
                    new SQLiteParameter("@newStreet2", newParticipant.Street2),
                    new SQLiteParameter("@newOwes", newParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@newOther", newParticipant.EventSpecific.Other),
                    new SQLiteParameter("@newGender", newParticipant.Gender),
                    new SQLiteParameter("@newEarlyStart", newParticipant.EventSpecific.EarlyStart),
                });
                command.ExecuteNonQuery();
            }
        }

        public List<Change> GetChanges()
        {
            Log.D("Getting changes.");
            List<Change> output = new List<Change>();
            Hashtable divisions = new Hashtable();
            List<Division> divs = GetDivisions();
            foreach (Division d in divs)
            {
                divisions.Add(d.Identifier, d.Name);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM changes";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int val = Convert.ToInt32(reader["old_event_spec_division_id"]);
                String oldDivName = Convert.ToInt32(reader["old_event_spec_division_id"]) == -1 ? "" : divisions[Convert.ToInt32(reader["old_event_spec_division_id"])].ToString();
                output.Add(new Change(
                    Convert.ToInt32(reader["change_id"]),
                    new Participant(
                        Convert.ToInt32(reader["new_participant_id"]),
                        reader["new_first"].ToString(),
                        reader["new_last"].ToString(),
                        reader["new_street"].ToString(),
                        reader["new_city"].ToString(),
                        reader["new_state"].ToString(),
                        reader["new_zip"].ToString(),
                        reader["new_birthday"].ToString(),
                        new EventSpecific(
                            Convert.ToInt32(reader["new_event_spec_id"]),
                            Convert.ToInt32(reader["new_event_spec_event_id"]),
                            Convert.ToInt32(reader["new_event_spec_division_id"]),
                            divisions[Convert.ToInt32(reader["new_event_spec_division_id"])].ToString(),
                            Convert.ToInt32(reader["new_event_spec_bib"]),
                            Convert.ToInt32(reader["new_event_spec_checkedin"]),
                            reader["new_event_spec_comments"].ToString(),
                            reader["new_owes"].ToString(),
                            reader["new_other"].ToString(),
                            Convert.ToInt32(reader["new_earlystart"]),
                            Convert.ToInt32(reader["new_next_year"])
                            ),
                        reader["new_email"].ToString(),
                        reader["new_mobile"].ToString(),
                        reader["new_parent"].ToString(),
                        reader["new_country"].ToString(),
                        reader["new_street2"].ToString(),
                        reader["old_gender"].ToString(),
                        reader["new_emergency_name"].ToString(),
                        reader["new_emergency_phone"].ToString()
                    ),
                    new Participant(
                        Convert.ToInt32(reader["old_participant_id"]),
                        reader["old_first"].ToString(),
                        reader["old_last"].ToString(),
                        reader["old_street"].ToString(),
                        reader["old_city"].ToString(),
                        reader["old_state"].ToString(),
                        reader["old_zip"].ToString(),
                        reader["old_birthday"].ToString(),
                        new EventSpecific(
                            Convert.ToInt32(reader["old_event_spec_id"]),
                            Convert.ToInt32(reader["old_event_spec_event_id"]),
                            Convert.ToInt32(reader["old_event_spec_division_id"]),
                            oldDivName,
                            Convert.ToInt32(reader["old_event_spec_bib"]),
                            Convert.ToInt32(reader["old_event_spec_checkedin"]),
                            reader["old_event_spec_comments"].ToString(),
                            reader["old_owes"].ToString(),
                            reader["old_other"].ToString(),
                            Convert.ToInt32(reader["old_earlystart"]),
                            Convert.ToInt32(reader["old_next_year"])
                            ),
                        reader["old_email"].ToString(),
                        reader["old_mobile"].ToString(),
                        reader["old_parent"].ToString(),
                        reader["old_country"].ToString(),
                        reader["old_street2"].ToString(),
                        reader["old_gender"].ToString(),
                        reader["old_emergency_name"].ToString(),
                        reader["old_emergency_phone"].ToString()
                    )
                ));
            }
            return output;
        }

        /*
         * Database Functions
         */

        public void HardResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("DROP TABLE bib_chip_assoc; DROP TABLE events; DROP TABLE dayof_participant; DROP TABLE kiosk; " +
                    "DROP TABLE divisions; DROP TABLE timing_locations; DROP TABLE participants; DROP TABLE eventspecific;" +
                    "DROP TABLE eventspecific_apparel; DROP TABLE segments; DROP TABLE time_results; DROP TABLE chipreads;" +
                    "DROP TABLE changes; DROP TABLE app_settings; DROP TABLE settings;", connection);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            Initialize();
        }

        public void ResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("DELETE FROM bib_chip_assoc; DELETE FROM events; DELETE FROM dayof_participant; DELETE FROM kiosk;" +
                    "DELETE FROM divisions; DELETE FROM timing_locations; DELETE FROM participants; DELETE FROM eventspecific;" +
                    "DELETE FROM eventspecific_apparel; DELETE FROM segments; DELETE FROM time_results; DELETE FROM chipreads;" +
                    "DELETE FROM changes; DELETE FROM app_settings; DELETE FROM settings;", connection);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        /*
         * Kiosk settings
         */

        public void AddDayOfParticipant(DayOfParticipant part)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO dayof_participant (dop_event_id, dop_division_id, dop_first, dop_last, dop_street, dop_city," +
                    "dop_state, dop_zip, dop_birthday, dop_email, dop_mobile, dop_parent, dop_country, dop_street2, dop_gender, dop_comments," +
                    "dop_other, dop_other2, dop_emergency_name, dop_emergency_phone)" +
                    " VALUES (@eventId, @divisionId, @first, @last, @street, @city, @state, @zip, @birthday, @email, @mobile, @parent, @country," +
                    "@street2, @gender, @comments, @other, @other2, @eName, @ePhone);";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@eventId", part.EventIdentifier),
                    new SQLiteParameter("@divisionId", part.DivisionIdentifier),
                    new SQLiteParameter("@first", part.First),
                    new SQLiteParameter("@last", part.Last),
                    new SQLiteParameter("@street", part.Street),
                    new SQLiteParameter("@city", part.City),
                    new SQLiteParameter("@state", part.State),
                    new SQLiteParameter("@zip", part.Zip),
                    new SQLiteParameter("@birthday", part.Birthday),
                    new SQLiteParameter("@email", part.Email),
                    new SQLiteParameter("@mobile", part.Mobile),
                    new SQLiteParameter("@parent", part.Parent),
                    new SQLiteParameter("@country", part.Country),
                    new SQLiteParameter("@street2", part.Street2),
                    new SQLiteParameter("@gender", part.Gender),
                    new SQLiteParameter("@comments", part.Comments),
                    new SQLiteParameter("@other", part.Other),
                    new SQLiteParameter("@other2", part.Other2),
                    new SQLiteParameter("@eName", part.EmergencyName),
                    new SQLiteParameter("@ePhone", part.EmergencyPhone)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<DayOfParticipant> GetDayOfParticipants(int eventId)
        {
            return InternalGetDayOfParticipants("SELECT * FROM dayof_participant WHERE dop_event_id=@eventId;", eventId);
        }

        public List<DayOfParticipant> GetDayOfParticipants()
        {
            return InternalGetDayOfParticipants("SELECT * FROM dayof_participant;", -1);
        }

        private List<DayOfParticipant> InternalGetDayOfParticipants(String query, int eventId)
        {
            List<DayOfParticipant> output = new List<DayOfParticipant>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (eventId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            }
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new DayOfParticipant(
                    Convert.ToInt32(reader["dop_id"]),
                    Convert.ToInt32(reader["dop_event_id"]),
                    Convert.ToInt32(reader["dop_division_id"]),
                    reader["dop_first"].ToString(),
                    reader["dop_last"].ToString(),
                    reader["dop_street"].ToString(),
                    reader["dop_city"].ToString(),
                    reader["dop_state"].ToString(),
                    reader["dop_zip"].ToString(),
                    reader["dop_birthday"].ToString(),
                    reader["dop_email"].ToString(),
                    reader["dop_mobile"].ToString(),
                    reader["dop_parent"].ToString(),
                    reader["dop_country"].ToString(),
                    reader["dop_street2"].ToString(),
                    reader["dop_gender"].ToString(),
                    reader["dop_comments"].ToString(),
                    reader["dop_other"].ToString(),
                    reader["dop_other2"].ToString(),
                    reader["dop_emergency_name"].ToString(),
                    reader["dop_emergency_phone"].ToString()
                    ));
            }
            return output;
        }

        public bool ApproveDayOfParticipant(int eventId, int identifier, int bib, int earlystart)
        {
            Participant newPart = null;
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM dayof_participant AS dop, divisions AS d WHERE dop.dop_id=@id AND dop.dop_division_id=d.division_id;";
                command.Parameters.Add(new SQLiteParameter("@id", identifier));
                Log.D("Identifier is " + identifier);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Log.D("We've found something.");
                        EventSpecific newSpecific = new EventSpecific(
                            Convert.ToInt32(reader["dop_event_id"]),
                            Convert.ToInt32(reader["dop_division_id"]),
                            reader["division_name"].ToString(),
                            bib.ToString(),
                            1,
                            reader["dop_comments"].ToString(),
                            "",
                            reader["dop_other"].ToString(),
                            earlystart,
                            0
                            );
                        newPart = new Participant(
                            reader["dop_first"].ToString(),
                            reader["dop_last"].ToString(),
                            reader["dop_street"].ToString(),
                            reader["dop_city"].ToString(),
                            reader["dop_state"].ToString(),
                            reader["dop_zip"].ToString(),
                            reader["dop_birthday"].ToString(),
                            newSpecific,
                            reader["dop_email"].ToString(),
                            reader["dop_mobile"].ToString(),
                            reader["dop_parent"].ToString(),
                            reader["dop_country"].ToString(),
                            reader["dop_street2"].ToString(),
                            reader["dop_gender"].ToString(),
                            reader["dop_emergency_name"].ToString(),
                            reader["dop_emergency_phone"].ToString()
                            );
                    }
                }
            }
            if (newPart != null)
            {
                AddParticipant(newPart);
                using (var transaction = connection.BeginTransaction())
                {
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM dayof_participant WHERE dop_id=@id";
                        command.Parameters.Add(new SQLiteParameter("@id", identifier));
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                return true;
            }
            return false;
        }

        public bool ApproveDayOfParticipant(DayOfParticipant part, int bib, int earlystart)
        {
            return ApproveDayOfParticipant(part.EventIdentifier, part.Identifier, bib, earlystart);
        }

        public void SetLiabilityWaiver(int eventId, string waiver)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO kiosk (event_id, kiosk_waiver_text) VALUES (@eventId, @waiver);";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@eventId", eventId),
                    new SQLiteParameter("@waiver", waiver)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public string GetLiabilityWaiver(int eventId)
        {
            String output = "";
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM kiosk WHERE event_id=@eventId;";
            command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = reader["kiosk_waiver_text"].ToString();
            }
            return output;
        }

        public DayOfParticipant GetDayOfParticipant(DayOfParticipant part)
        {
            DayOfParticipant output = null;
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM dayof_participant WHERE dop_first=@first AND dop_last=@last AND dop_street=@street AND dop_city=@city AND dop_state=@state AND dop_zip=@zip AND dop_birthday=@birthday";
                command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@first", part.First),
                new SQLiteParameter("@last", part.Last),
                new SQLiteParameter("@street", part.Street),
                new SQLiteParameter("@city", part.City),
                new SQLiteParameter("@state", part.State),
                new SQLiteParameter("@zip", part.Zip),
                new SQLiteParameter("@birthday", part.Birthday)
            });
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        output = new DayOfParticipant(
                            Convert.ToInt32(reader["dop_id"]),
                            Convert.ToInt32(reader["dop_event_id"]),
                            reader["dop_first"].ToString(),
                            reader["dop_last"].ToString(),
                            reader["dop_street"].ToString(),
                            reader["dop_city"].ToString(),
                            reader["dop_state"].ToString(),
                            reader["dop_zip"].ToString(),
                            reader["dop_birthday"].ToString(),
                            reader["dop_email"].ToString(),
                            reader["dop_mobile"].ToString(),
                            reader["dop_parent"].ToString(),
                            reader["dop_country"].ToString(),
                            reader["dop_street2"].ToString(),
                            reader["dop_gender"].ToString(),
                            reader["dop_comments"].ToString(),
                            reader["dop_other"].ToString(),
                            reader["dop_other2"].ToString(),
                            reader["dop_emergency_name"].ToString(),
                            reader["dop_emergency_phone"].ToString()
                            );
                    }
                }
            }
            return output;
        }

        public void SetPrintOption(int eventId, int print)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE kiosk SET kiosk_print_new=@print WHERE event_id=@eventId;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("print", print),
                    new SQLiteParameter("eventId", eventId)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public int GetPrintOption(int eventId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM kiosk WHERE event_id=@eventId";
            command.Parameters.Add(new SQLiteParameter("eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            int outval = 0;
            if (reader.Read())
            {
                try
                {
                    outval = Convert.ToInt32(reader["kiosk_print_new"]);
                }
                catch
                {
                    outval = 0;
                }
            }
            return outval;
        }

        /*
         * Bib Chip Associations
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO bib_chip_assoc (event_id, bib, chip) VALUES (@eventId, @bib, @chip);";
                foreach (BibChipAssociation item in assoc)
                {
                    Log.D("Event id " + eventId + " Bib " + item.Bib + " Chip " + item.Chip);
                    command.Parameters.AddRange(new SQLiteParameter[]
                    {
                        new SQLiteParameter("@eventId", eventId),
                        new SQLiteParameter("@bib", item.Bib),
                        new SQLiteParameter("@chip", item.Chip),
                    });
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public List<BibChipAssociation> GetBibChips()
        {
            List<BibChipAssociation> output = new List<BibChipAssociation>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bib_chip_assoc";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new BibChipAssociation
                {
                    EventId = Convert.ToInt32(reader["event_id"]),
                    Bib = Convert.ToInt32(reader["bib"]),
                    Chip = Convert.ToInt32(reader["chip"])
                });
            }
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            List<BibChipAssociation> output = new List<BibChipAssociation>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bib_chip_assoc WHERE event_id=@eventId";
            command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new BibChipAssociation
                {
                    EventId = Convert.ToInt32(reader["event_id"]),
                    Bib = Convert.ToInt32(reader["bib"]),
                    Chip = Convert.ToInt32(reader["chip"])
                });
            }
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, int chip)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM bib_chip_assoc WHERE event_id=@event AND chip=@chip;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@chip", chip) });
            command.ExecuteNonQuery();
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            if (assoc != null) RemoveBibChipAssociation(assoc.EventId, assoc.Chip);
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (BibChipAssociation b in assocs)
                {
                    RemoveBibChipAssociation(b);
                }
                transaction.Commit();
            }
        }

        /*
         * Chip Reads
         */

        public void AddChipRead(ChipRead read)
        {
            Log.D("Database - Add chip read.");
            Log.D("Box " + read.Box + " Antenna " + read.Antenna + " Chip " + read.ChipNumber +
                " Time " + read.Time.ToLongTimeString() + " " + read.Time.ToLongDateString());
        }

        public List<ChipRead> GetChipReads()
        {
            throw new NotImplementedException();
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            throw new NotImplementedException();
        }

        /*
         * Settings
         */

        public void SetServerName(string name)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE settings SET name=@name";
                command.Parameters.Add(new SQLiteParameter("@name", name));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public string GetServerName()
        {
            String output = "Northwest Endurance Events";
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM settings;";
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = reader["name"].ToString();
            }
            return output;
        }

        public AppSetting GetAppSetting(string name)
        {
            AppSetting output = null;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM app_settings WHERE setting=@name";
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
            return output;
        }

        public void SetAppSetting(string n, string v)
        {
            AppSetting setting = new AppSetting()
            {
                name = n,
                value = v
            };
            SetAppSetting(setting);
        }

        public void SetAppSetting(AppSetting setting)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO app_settings (setting, value)" +
                    " VALUES (@name,@value)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", setting.name),
                new SQLiteParameter("@value", setting.value) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        /*
         * Bib Groups
         */
        public void AddBibGroup(int eventId, BibGroup group)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO bib_group (event_id, bib_group_number, bib_group_name) " +
                    "VALUES (@event, @number, @name);";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@number", group.Number),
                    new SQLiteParameter("@name", group.Name)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<BibGroup> GetBibGroups(int eventId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bib_group WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId)
            });
            List<BibGroup> output = new List<BibGroup>();
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new BibGroup()
                {
                    EventId = Convert.ToInt32(reader["event_id"]),
                    Number = Convert.ToInt32(reader["bib_group_number"]),
                    Name = reader["bib_group_name"].ToString()
                });
            }
            return output;
        }

        public void RemoveBibGroup(BibGroup group)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM available_bibs WHERE event_id=@event AND bib_group_number=@number;" +
                "DELETE FROM bib_group WHERE event_id=@event AND bib_group_number=@number;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", group.EventId),
                new SQLiteParameter("@number", group.Number)
            });
            command.ExecuteNonQuery();
        }

        /*
         * Bibs
         */
        public void AddBibs(int eventId, int group, List<int> bibs)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (int bib in bibs)
                {
                    AddBib(eventId, group, bib);
                }
                transaction.Commit();
            }
        }

        public void AddBibs(int eventId, List<AvailableBib> bibs)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AvailableBib bib in bibs)
                {
                    AddBib(eventId, bib.GroupNumber, bib.Bib);
                }
                transaction.Commit();
            }
        }

        public void AddBib(int eventId, int group, int bib)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO available_bibs (event_id, bib_group_number, bib) " +
                "VALUES (@event, @group, @bib);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@group", group),
                new SQLiteParameter("@bib", bib)
            });
            command.ExecuteNonQuery();
        }

        public List<AvailableBib> GetBibs(int eventId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT a.bib_group_number as bib_group_number, a.event_id as event_id," +
                " a.bib as bib, b.bib_group_name as bib_group_name" +
                " FROM available_bibs a LEFT JOIN bib_group b ON a.event_id=b.event_id AND b.bib_group_number=a.bib_group_number" +
                " WHERE a.event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId)
            });
            List<AvailableBib> output = new List<AvailableBib>();
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new AvailableBib(Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["bib_group_number"]),
                    reader["bib_group_name"].ToString(), Convert.ToInt32(reader["bib"])));
            }
            return output;
        }

        public int LargestBib(int eventId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MAX(bib) as max_bib FROM available_bibs WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId)
            });
            int largest = -1;
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                if (!(reader["max_bib"] is DBNull)) largest = Convert.ToInt32(reader["max_bib"]);
            }
            return largest;
        }

        public void RemoveBib(int eventId, int bib)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM available_bibs WHERE event_id=@event AND bib=@bib;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@bib", bib)
            });
            command.ExecuteNonQuery();
        }

        public void RemoveBibs(List<AvailableBib> bibs)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AvailableBib bib in bibs)
                {
                    RemoveBib(bib.EventId, bib.Bib);
                }
                transaction.Commit();
            }
        }

        /*
         * Age Group Functions
         */
        public void AddAgeGroup(AgeGroup group)
        {
            using (var transaction = connection.BeginTransaction())
            {
                AddAgeGroupInternal(group);
                transaction.Commit();
            }
        }

        public void AddAgeGroups(List<AgeGroup> groups)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AgeGroup group in groups)
                {
                    AddAgeGroupInternal(group);
                }
                transaction.Commit();
            }
        }

        private void AddAgeGroupInternal(AgeGroup group)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO age_groups (event_id, division_id, start_age, end_age)" +
                " VALUES (@event, @division, @start, @end);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", group.EventId),
                    new SQLiteParameter("@division", group.DivisionId),
                    new SQLiteParameter("@start", group.StartAge),
                    new SQLiteParameter("@end", group.EndAge)
            });
            command.ExecuteNonQuery();
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE age_groups SET event_id=@event, division_id=@division, " +
                    "start_age=@start, end_age=@end WHERE group_id=@group;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", group.EventId),
                    new SQLiteParameter("@division", group.DivisionId),
                    new SQLiteParameter("@start", group.StartAge),
                    new SQLiteParameter("@end", group.EndAge),
                    new SQLiteParameter("@group", group.GroupId)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM age_groups WHERE group_id=@group;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@group", group.GroupId)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void RemoveAgeGroups(int eventId, int divisionId)
        {

            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM age_groups WHERE event_id=@event AND division_id=@division;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@division", divisionId),
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", eventId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = new List<AgeGroup>();
            while (reader.Read())
            {
                output.Add(new AgeGroup(Convert.ToInt32(reader["group_id"]), Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_id"]), Convert.ToInt32(reader["start_age"]), Convert.ToInt32(reader["end_age"])));
            }
            return output;
        }
    }
}
