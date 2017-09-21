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
        private readonly int version = 3;
        SQLiteConnection connection;

        public SQLiteInterface(String info)
        {
            connection = new SQLiteConnection(String.Format("Data Source=%s;Version=3", info));
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
                reader.Read();
                int oldVersion = Convert.ToInt32(reader["version"]);
                if (oldVersion < version) UpdateDatabase(oldVersion,version);
            }
            else
            {
                Log.D("Tables haven't been created. Doing so now.");
                command = new SQLiteCommand("PRAGMA foreign_keys = ON; PRAGMA foreign_keys", connection); // Ensure Foreign key constraints work.
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    if (reader.GetInt32(0) == 1)
                    {
                        Log.D("Foreign keys work. Setting table creation queries.");
                        queries.Add("CREATE TABLE IF NOT EXISTS events (event_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, date INTEGER NOT NULL)");
                        queries.Add("CREATE TABLE IF NOT EXISTS divisions (division_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, event_id INTEGER NOT NULL REFERENCES events(event_id))");
                        queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (timingpoint_id INTEGER PRIMARY KEY, event_id INTEGER NOT NULL REFERENCES events(event_id), division_id INTEGER NOT NULL REFERENCES divisions(division_id), name VARCHAR(100) NOT NULL, distance VARCHAR(5), unit VARCHAR(2))");
                        queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (emergencycontact_id INTEGER PRIMARY KEY, name VARCHAR(150) UNIQUE NOT NULL, phone VARCHAR(20), email VARCHAR(150))");
                        queries.Add("CREATE TABLE IF NOT EXISTS participants (participant_id INTEGER PRIMARY KEY, first VARCHAR(50) NOT NULL, last VARCHAR(75) NOT NULL, street VARCHAR(150), city VARCHAR(75), state VARCHAR(25), zip VARCHAR(10), birthday INTEGER NOT NULL, emergencycontact_id INTEGER REFERENCES emergencycontacts(emergencycontact_id), phone VARCHAR(20), email VARCHAR(150), mobile VARCHAR(20), parent VARCHAR(150), country VARCHAR(50), street2 VARCHAR(50))");
                        queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (eventspecific_id INTEGER PRIMARY KEY, participant_id INTEGER NOT NULL REFERENCES participants(participant_id), event_id INTEGER NOT NULL REFERENCES events(event_id), division_id INTEGER NOT NULL REFERENCES divisions(division_id), bib INTEGER, chip INTEGER, checkedin INTEGER DEFAULT 0, shirpurchase INTEGER DEFAULT 0, shirtsize VARCHAR(5), comments VARCHAR, secondshirt VARCHAR, owes VARCHAR(50), hat VARCHAR(20), other VARCHAR)");
                        queries.Add("CREATE TABLE IF NOT EXISTS timeresults (event_id INTEGER NOT NULL REFERENCES events(event_id), eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id), timingpoint_id INTEGER NOT NULL REFERENCES timingpoints(timingpoint_id), time INTEGER NOT NULL)");
                    }
                    else
                    {
                        Log.D("Foreign keys DO NOT work. Setting table creation queries.");
                        queries.Add("CREATE TABLE IF NOT EXISTS events (event_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, date INTEGER NOT NULL)");
                        queries.Add("CREATE TABLE IF NOT EXISTS divisions (division_id INTEGER PRIMARY KEY, event_id INTEGER NOT NULL REFERENCES events(event_id), division_id INTEGER NOT NULL REFERENCES divisions(division_id), name VARCHAR(100) NOT NULL, event_id INTEGER NOT NULL)");
                        queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (timingpoint_id INTEGER PRIMARY KEY, event_id INTEGER NOT NULL, event_id INTEGER NOT NULL REFERENCES events(event_id), name VARCHAR(100) NOT NULL, distance VARCHAR(5), unit VARCHAR(2))");
                        queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (emergencycontact_id INTEGER PRIMARY KEY, name VARCHAR(150) NOT NULL, phone VARCHAR(20), email VARCHAR(150))");
                        queries.Add("CREATE TABLE IF NOT EXISTS participants (participant_id INTEGER PRIMARY KEY, first VARCHAR(50) NOT NULL, last VARCHAR(75) NOT NULL, street VARCHAR(150), city VARCHAR(75), state VARCHAR(25), zip VARCHAR(10), birthday INTEGER NOT NULL, emergencycontact_id INTEGER NOT NULL, phone VARCHAR(20), email VARCHAR(150), mobile VARCHAR(20), parent VARCHAR(150), country VARCHAR(50), street2 VARCHAR(50))");
                        queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (eventspecific_id INTEGER PRIMARY KEY, participant_id INTEGER NOT NULL, event_id INTEGER NOT NULL, division_id INTEGER NOT NULL, bib INTEGER, chip INTEGER, checkedin INTEGER DEFAULT 0, shirpurchase INTEGER DEFAULT 0, shirtsize VARCHAR(5), comments VARCHAR, secondshirt VARCHAR, owes VARCHAR(50), hat VARCHAR(20), other VARCHAR)");
                        queries.Add("CREATE TABLE IF NOT EXISTS timeresults (event_id INTEGER NOT NULL, eventspecific_id INTEGER NOT NULL, timingpoint_id INTEGER NOT NULL, time INTEGER NOT NULL)");
                    }
                }
                else
                {
                    return;
                }
                queries.Add("CREATE TABLE IF NOT EXISTS settings (version INTEGER NOT NULL); INSERT INTO settings (version) VALUES (" + version + ")");
                queries.Add("CREATE TABLE IF NOT EXISTS changes (change_id INTEGER PRIMARY KEY, " +
                                                                "old_participant_id INTEGER NOT NULL," +
                                                                "old_first VARCHAR(50) NOT NULL," +
                                                                "old_last VARCHAR(75) NOT NULL," +
                                                                "old_street VARCHAR(150)," +
                                                                "old_city VARCHAR(75)," +
                                                                "old_state VARCHAR(25)," +
                                                                "old_zip VARCHAR(10)," +
                                                                "old_birthday INTEGER NOT NULL," +
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
                                                                "old_event_spec_shirtpurchase INTEGER NOT NULL," +
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
                                                                "new_participant_id INTEGER NOT NULL," +
                                                                "new_first VARCHAR(50) NOT NULL," +
                                                                "new_last VARCHAR(75) NOT NULL," +
                                                                "new_street VARCHAR(150)," +
                                                                "new_city VARCHAR(75)," +
                                                                "new_state VARCHAR(25)," +
                                                                "new_zip VARCHAR(10)," +
                                                                "new_birthday INTEGER NOT NULL," +
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
                                                                "new_event_spec_shirtpurchase INTEGER NOT NULL," +
                                                                "new_event_spec_shirtsize VARCHAR(5)," +
                                                                "new_event_spec_comments VARCHAR" +
                                                                "new_mobile VARCHAR(20)," +
                                                                "new_parent VARCHAR(150)," +
                                                                "new_country VARCHAR(50)," +
                                                                "new_street2 VARCHAR(50)," +
                                                                "new_secondshirt VARCHAR," +
                                                                "new_owes VARCHAR(50)," +
                                                                "new_hat VARCHAR(20)," +
                                                                "new_other VARCHAR" +
                                                                ")");
                queries.Add("INSERT INTO emergencycontacts (emergencycontact_id, name) VALUES (0,'')");

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

        public void AddDivision(Division div)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO divisions (name, event_id) values (@name,@event_id)";
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
            command.CommandText = "INSERT INTO events(name, date) values(@name,@date)";
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

        public void AddParticipants(ArrayList people)
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
            command.CommandText = "INSERT INTO emergencycontacts (name, phone, email) VALUES (@name,@phone,@email); SELECT emergencycontact_id FROM emergencycontacts WHERE name=@name";
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
            command.CommandText = "INSERT INTO participants (first, last, street, city, state, zip, birthday, emergencycontact_id, phone, email, mobile, parent, country, street2) VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@mobile,@parent,@country,@street2); SELECT participant_id FROM participants WHERE first=@0 AND last=@1 AND street=@2 AND city=@3";
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
                new SQLiteParameter("@street2", person.Street2) } );
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                person.Identifier = Convert.ToInt32(reader["participant_id"]);
            }
            command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO eventspecific (participant_id, event_id, division_id, bib, chip, checkedin, shirtpurchase, shirtsize, comments, secondshirt, owes, hat, other) VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@comments,@secondshirt,@owes,@hat,@other)";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", person.Identifier),
                new SQLiteParameter("@1", person.EventSpecific.EventIdentifier),
                new SQLiteParameter("@2", person.EventSpecific.DivisionIdentifier),
                new SQLiteParameter("@3", person.EventSpecific.Bib),
                new SQLiteParameter("@4", person.EventSpecific.Chip),
                new SQLiteParameter("@5", person.EventSpecific.CheckedIn),
                new SQLiteParameter("@6", person.EventSpecific.ShirtPurchase),
                new SQLiteParameter("@7", person.EventSpecific.ShirtSize),
                new SQLiteParameter("@comments", person.EventSpecific.Comments),
                new SQLiteParameter("@secondshirt", person.EventSpecific.SecondShirt),
                new SQLiteParameter("@owes", person.EventSpecific.Owes),
                new SQLiteParameter("@hat", person.EventSpecific.Hat),
                new SQLiteParameter("@other", person.EventSpecific.Other) } );
            command.ExecuteNonQuery();
        }

        public void AddTimingPoint(TimingPoint tp)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = "INSERT INTO timingpoints (event_id, division_id, name, distance, unit) VALUES (@0,@1,@2,@3,@4)";
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
            command.CommandText = "INSERT INTO timeresults (event_id, eventspecific_id, timingpoint_id, time) VALUES (@0,@1,@2,@3)";
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
            command.CommandText = "UPDATE events SET name=@0, date=@1 WHERE event_id=@2";
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
                command.CommandText = "INSERT INTO emergencycontacts (name, phone, email) VALUES (@0,@1,@2); DELETE FROM emergencycontacts AS e LEFT OUTER JOIN participants AS p on e.emergencycontact_id=p.emergencycontact_id WHERE p.participant_id IS NULL AND e.emergencycontact_id != 0; SELECT emergencycontact_id FROM emergencycontacts WHERE name=@0";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EmergencyContact.Name),
                    new SQLiteParameter("@1", person.EmergencyContact.Phone),
                    new SQLiteParameter("@2", person.EmergencyContact.Email) } );
                SQLiteDataReader reader = command.ExecuteReader();
                person.EmergencyContact.Identifier = reader.Read() ? Convert.ToInt32(reader["emergencycontact_id"]) : 0;
                command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "UPDATE participants SET first=@0, last=@1, street=@2, city=@3, state=@4, zip=@5, birthday=@6, emergencycontact_id=@7, phone=@8, email=@9, mobile=@mobile, parent=@parent, country=@country, street2=@street2 WHERE participant_id=@10";
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
                command.CommandText = "UPDATE eventspecific SET division_id=@0, bib=@1, chip=@2, checkedin=@3, shirtpurchase=@4, shirtsize=@5, secondshirt=@secondshirt, owes=@owes, hat=@hat, other=@other WHERE eventspecific_id=@6";
                command.Parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("@0", person.EventSpecific.DivisionIdentifier),
                    new SQLiteParameter("@1", person.EventSpecific.Bib),
                    new SQLiteParameter("@2", person.EventSpecific.Chip),
                    new SQLiteParameter("@3", person.EventSpecific.CheckedIn),
                    new SQLiteParameter("@4", person.EventSpecific.ShirtPurchase),
                    new SQLiteParameter("@5", person.EventSpecific.ShirtSize),
                    new SQLiteParameter("@6", person.EventSpecific.Identifier),
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
            command.CommandText = "UPDATE timingpoints SET event_id=@0, division_id=@divisionId name=@1, distance=@2, unit=@3 WHERE timingpoint_id=@4";
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
            command.CommandText = "UPDATE timeresult SET time=@0 WHERE eventspecific_id=@1 AND timingpoint_id=@2";
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
            command.CommandText = "UPDATE participants SET checkedin=@0 WHERE participant_id=@1";
            command.Parameters.AddRange(new SQLiteParameter[] {
                new SQLiteParameter("@0", checkedIn),
                new SQLiteParameter("@1", identifier) });
            command.ExecuteNonQuery();
        }

        public void CheckInParticipant(Participant person)
        {
            CheckInParticipant(person.Identifier, person.EventSpecific.CheckedIn);
        }

        public void HardResetDatabase()
        {
            using (var transaction = connection.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master", connection);
                SQLiteDataReader reader = command.ExecuteReader();
                StringBuilder sb = new StringBuilder("Table names are as follows:");
                while (reader.Read())
                {
                    sb.Append(" " + reader["name"].ToString());
                }
                Log.D(sb.ToString());
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
                StringBuilder sb = new StringBuilder("Table names are as follows:");
                while (reader.Read())
                {
                    sb.Append(" "+reader["name"].ToString());
                }
                Log.D(sb.ToString());
                command = new SQLiteCommand("DELETE FROM events; DELETE FROM divisions; DELETE FROM timingpoints; DELETE FROM emergencycontacts; DELETE FROM participants; DELETE FROM eventspecific; DELETE FROM timeresults; DELETE FROM changes; DELETE FROM settings", connection);
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public ArrayList GetEvents()
        {
            ArrayList output = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM events", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Event(Convert.ToInt32(reader["event_id"]), reader["name"].ToString(), Convert.ToInt64(reader["date"])));
            }
            return output;
        }

        public ArrayList GetDivisions(int eventId)
        {
            ArrayList output = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM divisions WHERE event_id="+eventId, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Division(Convert.ToInt32(reader["division_id"]),reader["name"].ToString(),Convert.ToInt32(reader["event_id"])));
            }
            return output;
        }

        public ArrayList GetTimingPoints(int eventId)
        {
            ArrayList output = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM timingpoints WHERE event_id=" + eventId, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new TimingPoint(Convert.ToInt32(reader["timingpoint_id"]), Convert.ToInt32(reader["event_id"]), Convert.ToInt32(reader["division_id"]), reader["name"].ToString(), reader["distance"].ToString(), reader["unit"].ToString()));
            }
            return output;
        }

        public ArrayList GetParticipants()
        {
            Log.D("Getting all participants for all events.");
            ArrayList output = new ArrayList();
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific as s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND d.division_id=s.division_id", connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Participant(
                    Convert.ToInt32(reader["p.participant_id"]),
                    reader["p.first"].ToString(),
                    reader["p.last"].ToString(),
                    reader["p.street"].ToString(),
                    reader["p.city"].ToString(),
                    reader["p.state"].ToString(),
                    reader["p.zip"].ToString(),
                    Convert.ToInt64(reader["p.birthday"]),
                    new EmergencyContact(
                        Convert.ToInt32(reader["e.emergencycontact_id"]),
                        reader["e.name"].ToString(),
                        reader["e.phone"].ToString(),
                        reader["e.email"].ToString()),
                    new EventSpecific(
                        Convert.ToInt32(reader["s.eventspecific_id"]),
                        Convert.ToInt32(reader["s.event_id"]),
                        Convert.ToInt32(reader["s.division_id"]),
                        reader["d.name"].ToString(),
                        Convert.ToInt32(reader["s.bib"]),
                        Convert.ToInt32(reader["s.chip"]),
                        Convert.ToInt32(reader["s.checkedin"]),
                        Convert.ToInt32(reader["s.shirtpurchase"]),
                        reader["s.shirtsize"].ToString(),
                        reader["s.comments"].ToString(),
                        reader["s.secondshirt"].ToString(),
                        reader["s.owes"].ToString(),
                        reader["s.hat"].ToString(),
                        reader["s.other"].ToString()
                        ),
                    reader["p.phone"].ToString(),
                    reader["p.email"].ToString(),
                    reader["p.mobile"].ToString(),
                    reader["p.parent"].ToString(),
                    reader["p.country"].ToString(),
                    reader["p.street2"].ToString()
                    ));
            }
            return output;
        }

        public ArrayList GetParticipants(int eventId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            ArrayList output = new ArrayList();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM participants AS p, emergencycontacts AS e, eventspecific AS s, divisions AS d WHERE p.emergencycontact_id=e.emergencycontact_id AND p.participant_id=s.participant_id AND s.event_id=@eventid AND d.division_id=s.division_id";
            command.Parameters.Add(new SQLiteParameter("@eventid",eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Participant(
                    Convert.ToInt32(reader["p.participant_id"]),
                    reader["p.first"].ToString(),
                    reader["p.last"].ToString(),
                    reader["p.street"].ToString(),
                    reader["p.city"].ToString(),
                    reader["p.state"].ToString(),
                    reader["p.zip"].ToString(),
                    Convert.ToInt64(reader["p.birthday"]),
                    new EmergencyContact(Convert.ToInt32(reader["e.emergencycontact_id"]),
                        reader["e.name"].ToString(),
                        reader["e.phone"].ToString(),
                        reader["e.email"].ToString()),
                    new EventSpecific(Convert.ToInt32(reader["s.eventspecific_id"]),
                        Convert.ToInt32(reader["s.event_id"]),
                        Convert.ToInt32(reader["s.division_id"]),
                        reader["d.name"].ToString(),
                        Convert.ToInt32(reader["s.bib"]),
                        Convert.ToInt32(reader["s.chip"]),
                        Convert.ToInt32(reader["s.checkedin"]),
                        Convert.ToInt32(reader["s.shirtpurchase"]),
                        reader["s.shirtsize"].ToString(),
                        reader["s.comments"].ToString(),
                        reader["s.secondshirt"].ToString(),
                        reader["s.owes"].ToString(),
                        reader["s.hat"].ToString(),
                        reader["s.other"].ToString()
                        ),
                    reader["p.phone"].ToString(),
                    reader["p.email"].ToString(),
                    reader["p.mobile"].ToString(),
                    reader["p.parent"].ToString(),
                    reader["p.country"].ToString(),
                    reader["p.street2"].ToString()
                    ));
            }
            return output;
        }

        public ArrayList GetTimingResults(int eventId)
        {
            Log.D("Getting timing results for event id of " + eventId);
            ArrayList output = new ArrayList();
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
                    Convert.ToInt32(reader["time"])
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

        public ArrayList GetChanges(int eventId)
        {
            Log.D("Getting changes.");
            ArrayList output = new ArrayList();
            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM changes AS c, divisions AS d1, divisions as d2 WHERE old_event_spec_event_id=@eventId AND c.old_event_spec_division_id=d1.division_id AND c.new_event_spec_division_id=d2.division_id ";
            command.Parameters.Add(new SQLiteParameter("@eventId", eventId));
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                output.Add(new Change(
                    Convert.ToInt32(reader["c.change_id"]),
                    new Participant(
                        Convert.ToInt32(reader["c.old_participant_id"]),
                        reader["c.old_first"].ToString(),
                        reader["c.old_last"].ToString(),
                        reader["c.old_street"].ToString(),
                        reader["c.old_city"].ToString(),
                        reader["c.old_state"].ToString(),
                        reader["c.old_zip"].ToString(),
                        Convert.ToInt64(reader["c.old_birthday"]),
                        new EmergencyContact(
                            Convert.ToInt32(reader["c.old_emergency_id"]),
                            reader["c.old_emergency_name"].ToString(),
                            reader["c.old_emergency_phone"].ToString(),
                            reader["c.old_emergency_email"].ToString()),
                        new EventSpecific(
                            Convert.ToInt32(reader["c.old_event_spec_id"]),
                            Convert.ToInt32(reader["c.old_event_spec_event_id"]),
                            Convert.ToInt32(reader["c.old_event_spec_division_id"]),
                            reader["d1.name"].ToString(),
                            Convert.ToInt32(reader["c.old_event_spec_bib"]),
                            Convert.ToInt32(reader["c.old_event_spec_chip"]),
                            Convert.ToInt32(reader["c.old_event_spec_checkedin"]),
                            Convert.ToInt32(reader["c.old_event_spec_shirtpurchase"]),
                            reader["c.old_event_spec_shirtsize"].ToString(),
                            reader["c.old_event_spec_comments"].ToString(),
                            reader["c.old_secondshirt"].ToString(),
                            reader["c.old_owes"].ToString(),
                            reader["c.old_hat"].ToString(),
                            reader["c.old_other"].ToString()
                            ),
                        reader["c.old_phone"].ToString(),
                        reader["c.old_email"].ToString(),
                        reader["c.old_mobile"].ToString(),
                        reader["c.old_parent"].ToString(),
                        reader["c.old_country"].ToString(),
                        reader["c.old_street2"].ToString()
                    ),
                    new Participant(
                        Convert.ToInt32(reader["c.new_participant_id"]),
                        reader["c.new_first"].ToString(),
                        reader["c.new_last"].ToString(),
                        reader["c.new_street"].ToString(),
                        reader["c.new_city"].ToString(),
                        reader["c.new_state"].ToString(),
                        reader["c.new_zip"].ToString(),
                        Convert.ToInt64(reader["c.new_birthday"]),
                        new EmergencyContact(Convert.ToInt32(reader["c.new_emergency_id"]),
                            reader["c.new_emergency_name"].ToString(),
                            reader["c.new_emergency_phone"].ToString(),
                            reader["c.new_emergency_email"].ToString()),
                        new EventSpecific(Convert.ToInt32(reader["c.new_event_spec_id"]),
                            Convert.ToInt32(reader["c.new_event_spec_event_id"]),
                            Convert.ToInt32(reader["c.new_event_spec_division_id"]),
                            reader["d2.name"].ToString(),
                            Convert.ToInt32(reader["c.new_event_spec_bib"]),
                            Convert.ToInt32(reader["c.new_event_spec_chip"]),
                            Convert.ToInt32(reader["c.new_event_spec_checkedin"]),
                            Convert.ToInt32(reader["c.new_event_spec_shirtpurchase"]),
                            reader["c.new_event_spec_shirtsize"].ToString(),
                            reader["c.new_event_spec_comments"].ToString(),
                            reader["c.new_secondshirt"].ToString(),
                            reader["c.new_owes"].ToString(),
                            reader["c.new_hat"].ToString(),
                            reader["c.new_other"].ToString()
                            ),
                        reader["c.new_phone"].ToString(),
                        reader["c.new_email"].ToString(),
                        reader["c.new_mobile"].ToString(),
                        reader["c.new_parent"].ToString(),
                        reader["c.new_country"].ToString(),
                        reader["c.new_street2"].ToString()
                    )
                ));
            }
            return output;
        }

        private void UpdateDatabase(int oldversion, int newversion)
        {
            Log.D("Database is version " + oldversion + " but it needs to be upgraded to version " + newversion);
            SQLiteCommand command;
            switch (oldversion)
            {
                case 1:
                    Log.D("Updating from version 1.");
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = new SQLiteCommand("ALTER TABLE eventspecific ADD comments VARCHAR; ALTER TABLE changes ADD old_phone VARCHAR(20); ALTER TABLE changes ADD old_email VARCHAR(150); ALTER TABLE changes ADD old_event_spec_comments VARCHAR; ALTER TABLE changes ADD new_phone VARCHAR(20); ALTER TABLE changes ADD new_email VARCHAR(150); ALTER TABLE changes ADD new_event_spec_comments VARCHAR; UPDATE settings SET version=2 WHERE version=1", connection);
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto case 2;
                case 2:
                    Log.D("Updating from version 2.");
                    using (var transaction = connection.BeginTransaction())
                    {
                        command = new SQLiteCommand(
                            "ALTER TABLE participants ADD mobile VARCHAR(20);" +
                            "ALTER TABLE participants ADD parent VARCHAR(150);" +
                            "ALTER TABLE participants ADD country VARCHAR(50);" +
                            "ALTER TABLE participants ADD street2 VARCHAR(50);" +
                            "ALTER TABLE eventspecific ADD secondshirt VARCHAR;" +
                            "ALTER TABLE eventspecific ADD owes VARCHAR(50);" +
                            "ALTER TABLE eventspecific ADD hat VARCHAR(20);" +
                            "ALTER TABLE eventspecific ADD other VARCHAR;" +
                            "ALTER TABLE changes ADD old_mobile VARCHAR(20);" +
                            "ALTER TABLE changes ADD old_parent VARCHAR(150);" +
                            "ALTER TABLE changes ADD old_country VARCHAR(50);" +
                            "ALTER TABLE changes ADD old_street2 VARCHAR(50);" +
                            "ALTER TABLE changes ADD old_secondshirt VARCHAR;" +
                            "ALTER TABLE changes ADD old_owes VARCHAR(50);" +
                            "ALTER TABLE changes ADD old_hat VARCHAR(20);" +
                            "ALTER TABLE changes ADD old_other VARCHAR;" +
                            "ALTER TABLE changes ADD new_mobile VARCHAR(20);" +
                            "ALTER TABLE changes ADD new_parent VARCHAR(150);" +
                            "ALTER TABLE changes ADD new_country VARCHAR(50);" +
                            "ALTER TABLE changes ADD new_street2 VARCHAR(50);" +
                            "ALTER TABLE changes ADD new_secondshirt VARCHAR;" +
                            "ALTER TABLE changes ADD new_owes VARCHAR(50);" +
                            "ALTER TABLE changes ADD new_hat VARCHAR(20);" +
                            "ALTER TABLE changes ADD new_other VARCHAR;" +
                            "UPDATE settings SET version=3 WHERE version=2"
                            , connection);
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    goto default;
                default:
                    break;
            }
        }
    }
}
