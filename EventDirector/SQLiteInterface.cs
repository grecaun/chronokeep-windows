using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class SQLiteInterface : IDBInterface
    {
        private readonly int version = 12;
        SQLiteConnection connection;

        public SQLiteInterface(String info)
        {
            connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", info));
            connection.Open();
        }

        public void Initialize()
        {
            ArrayList queries = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='settings'", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                Log.D("Tables do not need to be made.");
                command = new SQLiteCommand("SELECT version FROM settings;", connection);
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int oldVersion = Convert.ToInt32(reader["version"]);
                    if (oldVersion < version) UpdateDatabase(oldVersion, version);
                }
                else
                {
                    Log.D("Something went wrong when checking the version...");
                }
            }
            else
            {
                Log.D("Tables haven't been created. Doing so now.");
                command = new SQLiteCommand("PRAGMA foreign_keys = ON; PRAGMA foreign_keys", connection); // Ensure Foreign key constraints work.
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    queries.Add("CREATE TABLE IF NOT EXISTS bib_chip_assoc (" +
                            "event_id INTEGER NOT NULL," +
                            "bib INTEGER NOT NULL," +
                            "chip INTEGER NOT NULL," +
                            "UNIQUE (event_id, bib) ON CONFLICT REPLACE," +
                            "UNIQUE (event_id, chip) ON CONFLICT REPLACE" +
                            ")");
                    queries.Add("CREATE TABLE IF NOT EXISTS events (" +
                            "event_id INTEGER PRIMARY KEY," +
                            "event_name VARCHAR(100) NOT NULL," +
                            "event_date VARCHAR(15) NOT NULL," +
                            "event_registration_open INTEGER DEFAULT 0," +
                            "event_results_open INTEGER DEFAULT 0," +
                            "event_announce_available INTEGER DEFAULT 0," +
                            "event_allow_early_start INTEGER DEFAULT 0," +
                            "event_kiosk INTEGER DEFAULT 0," +
                            "event_next_year_event_id INTEGER DEFAULT -1," +
                            "event_shirt_optional INTEGER DEFAULT 1," +
                            "event_shirt_price INTEGER DEFAULT 0," +
                            "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                            ")");
                    queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (" +
                            "emergencycontact_id INTEGER PRIMARY KEY," +
                            "emergencycontact_name VARCHAR(150) UNIQUE NOT NULL," +
                            "emergencycontact_phone VARCHAR(20)," +
                            "emergencycontact_email VARCHAR(150)" +
                            ")");
                    queries.Add("CREATE TABLE IF NOT EXISTS dayof_participant (" +
                            "dop_id INTEGER PRIMARY KEY," +
                            "dop_event_id INTEGER NOT NULL," +
                            "dop_division_id INTEGER NOT NULL," +
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
                            ")");
                    if (reader.GetInt32(0) == 1)
                    {
                        Log.D("Foreign keys work. Setting table creation queries.");
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
                            "UNIQUE (division_name, event_id) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (" +
                            "timingpoint_id INTEGER PRIMARY KEY," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "division_id INTEGER NOT NULL REFERENCES divisions(division_id)," +
                            "timingpoint_name VARCHAR(100) NOT NULL," +
                            "timingpoint_distance VARCHAR(5)," +
                            "timingpoint_unit VARCHAR(2)," +
                            "UNIQUE (event_id, division_id, timingpoint_name) ON CONFLICT IGNORE" +
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
                            "emergencycontact_id INTEGER REFERENCES emergencycontacts(emergencycontact_id)," +
                            "participant_phone VARCHAR(20)," +
                            "participant_email VARCHAR(150)," +
                            "participant_mobile VARCHAR(20)," +
                            "participant_parent VARCHAR(150)," +
                            "participant_country VARCHAR(50)," +
                            "participant_street2 VARCHAR(50)," +
                            "participant_gender VARCHAR(10)," +
                            "UNIQUE (participant_first, participant_last, participant_street, participant_city, participant_state, participant_zip, participant_birthday) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (" +
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
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS timeresults (" +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
                            "eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id)," +
                            "timingpoint_id INTEGER NOT NULL REFERENCES timingpoints(timingpoint_id)," +
                            "timeresult_time INTEGER NOT NULL," +
                            "UNIQUE (event_id, eventspecific_id, timingpoint_id) ON CONFLICT IGNORE" +
                            ")");
                    }
                    else
                    {
                        Log.D("Foreign keys DO NOT work. Setting table creation queries.");
                        queries.Add("CREATE TABLE IF NOT EXISTS kiosk (" +
                            "event_id INTEGER NOT NULL," +
                            "kiosk_waiver_text VARCHAR NOT NULL," +
                            "kiosk_print_new INTEGER DEFAULT 0," +
                            "UNIQUE (event_id) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS divisions (" +
                            "division_id INTEGER PRIMARY KEY," +
                            "division_name VARCHAR(100) NOT NULL," +
                            "event_id INTEGER NOT NULL," +
                            "division_cost INTEGER DEFAULT 7000," +
                            "UNIQUE (division_name, event_id) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (" +
                            "timingpoint_id INTEGER PRIMARY KEY," +
                            "event_id INTEGER NOT NULL," +
                            "division_id INTEGER NOT NULL," +
                            "timingpoint_name VARCHAR(100) NOT NULL," +
                            "timingpoint_distance VARCHAR(5)," +
                            "timingpoint_unit VARCHAR(2)," +
                            "UNIQUE (event_id, division_id, timingpoint_name) ON CONFLICT IGNORE" +
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
                            "participant_emergencycontact_id INTEGER NOT NULL," +
                            "participant_phone VARCHAR(20)," +
                            "participant_email VARCHAR(150)," +
                            "participant_mobile VARCHAR(20)," +
                            "participant_parent VARCHAR(150)," +
                            "participant_country VARCHAR(50)," +
                            "participant_street2 VARCHAR(50)," +
                            "participant_gender VARCHAR(10)," +
                            "UNIQUE (participant_first, participant_last, participant_street, participant_city, participant_state, participant_zip, participant_birthday) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (" +
                            "eventspecific_id INTEGER PRIMARY KEY," +
                            "participant_id INTEGER NOT NULL," +
                            "event_id INTEGER NOT NULL," +
                            "division_id INTEGER NOT NULL," +
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
                            "UNIQUE (participant_id, event_id) ON CONFLICT IGNORE" +
                            ")");
                        queries.Add("CREATE TABLE IF NOT EXISTS timeresults (" +
                            "event_id INTEGER NOT NULL," +
                            "eventspecific_id INTEGER NOT NULL," +
                            "timingpoint_id INTEGER NOT NULL," +
                            "timeresult_time INTEGER NOT NULL," +
                            "UNIQUE (event_id, eventspecific_id, timingpoint_id) ON CONFLICT IGNORE" +
                            ")");
                    }
                }
                else
                {
                    return;
                }
                queries.Add("CREATE TABLE IF NOT EXISTS settings (version INTEGER NOT NULL, name VARCHAR NOT NULL); INSERT INTO settings (version, name) VALUES (" + version + ", 'Northwest Endurance Events')");
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
                    "old_phone VARCHAR(20)," +
                    "old_email VARCHAR(150)," +
                    "old_emergency_id INTEGER DEFAULT -1," +
                    "old_emergency_name VARCHAR(150)," +
                    "old_emergency_phone VARCHAR(20)," +
                    "old_emergency_email VARCHAR(150)," +
                    "old_event_spec_id INTEGER DEFAULT -1," +
                    "old_event_spec_event_id INTEGER DEFAULT -1," +
                    "old_event_spec_division_id INTEGER DEFAULT -1," +
                    "old_event_spec_bib INTEGER," +
                    "old_event_spec_checkedin INTEGER DEFAULT -1," +
                    "old_event_spec_shirtsize VARCHAR," +
                    "old_event_spec_comments VARCHAR," +
                    "old_mobile VARCHAR(20)," +
                    "old_parent VARCHAR(150)," +
                    "old_country VARCHAR(50)," +
                    "old_street2 VARCHAR(50)," +
                    "old_secondshirt VARCHAR," +
                    "old_owes VARCHAR(50)," +
                    "old_hat VARCHAR(20)," +
                    "old_other VARCHAR," +
                    "old_gender VARCHAR(10)," +
                    "old_earlystart INTEGER DEFAULT -1," +
                    "old_fleece VARCHAR DEFAULT ''," +
                    "old_next_year INTEGER DEFAULT 0," +

                    "new_participant_id INTEGER NOT NULL," +
                    "new_first VARCHAR(50) NOT NULL," +
                    "new_last VARCHAR(75) NOT NULL," +
                    "new_street VARCHAR(150)," +
                    "new_city VARCHAR(75)," +
                    "new_state VARCHAR(25)," +
                    "new_zip VARCHAR(10)," +
                    "new_birthday VARCHAR(15) NOT NULL," +
                    "new_phone VARCHAR(20)," +
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
                    "new_event_spec_shirtsize VARCHAR(5)," +
                    "new_event_spec_comments VARCHAR," +
                    "new_mobile VARCHAR(20)," +
                    "new_parent VARCHAR(150)," +
                    "new_country VARCHAR(50)," +
                    "new_street2 VARCHAR(50)," +
                    "new_secondshirt VARCHAR," +
                    "new_owes VARCHAR(50)," +
                    "new_hat VARCHAR(20)," +
                    "new_other VARCHAR," +
                    "new_gender VARCHAR(10)," +
                    "new_earlystart INTEGER DEFAULT -1," +
                    "new_fleece VARCHAR DEFAULT ''," +
                    "new_next_year INTEGER DEFAULT 0" +
                    ")");
                queries.Add("INSERT INTO emergencycontacts (emergencycontact_id, emergencycontact_name) VALUES (0,'')");
                queries.Add("CREATE INDEX idx_participants_emergencyId ON participants(emergencycontact_id);");
                queries.Add("INSERT INTO participants (participant_id, participant_first, participant_last, participant_birthday, emergencycontact_id) VALUES (0, 'J', 'Doe', '01/01/1901', 0)");

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
            }
        }

        private void UpdateDatabase(int oldversion, int newversion)
        {
            Log.D("Database is version " + oldversion + " but it needs to be upgraded to version " + newversion);
            SQLiteCommand command = connection.CreateCommand();
            switch (oldversion)
            {
                case 1:
                    Log.D("Updating from version 1.");
                    using (var transaction = connection.BeginTransaction()) {
                        command.CommandText = "ALTER TABLE divisions ADD division_cost INTEGER DEFAULT 7000; ALTER TABLE eventspecific ADD eventspecific_fleece VARCHAR DEFAULT '';" +
                                "ALTER TABLE changes ADD old_fleece VARCHAR DEFAULT ''; ALTER TABLE changes ADD new_fleece VARCHAR DEFAULT '';UPDATE settings SET version=2 WHERE version=1;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 2;
                case 2:
                    Log.D("Updating from version 2.");
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE settings ADD name VARCHAR DEFAULT 'Northwest Endurance Events'; ALTER TABLE events ADD event_kiosk INTEGER DEFAULT 0; CREATE TABLE IF NOT EXISTS kiosk (event_id INTEGER NOT NULL, kiosk_waiver_text VARCHAR NOT NULL, UNIQUE (event_id) ON CONFLICT IGNORE);UPDATE settings SET version=3 WHERE version=2;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 3;
                case 3:
                    Log.D("Updating from version 3");
                    using (var transaction = connection.BeginTransaction())
                    {
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
                        transaction.Commit();
                    }
                    goto case 4;
                case 4:
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE dayof_participant ADD dop_division_id INTEGER NOT NULL DEFAULT -1;UPDATE settings SET version=5 WHERE version=4;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 5;
                case 5:
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE kiosk ADD kiosk_print_new INTEGER DEFAULT 0; UPDATE settings SET version=6 WHERE version=5;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 6;
                case 6:
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_next_year_event_id INTEGER DEFAULT -1; ALTER TABLE events ADD event_shirt_optional INTEGER DEFAULT 1; ALTER TABLE eventspecific ADD eventspecific_next_year INTEGER DEFAULT 0; ALTER TABLE changes ADD old_next_year INTEGER DEFAULT 0; ALTER TABLE changes ADD new_next_year INTEGER DEFAULT 0; UPDATE settings SET version=7 WHERE version=6;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 7;
                case 7:
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE events ADD event_shirt_price INTEGER DEFAULT 0; UPDATE settings SET version=8 WHERE version=7;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 8;
                case 8:
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "CREATE INDEX idx_participants_emergencyId ON participants(emergencycontact_id); UPDATE settings SET version=9 WHERE version=8;";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 9;
                case 9:
                    using (var transcation = connection.BeginTransaction())
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE eventspecific RENAME TO old_eventspecific;"+
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
                            ");"+
                            "INSERT INTO eventspecific SELECT * FROM old_eventspecific; UPDATE settings SET version=10 WHERE version=9;";
                        command.ExecuteNonQuery();
                        transcation.Commit();
                    }
                    goto case 10;
                case 10:
                    using (var transaction = connection.BeginTransaction())
                    {
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
                        transaction.Commit();
                    }
                    goto case 11;
                case 11:
                    using (var transaction = connection.BeginTransaction())
                    {
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
                        transaction.Commit();
                    }
                    break;
            }
        }

        public void AddDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (division_name, event_id, division_cost) values (@name,@event_id,@cost)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", div.Name),
                new SQLiteParameter("@event_id", div.EventIdentifier),
                new SQLiteParameter("@cost", div.Cost)
            });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void AddEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO events(event_name, event_date, event_shirt_optional, event_shirt_price) values(@name,@date,@so,@price)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date),
                new SQLiteParameter("@so", anEvent.ShirtOptional),
                new SQLiteParameter("@price", anEvent.ShirtPrice) });
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

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

            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT OR IGNORE INTO emergencycontacts (emergencycontact_name, emergencycontact_phone, emergencycontact_email) VALUES (@name,@phone,@email); SELECT emergencycontact_id FROM emergencycontacts WHERE emergencycontact_name=@name";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", person.EmergencyContact.Name),
                new SQLiteParameter("@phone", person.EmergencyContact.Phone),
                new SQLiteParameter("@email", person.EmergencyContact.Email) });
            Log.D("SQL query: '" + command.CommandText + "'");
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                person.EmergencyContact.Identifier = Convert.ToInt32(reader["emergencycontact_id"]);
            }
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO participants (participant_first, participant_last, participant_street, participant_city, participant_state, participant_zip, participant_birthday, emergencycontact_id, participant_phone, participant_email, participant_mobile, participant_parent, participant_country, participant_street2, participant_gender) VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@mobile,@parent,@country,@street2,@gender); SELECT participant_id FROM participants WHERE participant_first=@0 AND participant_last=@1 AND participant_street=@2 AND participant_city=@3 AND participant_state=@4 AND participant_zip=@5";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", person.FirstName),
                new SQLiteParameter("@1", person.LastName),
                new SQLiteParameter("@2", person.Street),
                new SQLiteParameter("@3", person.City),
                new SQLiteParameter("@4", person.State),
                new SQLiteParameter("@5", person.Zip),
                new SQLiteParameter("@6", person.Birthdate),
                new SQLiteParameter("@7", person.EmergencyContact.Identifier),
                new SQLiteParameter("@8", person.Phone),
                new SQLiteParameter("@9", person.Email),
                new SQLiteParameter("@mobile", person.Mobile),
                new SQLiteParameter("@parent", person.Parent),
                new SQLiteParameter("@country", person.Country),
                new SQLiteParameter("@street2", person.Street2),
                new SQLiteParameter("@gender", person.Gender) } );
            Log.D("SQL query: '" + command.CommandText + "'");
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                person.Identifier = Convert.ToInt32(reader["participant_id"]);
            }
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO eventspecific (participant_id, event_id, division_id, eventspecific_bib, eventspecific_checkedin, eventspecific_shirtsize, eventspecific_comments, eventspecific_secondshirt, eventspecific_owes, eventspecific_hat, eventspecific_other, eventspecific_earlystart, eventspecific_fleece, eventspecific_next_year) " +
                "VALUES (@0,@1,@2,@3,@5,@6,@comments,@secondshirt,@owes,@hat,@other,@earlystart,@fleece,@nextYear)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", person.Identifier),
                new SQLiteParameter("@1", person.EventSpecific.EventIdentifier),
                new SQLiteParameter("@2", person.EventSpecific.DivisionIdentifier),
                new SQLiteParameter("@3", person.EventSpecific.Bib),
                new SQLiteParameter("@5", person.EventSpecific.CheckedIn),
                new SQLiteParameter("@6", person.EventSpecific.ShirtSize),
                new SQLiteParameter("@comments", person.EventSpecific.Comments),
                new SQLiteParameter("@secondshirt", person.EventSpecific.SecondShirt),
                new SQLiteParameter("@owes", person.EventSpecific.Owes),
                new SQLiteParameter("@hat", person.EventSpecific.Hat),
                new SQLiteParameter("@other", person.EventSpecific.Other),
                new SQLiteParameter("@fleece", person.EventSpecific.Fleece),
                new SQLiteParameter("@earlystart", person.EventSpecific.EarlyStart),
                new SQLiteParameter("@nextYear", person.EventSpecific.NextYear) } );
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void AddTimingPoint(TimingPoint tp)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timingpoints (event_id, division_id, timingpoint_name, timingpoint_distance, timingpoint_unit) VALUES (@0,@1,@2,@3,@4)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", tp.EventIdentifier),
                new SQLiteParameter("@1", tp.DivisionIdentifier),
                new SQLiteParameter("@2", tp.Name),
                new SQLiteParameter("@3", tp.Distance),
                new SQLiteParameter("@4", tp.Unit) } );
            command.ExecuteNonQuery();
        }

        public void AddTimingResult(TimeResult tr)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timeresults (event_id, eventspecific_id, timingpoint_id, timeresult_time) VALUES (@0,@1,@2,@3)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", tr.EventIdentifier),
                new SQLiteParameter("@1", tr.EventSpecificId),
                new SQLiteParameter("@2", tr.TimingPointId),
                new SQLiteParameter("@3", tr.Time) } );
            command.ExecuteNonQuery();
        }

        public void RemoveDivision(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = " DELETE FROM eventspecific WHERE division_id=@0; DELETE FROM divisions WHERE division_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", identifier) } );
            command.ExecuteNonQuery();
        }

        public void RemoveDivision(Division div)
        {
            RemoveDivision(div.Identifier);
        }

        public void RemoveEvent(int identifier)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "DELETE FROM events WHERE event_id=@0; DELETE FROM divisions WHERE event_id=@0; DELETE FROM timingpoints WHERE event_id=@0; DELETE FROM timeresults WHERE event_id=@0;" +
                    "DELETE FROM eventspecific WHERE event_id=@0; DELETE FROM changes WHERE old_event_spec_event_id=@0 OR new_event_spec_event_id=@0; DELETE FROM kiosk WHERE event_id=@0;" +
                    "UPDATE events SET event_next_year_event_id='-1' WHERE event_next_year_event_id=@0";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void RemoveEvent(Event anEvent)
        {
            RemoveEvent(anEvent.Identifier);
        }

        public void RemoveParticipant(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM eventspecific WHERE participant_id=@0; DELETE FROM participant WHERE participant_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveParticipant(Participant person)
        {
            RemoveParticipant(person.Identifier);
        }

        public void RemoveTimingPoint(TimingPoint tp)
        {
            RemoveTimingPoint(tp.Identifier);
        }

        public void RemoveTimingPoint(int identifier)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM timeresults WHERE timingpoint_id=@0; DELETE FROM timingpoints WHERE timingpoint_id=@0";
            command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", identifier) });
            command.ExecuteNonQuery();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "DELETE FROM timeresults WHERE eventspecific_id=@0 AND timingpoint_id=@1";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", tr.EventSpecificId),
                new SQLiteParameter("@1", tr.TimingPointId) } );
            command.ExecuteNonQuery();
        }

        public void UpdateDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE divisions SET division_name=@0, event_id=@1, division_cost=@cost WHERE division_id=@2";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", div.Name),
                new SQLiteParameter("@1", div.EventIdentifier),
                new SQLiteParameter("@cost", div.Cost),
                new SQLiteParameter("@2", div.Identifier) } );
            command.ExecuteNonQuery();
        }

        public void UpdateEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE events SET event_name=@0, event_date=@1, event_next_year_event_id=@ny, event_shirt_optional=@so, event_shirt_price=@price WHERE event_id=@2";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", anEvent.Name),
                new SQLiteParameter("@1", anEvent.Date),
                new SQLiteParameter("@2", anEvent.Identifier),
                new SQLiteParameter("@ny", anEvent.NextYear),
                new SQLiteParameter("@so", anEvent.ShirtOptional),
                new SQLiteParameter("@price", anEvent.ShirtPrice) } );
            command.ExecuteNonQuery();
        }

        public void UpdateParticipant(Participant person)
        {
            using (var transaction = connection.BeginTransaction()) {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                Log.D("Inserting into Emergency Contacts.");
                command.CommandText = "INSERT OR IGNORE INTO emergencycontacts (emergencycontact_name, emergencycontact_phone, emergencycontact_email) VALUES (@0,@1,@2); DELETE FROM emergencycontacts WHERE emergencycontact_id IN (SELECT e.emergencycontact_id FROM emergencycontacts AS e LEFT OUTER JOIN participants AS p ON e.emergencycontact_id=p.emergencycontact_id WHERE p.participant_id IS NULL AND e.emergencycontact_id != 0); SELECT * FROM emergencycontacts WHERE emergencycontact_name=@0";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EmergencyContact.Name),
                    new SQLiteParameter("@1", person.EmergencyContact.Phone),
                    new SQLiteParameter("@2", person.EmergencyContact.Email) } );
                SQLiteDataReader reader = command.ExecuteReader();
                person.EmergencyContact.Identifier = reader.Read() ? Convert.ToInt32(reader["emergencycontact_id"]) : 0;
                Log.D("Emergency contact identifier is now " + person.EmergencyContact.Identifier + " name is " + person.EmergencyContact.Name);
                command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                Log.D("Updating participant values.");
                command.CommandText = "UPDATE participants SET participant_first=@0, participant_last=@1, participant_street=@2, participant_city=@3, participant_state=@4, participant_zip=@5, participant_birthday=@6, emergencycontact_id=@7, participant_phone=@8, participant_email=@9, participant_mobile=@mobile, participant_parent=@parent, participant_country=@country, participant_street2=@street2 WHERE participant_id=@10";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.FirstName),
                    new SQLiteParameter("@1", person.LastName),
                    new SQLiteParameter("@2", person.Street),
                    new SQLiteParameter("@3", person.City),
                    new SQLiteParameter("@4", person.State),
                    new SQLiteParameter("@5", person.Zip),
                    new SQLiteParameter("@6", person.Birthdate),
                    new SQLiteParameter("@7", person.EmergencyContact.Identifier),
                    new SQLiteParameter("@8", person.Phone),
                    new SQLiteParameter("@9", person.Email),
                    new SQLiteParameter("@10", person.Identifier),
                    new SQLiteParameter("@mobile", person.Mobile),
                    new SQLiteParameter("@parent", person.Parent),
                    new SQLiteParameter("@country", person.Country),
                    new SQLiteParameter("@street2", person.Street2) } );
                command.ExecuteNonQuery();
                command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                Log.D("Updating event specific.... bib is " + person.EventSpecific.Bib);
                command.CommandText = "UPDATE eventspecific SET division_id=@0, eventspecific_bib=@1, eventspecific_checkedin=@3, eventspecific_shirtsize=@4, eventspecific_secondshirt=@secondshirt, eventspecific_owes=@owes, eventspecific_hat=@hat, eventspecific_other=@other, eventspecific_earlystart=@earlystart, eventspecific_fleece=@fleece, eventspecific_next_year=@nextYear WHERE eventspecific_id=@5";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@1", person.EventSpecific.Bib),
                    new SQLiteParameter("@3", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@4", person.EventSpecific.ShirtSize),
                    new SQLiteParameter("@5", person.EventSpecific.Identifier),
                    new SQLiteParameter("@secondshirt", person.EventSpecific.SecondShirt),
                    new SQLiteParameter("@owes", person.EventSpecific.Owes),
                    new SQLiteParameter("@hat", person.EventSpecific.Hat),
                    new SQLiteParameter("@other", person.EventSpecific.Other),
                    new SQLiteParameter("@fleece", person.EventSpecific.Fleece),
                    new SQLiteParameter("@earlystart", person.EventSpecific.EarlyStart),
                    new SQLiteParameter("@nextYear", person.EventSpecific.NextYear)
                } );
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void UpdateTimingPoint(TimingPoint tp)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE timingpoints SET event_id=@0, division_id=@divisionId timingpoint_name=@1, timingpoint_distance=@2, timingpoint_unit=@3 WHERE timingpoint_id=@4";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", tp.EventIdentifier),
                new SQLiteParameter("@divisionId", tp.DivisionIdentifier),
                new SQLiteParameter("@1", tp.Name),
                new SQLiteParameter("@2", tp.Distance),
                new SQLiteParameter("@3", tp.Unit),
                new SQLiteParameter("@4", tp.Identifier) } );
            command.ExecuteNonQuery();
        }

        public void UpdateTimingResult(TimeResult oldResult, TimeResult newResult)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE timeresult SET timeresult_time=@0 WHERE eventspecific_id=@1 AND timingpoint_id=@2";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", newResult.Time),
                new SQLiteParameter("@1", oldResult.EventSpecificId),
                new SQLiteParameter("@2", oldResult.TimingPointId) } );
            command.ExecuteNonQuery();
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

        public void HardResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("DROP TABLE events; DROP TABLE divisions; DROP TABLE timingpoints; DROP TABLE emergencycontacts; DROP TABLE participants; DROP TABLE eventspecific; DROP TABLE timeresults; DROP TABLE changes; DROP TABLE settings", connection);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            Initialize();
        }

        public void ResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master", connection);
                SQLiteDataReader reader = command.ExecuteReader();
                command = new SQLiteCommand("DELETE FROM events; DELETE FROM divisions; DELETE FROM timingpoints; DELETE FROM emergencycontacts; DELETE FROM participants; DELETE FROM eventspecific; DELETE FROM timeresults; DELETE FROM changes; DELETE FROM settings", connection);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public List<Event> GetEvents()
        {
            List<Event> output = new List<Event>();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM events", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString(), Convert.ToInt32(reader["event_next_year_event_id"]), Convert.ToInt32(reader["event_shirt_optional"]), Convert.ToInt32(reader["event_shirt_price"])));
            }
            return output;
        }

        public List<Division> GetDivisions()
        {
            List<Division> output = new List<Division>();
            String commandTxt = "SELECT * FROM divisions";
            SQLiteCommand command = new SQLiteCommand(commandTxt, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]), reader["division_name"].ToString(), Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_cost"])));
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
                output.Add(new Division(Convert.ToInt32(reader["division_id"]), reader["division_name"].ToString(), Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_cost"])));
            }
            return output;
        }

        public List<TimingPoint> GetTimingPoints(int eventId)
        {
            List<TimingPoint> output = new List<TimingPoint>();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM timingpoints WHERE event_id=" + eventId, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingPoint(Convert.ToInt32(reader["timingpoint_id"]), Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_id"]), reader["timingpoint_name"].ToString(), reader["timingpoint_distance"].ToString(), reader["timingpoint_unit"].ToString()));
            }
            return output;
        }

        public List<Participant> GetParticipants()
        {
            Log.D("Getting all participants for all events.");
            return GetParticipantsWorker("SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific as s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND d.division_id=s.division_id", -1);
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            return GetParticipantsWorker("SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific AS s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id", eventId);
        }

        public List<Participant> GetParticipantsWorker(string query, int eventId)
        {
            List<Participant> output = new List<Participant>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (eventId != -1)
            {
                command.Parameters.Add(new SQLiteParameter("@eventid", eventId));
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
                    new EmergencyContact(
                        Convert.ToInt32(reader["emergencycontact_id"]),
                        reader["emergencycontact_name"].ToString(),
                        reader["emergencycontact_phone"].ToString(),
                        reader["emergencycontact_email"].ToString()
                        ),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_shirtsize"].ToString(),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_secondshirt"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_hat"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        reader["eventspecific_fleece"].ToString(),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_phone"].ToString(),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString()
                    ));
            }
            return output;
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            Log.D("Getting timing results for event id of " + eventId);
            List<TimeResult> output = new List<TimeResult>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM timeresults WHERE event_id=@eventid";
            command.Parameters.Add(new SQLiteParameter("@eventid",eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimeResult(
                    Convert.ToInt32(reader["event_id"]),
                    Convert.ToInt32(reader["eventspecific_id"]),
                    Convert.ToInt32(reader["timingpoint_id"]),
                    Convert.ToInt32(reader["timeresult_time"])
                    ));
            }
            return output;
        }

        public void AddChange(Participant newParticipant, Participant oldParticipant)
        {
            SQLiteCommand command = connection.CreateCommand();
            Log.D("Adding change - new participant id is " + newParticipant.Identifier + " old participant id is" + (oldParticipant == null ? -1 : oldParticipant.Identifier));
            if (oldParticipant == null)
            {
                Log.D("Found an add player change.");
                command.CommandText = "INSERT INTO changes (" +
                    "old_participant_id, old_first, old_last, old_street, old_city, old_state, old_zip, old_birthday, old_phone, old_email," +
                    "old_emergency_id, old_emergency_name, old_emergency_phone, old_emergency_email, old_event_spec_id, old_event_spec_event_id," +
                    "old_event_spec_division_id, old_event_spec_bib, old_event_spec_checkedin, old_event_spec_shirtsize," +
                    "old_event_spec_comments, old_mobile, old_parent, old_country, old_street2, old_secondshirt, old_owes, old_hat, old_other," +
                    "old_gender, old_earlystart, old_fleece," +
                    "new_participant_id, new_first, new_last, new_street, new_city, new_state, new_zip, new_birthday, new_phone, new_email," +
                    "new_emergency_id, new_emergency_name, new_emergency_phone, new_emergency_email, new_event_spec_id, new_event_spec_event_id," +
                    "new_event_spec_division_id, new_event_spec_bib, new_event_spec_checkedin, new_event_spec_shirtsize," +
                    "new_event_spec_comments, new_mobile, new_parent, new_country, new_street2, new_secondshirt, new_owes, new_hat, new_other," +
                    "new_gender, new_earlystart, new_fleece)" +
                    " VALUES" +
                    "(0, 'J', 'Doe', '', '', '', '', '01/01/1901', '', '', " +
                    "0, '', '', '', 0, @newESEvId, " +
                    "-1, -1, 0, '', " +
                    "'New Participant', '', '', '', '', '', '', '', ''," +
                    "'', 0, ''," +
                    " @newPartId, @newFirst, @newLast, @newStreet," +
                    "@newCity, @newState, @newZip, @newBirthday, @newPhone, @newEmail, @newEId, @newEName, @newEPhone, @newEEmail, @newESId, @newESEvId," +
                    "@newESDId, @newESBib, @newESCheckedIn, @newESShirtSize, @newESComments, @newMobile, @newParent, @newCountry, @newStreet2," +
                    "@newShirt2, @newOwes, @newHat, @newOther, @newGender, @newEarlyStart, @newFleece)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@newPartId", newParticipant.Identifier),
                    new SQLiteParameter("@newFirst", newParticipant.FirstName),
                    new SQLiteParameter("@newLast", newParticipant.LastName),
                    new SQLiteParameter("@newStreet", newParticipant.Street),
                    new SQLiteParameter("@newCity", newParticipant.City),
                    new SQLiteParameter("@newState", newParticipant.State),
                    new SQLiteParameter("@newZip", newParticipant.Zip),
                    new SQLiteParameter("@newBirthday", newParticipant.Birthdate),
                    new SQLiteParameter("@newPhone", newParticipant.Phone),
                    new SQLiteParameter("@newEmail", newParticipant.Email),
                    new SQLiteParameter("@newEId", newParticipant.EmergencyContact.Identifier),
                    new SQLiteParameter("@newEName", newParticipant.EmergencyContact.Name),
                    new SQLiteParameter("@newEPhone", newParticipant.EmergencyContact.Phone),
                    new SQLiteParameter("@newEEmail", newParticipant.EmergencyContact.Email),
                    new SQLiteParameter("@newESId", newParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@newESEvId", newParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@newESDId", newParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@newESBib", newParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@newESCheckedIn", newParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@newESShirtSize", newParticipant.EventSpecific.ShirtSize),
                    new SQLiteParameter("@newESComments", newParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@newMobile", newParticipant.Mobile),
                    new SQLiteParameter("@newParent", newParticipant.Parent),
                    new SQLiteParameter("@newCountry", newParticipant.Country),
                    new SQLiteParameter("@newStreet2", newParticipant.Street2),
                    new SQLiteParameter("@newShirt2", newParticipant.EventSpecific.SecondShirt),
                    new SQLiteParameter("@newOwes", newParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@newHat", newParticipant.EventSpecific.Hat),
                    new SQLiteParameter("@newOther", newParticipant.EventSpecific.Other),
                    new SQLiteParameter("@newGender", newParticipant.Gender),
                    new SQLiteParameter("@newEarlyStart", newParticipant.EventSpecific.EarlyStart),
                    new SQLiteParameter("@newFleece", newParticipant.EventSpecific.Fleece)
                });
                command.ExecuteNonQuery();
            }
            else
            {
                Log.D("An update occured.");
                command.CommandText = "INSERT INTO changes (" +
                    "old_participant_id, old_first, old_last, old_street, old_city, old_state, old_zip, old_birthday, old_phone, old_email," +
                    "old_emergency_id, old_emergency_name, old_emergency_phone, old_emergency_email, old_event_spec_id, old_event_spec_event_id," +
                    "old_event_spec_division_id, old_event_spec_bib, old_event_spec_checkedin, old_event_spec_shirtsize," +
                    "old_event_spec_comments, old_mobile, old_parent, old_country, old_street2, old_secondshirt, old_owes, old_hat, old_other," +
                    "old_gender, old_earlystart, old_fleece," +
                    "new_participant_id, new_first, new_last, new_street, new_city, new_state, new_zip, new_birthday, new_phone, new_email," +
                    "new_emergency_id, new_emergency_name, new_emergency_phone, new_emergency_email, new_event_spec_id, new_event_spec_event_id," +
                    "new_event_spec_division_id, new_event_spec_bib, new_event_spec_checkedin, new_event_spec_shirtsize," +
                    "new_event_spec_comments, new_mobile, new_parent, new_country, new_street2, new_secondshirt, new_owes, new_hat, new_other," +
                    "new_gender, new_earlystart, new_fleece)" +
                    "VALUES" +
                    "(@oldPartId, @oldFirst, @oldLast, @oldStreet, @oldCity, @oldState, @oldZip, @oldBirthday, @oldPhone, @oldEmail," +
                    "@oldEId, @oldEName, @oldEPhone, @oldEEmail, @oldESId, @oldESEvId," +
                    "@oldESDId, @oldESBib, @oldESCheckedIn, @oldESShirtSize," +
                    "@oldESComments, @oldMobile, @oldParent, @oldCountry, @oldStreet2, @oldShirt2, @oldOwes, @oldHat, @oldOther," +
                    "@oldGender, @oldEarlyStart, @oldFleece," +
                    "@newPartId, @newFirst, @newLast, @newStreet, @newCity, @newState, @newZip, @newBirthday, @newPhone, @newEmail," +
                    "@newEId, @newEName, @newEPhone, @newEEmail, @newESId, @newESEvId," +
                    "@newESDId, @newESBib, @newESCheckedIn, @newESShirtSize," +
                    "@newESComments, @newMobile, @newParent, @newCountry, @newStreet2, @newShirt2, @newOwes, @newHat, @newOther," +
                    "@newGender, @newEarlyStart, @newFleece)";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@oldPartId", oldParticipant.Identifier),
                    new SQLiteParameter("@oldFirst", oldParticipant.FirstName),
                    new SQLiteParameter("@oldLast", oldParticipant.LastName),
                    new SQLiteParameter("@oldStreet", oldParticipant.Street),
                    new SQLiteParameter("@oldCity", oldParticipant.City),
                    new SQLiteParameter("@oldState", oldParticipant.State),
                    new SQLiteParameter("@oldZip", oldParticipant.Zip),
                    new SQLiteParameter("@oldBirthday", oldParticipant.Birthdate),
                    new SQLiteParameter("@oldPhone", oldParticipant.Phone),
                    new SQLiteParameter("@oldEmail", oldParticipant.Email),
                    new SQLiteParameter("@oldEId", oldParticipant.EmergencyContact.Identifier),
                    new SQLiteParameter("@oldEName", oldParticipant.EmergencyContact.Name),
                    new SQLiteParameter("@oldEPhone", oldParticipant.EmergencyContact.Phone),
                    new SQLiteParameter("@oldEEmail", oldParticipant.EmergencyContact.Email),
                    new SQLiteParameter("@oldESId", oldParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@oldESEvId", oldParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@oldESDId", oldParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@oldESBib", oldParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@oldESCheckedIn", oldParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@oldESShirtSize", oldParticipant.EventSpecific.ShirtSize),
                    new SQLiteParameter("@oldESComments", oldParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@oldMobile", oldParticipant.Mobile),
                    new SQLiteParameter("@oldParent", oldParticipant.Parent),
                    new SQLiteParameter("@oldCountry", oldParticipant.Country),
                    new SQLiteParameter("@oldStreet2", oldParticipant.Street2),
                    new SQLiteParameter("@oldShirt2", oldParticipant.EventSpecific.SecondShirt),
                    new SQLiteParameter("@oldOwes", oldParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@oldHat", oldParticipant.EventSpecific.Hat),
                    new SQLiteParameter("@oldOther", oldParticipant.EventSpecific.Other),
                    new SQLiteParameter("@oldGender", oldParticipant.Gender),
                    new SQLiteParameter("@oldEarlyStart", oldParticipant.EventSpecific.EarlyStart),
                    new SQLiteParameter("@oldFleece", oldParticipant.EventSpecific.Fleece),

                    new SQLiteParameter("@newPartId", newParticipant.Identifier),
                    new SQLiteParameter("@newFirst", newParticipant.FirstName),
                    new SQLiteParameter("@newLast", newParticipant.LastName),
                    new SQLiteParameter("@newStreet", newParticipant.Street),
                    new SQLiteParameter("@newCity", newParticipant.City),
                    new SQLiteParameter("@newState", newParticipant.State),
                    new SQLiteParameter("@newZip", newParticipant.Zip),
                    new SQLiteParameter("@newBirthday", newParticipant.Birthdate),
                    new SQLiteParameter("@newPhone", newParticipant.Phone),
                    new SQLiteParameter("@newEmail", newParticipant.Email),
                    new SQLiteParameter("@newEId", newParticipant.EmergencyContact.Identifier),
                    new SQLiteParameter("@newEName", newParticipant.EmergencyContact.Name),
                    new SQLiteParameter("@newEPhone", newParticipant.EmergencyContact.Phone),
                    new SQLiteParameter("@newEEmail", newParticipant.EmergencyContact.Email),
                    new SQLiteParameter("@newESId", newParticipant.EventSpecific.Identifier),
                    new SQLiteParameter("@newESEvId", newParticipant.EventSpecific.EventIdentifier),
                    new SQLiteParameter("@newESDId", newParticipant.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@newESBib", newParticipant.EventSpecific.Bib),
                    new SQLiteParameter("@newESCheckedIn", newParticipant.EventSpecific.CheckedIn),
                    new SQLiteParameter("@newESShirtSize", newParticipant.EventSpecific.ShirtSize),
                    new SQLiteParameter("@newESComments", newParticipant.EventSpecific.Comments),
                    new SQLiteParameter("@newMobile", newParticipant.Mobile),
                    new SQLiteParameter("@newParent", newParticipant.Parent),
                    new SQLiteParameter("@newCountry", newParticipant.Country),
                    new SQLiteParameter("@newStreet2", newParticipant.Street2),
                    new SQLiteParameter("@newShirt2", newParticipant.EventSpecific.SecondShirt),
                    new SQLiteParameter("@newOwes", newParticipant.EventSpecific.Owes),
                    new SQLiteParameter("@newHat", newParticipant.EventSpecific.Hat),
                    new SQLiteParameter("@newOther", newParticipant.EventSpecific.Other),
                    new SQLiteParameter("@newGender", newParticipant.Gender),
                    new SQLiteParameter("@newEarlyStart", newParticipant.EventSpecific.EarlyStart),
                    new SQLiteParameter("@newFleece", newParticipant.EventSpecific.Fleece)
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
                        new EmergencyContact(Convert.ToInt32(reader["new_emergency_id"]),
                            reader["new_emergency_name"].ToString(),
                            reader["new_emergency_phone"].ToString(),
                            reader["new_emergency_email"].ToString()),
                        new EventSpecific(Convert.ToInt32(reader["new_event_spec_id"]),
                            Convert.ToInt32(reader["new_event_spec_event_id"]),
                            Convert.ToInt32(reader["new_event_spec_division_id"]),
                            divisions[Convert.ToInt32(reader["new_event_spec_division_id"])].ToString(),
                            Convert.ToInt32(reader["new_event_spec_bib"]),
                            Convert.ToInt32(reader["new_event_spec_checkedin"]),
                            reader["new_event_spec_shirtsize"].ToString(),
                            reader["new_event_spec_comments"].ToString(),
                            reader["new_secondshirt"].ToString(),
                            reader["new_owes"].ToString(),
                            reader["new_hat"].ToString(),
                            reader["new_other"].ToString(),
                            Convert.ToInt32(reader["new_earlystart"]),
                            reader["new_fleece"].ToString(),
                            Convert.ToInt32(reader["new_next_year"])
                            ),
                        reader["new_phone"].ToString(),
                        reader["new_email"].ToString(),
                        reader["new_mobile"].ToString(),
                        reader["new_parent"].ToString(),
                        reader["new_country"].ToString(),
                        reader["new_street2"].ToString(),
                        reader["old_gender"].ToString()
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
                        new EmergencyContact(
                            Convert.ToInt32(reader["old_emergency_id"]),
                            reader["old_emergency_name"].ToString(),
                            reader["old_emergency_phone"].ToString(),
                            reader["old_emergency_email"].ToString()),
                        new EventSpecific(
                            Convert.ToInt32(reader["old_event_spec_id"]),
                            Convert.ToInt32(reader["old_event_spec_event_id"]),
                            Convert.ToInt32(reader["old_event_spec_division_id"]),
                            oldDivName,
                            Convert.ToInt32(reader["old_event_spec_bib"]),
                            Convert.ToInt32(reader["old_event_spec_checkedin"]),
                            reader["old_event_spec_shirtsize"].ToString(),
                            reader["old_event_spec_comments"].ToString(),
                            reader["old_secondshirt"].ToString(),
                            reader["old_owes"].ToString(),
                            reader["old_hat"].ToString(),
                            reader["old_other"].ToString(),
                            Convert.ToInt32(reader["old_earlystart"]),
                            reader["old_fleece"].ToString(),
                            Convert.ToInt32(reader["old_next_year"])
                            ),
                        reader["old_phone"].ToString(),
                        reader["old_email"].ToString(),
                        reader["old_mobile"].ToString(),
                        reader["old_parent"].ToString(),
                        reader["old_country"].ToString(),
                        reader["old_street2"].ToString(),
                        reader["old_gender"].ToString()
                    )
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

        public int GetTimingPointID(TimingPoint tp)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT timingpoint_id FROM timingpoints WHERE event_id=@eventid AND division_id=@divid AND timingpoint_name=@name";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@name", tp.Name),
                new SQLiteParameter("@eventid", tp.EventIdentifier),
                new SQLiteParameter("@divid",tp.DivisionIdentifier)
            });
            SQLiteDataReader reader = command.ExecuteReader();
            int output = -1;
            if (reader.Read())
            {
                output = Convert.ToInt32(reader["timingpoint_id"]);
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

        public Event GetEvent(int id)
        {
            if (id < 0)
            {
                return null;
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id",id));
            SQLiteDataReader reader = command.ExecuteReader();
            Event output = null;
            if (reader.Read())
            {
                output = new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString(), Convert.ToInt32(reader["event_next_year_event_id"]), Convert.ToInt32(reader["event_shirt_optional"]), Convert.ToInt32(reader["event_shirt_price"]));
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

        public Participant GetParticipant(int eventId, int identifier)
        {
            Participant output = null;
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific AS s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND p.participant_id=@partId";
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
                    new EmergencyContact(
                        Convert.ToInt32(reader["emergencycontact_id"]),
                        reader["emergencycontact_name"].ToString(),
                        reader["emergencycontact_phone"].ToString(),
                        reader["emergencycontact_email"].ToString()
                        ),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_shirtsize"].ToString(),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_secondshirt"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_hat"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        reader["eventspecific_fleece"].ToString(),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_phone"].ToString(),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString()
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
                command.CommandText = "SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific AS s, divisions AS d, bib_chip_assoc as b WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND " +
                    "s.eventspecific_bib=b.bib AND b.chip=@chip AND b.event_id=s.event_id;";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@eventid", eventId),
                    new SQLiteParameter("@chip", unknown.EventSpecific.Chip),
                });
            } else
            {
                command.CommandText = "SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific AS s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id AND " +
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
                    new EmergencyContact(
                        Convert.ToInt32(reader["emergencycontact_id"]),
                        reader["emergencycontact_name"].ToString(),
                        reader["emergencycontact_phone"].ToString(),
                        reader["emergencycontact_email"].ToString()
                        ),
                    new EventSpecific(
                        Convert.ToInt32(reader["eventspecific_id"]),
                        Convert.ToInt32(reader["event_id"]),
                        Convert.ToInt32(reader["division_id"]),
                        reader["division_name"].ToString(),
                        Convert.ToInt32(reader["eventspecific_bib"]),
                        Convert.ToInt32(reader["chip"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_shirtsize"].ToString(),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_secondshirt"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_hat"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"]),
                        reader["eventspecific_fleece"].ToString(),
                        Convert.ToInt32(reader["eventspecific_next_year"])
                        ),
                    reader["participant_phone"].ToString(),
                    reader["participant_email"].ToString(),
                    reader["participant_mobile"].ToString(),
                    reader["participant_parent"].ToString(),
                    reader["participant_country"].ToString(),
                    reader["participant_street2"].ToString(),
                    reader["participant_gender"].ToString()
                    );
            }
            return output;
        }

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

        public void AddDayOfParticipant(DayOfParticipant part)
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO dayof_participant (dop_event_id, dop_division_id, dop_first, dop_last, dop_street, dop_city, dop_state, dop_zip, dop_birthday, dop_phone, dop_email, dop_mobile, dop_parent, dop_country, dop_street2, dop_gender, dop_comments, dop_other, dop_other2, dop_emergency_name, dop_emergency_phone)" +
                                                            " VALUES (@eventId, @divisionId, @first, @last, @street, @city, @state, @zip, @birthday, @phone, @email, @mobile, @parent, @country, @street2, @gender, @comments, @other, @other2, @eName, @ePhone);";
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
                    new SQLiteParameter("@phone", part.Phone),
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
                    reader["dop_phone"].ToString(),
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
                            "",
                            reader["dop_comments"].ToString(),
                            "",
                            "",
                            "",
                            reader["dop_other"].ToString(),
                            earlystart,
                            "",
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
                            new EmergencyContact(
                                reader["dop_emergency_name"].ToString(),
                                reader["dop_emergency_phone"].ToString(),
                                ""
                                ),
                            newSpecific,
                            reader["dop_phone"].ToString(),
                            reader["dop_email"].ToString(),
                            reader["dop_mobile"].ToString(),
                            reader["dop_parent"].ToString(),
                            reader["dop_country"].ToString(),
                            reader["dop_street2"].ToString(),
                            reader["dop_gender"].ToString()
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
                            reader["dop_phone"].ToString(),
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
    }
}
