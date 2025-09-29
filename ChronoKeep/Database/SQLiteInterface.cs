﻿using Chronokeep.Database.SQLite;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;

namespace Chronokeep
{
    class SQLiteInterface(string info) : IDBInterface
    {
        /**
         * HIGHEST LOCK ID = 170
         * NEXT AVAILABLE   = 171
         */
        private readonly int version = 71;
        public const int minimum_compatible_version = 63;
        readonly string connectionInfo = info;
        readonly Lock dbLock = new();

        public void Initialize()
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 0");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 0");
                return;
            }
            try
            {
                Setup.Initialize(version, connectionInfo);
            }
            finally
            {
                dbLock.Exit();
            }
            Results.GetStaticVariables(this);
        }

        /*
         * Distances
         */

        public int AddDistance(Distance d)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 1");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 1");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Distances.AddDistance(d, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Distance> AddDistances(List<Distance> distances)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 117");
            List<Distance> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 117");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                foreach (Distance dis in distances)
                {
                    dis.Identifier = Distances.AddDistance(dis, connection);
                    output.Add(dis);
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveDistance(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 2");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 2");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Distances.RemoveDistance(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveDistance(Distance d)
        {
            RemoveDistance(d.Identifier);
        }

        public void UpdateDistance(Distance d)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 3");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 3");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Distances.UpdateDistance(d, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<Distance> GetDistances()
        {
            List<Distance> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 4");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 4");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Distances.GetDistances(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Distance> GetDistances(int eventId)
        {
            List<Distance> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 5");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 5");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Distances.GetDistances(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public int GetDistanceID(Distance d)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 6");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 6");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Distances.GetDistanceID(d, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Distance GetDistance(int distanceId)
        {
            Distance output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 7");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 7");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Distances.GetDistance(distanceId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 8");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 8");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Distances.SetWaveTimes(eventId, wave, seconds, milliseconds, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Events
         */

        public int AddEvent(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 9");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 9");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Events.AddEvent(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveEvent(Event anEvent)
        {
            RemoveEvent(anEvent.Identifier);
        }

        public void RemoveEvent(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 10");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 10");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Events.RemoveEvent(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateEvent(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 11");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 11");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Events.UpdateEvent(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<Event> GetEvents()
        {
            List<Event> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 12");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 12");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Events.GetEvents(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public int GetEventID(Event anEvent)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 13");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 13");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Events.GetEventID(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Event GetCurrentEvent()
        {
            AppSetting CurEvent = GetAppSetting(Constants.Settings.CURRENT_EVENT);
            if (CurEvent == null)
            {
                return null;
            }
            return GetEvent(Convert.ToInt32(CurEvent.Value));
        }

        public void SetCurrentEvent(int eventID)
        {
            SetAppSetting(new()
            {
                Name = Constants.Settings.CURRENT_EVENT,
                Value = eventID.ToString(),
            });
        }

        public Event GetEvent(int id)
        {
            Event output = null;
            if (id < 0)
            {
                return output;
            }
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 14");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 14");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Events.GetEvent(id, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void SetStartOptions(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 17");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 17");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Events.SetStartOptions(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetFinishOptions(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 18");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 18");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Events.SetFinishOptions(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetStartFinishOptions(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 170");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 170");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Events.SetStartFinishOptions(anEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Participants
         */

        public Participant AddParticipant(Participant person)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 19");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 19");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    output = Participants.AddParticipant(person, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Participant> AddParticipants(List<Participant> people)
        {
            List<Participant> output = new();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 20");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 20");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Participant person in people)
                    {
                        output.Add(Participants.AddParticipant(person, connection));
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveParticipant(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 21");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 21");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Participants.RemoveParticipant(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveParticipantEntry(Participant person)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 124");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 124");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Participants.RemoveEventSpecific(person.Identifier, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 22");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 22");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Participant p in participants)
                    {
                        Participants.RemoveEventSpecific(p.Identifier, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateParticipant(Participant person)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 25");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 25");
                return;
            }
            try
            {
                person.FormatData();
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Participants.UpdateParticipant(person, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateParticipants(List<Participant> participants)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 26");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 26");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Participant person in participants)
                    {
                        person.FormatData();
                        Participants.UpdateParticipant(person, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<Participant> GetParticipants()
        {
            List<Participant> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 29");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 29");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipants(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Participant> GetParticipants(int eventId)
        {
            List<Participant> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 131");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 131");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipants(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            List<Participant> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 132");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 132");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipants(eventId, distanceId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 30");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 30");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipantEventSpecific(eventIdentifier, eventSpecificId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Participant GetParticipantBib(int eventIdentifier, string bib)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 31");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 31");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipantBib(eventIdentifier, bib, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Participant GetParticipant(int eventId, int identifier)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 32");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 32");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipant(eventId, identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Participant GetParticipant(int eventId, Participant unknown)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 33");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 33");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipant(eventId, unknown, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public int GetParticipantID(Participant person)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 34");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 34");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetParticipantID(person, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<string> GetDivisions(int eventIdentifier)
        {
            List<string> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 169");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 169");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Participants.GetDivisions(eventIdentifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        /*
         * Timing Locations
         */

        public int AddTimingLocation(TimingLocation tl)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 35");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 35");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = TimingLocations.AddTimingLocation(tl, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }
        public List<TimingLocation> AddTimingLocations(List<TimingLocation> locations)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 118");
            List<TimingLocation> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 118");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                foreach (TimingLocation tl in locations)
                {
                    tl.Identifier = TimingLocations.AddTimingLocation(tl, connection);
                    output.Add(tl);
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveTimingLocation(TimingLocation tl)
        {
            RemoveTimingLocation(tl.Identifier);
        }

        public void RemoveTimingLocation(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 36");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 36");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                TimingLocations.RemoveTimingLocation(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateTimingLocation(TimingLocation tl)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 37");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 37");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                TimingLocations.UpdateTimingLocation(tl, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            List<TimingLocation> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 38");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 38");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = TimingLocations.GetTimingLocations(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public int GetTimingLocationID(TimingLocation tl)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 39");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 39");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = TimingLocations.GetTimingLocationID(tl, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        /*
         * Segment
         */

        public int AddSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 40");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 40");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    output = Segments.AddSegment(seg, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Segment> AddSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 41");
            List<Segment> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 41");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Segment seg in segments)
                    {
                        seg.Identifier = Segments.AddSegment(seg, connection);
                        output.Add(seg);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 42");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 42");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Segments.RemoveSegment(seg.Identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveSegment(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 43");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 43");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Segments.RemoveSegment(identifier, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 44");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 44");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Segment seg in segments)
                    {
                        Segments.RemoveSegment(seg.Identifier, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 45");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 45");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Segments.UpdateSegment(seg, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 46");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 46");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Segment seg in segments)
                    {
                        Segments.UpdateSegment(seg, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public int GetSegmentId(Segment seg)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 47");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 47");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Segments.GetSegmentId(seg, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Segment> GetSegments(int eventId)
        {
            List<Segment> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 48");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 48");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Segments.GetSegments(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void ResetSegments(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 49");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 49");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Segments.ResetSegments(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public int GetMaxSegments(int eventId)
        {
            int output = 0;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 50");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 50");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Segments.GetMaxSegments(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        /*
         * Timing Results
         */

        public void AddTimingResult(TimeResult result)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 51");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 51");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.AddTimingResult(result, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            if (results.Count < 1)
            {
                return;
            }
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 52");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 52");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (TimeResult result in results)
                    {
                        Results.AddTimingResult(result, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 53");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 53");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.RemoveTimingResult(tr, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 54");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 54");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetTimingResults(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 120");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 120");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetFinishTimes(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 137");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 137");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetLastSeenResults(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 55");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 55");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetStartTimes(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 56");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 56");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetSegmentTimes(eventId, segmentId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void UpdateTimingResult(TimeResult oldResult, string newTime)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 57");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 57");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.UpdateTimingResult(oldResult, newTime, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 129");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 129");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.SetUploadedTimingResults(results, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            List<TimeResult> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 130");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 130");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.GetNonUploadedResults(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 58");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 58");
                return false;
            }
            long output = 0;
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.UnprocessedReadsExist(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output != 0;
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            long output = 0;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 59");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 59");
                return output != 0;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Results.UnprocessedResultsExist(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output != 0;
        }

        /*
         * Reset options for time_results and chipreads
         */

        public void ResetTimingResultsEvent(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 60");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 60");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.ResetTimingResultsEvent(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void ResetTimingResultsPlacements(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 64");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 64");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Results.ResetTimingResultsPlacements(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Database Functions
         */

        public void HardResetDatabase()
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 67");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 67");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                DatabaseHelpers.HardResetDatabase(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            Initialize();
        }

        public void ResetDatabase()
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 68");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 68");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                DatabaseHelpers.ResetDatabase(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Bib Chip Associations
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 77");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 77");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                BibChips.AddBibChipAssociation(eventId, assoc, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<BibChipAssociation> GetBibChips()
        {
            List<BibChipAssociation> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 78");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 78");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = BibChips.GetBibChips(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 79");
            List<BibChipAssociation> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 79");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = BibChips.GetBibChips(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 80");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 80");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                BibChips.RemoveBibChipAssociation(eventId, chip, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        private void RemoveBibChipAssociationInternal(BibChipAssociation assoc, SQLiteConnection connection)
        {
            if (assoc != null) BibChips.RemoveBibChipAssociation(assoc.EventId, assoc.Chip, connection);
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 81");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 81");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                if (assoc != null) BibChips.RemoveBibChipAssociation(assoc.EventId, assoc.Chip, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 82");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 82");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
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
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Chip Reads
         */

        public int AddChipRead(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 83");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 83");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    output = ChipReads.AddChipRead(read, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> AddChipReads(List<ChipRead> reads)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 84");
            List<ChipRead> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 84");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (ChipRead read in reads)
                    {
                        read.ReadId = ChipReads.AddChipRead(read, connection);
                        output.Add(read);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void UpdateChipRead(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 85");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 85");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    ChipReads.UpdateChipRead(read, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateChipReads(List<ChipRead> reads)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 86");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 86");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (ChipRead read in reads)
                    {
                        ChipReads.UpdateChipRead(read, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetChipReadStatus(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 87");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 87");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    ChipReads.SetChipReadStatus(read, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetChipReadStatuses(List<ChipRead> reads)
        {
            if (reads.Count < 1)
            {
                return;
            }
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 88");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 88");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (ChipRead read in reads)
                    {
                        ChipReads.SetChipReadStatus(read, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void DeleteChipReads(List<ChipRead> reads)
        {
            if (reads.Count < 1) return;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 89");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 89");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                ChipReads.DeleteChipReads(reads, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<ChipRead> GetChipReadsSafemode(int eventId)
        {
            List<ChipRead> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 158");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 158");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetChipReadsSafemode(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetChipReads()
        {
            Event theEvent = GetCurrentEvent();
            List<ChipRead> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 90");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 90");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetChipReads(theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            List<ChipRead> output = [];
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 91");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 91");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetChipReads(eventId, theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            List<ChipRead> output = [];
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 92");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 92");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetUsefulChipReads(eventId, theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetAnnouncerChipReads(int eventId)
        {
            List<ChipRead> output = [];
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 133");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 133");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetAnnouncerChipReads(eventId, theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetAnnouncerUsedChipReads(int eventId)
        {
            List<ChipRead> output = [];
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 134");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 134");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetAnnouncerUsedChipReads(eventId, theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<ChipRead> GetDNSChipReads(int eventId)
        {
            List<ChipRead> output = [];
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 136");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 136");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = ChipReads.GetDNSChipReads(eventId, theEvent, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        /*
         * Settings
         */

        public AppSetting GetAppSetting(string name)
        {
            AppSetting output = null;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 95");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 95");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Settings.GetAppSetting(name, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void SetAppSetting(string n, string v)
        {
            AppSetting setting = new AppSetting()
            {
                Name = n,
                Value = v
            };
            SetAppSetting(setting);
        }

        public void SetAppSetting(AppSetting setting)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 96");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 96");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Settings.SetAppSetting(setting, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        /*
         * Age Group Functions
         */
        public int AddAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 107");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 107");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    output = AgeGroups.AddAgeGroup(group, connection);
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 108");
            List<AgeGroup> output = new();
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 108");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (AgeGroup group in groups)
                    {
                        group.GroupId = AgeGroups.AddAgeGroup(group, connection);
                        output.Add(group);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 109");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 109");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                AgeGroups.UpdateAgeGroup(group, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 110");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 110");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                AgeGroups.RemoveAgeGroup(group, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 111");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 111");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                AgeGroups.RemoveAgeGroups(eventId, distanceId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 123");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 123");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                AgeGroups.RemoveAgeGroups(groups, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 135");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 135");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                AgeGroups.ResetAgeGroups(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            List<AgeGroup> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 112");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 112");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = AgeGroups.GetAgeGroups(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            List<AgeGroup> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 122");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 122");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = AgeGroups.GetAgeGroups(eventId, distanceId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        /*
         * Timing Systems
         */

        public int AddTimingSystem(TimingSystem system)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 113");
            int output = Constants.Timing.TIMINGSYSTEM_UNKNOWN;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 113");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = TimingSystems.AddTimingSystem(system, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 114");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 114");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                TimingSystems.UpdateTimingSystem(system, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 115");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 115");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                TimingSystems.SetTimingSystems(systems, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveTimingSystem(TimingSystem system)
        {
            RemoveTimingSystem(system.SystemIdentifier);
        }

        public void RemoveTimingSystem(int systemId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 116");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 116");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                TimingSystems.RemoveTimingSystem(systemId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<TimingSystem> GetTimingSystems()
        {
            List<TimingSystem> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 117");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 117");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = TimingSystems.GetTimingSystems(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            List<DistanceStat> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 121");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 121");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = DistanceStats.GetDistanceStats(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public Dictionary<int, List<Participant>> GetDistanceParticipantsStatus(int eventId, int distanceId)
        {
            Dictionary<int, List<Participant>> output = new Dictionary<int, List<Participant>>();
            List<Participant> parts = (distanceId == -1) ? GetParticipants(eventId) : GetParticipants(eventId, distanceId);
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

        /*
         * Results API Functions
         */

        public int AddAPI(APIObject anAPI)
        {
            int output = -1;
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 125");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 125");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = APIs.AddAPI(anAPI, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void UpdateAPI(APIObject anAPI)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 126");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 126");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                APIs.UpdateAPI(anAPI, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveAPI(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 127");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 127");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                APIs.RemoveAPI(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public APIObject GetAPI(int identifier)
        {
            APIObject output = null;
            if (identifier < 0)
            {
                return output;
            }
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 128");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 128");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = APIs.GetAPI(identifier, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<APIObject> GetAllAPI()
        {
            List<APIObject> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 129");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 129");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = APIs.GetAllAPI(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Alarm> SaveAlarms(int eventId, List<Alarm> alarms)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 138");
            List<Alarm> output = [];
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 138");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output.AddRange(Alarms.SaveAlarms(eventId, alarms, connection));
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public int SaveAlarm(int eventId, Alarm alarm)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 139");
            int output = -1;
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 139");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Alarms.SaveAlarm(eventId, alarm, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            List<Alarm> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 141");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 141");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Alarms.GetAlarms(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void DeleteAlarms(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 142");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 142");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Alarms.DeleteAlarms(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void DeleteAlarm(Alarm alarm)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 143");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 143");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Alarms.DeleteAlarm(alarm, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void AddRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 144");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 144");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                RemoteReaders.AddRemoteReaders(eventId, readers, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 145");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 145");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                RemoteReaders.DeleteRemoteReaders(eventId, readers, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 146");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 146");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                RemoteReaders.DeleteRemoteReader(eventId, reader, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            List<RemoteReader> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 147");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 147");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = RemoteReaders.GetRemoteReaders(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 148");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 148");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                SmsAlerts.AddSmsAlert(eventId, eventspecific_id, segment_id, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<(int, int)> GetSMSAlerts(int eventId)
        {
            List<(int, int)> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 149");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 149");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = SmsAlerts.GetSmsAlerts(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void AddEmailAlert(int eventId, int eventspecific_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 156");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 156");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                EmailAlerts.AddEmailAlert(eventId, eventspecific_id, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<int> GetEmailAlerts(int eventId)
        {
            List<int> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 157");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 157");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = EmailAlerts.GetEmailAlerts(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void RemoveEmailAlert(int eventId, int eventspecific_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 164");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 164");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                EmailAlerts.RemoveEmailAlert(eventId, eventspecific_id, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<string> GetBannedPhones()
        {
            List<string> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 150");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 150");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Banned.GetBannedPhones(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void AddBannedPhone(string phone)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 151");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 151");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.AddBannedPhone(phone, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void AddBannedPhones(List<string> phones)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 152");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 152");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.AddBannedPhones(phones, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveBannedPhones(List<string> phones)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 162");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 162");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.RemoveBannedPhones(phones, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<string> GetBannedEmails()
        {
            List<string> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 153");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 153");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = Banned.GetBannedEmails(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void AddBannedEmail(string email)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 154");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 154");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.AddBannedEmail(email, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void AddBannedEmails(List<string> emails)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 155");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 155");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.AddBannedEmails(emails, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveBannedEmails(List<string> emails)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 163");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 163");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.RemoveBannedEmails(emails, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public List<APISmsSubscription> GetSmsSubscriptions(int eventId)
        {
            List<APISmsSubscription> output = [];
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 159");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 159");
                return output;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                output = SmsSubscriptions.GetSmsSubscriptions(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
            return output;
        }

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 160");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 160");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (APISmsSubscription sub in subscriptions)
                    {
                        SmsSubscriptions.AddSmsSubscription(eventId, sub, connection);
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void DeleteSmsSubscriptions(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 161");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 161");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                SmsSubscriptions.DeleteSmsSubscriptions(eventId, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveBannedEmail(string email)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 165");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 165");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.RemoveBannedEmail(email, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void RemoveBannedPhone(string phone)
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 165");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 165");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.RemoveBannedPhone(phone, connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void ClearBannedEmails()
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 167");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 167");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.ClearBannedEmails(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void ClearBannedPhones()
        {
            Log.D("SQLiteInterface", "Attempting to grab Lock: ID 168");
            if (!dbLock.TryEnter(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Lock: ID 168");
                return;
            }
            try
            {
                SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
                connection.Open();
                Banned.ClearBannedPhones(connection);
                connection.Close();
            }
            finally
            {
                dbLock.Exit();
            }
        }

        public void UpdateDivisionsEnabled() { }

        public void UpdateStart() { }
    }
}
