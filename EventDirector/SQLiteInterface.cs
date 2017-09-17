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
        SQLiteConnection connection;

        public SQLiteInterface(String info)
        {
            connection = new SQLiteConnection(String.Format("Data Source=%s;Version=3", info));
            connection.Open();
        }

        public void Initialize()
        {
            string query = "PRAGMA foreign_keys = ON; PRAGMA foreign_keys"; // Ensure Foreign key constraints work.
            ArrayList queries = new ArrayList();
            SQLiteCommand command = new SQLiteCommand(query, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt32(0) == 1)
                {
                    Log.D("Foreign keys work. Setting table creation queries.");
                    queries.Add("CREATE TABLE IF NOT EXISTS events (event_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, date INTEGER NOT NULL)");
                    queries.Add("CREATE TABLE IF NOT EXISTS divisions (division_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, event_id INTEGER NOT NULL REFERENCES events(event_id))");
                    queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (timingpoint_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, distance VARCHAR(5), unit VARCHAR(2))");
                    queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (emergencycontact_id INTEGER PRIMARY KEY, name VARCHAR(150) NOT NULL, phone VARCHAR(20) NOT NULL, email VARCHAR(150))");
                    queries.Add("CREATE TABLE IF NOT EXISTS participants (participant_id INTEGER PRIMARY KEY, first VARCHAR(50) NOT NULL, last VARCHAR(75) NOT NULL, street VARCHAR(150), city VARCHAR(75), state VARCHAR(25), zip VARCHAR(10), birthday INTEGER NOT NULL, emergencycontact_id INTEGER NOT NULL REFERENCES emergencycontacts(emergencycontact_id), phone VARCHAR(20), email VARCHAR(150))");
                    queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (eventspecific_id INTEGER PRIMARY KEY, participant_id INTEGER NOT NULL REFERENCES participants(participant_id), event_id INTEGER NOT NULL REFERENCES events(event_id), division_id INTEGER NOT NULL REFERENCES divisions(division_id), bib INTEGER, chip INTEGER, checkedin INTEGER DEFAULT 0, shirpurchase INTEGER DEFAULT 0, shirtsize VARCHAR(5))");
                    queries.Add("CREATE TABLE IF NOT EXISTS timeresults (eventspecific_id INTEGER NOT NULL REFERENCES eventspecific(eventspecific_id), timingpoint_id INTEGER NOT NULL REFERENCES timingpoints(timingpoint_id), time INTEGER NOT NULL)");
                } else
                {
                    Log.D("Foreign keys DO NOT work. Setting table creation queries.");
                    queries.Add("CREATE TABLE IF NOT EXISTS events (event_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, date INTEGER NOT NULL)");
                    queries.Add("CREATE TABLE IF NOT EXISTS divisions (division_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, event_id INTEGER NOT NULL)");
                    queries.Add("CREATE TABLE IF NOT EXISTS timingpoints (timingpoint_id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL, distance VARCHAR(5), unit VARCHAR(2))");
                    queries.Add("CREATE TABLE IF NOT EXISTS emergencycontacts (emergencycontact_id INTEGER PRIMARY KEY, name VARCHAR(150) NOT NULL, phone VARCHAR(20) NOT NULL, email VARCHAR(150))");
                    queries.Add("CREATE TABLE IF NOT EXISTS participants (participant_id INTEGER PRIMARY KEY, first VARCHAR(50) NOT NULL, last VARCHAR(75) NOT NULL, street VARCHAR(150), city VARCHAR(75), state VARCHAR(25), zip VARCHAR(10), birthday INTEGER NOT NULL, emergencycontact_id INTEGER NOT NULL, phone VARCHAR(20), email VARCHAR(150))");
                    queries.Add("CREATE TABLE IF NOT EXISTS eventspecific (eventspecific_id INTEGER PRIMARY KEY, participant_id INTEGER NOT NULL, event_id INTEGER NOT NULL, division_id INTEGER NOT NULL, bib INTEGER, chip INTEGER, checkedin INTEGER DEFAULT 0, shirpurchase INTEGER DEFAULT 0, shirtsize VARCHAR(5))");
                    queries.Add("CREATE TABLE IF NOT EXISTS timeresults (eventspecific_id INTEGER NOT NULL, timingpoint_id INTEGER NOT NULL, time INTEGER NOT NULL)");
                }
            }

            int counter = 1;
            foreach (String q in queries) {
                Log.D("Table query number " + counter++ + "Query string is: " + q);
                command = new SQLiteCommand(q, connection);
                command.ExecuteNonQuery();
            }
        }

        public void AddDivision(Division div)
        {
            throw new NotImplementedException();
        }

        public void AddEvent(Event anEvent)
        {
            throw new NotImplementedException();
        }

        public void AddParticipant(Participant person)
        {
            throw new NotImplementedException();
        }

        public void AddTimingPoint(TimingPoint tp)
        {
            throw new NotImplementedException();
        }

        public void AddTimingResult(TimeResult tr)
        {
            throw new NotImplementedException();
        }

        public void ConnectionInformation(string info)
        {
            throw new NotImplementedException();
        }

        public void RemoveDivision(string identifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveDivision(Division div)
        {
            throw new NotImplementedException();
        }

        public void RemoveEvent(string identifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveEvent(Event anEvent)
        {
            throw new NotImplementedException();
        }

        public void RemoveParticipant(string identifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveParticipant(Participant person)
        {
            throw new NotImplementedException();
        }

        public void RemoveTimingPoint(TimingPoint tp)
        {
            throw new NotImplementedException();
        }

        public void RemoveTimingPoint(string identifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            throw new NotImplementedException();
        }

        public void UpdateDivision(Division div)
        {
            throw new NotImplementedException();
        }

        public void UpdateEvent(Event anEvent)
        {
            throw new NotImplementedException();
        }

        public void UpdateParticipant(Participant person)
        {
            throw new NotImplementedException();
        }

        public void UpdateTimingPoint(TimingPoint tp)
        {
            throw new NotImplementedException();
        }

        public void UpdateTimingResult(TimeResult oldResult, TimeResult newResult)
        {
            throw new NotImplementedException();
        }
    }
}
