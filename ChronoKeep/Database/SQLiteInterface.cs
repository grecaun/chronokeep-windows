using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChronoKeep.Database;
using ChronoKeep.Objects;

namespace ChronoKeep
{
    class SQLiteInterface : IDBInterface
    {
        private readonly int version = 40;
        readonly string connectionInfo;
        readonly Mutex mutex = new Mutex();

        public SQLiteInterface(String info)
        {
            connectionInfo = info;
        }

        public void Initialize()
        {
            ArrayList queries = new ArrayList();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                    versionChecker.Close();
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
                    "chip VARCHAR NOT NULL," +
                    "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                    ");");
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
                    "event_timing_system VARCHAR NOT NULL DEFAULT '" + Constants.Settings.TIMING_RFID + "'," +
                    "event_type INTEGER NOT NULL DEFAULT " + Constants.Timing.EVENT_TYPE_DISTANCE + "," +
                    "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                    ");");
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
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS kiosk (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "kiosk_waiver_text VARCHAR NOT NULL," +
                    "kiosk_print_new INTEGER DEFAULT 0," +
                    "UNIQUE (event_id) ON CONFLICT IGNORE" +
                    ");");
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
                    "division_end_offset_seconds INTEGER NOT NULL DEFAULT 0," +
                    "UNIQUE (division_name, event_id) ON CONFLICT IGNORE" +
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS timing_locations (" +
                    "location_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "location_name VARCHAR(100) NOT NULL," +
                    "location_max_occurances INTEGER NOT NULL DEFAULT 1," +
                    "location_ignore_within INTEGER NOT NULL DEFAULT -1," +
                    "UNIQUE (event_id, location_name) ON CONFLICT IGNORE" +
                    ");");
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
                    ");");
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
                    "eventspecific_status INT NOT NULL DEFAULT " + Constants.Timing.EVENTSPECIFIC_NOSHOW + "," +
                    "eventspecific_age_group_id INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYAGEGROUP + "," +
                    "eventspecific_age_group_name VARCHAR NOT NULL DEFAULT '0-110'," +
                    "UNIQUE (participant_id, event_id, division_id) ON CONFLICT REPLACE," +
                    "UNIQUE (event_id, eventspecific_bib) ON CONFLICT REPLACE" +
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS eventspecific_apparel (" +
                    "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                    "name VARCHAR NOT NULL," +
                    "value VARCHAR NOT NULL," +
                    "UNIQUE (eventspecific_id, name) ON CONFLICT IGNORE" +
                    ");");
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
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS chipreads (" +
                    "read_id INTEGER PRIMARY KEY," +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "read_status INTEGER NOT NULL DEFAULT 0," +
                    "location_id INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                    "read_chipnumber VARCHAR NOT NULL," +
                    "read_seconds INTEGER NOT NULL," +
                    "read_milliseconds INTEGER NOT NULL," +
                    "read_antenna INTEGER NOT NULL," +
                    "read_reader TEXT NOT NULL," +
                    "read_box TEXT NOT NULL," +
                    "read_logindex INTEGER NOT NULL," +
                    "read_rssi INTEGER NOT NULL," +
                    "read_isrewind INTEGER NOT NULL," +
                    "read_readertime TEXT NOT NULL," +
                    "read_starttime INTEGER NOT NULL," +
                    "read_time_seconds INTEGER NOT NULL," +
                    "read_time_milliseconds INTEGER NOT NULL," +
                    "read_split_seconds INTEGER NOT NULL DEFAULT 0," +
                    "read_split_milliseconds INTEGER NOT NULL DEFAULT 0," +
                    "read_bib INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_DUMMYBIB + "," +
                    "read_type INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_TYPE_CHIP + "," +
                    "UNIQUE (event_id, read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS time_results (" +
                    "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                    "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                    "read_id INTEGER NOT NULL REFERENCES chipreads(read_id)," +
                    "location_id INTEGER NOT NULL," +
                    "segment_id INTEGER NOT NULL DEFAULT " + Constants.Timing.SEGMENT_NONE + "," +
                    "timeresult_occurance INTEGER NOT NULL," +
                    "timeresult_time TEXT NOT NULL," +
                    "timeresult_splittime TEXT NOT NULL DEFAULT ''," +
                    "timeresult_chiptime TEXT NOT NULL," +
                    "timeresult_unknown_id TEXT NOT NULL DEFAULT ''," +
                    "timeresult_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                    "timeresult_age_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                    "timeresult_gender_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                    "timeresult_status INT NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_STATUS_NONE + "," +
                    "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance, timeresult_unknown_id) ON CONFLICT REPLACE" +
                    ");");
                queries.Add("CREATE TABLE IF NOT EXISTS settings (version INTEGER NOT NULL, name VARCHAR NOT NULL," +
                    " identifier VARCHAR NOT NULL); INSERT INTO settings (version, name, identifier) VALUES " +
                    "(" + version + ", 'Northwest Endurance Events', '" + Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "") + "');");
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
                    ");");
                queries.Add("INSERT INTO participants (participant_id, participant_first, participant_last," +
                    " participant_birthday) VALUES (0, 'J', 'Doe', '01/01/1901');");
                queries.Add("CREATE TABLE IF NOT EXISTS app_settings (" +
                    "setting VARCHAR NOT NULL," +
                    "value VARCHAR NOT NULL," +
                    "UNIQUE (setting) ON CONFLICT REPLACE" +
                    ");");
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
                    "end_age INTEGER NOT NULL," +
                    "last_group INTEGER DEFAULT " + Constants.Timing.AGEGROUPS_LASTGROUP_FALSE + " NOT NULL);");
                queries.Add("CREATE TABLE IF NOT EXISTS timing_systems (" +
                    "ts_identifier INTEGER PRIMARY KEY," +
                    "ts_ip TEXT NOT NULL," +
                    "ts_port INTEGER NOT NULL," +
                    "ts_location INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                    "ts_type TEXT NOT NULL," +
                    "UNIQUE (ts_ip, ts_location) ON CONFLICT REPLACE);");
                queries.Add("CREATE INDEX idx_eventspecific_bibs ON eventspecific(eventspecific_bib);");

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
                    versionChecker.Close();
                }
            }
            reader.Close();
            connection.Close();
            if (oldVersion == -1) Log.D("Unable to get a version number. Something is terribly wrong.");
            else if (oldVersion < version) UpdateDatabase(oldVersion, version);
            else if (oldVersion > version) UpdateClient(oldVersion, version);
        }

        private void UpdateClient(int dbVersion, int maxVersion)
        {
            throw new InvalidDatabaseVersion(dbVersion, maxVersion);
        }

        private void UpdateDatabase(int oldversion, int newversion)
        {
            Log.D("Database is version " + oldversion + " but it needs to be upgraded to version " + newversion);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                            "ALTER TABLE events ADD event_timing_system VARCHAR NOT NULL DEFAULT '" + Constants.Settings.TIMING_RFID + "';" +
                            "UPDATE settings SET version=25 WHERE version=24;";
                        command.ExecuteNonQuery();
                        goto case 25;
                    case 25:
                        Log.D("Upgrading from version 25.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE time_results; DROP TABLE chipreads;" +
                            "CREATE TABLE IF NOT EXISTS time_results (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "read_id INTEGER NOT NULL REFERENCES chipreads(read_id)," +
                                "location_id INTEGER NOT NULL," +
                                "timeresult_time INTEGER NOT NULL," +
                                "segment_id INTEGER NOT NULL DEFAULT -3," +
                                "timeresult_occurance INTEGER NOT NULL," +
                                "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance) ON CONFLICT IGNORE" +
                                ");" +
                            "CREATE TABLE IF NOT EXISTS chipreads (" +
                                "read_id INTEGER PRIMARY KEY," +
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
                                "read_time TEXT NOT NULL," +
                                "UNIQUE (event_id, read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                                ");" +
                            "CREATE TABLE IF NOT EXISTS timing_systems (" +
                                "ts_identifier INTEGER PRIMARY KEY," +
                                "ts_ip TEXT NOT NULL," +
                                "ts_port INTEGER NOT NULL," +
                                "ts_location INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                                "ts_type TEXT NOT NULL," +
                                "UNIQUE (ts_ip, ts_location) ON CONFLICT REPLACE);" +
                            "UPDATE settings SET version=26 WHERE version=25;";
                        command.ExecuteNonQuery();
                        goto case 26;
                    case 26:
                        Log.D("Upgrading from version 26.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE chipreads ADD read_bib INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_DUMMYBIB + ";" +
                            "ALTER TABLE chipreads ADD read_type INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_TYPE_CHIP + ";" +
                            "UPDATE settings SET version=27 WHERE version=26;";
                        command.ExecuteNonQuery();
                        goto case 27;
                    case 27:
                        Log.D("Upgrading from version 27.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE chipreads;" +
                            "CREATE TABLE IF NOT EXISTS chipreads (" +
                                "read_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "read_status INTEGER NOT NULL DEFAULT 0," +
                                "location_id INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                                "read_chipnumber INTEGER NOT NULL," +
                                "read_seconds INTEGER NOT NULL," +
                                "read_milliseconds INTEGER NOT NULL," +
                                "read_antenna INTEGER NOT NULL," +
                                "read_reader TEXT NOT NULL," +
                                "read_box TEXT NOT NULL," +
                                "read_logindex INTEGER NOT NULL," +
                                "read_rssi INTEGER NOT NULL," +
                                "read_isrewind INTEGER NOT NULL," +
                                "read_readertime TEXT NOT NULL," +
                                "read_starttime INTEGER NOT NULL," +
                                "read_time TEXT NOT NULL," +
                                "read_bib INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_DUMMYBIB + "," +
                                "read_type INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_TYPE_CHIP + "," +
                                "UNIQUE (event_id, read_chipnumber, read_bib, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                                "); UPDATE settings SET version=28 WHERE version=27;";
                        command.ExecuteNonQuery();
                        goto case 28;
                    case 28:
                        Log.D("Upgrading from version 28.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE time_results;" +
                            "CREATE TABLE IF NOT EXISTS time_results (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "read_id INTEGER NOT NULL REFERENCES chipreads(read_id)," +
                                "location_id INTEGER NOT NULL," +
                                "segment_id INTEGER NOT NULL DEFAULT " + Constants.Timing.SEGMENT_NONE + "," +
                                "timeresult_occurance INTEGER NOT NULL," +
                                "timeresult_time TEXT NOT NULL," +
                                "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance) ON CONFLICT REPLACE" +
                                "); UPDATE settings SET version=29 WHERE version=28;";
                        command.ExecuteNonQuery();
                        goto case 29;
                    case 29:
                        Log.D("Upgrading from version 29.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE time_results;" +
                            "CREATE TABLE IF NOT EXISTS time_results (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "read_id INTEGER NOT NULL REFERENCES chipreads(read_id)," +
                                "location_id INTEGER NOT NULL," +
                                "segment_id INTEGER NOT NULL DEFAULT " + Constants.Timing.SEGMENT_NONE + "," +
                                "timeresult_occurance INTEGER NOT NULL," +
                                "timeresult_time TEXT NOT NULL," +
                                "timeresult_unknown_id TEXT NOT NULL DEFAULT ''," +
                                "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance, " +
                                "timeresult_unknown_id) ON CONFLICT REPLACE" +
                                "); UPDATE settings SET version=30 WHERE version=29;";
                        command.ExecuteNonQuery();
                        goto case 30;
                    case 30:
                        Log.D("Upgrading from version 30.");
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE INDEX idx_eventspecific_bibs ON eventspecific(eventspecific_bib);" +
                            "UPDATE settings SET version=31 WHERE version=30;";
                        command.ExecuteNonQuery();
                        goto case 31;
                    case 31:
                        Log.D("Upgrading from version 31.");
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE time_results; UPDATE chipreads SET read_status=" +
                            Constants.Timing.CHIPREAD_STATUS_NONE + " WHERE read_status<>" +
                            Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE + ";" +
                            "CREATE TABLE IF NOT EXISTS time_results (" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                                "read_id INTEGER NOT NULL REFERENCES chipreads(read_id)," +
                                "location_id INTEGER NOT NULL," +
                                "segment_id INTEGER NOT NULL DEFAULT " + Constants.Timing.SEGMENT_NONE + "," +
                                "timeresult_occurance INTEGER NOT NULL," +
                                "timeresult_time TEXT NOT NULL," +
                                "timeresult_chiptime TEXT NOT NULL," +
                                "timeresult_unknown_id TEXT NOT NULL DEFAULT ''," +
                                "timeresult_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                                "timeresult_age_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                                "timeresult_gender_place INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYPLACE + "," +
                                "timeresult_status INT NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_STATUS_NONE + "," +
                                "UNIQUE (event_id, eventspecific_id, location_id, timeresult_occurance, timeresult_unknown_id) ON CONFLICT REPLACE" +
                                ");" +
                            "ALTER TABLE eventspecific ADD eventspecific_age_group_id INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYAGEGROUP + ";" +
                            "UPDATE settings SET version=32 WHERE version=31;";
                        command.ExecuteNonQuery();
                        goto case 32;
                    case 32:
                        Log.D("Upgrading from version 32.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD " +
                            "event_type INTEGER NOT NULL DEFAULT " + Constants.Timing.EVENT_TYPE_DISTANCE + ";" +
                            "ALTER TABLE divisions ADD division_end_offset_seconds INTEGER NOT NULL DEFAULT 0;" +
                            "UPDATE settings SET version=33 WHERE version=32;";
                        command.ExecuteNonQuery();
                        goto case 33;
                    case 33:
                        Log.D("Upgrading from version 33.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE chipreads RENAME TO chipreads_old;" +
                            "CREATE TABLE IF NOT EXISTS chipreads (" +
                            "read_id INTEGER PRIMARY KEY," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "read_status INTEGER NOT NULL DEFAULT 0," +
                            "location_id INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                            "read_chipnumber INTEGER NOT NULL," +
                            "read_seconds INTEGER NOT NULL," +
                            "read_milliseconds INTEGER NOT NULL," +
                            "read_antenna INTEGER NOT NULL," +
                            "read_reader TEXT NOT NULL," +
                            "read_box TEXT NOT NULL," +
                            "read_logindex INTEGER NOT NULL," +
                            "read_rssi INTEGER NOT NULL," +
                            "read_isrewind INTEGER NOT NULL," +
                            "read_readertime TEXT NOT NULL," +
                            "read_starttime INTEGER NOT NULL," +
                            "read_time_seconds INTEGER NOT NULL," +
                            "read_time_milliseconds INTEGER NOT NULL," +
                            "read_bib INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_DUMMYBIB + "," +
                            "read_type INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_TYPE_CHIP + "," +
                            "UNIQUE (event_id, read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                            ");";
                        command.ExecuteNonQuery();
                        command = connection.CreateCommand();
                        command.CommandText = "SELECT * FROM chipreads_old;";
                        SQLiteDataReader reader = command.ExecuteReader();
                        List<ChipRead> output = new List<ChipRead>();
                        while (reader.Read())
                        {
                            output.Add(new ChipRead(
                                Convert.ToInt32(reader["read_id"]),
                                Convert.ToInt32(reader["event_id"]),
                                Convert.ToInt32(reader["read_status"]),
                                Convert.ToInt32(reader["location_id"]),
                                Convert.ToInt64(reader["read_chipnumber"]),
                                Convert.ToInt64(reader["read_seconds"]),
                                Convert.ToInt32(reader["read_milliseconds"]),
                                Convert.ToInt32(reader["read_antenna"]),
                                reader["read_rssi"].ToString(),
                                Convert.ToInt32(reader["read_isrewind"]),
                                reader["read_reader"].ToString(),
                                reader["read_box"].ToString(),
                                reader["read_readertime"].ToString(),
                                Convert.ToInt32(reader["read_starttime"]),
                                Convert.ToInt32(reader["read_logindex"]),
                                DateTime.ParseExact(reader["read_time"].ToString(), "yyyy-MM-dd HH:mm:ss.fff", null),
                                Convert.ToInt32(reader["read_bib"]),
                                Convert.ToInt32(reader["read_type"])
                                ));
                        }
                        reader.Close();
                        foreach (ChipRead read in output)
                        {
                            AddChipReadInternal(read, connection);
                        }
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE chipreads_old; UPDATE settings SET version=34 WHERE version=33;";
                        command.ExecuteNonQuery();
                        goto case 34;
                    case 34:
                        Log.D("Upgrading from version 34.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE time_results ADD " +
                            "timeresult_splittime TEXT NOT NULL DEFAULT '';" +
                            "UPDATE settings SET version=35 WHERE version=34;";
                        command.ExecuteNonQuery();
                        goto case 35;
                    case 35:
                        Log.D("Upgrading from version 35.");
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE bib_chip_assoc RENAME TO " +
                            "old_bib_chip_assoc; ALTER TABLE chipreads RENAME TO " +
                            "old_chipreads; CREATE TABLE IF NOT EXISTS chipreads (" +
                                "read_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "read_status INTEGER NOT NULL DEFAULT 0," +
                                "location_id INTEGER NOT NULL REFERENCES timing_locations(location_id)," +
                                "read_chipnumber VARCHAR NOT NULL," +
                                "read_seconds INTEGER NOT NULL," +
                                "read_milliseconds INTEGER NOT NULL," +
                                "read_antenna INTEGER NOT NULL," +
                                "read_reader TEXT NOT NULL," +
                                "read_box TEXT NOT NULL," +
                                "read_logindex INTEGER NOT NULL," +
                                "read_rssi INTEGER NOT NULL," +
                                "read_isrewind INTEGER NOT NULL," +
                                "read_readertime TEXT NOT NULL," +
                                "read_starttime INTEGER NOT NULL," +
                                "read_time_seconds INTEGER NOT NULL," +
                                "read_time_milliseconds INTEGER NOT NULL," +
                                "read_split_seconds INTEGER NOT NULL DEFAULT 0," +
                                "read_split_milliseconds INTEGER NOT NULL DEFAULT 0," +
                                "read_bib INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_DUMMYBIB + "," +
                                "read_type INTEGER NOT NULL DEFAULT " + Constants.Timing.CHIPREAD_TYPE_CHIP + "," +
                                "UNIQUE (event_id, read_chipnumber, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                                ");" +
                            "CREATE TABLE IF NOT EXISTS bib_chip_assoc(" +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "bib INTEGER NOT NULL," +
                                "chip VARCHAR NOT NULL," +
                                "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                                ");";
                        command.ExecuteNonQuery();
                        command.CommandText = "SELECT * FROM old_bib_chip_assoc;";
                        SQLiteDataReader bibreader = command.ExecuteReader();
                        List<BibChipAssociation> assocs = new List<BibChipAssociation>();
                        while (bibreader.Read())
                        {
                            assocs.Add(new BibChipAssociation()
                            {
                                EventId = Convert.ToInt32(bibreader["event_id"]),
                                Bib = Convert.ToInt32(bibreader["bib"]),
                                Chip = bibreader["chip"].ToString()
                            });
                        }
                        bibreader.Close();
                        command.CommandText = "INSERT INTO bib_chip_assoc (event_id, bib, chip) VALUES (@eventId, @bib, @chip);";
                        foreach (BibChipAssociation a in assocs)
                        {
                                command.Parameters.AddRange(new SQLiteParameter[]
                                {
                                    new SQLiteParameter("@eventId", a.EventId),
                                    new SQLiteParameter("@bib", a.Bib),
                                    new SQLiteParameter("@chip", a.Chip),
                                });
                                command.ExecuteNonQuery();
                        }
                        command.CommandText = "SELECT * FROM old_chipreads;";
                        bibreader = command.ExecuteReader();
                        List<ChipRead> reads = new List<ChipRead>();
                        DateTime time = DateTime.Now;
                        while (bibreader.Read())
                        {
                            reads.Add(new ChipRead(
                                Convert.ToInt32(bibreader["read_id"]),
                                Convert.ToInt32(bibreader["event_id"]),
                                Convert.ToInt32(bibreader["read_status"]),
                                Convert.ToInt32(bibreader["location_id"]),
                                bibreader["read_chipnumber"].ToString(),
                                Convert.ToInt64(bibreader["read_seconds"]),
                                Convert.ToInt32(bibreader["read_milliseconds"]),
                                Convert.ToInt32(bibreader["read_antenna"]),
                                bibreader["read_rssi"].ToString(),
                                Convert.ToInt32(bibreader["read_isrewind"]),
                                bibreader["read_reader"].ToString(),
                                bibreader["read_box"].ToString(),
                                bibreader["read_readertime"].ToString(),
                                Convert.ToInt32(bibreader["read_starttime"]),
                                Convert.ToInt32(bibreader["read_logindex"]),
                                Convert.ToInt64(bibreader["read_time_seconds"]),
                                Convert.ToInt32(bibreader["read_time_milliseconds"]),
                                Convert.ToInt32(bibreader["read_bib"]),
                                Convert.ToInt32(bibreader["read_type"]),
                                Constants.Timing.CHIPREAD_DUMMYBIB,
                                "",
                                "",
                                time,
                                ""
                                ));
                        }
                        bibreader.Close();
                        foreach (ChipRead read in reads)
                        {
                            AddChipReadInternal(read, connection);
                        }
                        command.CommandText = "DROP TABLE old_chipreads; DROP TABLE old_bib_chip_assoc;" +
                            "UPDATE settings SET version=36 WHERE version=35;";
                        command.ExecuteNonQuery();
                        goto case 36;
                    case 36:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE eventspecific RENAME TO eventspecific_old;" +
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
                                "eventspecific_registration_date VARCHAR NOT NULL DEFAULT ''," +
                                "eventspecific_age_group_id INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYAGEGROUP + "," +
                                "UNIQUE (participant_id, event_id, division_id) ON CONFLICT REPLACE," +
                                "UNIQUE (event_id, eventspecific_bib) ON CONFLICT REPLACE" +
                                ");" +
                            "INSERT INTO eventspecific SELECT * FROM eventspecific_old;" +
                            "DROP TABLE eventspecific_old;" +
                            "UPDATE settings SET version=37 WHERE version=36;";
                        command.ExecuteNonQuery();
                        goto case 37;
                    case 37:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE eventspecific ADD " +
                            "eventspecific_status INT NOT NULL DEFAULT " + Constants.Timing.EVENTSPECIFIC_NOSHOW + ";" +
                            "UPDATE settings SET version=38 WHERE version=37;";
                        command.ExecuteNonQuery();
                        goto case 38;
                    case 38:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE age_groups ADD " +
                            "last_group INTEGER DEFAULT " + Constants.Timing.AGEGROUPS_LASTGROUP_FALSE + " NOT NULL;" +
                            "UPDATE settings SET version=39 WHERE version=38;";
                        command.ExecuteNonQuery();
                        goto case 39;
                    case 39:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE eventspecific ADD COLUMN eventspecific_age_group_name VARCHAR NOT NULL DEFAULT '0-110';" +
                            "UPDATE settings SET version=40 WHERE version=39;";
                        command.ExecuteNonQuery();
                        break;
                }
                transaction.Commit();
                connection.Close();
            }
        }

        /*
         * Divisions
         */

        private void AddDivisionInternal(Division div, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (division_name, event_id, division_cost, division_distance, division_distance_unit," +
                "division_start_location, division_start_within, division_finish_location, division_finish_occurance, division_wave, bib_group_number," +
                "division_start_offset_seconds, division_start_offset_milliseconds, division_end_offset_seconds) " +
                "values (@name,@event_id,@cost,@distance,@unit,@startloc,@startwithin,@finishloc,@finishocc,@wave,@bgn,@soffsec,@soffmill,@endSec)";
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
                new SQLiteParameter("@endSec", div.EndSeconds)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void AddDivision(Division div)
        {
            Log.D("Attempting to grab Mutex: ID 1");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 1");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AddDivisionInternal(div, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddDivisions(List<Division> divisions)
        {
            Log.D("Attempting to grab Mutex: ID 117");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 117");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            foreach (Division div in divisions)
            {
                AddDivisionInternal(div, connection);
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveDivision(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 2");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 2");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = " DELETE FROM segments WHERE division_id=@id; DELETE FROM divisions WHERE division_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveDivision(Division div)
        {
            RemoveDivision(div.Identifier);
        }

        public void UpdateDivision(Division div)
        {
            Log.D("Attempting to grab Mutex: ID 3");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 3");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE divisions SET division_name=@name, event_id=@event, division_cost=@cost, division_distance=@distance," +
                "division_distance_unit=@unit, division_start_location=@startloc, division_start_within=@within, division_finish_location=@finishloc," +
                "division_finish_occurance=@occurance, division_wave=@wave, bib_group_number=@bgn, division_start_offset_seconds=@soffsec, " +
                "division_start_offset_milliseconds=@soffmill, division_end_offset_seconds=@endSec WHERE division_id=@id";
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
                new SQLiteParameter("@id", div.Identifier),
                new SQLiteParameter("@endSec", div.EndSeconds)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Division> GetDivisions()
        {
            Log.D("Attempting to grab Mutex: ID 4");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 4");
                return new List<Division>();
            }
            List<Division> output = new List<Division>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM divisions";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"])
                    ));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Division> GetDivisions(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 5");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 5");
                return new List<Division>();
            }
            List<Division> output = new List<Division>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            if (eventId < 0)
            {
                mutex.ReleaseMutex();
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
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = commandTxt;
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"])
                    ));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetDivisionID(Division div)
        {
            Log.D("Attempting to grab Mutex: ID 6");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 6");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Division GetDivision(int divId)
        {
            Log.D("Attempting to grab Mutex: ID 7");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 7");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM divisions WHERE division_id=@div";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@div", divId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            Division output = null;
            if (reader.Read())
            {
                output = new Division(Convert.ToInt32(reader["division_id"]),
                    reader["division_name"].ToString(),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_cost"]),
                    Convert.ToDouble(reader["division_distance"]),
                    Convert.ToInt32(reader["division_distance_unit"]),
                    Convert.ToInt32(reader["division_finish_location"]),
                    Convert.ToInt32(reader["division_finish_occurance"]),
                    Convert.ToInt32(reader["division_start_location"]),
                    Convert.ToInt32(reader["division_start_within"]),
                    Convert.ToInt32(reader["division_wave"]),
                    Convert.ToInt32(reader["bib_group_number"]),
                    Convert.ToInt32(reader["division_start_offset_seconds"]),
                    Convert.ToInt32(reader["division_start_offset_milliseconds"]),
                    Convert.ToInt32(reader["division_end_offset_seconds"])
                    );
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            Log.D("Attempting to grab Mutex: ID 8");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 8");
                return;
            }
            Log.D(String.Format("Setting wave {0} for event {1}", wave, eventId));
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE divisions SET division_start_offset_seconds=@seconds," +
                    " division_start_offset_milliseconds=@milli WHERE event_id=@event AND division_wave=@wave;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@wave", wave),
                    new SQLiteParameter("@seconds", seconds),
                    new SQLiteParameter("@milli", milliseconds)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Events
         */

        public void AddEvent(Event anEvent)
        {
            Log.D("Attempting to grab Mutex: ID 9");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 9");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO events(event_name, event_date, event_shirt_optional, event_shirt_price," +
                "event_common_age_groups, event_common_start_finish, event_rank_by_gun, event_division_specific_segments, event_yearcode, " +
                "event_next_year_event_id, event_allow_early_start, event_early_start_difference, event_start_time_seconds, " +
                "event_start_time_milliseconds, event_timing_system, event_type)" +
                " values(@name,@date,@so,@price,@age,@start,@gun,@sepseg,@yearcode,@ny,@early,@diff,@startsec,@startmill,@system," +
                "@type)";
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
                new SQLiteParameter("@system", anEvent.TimingSystem),
                new SQLiteParameter("@type", anEvent.EventType)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveEvent(Event anEvent)
        {
            RemoveEvent(anEvent.Identifier);
        }

        public void RemoveEvent(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 10");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 10");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM eventspecific_apparel AS a WHERE EXISTS " +
                    "(SELECT * FROM eventspecific AS e WHERE a.eventspecific_id=e.eventspecific_id" +
                    " AND e.event_id=@event); DELETE FROM dayof_participant WHERE event_id=@event; DELETE FROM" +
                    " kiosk WHERE event_id=@event; DELETE FROM time_results WHERE event_id=@event;" +
                    "DELETE FROM bib_group WHERE event_id=@event; DELETE FROM bib_chip_assoc WHERE event_id=@event;" +
                    "DELETE FROM segments WHERE event_id=@event; DELETE FROM chipreads WHERE event_id=@event;" +
                    "DELETE FROM age_groups WHERE event_id=@event; DELETE FROM available_bibs WHERE event_id=@event;" +
                    "DELETE FROM divisions WHERE event_id=@event; DELETE FROM timing_locations WHERE event_id=@event;" +
                    "DELETE FROM eventspecific WHERE event_id=@event; DELETE FROM events WHERE event_id=@event;" +
                    "UPDATE events SET event_next_year_event_id='-1' WHERE event_next_year_event_id=@event";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateEvent(Event anEvent)
        {
            Log.D("Attempting to grab Mutex: ID 11");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 11");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE events SET event_name=@name, event_date=@date, event_next_year_event_id=@ny, event_shirt_optional=@so," +
                "event_shirt_price=@price, event_common_age_groups=@age, event_common_start_finish=@start, event_rank_by_gun=@gun, " +
                "event_division_specific_segments=@seg, event_yearcode=@yearcode, event_allow_early_start=@early, " +
                "event_early_start_difference=@diff, event_start_time_seconds=@startsec, event_start_time_milliseconds=@startmill, " +
                "event_timing_system=@system, event_type=@type WHERE event_id=@id";
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
                new SQLiteParameter("@system", anEvent.TimingSystem),
                new SQLiteParameter("@type", anEvent.EventType)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Event> GetEvents()
        {
            Log.D("Attempting to grab Mutex: ID 12");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 12");
                return new List<Event>();
            }
            List<Event> output = new List<Event>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Event(Convert.ToInt32(reader["event_id"]),
                    reader["event_name"].ToString(), reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_next_year_event_id"]),
                    Convert.ToInt32(reader["event_shirt_optional"]),
                    Convert.ToInt32(reader["event_shirt_price"]),
                    Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]),
                    Convert.ToInt32(reader["event_division_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]),
                    reader["event_yearcode"].ToString(),
                    Convert.ToInt32(reader["event_allow_early_start"]),
                    Convert.ToInt32(reader["event_early_start_difference"]),
                    Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]),
                    Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]),
                    Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString(),
                    Convert.ToInt32(reader["event_type"])
                    ));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetEventID(Event anEvent)
        {
            Log.D("Attempting to grab Mutex: ID 13");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 13");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Event GetCurrentEvent()
        {
            AppSetting CurEvent = GetAppSetting(Constants.Settings.CURRENT_EVENT);
            if (CurEvent == null)
            {
                return null;
            }
            return GetEvent(Convert.ToInt32(CurEvent.value));
        }

        public Event GetEvent(int id)
        {
            if (id < 0)
            {
                return null;
            }
            Log.D("Attempting to grab Mutex: ID 14");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 14");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id", id));
            SQLiteDataReader reader = command.ExecuteReader();
            Event output = null;
            if (reader.Read())
            {
                output = new Event(Convert.ToInt32(reader["event_id"]),
                    reader["event_name"].ToString(), reader["event_date"].ToString(),
                    Convert.ToInt32(reader["event_next_year_event_id"]),
                    Convert.ToInt32(reader["event_shirt_optional"]),
                    Convert.ToInt32(reader["event_shirt_price"]),
                    Convert.ToInt32(reader["event_common_age_groups"]),
                    Convert.ToInt32(reader["event_common_start_finish"]),
                    Convert.ToInt32(reader["event_division_specific_segments"]),
                    Convert.ToInt32(reader["event_rank_by_gun"]),
                    reader["event_yearcode"].ToString(),
                    Convert.ToInt32(reader["event_allow_early_start"]),
                    Convert.ToInt32(reader["event_early_start_difference"]),
                    Convert.ToInt32(reader["event_finish_max_occurances"]),
                    Convert.ToInt32(reader["event_finish_ignore_within"]),
                    Convert.ToInt32(reader["event_start_window"]),
                    Convert.ToInt64(reader["event_start_time_seconds"]),
                    Convert.ToInt32(reader["event_start_time_milliseconds"]),
                    reader["event_timing_system"].ToString(),
                    Convert.ToInt32(reader["event_type"])
                    );
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<JsonOption> GetEventOptions(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 15");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 15");
                return new List<JsonOption>();
            }
            List<JsonOption> output = new List<JsonOption>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void SetEventOptions(int eventId, List<JsonOption> options)
        {
            Log.D("Attempting to grab Mutex: ID 16");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 16");
                return;
            }
            List<JsonOption> output = new List<JsonOption>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetStartWindow(Event anEvent)
        {
            Log.D("Attempting to grab Mutex: ID 17");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 17");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_start_window=@window WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@window", anEvent.StartWindow),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetFinishOptions(Event anEvent)
        {
            Log.D("Attempting to grab Mutex: ID 18");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 18");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_finish_max_occurances=@occ, event_finish_ignore_within=@ignore WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@occ", anEvent.FinishMaxOccurrences),
                new SQLiteParameter("@ignore", anEvent.FinishIgnoreWithin),
                new SQLiteParameter("@event", anEvent.Identifier)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Participants
         */

        public void AddParticipant(Participant person)
        {
            Log.D("Attempting to grab Mutex: ID 19");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 19");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                AddParticipantInternal(person, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddParticipants(List<Participant> people)
        {
            Log.D("Attempting to grab Mutex: ID 20");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 20");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant person in people)
                {
                    AddParticipantInternal(person, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void AddParticipantInternal(Participant person, SQLiteConnection connection)
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
                new SQLiteParameter("@gender", person.Gender) });
            command.ExecuteNonQuery();
            person.Identifier = GetParticipantIDInternal(person, connection);
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
                new SQLiteParameter("@nextYear", person.EventSpecific.NextYear) });
            command.ExecuteNonQuery();
        }

        private void RemoveParticipantInternal(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@0; DELETE FROM participant WHERE participant_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveParticipant(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 21");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 21");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoveParticipantInternal(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            RemoveParticipant(person.Identifier);
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            Log.D("Attempting to grab Mutex: ID 22");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 22");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant p in participants)
                {
                    RemoveParticipantInternal(p.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void RemoveEntryInternal(int eventId, int participantId, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@participant AND event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@participant", participantId) });
            command.ExecuteNonQuery();
        }

        public void RemoveEntry(int eventId, int participantId)
        {
            Log.D("Attempting to grab Mutex: ID 23");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 23");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoveEntryInternal(eventId, participantId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveEntry(Participant person)
        {
            RemoveEntry(person.EventIdentifier, person.Identifier);
        }

        public void RemoveEntries(List<Participant> people)
        {
            Log.D("Attempting to grab Mutex: ID 24");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 24");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant p in people)
                {
                    RemoveEntryInternal(p.EventIdentifier, p.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void UpdateParticipantInternal(Participant person, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
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
            command.CommandText = "UPDATE eventspecific SET division_id=@divid, eventspecific_bib=@bib, eventspecific_checkedin=@checkedin, " +
                "eventspecific_owes=@owes, eventspecific_other=@other, eventspecific_earlystart=@earlystart, eventspecific_next_year=@nextYear," +
                "eventspecific_comments=@comments, eventspecific_status=@status, eventspecific_age_group_name=@ageGroupName, eventspecific_age_group_id=@ageGroupId " +
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
                    new SQLiteParameter("@comments", person.EventSpecific.Comments),
                    new SQLiteParameter("@status", person.EventSpecific.Status),
                    new SQLiteParameter("@ageGroupName", person.EventSpecific.AgeGroupName),
                    new SQLiteParameter("@ageGroupId", person.EventSpecific.AgeGroupId)
                });
            command.ExecuteNonQuery();
        }

        public void UpdateParticipant(Participant person)
        {
            Log.D("Attempting to grab Mutex: ID 25");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 25");
                return;
            }
            person.FormatData();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                UpdateParticipantInternal(person, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateParticipants(List<Participant> participants)
        {
            Log.D("Attempting to grab Mutex: ID 26");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 26");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Participant person in participants)
                {
                    person.FormatData();
                    UpdateParticipantInternal(person, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void CheckInParticipant(int eventId, int identifier, int checkedIn)
        {
            Log.D("Attempting to grab Mutex: ID 27");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 27");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE eventspecific SET eventspecific_checkedin=@0 WHERE participant_id=@id AND event_id=@eventId";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", checkedIn),
                new SQLiteParameter("@id", identifier),
                new SQLiteParameter("@eventId", eventId)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void CheckInParticipant(Participant person)
        {
            CheckInParticipant((int)person.EventSpecific.EventIdentifier, person.Identifier, (int)person.EventSpecific.CheckedIn);
        }

        public void SetEarlyStartParticipant(int eventId, int identifier, int earlystart)
        {
            Log.D("Attempting to grab Mutex: ID 28");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 28");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE eventspecific SET eventspecific_earlystart=@earlystart WHERE event_id=@eventid AND participant_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@earlystart", earlystart),
                new SQLiteParameter("@id", identifier),
                new SQLiteParameter("@eventid", eventId)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetEarlyStartParticipant(Participant person)
        {
            SetEarlyStartParticipant((int)person.EventSpecific.EventIdentifier, person.Identifier, (int)person.EventSpecific.EarlyStart);
        }

        public List<Participant> GetParticipants()
        {
            Log.D("Getting all participants for all events.");
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "JOIN divisions d ON s.division_id = d.division_id ORDER BY p.participant_last ASC, p.participant_first ASC", -1, -1);
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "JOIN divisions d ON s.division_id = d.division_id " +
                "WHERE s.event_id=@event ORDER BY p.participant_last ASC, p.participant_first ASC", eventId, -1);
        }


        public List<Participant> GetParticipants(int eventId, int divisionId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants p " +
                "JOIN eventspecific s ON p.participant_id = s.participant_id " +
                "JOIN divisions d ON s.division_id = d.division_id " +
                "WHERE s.event_id=@event AND d.division_id=@division ORDER BY p.participant_last ASC, p.participant_first ASC", eventId, divisionId);
        }

        public List<Participant> GetParticipantsWorker(string query, int eventId, int divisionId)
        {
            Log.D("Attempting to grab Mutex: ID 29");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 29");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                        Convert.ToInt32(reader["eventspecific_next_year"]),
                        Convert.ToInt32(reader["eventspecific_status"]),
                        reader["eventspecific_age_group_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_age_group_id"])
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        private Participant GetParticipantWorker(SQLiteDataReader reader)
        {
            if (reader.Read())
            {
                return new Participant(
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
                        Convert.ToInt32(reader["eventspecific_next_year"]),
                        Convert.ToInt32(reader["eventspecific_status"]),
                        reader["eventspecific_age_group_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_age_group_id"])
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
            return null;
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            Log.D("Attempting to grab Mutex: ID 30");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 30");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p JOIN eventspecific AS s ON p.participant_id=s.participant_id" +
                " JOIN divisions AS d ON s.division_id=d.division_id WHERE s.event_id=@eventid " +
                "AND s.eventspecific_id=@eventSpecId";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventIdentifier));
            command.Parameters.Add(new SQLiteParameter("@eventSpecId", eventSpecificId));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipantBib(int eventIdentifier, int bib)
        {
            Log.D("Attempting to grab Mutex: ID 31");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 31");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p JOIN eventspecific AS s ON p.participant_id=s.participant_id" +
                " JOIN divisions AS d ON s.division_id=d.division_id WHERE s.event_id=@eventid " +
                "AND s.eventspecific_bib=@bib";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventIdentifier));
            command.Parameters.Add(new SQLiteParameter("@bib", bib));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipant(int eventId, int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 32");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 32");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d WHERE " +
                "p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id " +
                "AND p.participant_id=@partId";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            command.Parameters.Add(new SQLiteParameter("@partId", identifier));
            SQLiteDataReader reader = command.ExecuteReader();
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipant(int eventId, Participant unknown)
        {
            Log.D("Attempting to grab Mutex: ID 33");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 33");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            if (unknown.EventSpecific.Chip != -1)
            {
                command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d, " +
                    "bib_chip_assoc as b WHERE p.participant_id=s.participant_id AND s.event_id=@eventid " +
                    "AND d.division_id=s.division_id AND " +
                    "s.eventspecific_bib=b.bib AND b.chip=@chip AND b.event_id=s.event_id;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@chip", unknown.EventSpecific.Chip),
                });
            }
            else
            {
                command.CommandText = "SELECT * FROM participants AS p, eventspecific AS s, divisions AS d " +
                    "WHERE p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id " +
                    "AND p.participant_first=@first AND p.participant_last=@last AND p.participant_street=@street " +
                    "AND p.participant_city=@city AND p.participant_state=@state AND p.participant_zip=@zip " +
                    "AND p.participant_birthday=@birthday";
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
            Participant output = GetParticipantWorker(reader);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetParticipantID(Participant person)
        {
            Log.D("Attempting to grab Mutex: ID 34");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 34");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = GetParticipantIDInternal(person, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        private int GetParticipantIDInternal(Participant person, SQLiteConnection connection)
        {
            int output = -1;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT participant_id FROM participants WHERE participant_first=@first AND" +
                " participant_last=@last AND participant_street=@street AND " +
                "participant_zip=@zip AND participant_birthday=@birthday";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@first", person.FirstName),
                new SQLiteParameter("@last", person.LastName),
                new SQLiteParameter("@street", person.Street),
                new SQLiteParameter("@zip", person.Zip),
                new SQLiteParameter("@birthday", person.Birthdate)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                try
                {
                    output = Convert.ToInt32(reader["participant_id"]);
                }
                catch
                {
                    output = -1;
                }
            }
            reader.Close();
            return output;
        }

        /*
         * Timing Locations
         */

        private void AddTimingLocationInternal(TimingLocation tl, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timing_locations (event_id, location_name, location_max_occurances, location_ignore_within) " +
                "VALUES (@event,@name,@max,@ignore)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tl.EventIdentifier),
                new SQLiteParameter("@name", tl.Name),
                new SQLiteParameter("@max", tl.MaxOccurrences),
                new SQLiteParameter("@ignore", tl.IgnoreWithin) });
            command.ExecuteNonQuery();
        }

        public void AddTimingLocation(TimingLocation tl)
        {
            Log.D("Attempting to grab Mutex: ID 35");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 35");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AddTimingLocationInternal(tl, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }
        public void AddTimingLocations(List<TimingLocation> locations)
        {
            Log.D("Attempting to grab Mutex: ID 118");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 118");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            foreach (TimingLocation tl in locations)
            {
                AddTimingLocationInternal(tl, connection);
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveTimingLocation(TimingLocation tl)
        {
            RemoveTimingLocation(tl.Identifier);
        }

        public void RemoveTimingLocation(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 36");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 36");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM timing_locations WHERE location_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateTimingLocation(TimingLocation tl)
        {
            Log.D("Attempting to grab Mutex: ID 37");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 37");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 38");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 38");
                return new List<TimingLocation>();
            }
            List<TimingLocation> output = new List<TimingLocation>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM timing_locations WHERE event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingLocation(Convert.ToInt32(reader["location_id"]), Convert.ToInt32(reader["event_id"]),
                    reader["location_name"].ToString(), Convert.ToInt32(reader["location_max_occurances"]), Convert.ToInt32(reader["location_ignore_within"])));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetTimingLocationID(TimingLocation tl)
        {
            Log.D("Attempting to grab Mutex: ID 39");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 39");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Segment
         */

        private void AddSegmentInternal(Segment seg, SQLiteConnection connection)
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
        }

        public void AddSegment(Segment seg)
        {
            Log.D("Attempting to grab Mutex: ID 40");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 40");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                AddSegmentInternal(seg, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddSegments(List<Segment> segments)
        {
            Log.D("Attempting to grab Mutex: ID 41");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 41");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Segment seg in segments)
                {
                    AddSegmentInternal(seg, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void RemoveSegmentInternal(int identifier, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM segments WHERE segment_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@id", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveSegment(Segment seg)
        {
            Log.D("Attempting to grab Mutex: ID 42");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 42");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoveSegmentInternal(seg.Identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveSegment(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 43");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 43");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                RemoveSegmentInternal(identifier, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveSegments(List<Segment> segments)
        {
            Log.D("Attempting to grab Mutex: ID 44");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 44");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (Segment seg in segments)
                {
                    RemoveSegmentInternal(seg.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void UpdateSegmentInternal(Segment seg, SQLiteConnection connection)
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
        }

        public void UpdateSegment(Segment seg)
        {
            Log.D("Attempting to grab Mutex: ID 45");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 45");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                UpdateSegmentInternal(seg, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateSegments(List<Segment> segments)
        {
            Log.D("Attempting to grab Mutex: ID 46");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 46");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Log.D("Segments count is " + segments.Count);
                foreach (Segment seg in segments)
                {
                    Log.D("Division ID " + seg.DivisionId + " Segment Name " + seg.Name + " Segment ID " + seg.Identifier);
                    UpdateSegmentInternal(seg, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public int GetSegmentId(Segment seg)
        {
            Log.D("Attempting to grab Mutex: ID 47");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 47");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM segments WHERE event_id=@event, division_id=@division, location_id=@location, occurance=@occurance;";
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["segment_id"]);
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Segment> GetSegments(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 48");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 48");
                return new List<Segment>();
            }
            List<Segment> output = new List<Segment>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void ResetSegments(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 49");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 49");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM segments WHERE event_id=@id";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@id", eventId) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public int GetMaxSegments(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 50");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 50");
                return 0;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT MAX(seg_count) max_segments FROM" +
                " (SELECT COUNT(segment_id) seg_count, division_id FROM segments" +
                " WHERE event_id=@event GROUP BY division_id);";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            int output = 0;
            if (reader.Read() && reader["max_segments"] != DBNull.Value)
            {
                output = Convert.ToInt32(reader["max_segments"]);
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Results
         */

        private void AddTimingResultInternal(TimeResult tr, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO time_results (event_id, eventspecific_id, location_id, segment_id, " +
                "timeresult_occurance, timeresult_time, timeresult_unknown_id, read_id, timeresult_chiptime," +
                "timeresult_place, timeresult_age_place, timeresult_gender_place," +
                "timeresult_status, timeresult_splittime)" +
                " VALUES (@event,@specific,@location,@segment,@occ,@time,@unknown,@read,@chip,@place,@agplace," +
                "@gendplace,@status,@split)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventIdentifier),
                new SQLiteParameter("@specific", tr.EventSpecificId),
                new SQLiteParameter("@location", tr.LocationId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@occ", tr.Occurrence),
                new SQLiteParameter("@time", tr.Time),
                new SQLiteParameter("@unknown", tr.UnknownId),
                new SQLiteParameter("@read", tr.ReadId),
                new SQLiteParameter("@chip", tr.ChipTime),
                new SQLiteParameter("@place", tr.Place),
                new SQLiteParameter("@agplace", tr.AgePlace),
                new SQLiteParameter("@gendplace", tr.GenderPlace),
                new SQLiteParameter("@status", tr.Status),
                new SQLiteParameter("@split", tr.LapTime) });
            command.ExecuteNonQuery();
        }

        public void AddTimingResult(TimeResult tr)
        {
            Log.D("Attempting to grab Mutex: ID 51");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 51");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AddTimingResultInternal(tr, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            if (results.Count < 1)
            {
                return;
            }
            Log.D("Attempting to grab Mutex: ID 52");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 52");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (TimeResult result in results)
                {
                    AddTimingResultInternal(result, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            Log.D("Attempting to grab Mutex: ID 53");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 53");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM time_results WHERE eventspecific_id=@event AND location_id=@location AND " +
                "segment_id=@segment AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", tr.EventSpecificId),
                new SQLiteParameter("@segment", tr.SegmentId),
                new SQLiteParameter("@occurance", tr.Occurrence),
                new SQLiteParameter("@location", tr.LocationId) });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        private List<TimeResult> GetResultsInternal(SQLiteDataReader reader)
        {
            List<TimeResult> output = new List<TimeResult>();
            while (reader.Read())
            {
                output.Add(new TimeResult(
                    Convert.ToInt32(reader["event_id"]),
                    reader["eventspecific_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_id"]),
                    Convert.ToInt32(reader["location_id"]),
                    Convert.ToInt32(reader["segment_id"]),
                    reader["timeresult_time"].ToString(),
                    Convert.ToInt32(reader["timeresult_occurance"]),
                    reader["participant_first"] == DBNull.Value ? "" : reader["participant_first"].ToString(),
                    reader["participant_last"] == DBNull.Value ? "" : reader["participant_last"].ToString(),
                    reader["division_name"] == DBNull.Value ? "" : reader["division_name"].ToString(),
                    reader["eventspecific_bib"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_bib"]),
                    Convert.ToInt32(reader["read_id"]),
                    reader["timeresult_unknown_id"].ToString(),
                    Convert.ToInt64(reader["read_time_seconds"]),
                    Convert.ToInt32(reader["read_time_milliseconds"]),
                    reader["timeresult_chiptime"].ToString(),
                    Convert.ToInt32(reader["timeresult_place"]),
                    Convert.ToInt32(reader["timeresult_age_place"]),
                    Convert.ToInt32(reader["timeresult_gender_place"]),
                    reader["participant_gender"].ToString(),
                    Convert.ToInt32(reader["timeresult_status"]),
                    reader["eventspecific_earlystart"] == DBNull.Value ? 0 : Convert.ToInt32(reader["eventspecific_earlystart"]),
                    reader["timeresult_splittime"].ToString(),
                    reader["eventspecific_age_group_id"] == DBNull.Value ? -1 : Convert.ToInt32(reader["eventspecific_age_group_id"]),
                    reader["eventspecific_age_group_name"].ToString()
                    ));
            }
            reader.Close();
            return output;
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 54");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 54");
                return new List<TimeResult>();
            }
            Log.D("Getting timing results for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN divisions d ON d.division_id=e.division_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid;";
            command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 120");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 120");
                return new List<TimeResult>();
            }
            Log.D("Getting finish times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN divisions d ON d.division_id=e.division_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", Constants.Timing.SEGMENT_FINISH)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 55");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 55");
                return new List<TimeResult>();
            }
            Log.D("Getting start times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN divisions d ON d.division_id=e.division_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", Constants.Timing.SEGMENT_START)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            Log.D("Attempting to grab Mutex: ID 56");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 56");
                return new List<TimeResult>();
            }
            Log.D("Getting segment times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM time_results r " +
                "JOIN chipreads c ON c.read_id=r.read_id " +
                "LEFT JOIN (eventspecific e " +
                "JOIN participants p ON p.participant_id=e.participant_id " +
                "JOIN divisions d ON d.division_id=e.division_id) ON e.eventspecific_id=r.eventspecific_id " +
                "WHERE r.event_id=@eventid AND r.segment_id=@segment;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@eventid", eventId),
                new SQLiteParameter("@segment", segmentId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<TimeResult> output = GetResultsInternal(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateTimingResult(TimeResult oldResult, String newTime)
        {
            Log.D("Attempting to grab Mutex: ID 57");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 57");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE time_results SET timeresult_time=@time WHERE event_id=@event AND eventspecific_id=@eventspecific AND location_id=@location AND timeresult_occurance=@occurance";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@time", newTime),
                new SQLiteParameter("@event", oldResult.EventIdentifier),
                new SQLiteParameter("@eventspecific", oldResult.EventSpecificId),
                new SQLiteParameter("@location", oldResult.LocationId),
                new SQLiteParameter("@occurance", oldResult.Occurrence)});
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 58");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 58");
                return false;
            }
            Log.D("Checking for unprocessed reads.");
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM chipreads WHERE event_id=@event AND read_status=@status;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            long output = reader.GetInt64(0);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output != 0;
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 59");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 59");
                return false;
            }
            Log.D("Checking for unprocessed results.");
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM time_results " +
                "WHERE event_id=@event AND timeresult_status=@status " +
                "AND segment_id<>@start AND segment_id<>@none " +
                "AND eventspecific_id<>@dummy;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@start", Constants.Timing.SEGMENT_START),
                new SQLiteParameter("@dummy", Constants.Timing.TIMERESULT_DUMMYPERSON),
                new SQLiteParameter("@none", Constants.Timing.SEGMENT_NONE)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            long output = reader.GetInt64(0);
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output != 0;
        }

        /*
         * Reset options for time_results and chipreads
         */

        public void ResetTimingResultsEvent(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 60");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 60");
                return;
            }
            Log.D("Resetting timing results for event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM time_results WHERE event_id=@event;" +
                "UPDATE chipreads SET read_status=@status WHERE event_id=@event AND read_status!=@ignore AND read_status!=@dnf;" +
                "UPDATE eventspecific SET eventspecific_status=@estatus WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@ignore", Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE),
                new SQLiteParameter("@dnf", Constants.Timing.CHIPREAD_STATUS_DNF),
                new SQLiteParameter("estatus", Constants.Timing.EVENTSPECIFIC_NOSHOW)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*  These functions *work*, but are slower than just resetting the whole darn thing.
        public void ResetTimingResultsBib(int eventId, int bib)
        {
            Log.D("Attempting to grab Mutex: ID 61");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 61");
                return;
            }
            Log.D("Resetting timing results for bib " + bib + " and event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ResetTimingResultsBibInternal(eventId, bib, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void ResetTimingResultsBibInternal(int eventId, int bib, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM time_results WHERE event_id=@event AND" +
                " EXISTS (SELECT * FROM eventspecific s WHERE s.eventspecific_id=time_results.eventspecific_id" +
                " AND s.eventspecific_bib=@bib);" +
                "UPDATE chipreads SET read_status=@status WHERE chipreads.event_id=@event AND" +
                " (chipreads.read_bib=@bib OR EXISTS (SELECT * FROM bib_chip_assoc c WHERE chipreads.read_chipnumber=c.chip" +
                " AND c.bib=@bib)) AND chipreads.read_status<>@ignore;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@ignore", Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE),
                new SQLiteParameter("@bib", bib)
            });
            command.ExecuteNonQuery();
        }

        public void ResetTimingResultsChip(int eventId, string chip)
        {
            Log.D("Attempting to grab Mutex: ID 62");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 62");
                return;
            }
            Log.D("Resetting timing results for chip " + chip + " and event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ResetTimingResultsChipInternal(eventId, chip, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void ResetTimingResultsChipInternal(int eventId, string chip, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM time_results WHERE event_id=@event AND" +
                " EXISTS (SELECT * FROM eventspecific s JOIN bib_chip_assoc b ON b.bib=s.eventspecific_bib " +
                " WHERE s.eventspecific_id=time_results.eventspecific_id" +
                " AND b.chip=@chip);" +
                "UPDATE chipreads  SET read_status=@status WHERE event_id=@event AND" +
                " read_chipnumber=@chip AND read_status<>@ignore;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@ignore", Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE),
                new SQLiteParameter("@chip", chip)
            });
            command.ExecuteNonQuery();
        }

        public void ResetTimingResultsDivision(int eventId, int divisionId)
        {
            Log.D("Attempting to grab Mutex: ID 63");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 63");
                return;
            }
            Log.D("Resetting timing results for division " + divisionId + " and event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM time_results WHERE time_results.event_id=@event AND" +
                " EXISTS (SELECT * FROM eventspecific s WHERE s.eventspecific_id=time_results.eventspecific_id" +
                " AND s.division_id=@div);" +
                "UPDATE chipreads SET read_status=@status WHERE chipreads.event_id=@event AND" +
                " EXISTS (SELECT * FROM divisions d JOIN eventspecific s ON s.division_id=d.division_id " +
                " JOIN bib_chip_assoc b on b.bib=s.eventspecific_bib " +
                " WHERE d.event_id=@event AND d.division_id=@div AND (chipreads.read_bib=b.bib OR chipreads.read_chipnumber=b.chip)" +
                " ) AND chipreads.read_status<>@ignore;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@ignore", Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE),
                new SQLiteParameter("@div", divisionId)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        } //*/

        public void ResetTimingResultsPlacements(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 64");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 64");
                return;
            }
            Log.D("Resetting timing result placements for event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE time_results SET timeresult_status=@status WHERE event_id=@event;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Changes
         */

        public void AddChange(Participant newParticipant, Participant oldParticipant)
        {
            Log.D("Attempting to grab Mutex: ID 65");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 65");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Change> GetChanges()
        {
            Log.D("Attempting to grab Mutex: ID 66");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 66");
                return new List<Change>();
            }
            Log.D("Getting changes.");
            List<Change> output = new List<Change>();
            Hashtable divisions = new Hashtable();
            List<Division> divs = GetDivisions();
            foreach (Division d in divs)
            {
                divisions.Add(d.Identifier, d.Name);
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                            Convert.ToInt32(reader["new_next_year"]),
                            Constants.Timing.EVENTSPECIFIC_NOSHOW,
                            "0-110",
                            Constants.Timing.TIMERESULT_DUMMYAGEGROUP
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
                            Convert.ToInt32(reader["old_next_year"]),
                            Constants.Timing.EVENTSPECIFIC_NOSHOW,
                            "0-110",
                            Constants.Timing.TIMERESULT_DUMMYAGEGROUP
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Database Functions
         */

        public void HardResetDatabase()
        {
            Log.D("Attempting to grab Mutex: ID 67");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 67");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DROP TABLE timing_systems; DROP TABLE age_groups; DROP TABLE available_bibs;" +
                    "DROP TABLE bib_group; DROP TABLE settings; DROP TABLE app_settings; DROP TABLE changes; DROP TABLE chipreads;" +
                    "DROP TABLE time_results; DROP TABLE segments; DROP TABLE eventspecific_apparel; DROP TABLE eventspecific;" +
                    "DROP TABLE participants; DROP TABLE timing_locations; DROP TABLE divisions; DROP TABLE kiosk; DROP TABLE dayof_participant;" +
                    "DROP TABLE events; DROP TABLE bib_chip_assoc;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
            Initialize();
        }

        public void ResetDatabase()
        {
            Log.D("Attempting to grab Mutex: ID 68");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 68");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems; DELETE FROM age_groups; DELETE FROM available_bibs;" +
                    "DELETE FROM bib_group; DELETE FROM settings; DELETE FROM app_settings; DELETE FROM changes; DELETE FROM chipreads;" +
                    "DELETE FROM time_results; DELETE FROM segments; DELETE FROM eventspecific_apparel; DELETE FROM eventspecific;" +
                    "DELETE FROM participants; DELETE FROM timing_locations; DELETE FROM divisions; DELETE FROM kiosk; DELETE FROM dayof_participant;" +
                    "DELETE FROM events; DELETE FROM bib_chip_assoc;";
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Kiosk settings
         */

        private void AddDayOfParticipantInternal(DayOfParticipant part, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dayof_participant (event_id, division_id, dop_first, dop_last, dop_street, dop_city," +
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
        }

        public void AddDayOfParticipant(DayOfParticipant part)
        {
            Log.D("Attempting to grab Mutex: ID 69");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 69");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                AddDayOfParticipantInternal(part, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddDayOfParticipants(List<DayOfParticipant> participants)
        {
            Log.D("Attempting to grab Mutex: ID 119");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 119");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (DayOfParticipant part in participants)
                {
                    AddDayOfParticipantInternal(part, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<DayOfParticipant> GetDayOfParticipants(int eventId)
        {
            return InternalGetDayOfParticipants("SELECT * FROM dayof_participant WHERE event_id=@eventId;", eventId);
        }

        public List<DayOfParticipant> GetDayOfParticipants()
        {
            return InternalGetDayOfParticipants("SELECT * FROM dayof_participant;", -1);
        }

        private List<DayOfParticipant> InternalGetDayOfParticipants(String query, int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 70");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 70");
                return new List<DayOfParticipant>();
            }
            List<DayOfParticipant> output = new List<DayOfParticipant>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_id"]),
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public bool ApproveDayOfParticipant(int eventId, int identifier, int bib, int earlystart)
        {
            Log.D("Attempting to grab Mutex: ID 71");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 71");
                return false;
            }
            Participant newPart = null;
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                            Convert.ToInt32(reader["event_id"]),
                            Convert.ToInt32(reader["division_id"]),
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
                    reader.Close();
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
                connection.Close();
                mutex.ReleaseMutex();
                return true;
            }
            connection.Close();
            mutex.ReleaseMutex();
            return false;
        }

        public bool ApproveDayOfParticipant(DayOfParticipant part, int bib, int earlystart)
        {
            return ApproveDayOfParticipant(part.EventIdentifier, part.Identifier, bib, earlystart);
        }

        public void SetLiabilityWaiver(int eventId, string waiver)
        {
            Log.D("Attempting to grab Mutex: ID 72");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 72");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public string GetLiabilityWaiver(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 73");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 73");
                return "";
            }
            String output = "";
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM kiosk WHERE event_id=@eventId;";
            command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = reader["kiosk_waiver_text"].ToString();
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public DayOfParticipant GetDayOfParticipant(DayOfParticipant part)
        {
            Log.D("Attempting to grab Mutex: ID 74");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 74");
                return null;
            }
            DayOfParticipant output = null;
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                            Convert.ToInt32(reader["event_id"]),
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
                    reader.Close();
                }
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void SetPrintOption(int eventId, int print)
        {
            Log.D("Attempting to grab Mutex: ID 75");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 75");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public int GetPrintOption(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 76");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 76");
                return 0;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return outval;
        }

        /*
         * Bib Chip Associations
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            Log.D("Attempting to grab Mutex: ID 77");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 77");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<BibChipAssociation> GetBibChips()
        {
            Log.D("Attempting to grab Mutex: ID 78");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 78");
                return new List<BibChipAssociation>();
            }
            List<BibChipAssociation> output = new List<BibChipAssociation>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM bib_chip_assoc";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new BibChipAssociation
                {
                    EventId = Convert.ToInt32(reader["event_id"]),
                    Bib = Convert.ToInt32(reader["bib"]),
                    Chip = reader["chip"].ToString()
                });
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 79");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 79");
                return new List<BibChipAssociation>();
            }
            List<BibChipAssociation> output = new List<BibChipAssociation>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                    Chip = reader["chip"].ToString()
                });
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        private void RemoveBibChipAssociationInternal(int eventId, string chip, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM bib_chip_assoc WHERE event_id=@event AND chip=@chip;";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@chip", chip) });
            command.ExecuteNonQuery();
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("Attempting to grab Mutex: ID 80");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 80");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoveBibChipAssociationInternal(eventId, chip, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void RemoveBibChipAssociationInternal(BibChipAssociation assoc, SQLiteConnection connection)
        {
            if (assoc != null) RemoveBibChipAssociationInternal(assoc.EventId, assoc.Chip, connection);
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("Attempting to grab Mutex: ID 81");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 81");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            if (assoc != null) RemoveBibChipAssociationInternal(assoc.EventId, assoc.Chip, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("Attempting to grab Mutex: ID 82");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 82");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (BibChipAssociation b in assocs)
                {
                    RemoveBibChipAssociationInternal(b, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Chip Reads
         */

        private void AddChipReadInternal(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO chipreads (event_id, read_status, location_id, read_chipnumber, read_seconds," +
                "read_milliseconds, read_antenna, read_reader, read_box, read_logindex, read_rssi, read_isrewind, read_readertime," +
                "read_starttime, read_time_seconds, read_time_milliseconds, read_bib, read_type)" +
                " VALUES (@event, @status, @loc, @chip, @sec, @milli, @ant, @reader, @box, @logix, @rssi, @rewind, @readertime, " +
                "@starttime, @timesec, @timemill, @bib, @type);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", read.EventId),
                new SQLiteParameter("@status", read.Status),
                new SQLiteParameter("@loc", read.LocationID),
                new SQLiteParameter("@chip", read.ChipNumber),
                new SQLiteParameter("@sec", read.Seconds),
                new SQLiteParameter("@milli", read.Milliseconds),
                new SQLiteParameter("@ant", read.Antenna),
                new SQLiteParameter("@reader", read.Reader),
                new SQLiteParameter("@box", read.Box),
                new SQLiteParameter("@logix", read.LogId),
                new SQLiteParameter("@rssi", read.RSSI),
                new SQLiteParameter("@rewind", read.IsRewind),
                new SQLiteParameter("@readertime", read.ReaderTime),
                new SQLiteParameter("@starttime", read.StartTime),
                new SQLiteParameter("@timesec", read.TimeSeconds),
                new SQLiteParameter("@timemill", read.TimeMilliseconds),
                new SQLiteParameter("@bib", read.ReadBib),
                new SQLiteParameter("@type", read.Type)
            });
            command.ExecuteNonQuery();
            Log.D("EventID " + read.EventId);
        }

        public void AddChipRead(ChipRead read)
        {
            Log.D("Attempting to grab Mutex: ID 83");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 83");
                return;
            }
            Log.D("Database - Add chip read. Box " + read.Box + " Antenna " + read.Antenna + " Chip " + read.ChipNumber
                + " LogId " + read.LogId + " Time Given " + read.TimeString);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                AddChipReadInternal(read, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddChipReads(List<ChipRead> reads)
        {
            Log.D("Attempting to grab Mutex: ID 84");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 84");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (ChipRead read in reads)
                {
                    AddChipReadInternal(read, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void UpdateChipReadInternal(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE chipreads SET read_status=@status, read_time_seconds=@time, read_time_milliseconds=@mill WHERE read_id=@id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@status", read.Status),
                    new SQLiteParameter("@id", read.ReadId),
                    new SQLiteParameter("@time", read.TimeSeconds),
                    new SQLiteParameter("@mill", read.TimeMilliseconds)
            });
            command.ExecuteNonQuery();

        }

        public void UpdateChipRead(ChipRead read)
        {
            Log.D("Attempting to grab Mutex: ID 85");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 85");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                UpdateChipReadInternal(read, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateChipReads(List<ChipRead> reads)
        {
            Log.D("Attempting to grab Mutex: ID 86");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 86");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (ChipRead read in reads)
                {
                    UpdateChipReadInternal(read, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void SetChipReadStatusInternal(ChipRead read, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE chipreads SET read_status=@status WHERE read_id=@id;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@status", read.Status),
                    new SQLiteParameter("@id", read.ReadId)
            });
            command.ExecuteNonQuery();
        }

        public void SetChipReadStatus(ChipRead read)
        {
            Log.D("Attempting to grab Mutex: ID 87");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 87");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SetChipReadStatusInternal(read, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetChipReadStatuses(List<ChipRead> reads)
        {
            if (reads.Count < 1)
            {
                return;
            }
            Log.D("Attempting to grab Mutex: ID 88");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 88");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (ChipRead read in reads)
                {
                    SetChipReadStatusInternal(read, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void DeleteChipReads(List<ChipRead> reads)
        {
            Log.D("Attempting to grab Mutex: ID 89");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 89");
                return;
            }
            if (reads.Count < 1) return;
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (ChipRead read in reads)
                {
                    SQLiteCommand command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM chipreads WHERE read_id=@read;";
                    command.Parameters.Add(new SQLiteParameter("@read", read.ReadId));
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<ChipRead> GetChipReads()
        {
            Log.D("Attempting to grab Mutex: ID 90");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 90");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b ON (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id;";
            command.Parameters.Add(new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB));
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 91");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 91");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b ON (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id " +
                "WHERE c.event_id=@event;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            command.Parameters.Add(new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB));
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 92");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 92");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM chipreads c LEFT JOIN bib_chip_assoc b on (c.read_chipnumber=b.chip AND c.event_id=b.event_id) " +
                "LEFT JOIN eventspecific e ON ((e.eventspecific_bib=b.bib OR e.eventspecific_bib=c.read_bib) AND e.event_id=c.event_id " +
                "AND e.eventspecific_bib != @dummybib) " +
                "LEFT JOIN participants p ON p.participant_id=e.participant_id WHERE c.event_id=@event AND " +
                "(read_status=@status OR read_status=@used OR read_status=@start OR read_status=@dnf);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", eventId),
                new SQLiteParameter("@status", Constants.Timing.CHIPREAD_STATUS_NONE),
                new SQLiteParameter("@used", Constants.Timing.CHIPREAD_STATUS_USED),
                new SQLiteParameter("@start", Constants.Timing.CHIPREAD_STATUS_STARTTIME),
                new SQLiteParameter("@dnf", Constants.Timing.CHIPREAD_STATUS_DNF),
                new SQLiteParameter("@dummybib", Constants.Timing.CHIPREAD_DUMMYBIB)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<ChipRead> output = GetChipReadsWorker(reader);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        private List<ChipRead> GetChipReadsWorker(SQLiteDataReader reader)
        {
            Event theEvent = GetCurrentEvent();
            DateTime start = DateTime.Now;
            if (theEvent != null)
            {
                start = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
            }
            List<TimingLocation> locations = new List<TimingLocation>();
            if (theEvent != null) locations.AddRange(GetTimingLocations(theEvent.Identifier));
            if (theEvent != null && !theEvent.CommonStartFinish)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent == null ? -1 : theEvent.Identifier, "Start/Finish", theEvent == null ? 1 : theEvent.FinishMaxOccurrences, theEvent == null ? 0 : theEvent.FinishIgnoreWithin));
            }
            Dictionary<int, string> locDict = new Dictionary<int, string>();
            foreach (TimingLocation loc in locations)
            {
                locDict[loc.Identifier] = loc.Name;
            }
            List<ChipRead> output = new List<ChipRead>();
            while (reader.Read())
            {
                int locationId = Convert.ToInt32(reader["location_id"]);
                string locationName = locDict.ContainsKey(locationId) ? locDict[locationId] : "";
                output.Add(new ChipRead(
                    Convert.ToInt32(reader["read_id"]),
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["read_status"]),
                    locationId,
                    reader["read_chipnumber"].ToString(),
                    Convert.ToInt64(reader["read_seconds"]),
                    Convert.ToInt32(reader["read_milliseconds"]),
                    Convert.ToInt32(reader["read_antenna"]),
                    reader["read_rssi"].ToString(),
                    Convert.ToInt32(reader["read_isrewind"]),
                    reader["read_reader"].ToString(),
                    reader["read_box"].ToString(),
                    reader["read_readertime"].ToString(),
                    Convert.ToInt32(reader["read_starttime"]),
                    Convert.ToInt32(reader["read_logindex"]),
                    Convert.ToInt64(reader["read_time_seconds"]),
                    Convert.ToInt32(reader["read_time_milliseconds"]),
                    Convert.ToInt32(reader["read_bib"]),
                    Convert.ToInt32(reader["read_type"]),
                    reader["bib"] == DBNull.Value ? Constants.Timing.CHIPREAD_DUMMYBIB : Convert.ToInt32(reader["bib"]),
                    reader["participant_first"] == DBNull.Value ? "" : reader["participant_first"].ToString(),
                    reader["participant_last"] == DBNull.Value ? "" : reader["participant_last"].ToString(),
                    start,
                    locationName
                    ));
            }
            reader.Close();
            return output;
        }

        /*
         * Settings
         */

        public void SetServerName(string name)
        {
            Log.D("Attempting to grab Mutex: ID 93");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 93");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE settings SET name=@name";
                command.Parameters.Add(new SQLiteParameter("@name", name));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public string GetServerName()
        {
            Log.D("Attempting to grab Mutex: ID 94");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 94");
                return "";
            }
            String output = "Northwest Endurance Events";
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM settings;";
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                output = reader["name"].ToString();
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public AppSetting GetAppSetting(string name)
        {
            Log.D("Attempting to grab Mutex: ID 95");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 95");
                return null;
            }
            AppSetting output = null;
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
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
            Log.D("Attempting to grab Mutex: ID 96");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 96");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Bib Groups
         */
        public void AddBibGroup(int eventId, BibGroup group)
        {
            Log.D("Attempting to grab Mutex: ID 97");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 97");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<BibGroup> GetBibGroups(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 98");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 98");
                return new List<BibGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveBibGroup(BibGroup group)
        {
            Log.D("Attempting to grab Mutex: ID 99");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 99");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM available_bibs WHERE event_id=@event AND bib_group_number=@number;" +
                "DELETE FROM bib_group WHERE event_id=@event AND bib_group_number=@number;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@event", group.EventId),
                new SQLiteParameter("@number", group.Number)
            });
            command.ExecuteNonQuery();
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Bibs
         */
        public void AddBibs(int eventId, int group, List<int> bibs)
        {
            Log.D("Attempting to grab Mutex: ID 100");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 100");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (int bib in bibs)
                {
                    AddBib(eventId, group, bib, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddBibs(int eventId, List<AvailableBib> bibs)
        {
            Log.D("Attempting to grab Mutex: ID 101");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 101");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AvailableBib bib in bibs)
                {
                    AddBib(eventId, bib.GroupNumber, bib.Bib, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddBib(int eventId, int group, int bib)
        {
            Log.D("Attempting to grab Mutex: ID 102");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 102");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AddBib(eventId, group, bib, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void AddBib(int eventId, int group, int bib, SQLiteConnection connection)
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
            Log.D("Attempting to grab Mutex: ID 103");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 103");
                return new List<AvailableBib>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int LargestBib(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 104");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 104");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return largest;
        }

        private void RemoveBibInternal(int eventId, int bib, SQLiteConnection connection)
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

        public void RemoveBib(int eventId, int bib)
        {
            Log.D("Attempting to grab Mutex: ID 105");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 105");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoveBibInternal(eventId, bib, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBibs(List<AvailableBib> bibs)
        {
            Log.D("Attempting to grab Mutex: ID 106");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 106");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AvailableBib bib in bibs)
                {
                    RemoveBibInternal(bib.EventId, bib.Bib, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Age Group Functions
         */
        public void AddAgeGroup(AgeGroup group)
        {
            Log.D("Attempting to grab Mutex: ID 107");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 107");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                AddAgeGroupInternal(group, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("Attempting to grab Mutex: ID 108");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 108");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (AgeGroup group in groups)
                {
                    AddAgeGroupInternal(group, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void AddAgeGroupInternal(AgeGroup group, SQLiteConnection connection)
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
            Log.D("Attempting to grab Mutex: ID 109");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 109");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("Attempting to grab Mutex: ID 110");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 110");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroups(int eventId, int divisionId)
        {
            Log.D("Attempting to grab Mutex: ID 111");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 111");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("Attempting to grab Mutex: ID 123");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 123");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                foreach (AgeGroup ag in groups) {
                    command.CommandText = "DELETE FROM age_groups WHERE group_id=@group;";
                    command.Parameters.AddRange(new SQLiteParameter[]
                    {
                        new SQLiteParameter("@group", ag.GroupId),
                    });
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 112");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 112");
                return new List<AgeGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
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
                    Convert.ToInt32(reader["division_id"]), Convert.ToInt32(reader["start_age"]), Convert.ToInt32(reader["end_age"]), Convert.ToInt32(reader["last_group"])));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int divisionId)
        {
            Log.D("Attempting to grab Mutex: ID 122");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 122");
                return new List<AgeGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM age_groups WHERE event_id=@event AND division_id=@division;";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                    new SQLiteParameter("@event", eventId),
                    new SQLiteParameter("@division", divisionId)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            List<AgeGroup> output = new List<AgeGroup>();
            while (reader.Read())
            {
                output.Add(new AgeGroup(Convert.ToInt32(reader["group_id"]), Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["division_id"]), Convert.ToInt32(reader["start_age"]), Convert.ToInt32(reader["end_age"]),
                    Convert.ToInt32(reader["last_group"])));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        private void AddTimingSystemInternal(TimingSystem system, SQLiteConnection connection)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO timing_systems (ts_ip, ts_port, ts_location, ts_type)" +
                " VALUES (@ip, @port, @location, @type);";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@ip", system.IPAddress),
                new SQLiteParameter("@port", system.Port),
                new SQLiteParameter("@location", system.LocationID),
                new SQLiteParameter("@type", system.Type)
            });
            command.ExecuteNonQuery();
        }

        public void AddTimingSystem(TimingSystem system)
        {
            Log.D("Attempting to grab Mutex: ID 113");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 113");
                return;
            }
            Log.D("Database - Add Timing System");
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AddTimingSystemInternal(system, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            Log.D("Attempting to grab Mutex: ID 114");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 114");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "UPDATE timing_systems SET ts_ip=@ip, ts_port=@port, ts_location=@location, ts_type=@type WHERE ts_identifier=@id;";
                command.Parameters.AddRange(new SQLiteParameter[]
                {
                    new SQLiteParameter("@ip", system.IPAddress),
                    new SQLiteParameter("@port", system.Port),
                    new SQLiteParameter("@location", system.LocationID),
                    new SQLiteParameter("@type", system.Type),
                    new SQLiteParameter("@id", system.SystemIdentifier)
                });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            Log.D("Attempting to grab Mutex: ID 115");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 115");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems;";
                command.ExecuteNonQuery();
                foreach (TimingSystem sys in systems)
                {
                    AddTimingSystemInternal(sys, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveTimingSystem(TimingSystem system)
        {
            RemoveTimingSystem(system.SystemIdentifier);
        }

        public void RemoveTimingSystem(int systemId)
        {
            Log.D("Attempting to grab Mutex: ID 116");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 116");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM timing_systems WHERE ts_identifier=@id;";
                command.Parameters.Add(new SQLiteParameter("@id", systemId));
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimingSystem> GetTimingSystems()
        {
            Log.D("Attempting to grab Mutex: ID 117");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 117");
                return new List<TimingSystem>();
            }
            List<TimingSystem> output = new List<TimingSystem>();
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM timing_systems;";
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingSystem(Convert.ToInt32(reader["ts_identifier"]), reader["ts_ip"].ToString(),
                    Convert.ToInt32(reader["ts_port"]), Convert.ToInt32(reader["ts_location"]), reader["ts_type"].ToString()));
            }
            reader.Close();
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<DivisionStats> GetDivisionStats(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 121");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 121");
                return new List<DivisionStats>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT d.division_id AS id, d.division_name AS name, e.eventspecific_status AS status, COUNT(e.eventspecific_status) AS count " +
                "FROM divisions d JOIN eventspecific e ON d.division_id=e.division_id " +
                "WHERE e.event_id=@event " +
                "GROUP BY d.division_name, e.eventspecific_status;";
            command.Parameters.Add(new SQLiteParameter("@event", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            DivisionStats allstats = new DivisionStats
            {
                DivisionName = "All",
                DivisionID = -1,
                Active = 0,
                DNF = 0,
                DNS = 0,
                Finished = 0
            };
            Dictionary<int, DivisionStats> statsDictionary = new Dictionary<int, DivisionStats>();
            while (reader.Read())
            {
                int divId = Convert.ToInt32(reader["id"].ToString());
                if (!statsDictionary.ContainsKey(divId))
                {
                    statsDictionary[divId] = new DivisionStats()
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
            connection.Close();
            mutex.ReleaseMutex();
            List<DivisionStats> output = new List<DivisionStats>();
            output.Add(allstats);
            foreach (DivisionStats stats in statsDictionary.Values)
            {
                output.Add(stats);
            }
            return output;
        }

        public Dictionary<int, List<Participant>> GetDivisionParticipantsStatus(int eventId, int divisionId)
        {
            Dictionary<int, List<Participant>> output = new Dictionary<int, List<Participant>>();
            List<Participant> parts = (divisionId == -1) ? GetParticipants(eventId) : GetParticipants(eventId, divisionId);
            foreach (Participant person in parts)
            {
                if (!output.ContainsKey(person.Status))
                {
                    output[person.Status] = new List<Participant>();
                }
                output[person.Status].Add(person);
            }
            return output;
        }
    }
}
