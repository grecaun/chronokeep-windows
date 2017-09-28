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
        private readonly int version = 1;
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
                command = new SQLiteCommand("SELECT version FROM settings", connection);
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
                    queries.Add("CREATE TABLE IF NOT EXISTS events (" +
                            "event_id INTEGER PRIMARY KEY," +
                            "event_name VARCHAR(100) NOT NULL," +
                            "event_date VARCHAR(15) NOT NULL," +
                            "event_registration_open INTEGER DEFAULT 0," +
                            "event_results_open INTEGER DEFAULT 0," +
                            "event_announce_available INTEGER DEFAULT 0," +
                            "event_allow_early_start INTEGER DEFAULT 0," +
                            "UNIQUE (event_name, event_date) ON CONFLICT IGNORE" +
                            ")");
                    queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (" +
                            "emergencycontact_id INTEGER PRIMARY KEY," +
                            "emergencycontact_name VARCHAR(150) UNIQUE NOT NULL," +
                            "emergencycontact_phone VARCHAR(20)," +
                            "emergencycontact_email VARCHAR(150)" +
                            ")");
                    if (reader.GetInt32(0) == 1)
                    {
                        Log.D("Foreign keys work. Setting table creation queries.");
                        queries.Add("CREATE TABLE IF NOT EXISTS divisions (" +
                            "division_id INTEGER PRIMARY KEY," +
                            "division_name VARCHAR(100) NOT NULL," +
                            "event_id INTEGER NOT NULL REFERENCES events(event_id)," +
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
                            "eventspecific_chip INTEGER," +
                            "eventspecific_checkedin INTEGER DEFAULT 0," +
                            "eventspecific_shirtsize VARCHAR(5)," +
                            "eventspecific_comments VARCHAR," +
                            "eventspecific_secondshirt VARCHAR," +
                            "eventspecific_owes VARCHAR(50)," +
                            "eventspecific_hat VARCHAR(20)," +
                            "eventspecific_other VARCHAR," +
                            "eventspecific_earlystart INTEGER DEFAULT 0," +
                            "UNIQUE (participant_id, event_id) ON CONFLICT IGNORE" +
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
                        queries.Add("CREATE TABLE IF NOT EXISTS divisions (" +
                            "division_id INTEGER PRIMARY KEY," +
                            "division_name VARCHAR(100) NOT NULL," +
                            "event_id INTEGER NOT NULL," +
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
                            "eventspecific_chip INTEGER," +
                            "eventspecific_checkedin INTEGER DEFAULT 0," +
                            "eventspecific_shirtsize VARCHAR(5)," +
                            "eventspecific_comments VARCHAR," +
                            "eventspecific_secondshirt VARCHAR," +
                            "eventspecific_owes VARCHAR(50)," +
                            "eventspecific_hat VARCHAR(20)," +
                            "eventspecific_other VARCHAR," +
                            "eventspecific_earlystart INTEGER DEFAULT 0," +
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
                queries.Add("CREATE TABLE IF NOT EXISTS settings (version INTEGER NOT NULL); INSERT INTO settings (version) VALUES (" + version + ")");
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
                    "old_emergency_id INTEGER," +
                    "old_emergency_name VARCHAR(150)," +
                    "old_emergency_phone VARCHAR(20)," +
                    "old_emergency_email VARCHAR(150)," +
                    "old_event_spec_id INTEGER NOT NULL," +
                    "old_event_spec_event_id INTEGER NOT NULL," +
                    "old_event_spec_division_id INTEGER NOT NULL," +
                    "old_event_spec_bib INTEGER," +
                    "old_event_spec_chip INTEGER," +
                    "old_event_spec_checkedin INTEGER NOT NULL," +
                    "old_event_spec_shirtsize VARCHAR(5)," +
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
                    "old_earlystart INTEGER," +

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
                    "new_emergency_id INTEGER," +
                    "new_emergency_name VARCHAR(150)," +
                    "new_emergency_phone VARCHAR(20)," +
                    "new_emergency_email VARCHAR(150)," +
                    "new_event_spec_id INTEGER NOT NULL," +
                    "new_event_spec_event_id INTEGER NOT NULL," +
                    "new_event_spec_division_id INTEGER NOT NULL," +
                    "new_event_spec_bib INTEGER," +
                    "new_event_spec_chip INTEGER," +
                    "new_event_spec_checkedin INTEGER NOT NULL," +
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
                    "new_earlystart INTEGER" +
                    ")");
                queries.Add("INSERT INTO emergencycontacts (emergencycontact_id, emergencycontact_name) VALUES (0,'')");

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
            switch (oldversion)
            {
                case 1:
                    Log.D("Updating from version 1.");
                    break;
            }
        }

        public void AddDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (division_name, event_id) values (@name,@event_id)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", div.Name),
                new SQLiteParameter("@event_id", div.EventIdentifier)});
            Log.D("SQL query: '" + command.CommandText + "'");
            command.ExecuteNonQuery();
        }

        public void AddEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO events(event_name, event_date) values(@name,@date)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@name", anEvent.Name),
                new SQLiteParameter("@date", anEvent.Date) });
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
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                person.Identifier = Convert.ToInt32(reader["participant_id"]);
            }
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO eventspecific (participant_id, event_id, division_id, eventspecific_bib, eventspecific_chip, eventspecific_checkedin, eventspecific_shirtsize, eventspecific_comments, eventspecific_secondshirt, eventspecific_owes, eventspecific_hat, eventspecific_other, eventspecific_earlystart) VALUES (@0,@1,@2,@3,@4,@5,@6,@comments,@secondshirt,@owes,@hat,@other,@earlystart)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", person.Identifier),
                new SQLiteParameter("@1", person.EventSpecific.EventIdentifier),
                new SQLiteParameter("@2", person.EventSpecific.DivisionIdentifier),
                new SQLiteParameter("@3", person.EventSpecific.Bib),
                new SQLiteParameter("@4", person.EventSpecific.Chip),
                new SQLiteParameter("@5", person.EventSpecific.CheckedIn),
                new SQLiteParameter("@6", person.EventSpecific.ShirtSize),
                new SQLiteParameter("@comments", person.EventSpecific.Comments),
                new SQLiteParameter("@secondshirt", person.EventSpecific.SecondShirt),
                new SQLiteParameter("@owes", person.EventSpecific.Owes),
                new SQLiteParameter("@hat", person.EventSpecific.Hat),
                new SQLiteParameter("@other", person.EventSpecific.Other),
                new SQLiteParameter("@earlystart", person.EventSpecific.EarlyStart) } );
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
            command.CommandText = "DELETE FROM divisions WHERE division_id=@0";
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
                command.CommandText = "DELETE FROM events WHERE event_id=@0; DELETE FROM divisions WHERE event_id=@0; DELETE FROM timingpoints WHERE event_id=@0; DELETE FROM timeresults WHERE event_id=@0; DELETE FROM eventspecific WHERE event_id=@0";
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
            command.CommandText = "DELETE FROM timingpoints WHERE timingpoint_id=@0";
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
            command.CommandText = "UPDATE divisions SET name=@0, event_id=@1 WHERE division_id=@2";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", div.Name),
                new SQLiteParameter("@1", div.EventIdentifier),
                new SQLiteParameter("@2", div.Identifier) } );
            command.ExecuteNonQuery();
        }

        public void UpdateEvent(Event anEvent)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE events SET event_name=@0, event_date=@1 WHERE event_id=@2";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", anEvent.Name),
                new SQLiteParameter("@1", anEvent.Date),
                new SQLiteParameter("@2", anEvent.Identifier) } );
            command.ExecuteNonQuery();
        }

        public void UpdateParticipant(Participant person)
        {
            using (var transaction = connection.BeginTransaction()) {
                SQLiteCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "INSERT INTO emergencycontacts (emergencycontact_name, emergencycontact_phone, emergencycontact_email) VALUES (@0,@1,@2); DELETE FROM emergencycontacts AS e LEFT OUTER JOIN participants AS p on e.emergencycontact_id=p.emergencycontact_id WHERE p.participant_id IS NULL AND e.emergencycontact_id != 0; SELECT emergencycontact_id FROM emergencycontacts WHERE emergencycontact_name=@0";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EmergencyContact.Name),
                    new SQLiteParameter("@1", person.EmergencyContact.Phone),
                    new SQLiteParameter("@2", person.EmergencyContact.Email) } );
                SQLiteDataReader reader = command.ExecuteReader();
                person.EmergencyContact.Identifier = reader.Read() ? Convert.ToInt32(reader["emergencycontact_id"]) : 0;
                command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
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
                command.CommandText = "UPDATE eventspecific SET division_id=@0, eventspecific_bib=@1, eventspecific_chip=@2, eventspecific_checkedin=@3, eventspecific_shirtsize=@4, eventspecific_secondshirt=@secondshirt, eventspecific_owes=@owes, eventspecific_hat=@hat, eventspecific_other=@other WHERE eventspecific_id=@5";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@1", person.EventSpecific.Bib),
                    new SQLiteParameter("@2", person.EventSpecific.Chip),
                    new SQLiteParameter("@3", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@4", person.EventSpecific.ShirtSize),
                    new SQLiteParameter("@5", person.EventSpecific.Identifier),
                    new SQLiteParameter("@secondshirt", person.EventSpecific.SecondShirt),
                    new SQLiteParameter("@owes", person.EventSpecific.Owes),
                    new SQLiteParameter("@hat", person.EventSpecific.Hat),
                    new SQLiteParameter("@other", person.EventSpecific.Other) } );
                command.ExecuteNonQuery();
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

        public void CheckInParticipant(int identifier, int checkedIn)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "UPDATE eventspecific SET eventspecific_checkedin=@0 WHERE eventspecific_id=@1";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", checkedIn),
                new SQLiteParameter("@1", identifier) });
            command.ExecuteNonQuery();
        }

        public void CheckInParticipant(Participant person)
        {
            CheckInParticipant(person.EventSpecific.Identifier, person.EventSpecific.CheckedIn);
        }

        public void HardResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master", connection);
                SQLiteDataReader reader = command.ExecuteReader();
                command = new SQLiteCommand("DROP TABLE events; DROP TABLE divisions; DROP TABLE timingpoints; DROP TABLE emergencycontacts; DROP TABLE participants; DROP TABLE eventspecific; DROP TABLE timeresults; DROP TABLE changes; DROP TABLE settings", connection);
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
                output.Add(new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString()));
            }
            return output;
        }

        public List<Division> GetDivisions(int eventId)
        {
            String commandTxt;
            if (eventId != -1)
            {
                commandTxt = "SELECT * FROM divisions WHERE event_id = "+eventId;
            }
            else
            {
                commandTxt = "SELECT * FROM divisions";
            }
            List<Division> output = new List<Division>();
            SQLiteCommand command = new SQLiteCommand(commandTxt, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]),reader["division_name"].ToString(),Convert.ToInt32(reader["event_id"])));
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
                        Convert.ToInt32(reader["eventspecific_chip"]),
                        Convert.ToInt32(reader["eventspecific_checkedin"]),
                        reader["eventspecific_shirtsize"].ToString(),
                        reader["eventspecific_comments"].ToString(),
                        reader["eventspecific_secondshirt"].ToString(),
                        reader["eventspecific_owes"].ToString(),
                        reader["eventspecific_hat"].ToString(),
                        reader["eventspecific_other"].ToString(),
                        Convert.ToInt32(reader["eventspecific_earlystart"])
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
            Log.D("Adding change - new participant id is " + newParticipant.Identifier + " old participant id is" + oldParticipant.Identifier);
            if (newParticipant.Identifier == oldParticipant.Identifier && newParticipant.EventSpecific.EventIdentifier == oldParticipant.EventSpecific.EventIdentifier)
            {
                Log.D("Valid change found.");
                UpdateParticipant(newParticipant);
            }
            else
            {
                Log.D("Invalid change attempt. Nothing will be done.");
            }
        }

        public List<Change> GetChanges(int eventId)
        {
            Log.D("Getting changes.");
            List<Change> output = new List<Change>();
            Hashtable divisions = new Hashtable();
            List<Division> divs = GetDivisions(-1);
            foreach (Division d in divs)
            {
                divisions.Add(d.Identifier, d.Name);
            }
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM changes WHERE old_event_spec_event_id=@eventId";
            command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Change(
                    Convert.ToInt32(reader["change_id"]),
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
                            divisions[Convert.ToInt32(reader["old_event_spec_division_id"])].ToString(),
                            Convert.ToInt32(reader["old_event_spec_bib"]),
                            Convert.ToInt32(reader["old_event_spec_chip"]),
                            Convert.ToInt32(reader["old_event_spec_checkedin"]),
                            reader["old_event_spec_shirtsize"].ToString(),
                            reader["old_event_spec_comments"].ToString(),
                            reader["old_secondshirt"].ToString(),
                            reader["old_owes"].ToString(),
                            reader["old_hat"].ToString(),
                            reader["old_other"].ToString(),
                            Convert.ToInt32(reader["old_earlystart"])
                            ),
                        reader["old_phone"].ToString(),
                        reader["old_email"].ToString(),
                        reader["old_mobile"].ToString(),
                        reader["old_parent"].ToString(),
                        reader["old_country"].ToString(),
                        reader["old_street2"].ToString(),
                        reader["old_gender"].ToString()
                    ),
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
                            Convert.ToInt32(reader["new_event_spec_chip"]),
                            Convert.ToInt32(reader["new_event_spec_checkedin"]),
                            reader["new_event_spec_shirtsize"].ToString(),
                            reader["new_event_spec_comments"].ToString(),
                            reader["new_secondshirt"].ToString(),
                            reader["new_owes"].ToString(),
                            reader["new_hat"].ToString(),
                            reader["new_other"].ToString(),
                            Convert.ToInt32(reader["new_earlystart"])
                            ),
                        reader["new_phone"].ToString(),
                        reader["new_email"].ToString(),
                        reader["new_mobile"].ToString(),
                        reader["new_parent"].ToString(),
                        reader["new_country"].ToString(),
                        reader["new_street2"].ToString(),
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

        public void SetEarlyStartParticipant(int identifier, int earlystart)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE eventspecific SET eventspecific_earlystart=@earlystart WHERE eventspecific_id=@id";
            command.Parameters.AddRange(new SQLiteParameter[]
            {
                new SQLiteParameter("@earlystart", earlystart),
                new SQLiteParameter("@id", identifier)
            });
            command.ExecuteNonQuery();
        }

        public void SetEarlyStartParticipant(Participant person)
        {
            SetEarlyStartParticipant(person.EventSpecific.Identifier, person.EventSpecific.EarlyStart);
        }

        public Event GetEvent(int id)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM events WHERE event_id=@id";
            command.Parameters.Add(new SQLiteParameter("@id",id));
            SQLiteDataReader reader = command.ExecuteReader();
            Event output = null;
            if (reader.Read())
            {
                output = new Event(Convert.ToInt32(reader["event_id"]), reader["event_name"].ToString(), reader["event_date"].ToString());
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
            }
            return output;
        }

        public void SetEventOptions(int eventId, List<JsonOption> options)
        {
            List<JsonOption> output = new List<JsonOption>();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE events SET event_allow_early_start=@es, event_announce_available=@announce, event_results_open=@results, event_registration_open=@registration WHERE event_id=@id";
            int es = 0, results = 0, registration = 0, announce = 0;
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
                }
            }
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@es", es),
                new SQLiteParameter("@announce", announce),
                new SQLiteParameter("@results", results),
                new SQLiteParameter("@registration", registration),
                new SQLiteParameter("@id", eventId)
            });
            command.ExecuteNonQuery();
        }
    }
}
