using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChronoKeep.Database.SQLite
{
    class Update
    {
        internal static void UpdateClient(int dbVersion, int maxVersion)
        {
            throw new InvalidDatabaseVersion(dbVersion, maxVersion);
        }

        internal static void UpdateDatabase(int oldversion, int newversion, string connectionInfo)
        {
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
                            ChipReads.AddChipRead(read, connection);
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
                            ChipReads.AddChipRead(read, connection);
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
                        goto case 40;
                    case 40:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE divisions ADD " +
                            "division_early_start_offset_seconds INTEGER NOT NULL DEFAULT 0;" +
                            "UPDATE settings SET version=41 WHERE version=40;";
                        command.ExecuteNonQuery();
                        goto case 41;
                    case 41:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE chipreads RENAME TO chipreads_old;" +
                            "CREATE TABLE IF NOT EXISTS chipreads (" +
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
                            "UNIQUE (event_id, read_chipnumber, read_bib, read_seconds, read_milliseconds) ON CONFLICT IGNORE" +
                            ");" +
                            "INSERT INTO chipreads SELECT * FROM chipreads_old;" +
                            "DROP TABLE chipreads_old;" +
                            "UPDATE settings SET version=42 WHERE version=41;";
                        command.ExecuteNonQuery();
                        goto case 42;
                    case 42:
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE TABLE IF NOT EXISTS results_api(" +
                                "api_id INTEGER PRIMARY KEY," +
                                "api_type VARCHAR(50) NOT NULL," +
                                "api_url VARCHAR(150) NOT NULL," +
                                "api_auth_token VARCHAR(100) NOT NULL," +
                                "api_nickname VARCHAR(75) NOT NULL," +
                                "UNIQUE (api_url, api_auth_token) ON CONFLICT REPLACE);" +
                            "ALTER TABLE events ADD COLUMN api_id INTEGER REFERENCES results_api(api_id) NOT NULL DEFAULT -1;" +
                            "ALTER TABLE events ADD COLUMN api_event_id VARCHAR(200) NOT NULL DEFAULT '';" +
                            "ALTER TABLE time_results ADD COLUMN timeresult_uploaded INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_UPLOADED_FALSE + ";" +
                            "ALTER TABLE settings RENAME TO settings_old;" +
                            "ALTER TABLE app_settings RENAME TO settings;" +
                            "INSERT INTO settings (setting, value) VALUES ('" + Constants.Settings.DATABASE_VERSION + "', '43');" +
                            "INSERT INTO settings SELECT '" + Constants.Settings.SERVER_NAME + "', name FROM settings_old;" +
                            "DROP TABLE settings_old;";
                        command.ExecuteNonQuery();
                        goto case 43;
                    case 43:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE divisions ADD COLUMN division_linked_id INTEGER NOT NULL REFERENCES divisions(division_id) DEFAULT -1;" +
                            "ALTER TABLE divisions ADD COLUMN division_type INTEGER NOT NULL DEFAULT 0;" +
                            "ALTER TABLE divisions ADD COLUMN division_ranking_order INTEGER NOT NULL DEFAULT 0;" +
                            "UPDATE settings SET value='44' WHERE setting='" + Constants.Settings.DATABASE_VERSION + "'";
                        command.ExecuteNonQuery();
                        goto case 44;
                    case 44:
                        command = connection.CreateCommand();
                        command.CommandText = "DROP TABLE dayof_participant; DROP TABLE kiosk; DROP TABLE eventspecific_apparel; " +
                            "DROP TABLE changes; DROP TABLE bib_group; DROP TABLE available_bibs; " +
                            "CREATE TABLE IF NOT EXISTS distances (" +
                                "distance_id INTEGER PRIMARY KEY," +
                                "distance_name VARCHAR(100) NOT NULL," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "distance_distance DECIMAL(10,2) DEFAULT 0.0," +
                                "distance_distance_unit INTEGER DEFAULT 0," +
                                "distance_start_location INTEGER DEFAULT -2," +
                                "distance_start_within INTEGER DEFAULT -1," +
                                "distance_finish_location INTEGER DEFAULT -1," +
                                "distance_finish_occurance INTEGER DEFAULT 1," +
                                "distance_wave INTEGER NOT NULL DEFAULT 1," +
                                "distance_start_offset_seconds INTEGER NOT NULL DEFAULT 0," +
                                "distance_start_offset_milliseconds INTEGER NOT NULL DEFAULT 0," +
                                "distance_end_offset_seconds INTEGER NOT NULL DEFAULT 0," +
                                "distance_linked_id INTEGER NOT NULL REFERENCES divisions(distance_id) DEFAULT -1," +
                                "distance_type INTEGER NOT NULL DEFAULT 0," +
                                "distance_ranking_order INTEGER NOT NULL DEFAULT 0, " +
                                "UNIQUE (distance_name, event_id) ON CONFLICT IGNORE" +
                                ");" +
                            "INSERT INTO distances SELECT division_id, division_name, event_id, division_distance, division_distance_unit, " +
                                "division_start_location, division_start_within, division_finish_location, division_finish_occurance, division_wave, " +
                                "division_start_offset_seconds, division_start_offset_milliseconds, division_end_offset_seconds, division_linked_id, " +
                                "division_type, division_ranking_order FROM divisions;" +
                            "DROP TABLE divisions; " +
                            "ALTER TABLE age_groups RENAME TO age_groups_old; " +
                            "CREATE TABLE IF NOT EXISTS age_groups (" +
                                "group_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "distance_id INTEGER NOT NULL DEFAULT -1," +
                                "start_age INTEGER NOT NULL," +
                                "end_age INTEGER NOT NULL," +
                                "last_group INTEGER DEFAULT " + Constants.Timing.AGEGROUPS_LASTGROUP_FALSE + " NOT NULL);" +
                            "INSERT INTO age_groups SELECT * FROM age_groups_old;" +
                            "DROP TABLE age_groups_old; " +
                            "ALTER TABLE segments RENAME TO segments_old;" +
                            "CREATE TABLE IF NOT EXISTS segments (" +
                                "segment_id INTEGER PRIMARY KEY," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "distance_id INTEGER DEFAULT -1," +
                                "location_id INTEGER DEFAULT -1," +
                                "location_occurance INTEGER DEFAULT 1," +
                                "name VARCHAR DEFAULT ''," +
                                "distance_segment DECIMAL (10,2) DEFAULT 0.0," +
                                "distance_cumulative DECIMAL (10,2) DEFAULT 0.0," +
                                "distance_unit INTEGER DEFAULT 0," +
                                "UNIQUE (event_id, distance_id, location_id, location_occurance) ON CONFLICT IGNORE" +
                                "); " +
                            "INSERT INTO segments SELECT * FROM segments_old; " +
                            "DROP TABLE segments_old; " +
                            "ALTER TABLE eventspecific RENAME TO eventspecific_old; " +
                            "CREATE TABLE IF NOT EXISTS eventspecific (" +
                                "eventspecific_id INTEGER PRIMARY KEY," +
                                "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "distance_id INTEGER NOT NULL REFERENCES distance(distance_id)," +
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
                                "UNIQUE (participant_id, event_id, distance_id) ON CONFLICT REPLACE," +
                                "UNIQUE (event_id, eventspecific_bib) ON CONFLICT REPLACE" +
                                ");" +
                            "INSERT INTO eventspecific SELECT * FROM eventspecific_old;" +
                            "DROP TABLE eventspecific_old;" +
                            "ALTER TABLE events RENAME TO events_old;" +
                            "CREATE TABLE IF NOT EXISTS events (" +
                                "event_id INTEGER PRIMARY KEY," +
                                "event_name VARCHAR(100) NOT NULL," +
                                "event_date VARCHAR(15) NOT NULL," +
                                "event_yearcode VARCHAR(10) NOT NULL DEFAULT ''," +
                                "event_shirt_optional INTEGER DEFAULT 1," +
                                "event_shirt_price INTEGER DEFAULT 0," +
                                "event_rank_by_gun INTEGER DEFAULT 1," +
                                "event_common_age_groups INTEGER DEFAULT 1," +
                                "event_common_start_finish INTEGER DEFAULT 1," +
                                "event_distance_specific_segments INTEGER DEFAULT 0," +
                                "event_start_time_seconds INTEGER NOT NULL DEFAULT -1," +
                                "event_start_time_milliseconds INTEGER NOT NULL DEFAULT 0," +
                                "event_finish_max_occurances INTEGER NOT NULL DEFAULT 1," +
                                "event_finish_ignore_within INTEGER NOT NULL DEFAULT 0," +
                                "event_start_window INTEGER NOT NULL DEFAULT -1," +
                                "event_timing_system VARCHAR NOT NULL DEFAULT '" + Constants.Settings.TIMING_RFID + "'," +
                                "event_type INTEGER NOT NULL DEFAULT " + Constants.Timing.EVENT_TYPE_DISTANCE + "," +
                                "api_id INTEGER REFERENCES results_api(api_id) NOT NULL DEFAULT -1," +
                                "api_event_id VARCHAR(200) NOT NULL DEFAULT ''," +
                                "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                                ");" +
                            "INSERT INTO events SELECT event_id, event_name, event_date, event_yearcode, event_shirt_optional," +
                                "event_shirt_price, event_rank_by_gun, event_common_age_groups, event_common_start_finish, event_division_specific_segments," +
                                "event_start_time_seconds, event_start_time_milliseconds, event_finish_max_occurances, event_finish_ignore_within," +
                                "event_start_window, event_timing_system, event_type, api_id, api_event_id FROM events_old;" +
                            "DROP TABLE events_old;" + // This next line creates linked divisions that are early start divisions if there are people set to early start
                            "INSERT INTO distances (distance_name, event_id, distance_distance, distance_distance_unit, distance_start_location, " +
                                "distance_start_within, distance_finish_location, distance_finish_occurance, distance_wave, distance_start_offset_seconds, " +
                                "distance_start_offset_milliseconds, distance_end_offset_seconds, distance_linked_id, distance_type, distance_ranking_order) " +
                                "SELECT b.distance_name || ' Early Created' AS new_distance_name, b.event_id, b.distance_distance, " +
                                    "b.distance_distance_unit, b.distance_start_location, b.distance_start_within, b.distance_finish_location, " +
                                    "b.distance_finish_occurance, b.distance_wave, b.distance_start_offset_seconds, b.distance_start_offset_milliseconds, " +
                                    "b.distance_end_offset_seconds, b.distance_id, " + Constants.Timing.DISTANCE_TYPE_EARLY + ", 1 " +
                                    "FROM distances AS b JOIN " +
                                        "(SELECT DISTINCT(d.distance_id) AS unique_distance_id " +
                                            "FROM distances AS d JOIN eventspecific AS e ON e.distance_id=d.distance_id " +
                                            "WHERE e.eventspecific_earlystart != 0) " +
                                        "ON unique_distance_id=b.distance_id;" +
                            "UPDATE settings SET value='45' WHERE setting='" + Constants.Settings.DATABASE_VERSION + "'";
                        command.ExecuteNonQuery();
                        // Get all Distances and make a dictionary based on names.
                        command = connection.CreateCommand();
                        command.CommandText = "SELECT * FROM distances;";
                        reader = command.ExecuteReader();
                        List<Distance> distances = new List<Distance>();
                        while (reader.Read())
                        {
                            distances.Add(new Distance(Convert.ToInt32(reader["distance_id"]),
                                reader["distance_name"].ToString(),
                                Convert.ToInt32(reader["event_id"]),
                                Convert.ToDouble(reader["distance_distance"]),
                                Convert.ToInt32(reader["distance_distance_unit"]),
                                Convert.ToInt32(reader["distance_finish_location"]),
                                Convert.ToInt32(reader["distance_finish_occurance"]),
                                Convert.ToInt32(reader["distance_start_location"]),
                                Convert.ToInt32(reader["distance_start_within"]),
                                Convert.ToInt32(reader["distance_wave"]),
                                Convert.ToInt32(reader["distance_start_offset_seconds"]),
                                Convert.ToInt32(reader["distance_start_offset_milliseconds"]),
                                Convert.ToInt32(reader["distance_end_offset_seconds"]),
                                Convert.ToInt32(reader["distance_linked_id"]),
                                Convert.ToInt32(reader["distance_type"]),
                                Convert.ToInt32(reader["distance_ranking_order"])
                                ));
                        }
                        reader.Close();
                        // (string, int) is a key for the Distance Name and Linked Division, we need to find a
                        // DISTANCE NAME + " Early Created" with the right linked division ID;
                        Dictionary<(string, int), Distance> distDict = new Dictionary<(string, int), Distance>();
                        foreach (Distance d in distances)
                        {
                            distDict[(d.Name, d.LinkedDistance)] = d;
                        }
                        // Get all participants and update those who're early start
                        command = connection.CreateCommand();
                        command.CommandText = "SELECT * FROM eventspecific e JOIN participants p ON e.participant_id=p.participant_id " +
                            "JOIN distances d ON d.distance_id=e.distance_id WHERE eventspecific_earlystart != 0;";
                        reader = command.ExecuteReader();
                        List<Participant> people = new List<Participant>();
                        while (reader.Read())
                        {
                            people.Add(new Participant(
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
                                    Convert.ToInt32(reader["distance_id"]),
                                    reader["distance_name"].ToString(),
                                    Convert.ToInt32(reader["eventspecific_bib"]),
                                    Convert.ToInt32(reader["eventspecific_checkedin"]),
                                    reader["eventspecific_comments"].ToString(),
                                    reader["eventspecific_owes"].ToString(),
                                    reader["eventspecific_other"].ToString(),
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
                        // Go through each participant and update their division/distance.
                        foreach (Participant p in people)
                        {
                            if (distDict.ContainsKey((p.EventSpecific.DistanceName + " Early Created", p.EventSpecific.DistanceIdentifier)))
                            {
                                p.EventSpecific.DistanceIdentifier = distDict[(p.EventSpecific.DistanceName + " Early Created", p.EventSpecific.DistanceIdentifier)].Identifier;
                                Participants.UpdateParticipant(p, connection);
                            }
                        }
                        goto case 45;
                    case 45:
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events RENAME TO events_old; " +
                            "CREATE TABLE IF NOT EXISTS events (" +
                                "event_id INTEGER PRIMARY KEY," +
                                "event_name VARCHAR(100) NOT NULL," +
                                "event_date VARCHAR(15) NOT NULL," +
                                "event_yearcode VARCHAR(10) NOT NULL DEFAULT ''," +
                                "event_rank_by_gun INTEGER DEFAULT 1," +
                                "event_common_age_groups INTEGER DEFAULT 1," +
                                "event_common_start_finish INTEGER DEFAULT 1," +
                                "event_distance_specific_segments INTEGER DEFAULT 0," +
                                "event_start_time_seconds INTEGER NOT NULL DEFAULT -1," +
                                "event_start_time_milliseconds INTEGER NOT NULL DEFAULT 0," +
                                "event_finish_max_occurances INTEGER NOT NULL DEFAULT 1," +
                                "event_finish_ignore_within INTEGER NOT NULL DEFAULT 0," +
                                "event_start_window INTEGER NOT NULL DEFAULT -1," +
                                "event_timing_system VARCHAR NOT NULL DEFAULT '" + Constants.Settings.TIMING_RFID + "'," +
                                "event_type INTEGER NOT NULL DEFAULT " + Constants.Timing.EVENT_TYPE_DISTANCE + "," +
                                "api_id INTEGER REFERENCES results_api(api_id) NOT NULL DEFAULT -1," +
                                "api_event_id VARCHAR(200) NOT NULL DEFAULT ''," +
                                "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                                ");" +
                            "INSERT INTO events SELECT event_id, event_name, event_date, event_yearcode, event_rank_by_gun, event_common_age_groups, " +
                                "event_common_start_finish, event_distance_specific_segments, event_start_time_seconds, event_start_time_milliseconds, " +
                                "event_finish_max_occurances, event_finish_ignore_within, event_start_window, event_timing_system, event_type, " +
                                "api_id, api_event_id FROM events_old;" +
                            "DROP TABLE events_old;" +
                            "ALTER TABLE eventspecific RENAME TO eventspecific_old;" +
                            "CREATE TABLE IF NOT EXISTS eventspecific (" +
                                "eventspecific_id INTEGER PRIMARY KEY," +
                                "participant_id INTEGER NOT NULL REFERENCES participants(participant_id)," +
                                "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                                "distance_id INTEGER NOT NULL REFERENCES distances(distance_id)," +
                                "eventspecific_bib INTEGER," +
                                "eventspecific_checkedin INTEGER DEFAULT 0," +
                                "eventspecific_comments VARCHAR," +
                                "eventspecific_owes VARCHAR(50)," +
                                "eventspecific_other VARCHAR," +
                                "eventspecific_registration_date VARCHAR NOT NULL DEFAULT ''," +
                                "eventspecific_status INT NOT NULL DEFAULT " + Constants.Timing.EVENTSPECIFIC_NOSHOW + "," +
                                "eventspecific_age_group_id INT NOT NULL DEFAULT " + Constants.Timing.TIMERESULT_DUMMYAGEGROUP + "," +
                                "eventspecific_age_group_name VARCHAR NOT NULL DEFAULT '0-110'," +
                                "UNIQUE (participant_id, event_id, distance_id) ON CONFLICT REPLACE," +
                                "UNIQUE (event_id, eventspecific_bib) ON CONFLICT REPLACE" +
                                ");" +
                            "INSERT INTO eventspecific SELECT eventspecific_id, participant_id, event_id, distance_id, eventspecific_bib," +
                                "eventspecific_checkedin, eventspecific_comments, eventspecific_owes, eventspecific_other, eventspecific_registration_date," +
                                "eventspecific_status, eventspecific_age_group_id, eventspecific_age_group_name FROM eventspecific_old;" +
                            "DROP TABLE eventspecific_old;" +
                            "UPDATE settings SET value='46' WHERE setting='" + Constants.Settings.DATABASE_VERSION + "'";
                        command.ExecuteNonQuery();
                        break;
                }
                transaction.Commit();
                connection.Close();
            }
        }
    }
}
