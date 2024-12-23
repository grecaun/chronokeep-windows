using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using Chronokeep.Database.SQLite;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;

namespace Chronokeep
{
    class SQLiteInterface : IDBInterface
    {
        /**
         * HIGHEST MUTEX ID = 168
         * NEXT AVAILABLE   = 169
         */
        private readonly int version = 69;
        public const int minimum_compatible_version = 63;
        readonly string connectionInfo;
        readonly Mutex mutex = new Mutex();

        public SQLiteInterface(string info)
        {
            connectionInfo = info;
        }

        public void Initialize()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 0");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 0");
                return;
            }
            Setup.Initialize(version, connectionInfo);
            mutex.ReleaseMutex();
            Results.GetStaticVariables(this);
        }

        /*
         * Distances
         */

        public int AddDistance(Distance d)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 1");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 1");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output = Distances.AddDistance(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Distance> AddDistances(List<Distance> distances)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 117");
            List<Distance> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 117");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            foreach (Distance dis in distances)
            {
                dis.Identifier = Distances.AddDistance(dis, connection);
                output.Add(dis);
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveDistance(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 2");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 2");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distances.RemoveDistance(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveDistance(Distance d)
        {
            RemoveDistance(d.Identifier);
        }

        public void UpdateDistance(Distance d)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 3");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 3");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distances.UpdateDistance(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Distance> GetDistances()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 4");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 4");
                return new List<Distance>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Distance> output = Distances.GetDistances(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Distance> GetDistances(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 5");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 5");
                return new List<Distance>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Distance> output = Distances.GetDistances(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetDistanceID(Distance d)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 6");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 6");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();

            int output = Distances.GetDistanceID(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Distance GetDistance(int distanceId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 7");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 7");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distance output = Distances.GetDistance(distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 8");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 8");
                return;
            }
            Log.D("SQLiteInterface", string.Format("Setting wave {0} for event {1}", wave, eventId));
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distances.SetWaveTimes(eventId, wave, seconds, milliseconds, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Events
         */

        public int AddEvent(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 9");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 9");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output = Events.AddEvent(anEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveEvent(Event anEvent)
        {
            RemoveEvent(anEvent.Identifier);
        }

        public void RemoveEvent(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 10");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 10");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Events.RemoveEvent(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateEvent(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 11");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 11");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Events.UpdateEvent(anEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Event> GetEvents()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 12");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 12");
                return new List<Event>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Event> output = Events.GetEvents(connection);
            mutex.ReleaseMutex();
            return output;
        }

        public int GetEventID(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 13");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 13");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = Events.GetEventID(anEvent, connection);
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
            if (id < 0)
            {
                return null;
            }
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 14");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 14");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Event output = Events.GetEvent(id, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void SetStartWindow(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 17");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 17");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Events.SetStartWindow(anEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetFinishOptions(Event anEvent)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 18");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 18");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Events.SetFinishOptions(anEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Participants
         */

        public Participant AddParticipant(Participant person)
        {
            Participant output = null;
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 19");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 19");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                output = Participants.AddParticipant(person, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Participant> AddParticipants(List<Participant> people)
        {
            List<Participant> output = new();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 20");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 20");
                return output;
            }
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
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveParticipant(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 21");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 21");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Participants.RemoveParticipant(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 124");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 124");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Participants.RemoveEventSpecific(person.Identifier, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 22");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 22");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void UpdateParticipant(Participant person)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 25");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 25");
                return;
            }
            person.FormatData();
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Participants.UpdateParticipant(person, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateParticipants(List<Participant> participants)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 26");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 26");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public List<Participant> GetParticipants()
        {
            Log.D("SQLiteInterface", "Getting all participants for all events.");
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 29");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 29");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("SQLiteInterface", "Getting all participants for event with id of " + eventId);
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 131");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 131");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            Log.D("SQLiteInterface", "Getting all participants for event with id of " + eventId);
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 132");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 132");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(eventId, distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 30");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 30");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Participant output = Participants.GetParticipantEventSpecific(eventIdentifier, eventSpecificId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipantBib(int eventIdentifier, string bib)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 31");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 31");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Participant output = Participants.GetParticipantBib(eventIdentifier, bib, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipant(int eventId, int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 32");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 32");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Participant output = Participants.GetParticipant(eventId, identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Participant GetParticipant(int eventId, Participant unknown)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 33");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 33");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Participant output = Participants.GetParticipant(eventId, unknown, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetParticipantID(Participant person)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 34");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 34");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = Participants.GetParticipantID(person, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Locations
         */

        public int AddTimingLocation(TimingLocation tl)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 35");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 35");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output = TimingLocations.AddTimingLocation(tl, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }
        public List<TimingLocation> AddTimingLocations(List<TimingLocation> locations)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 118");
            List<TimingLocation> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 118");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            foreach (TimingLocation tl in locations)
            {
                tl.Identifier = TimingLocations.AddTimingLocation(tl, connection);
                output.Add(tl);
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveTimingLocation(TimingLocation tl)
        {
            RemoveTimingLocation(tl.Identifier);
        }

        public void RemoveTimingLocation(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 36");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 36");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingLocations.RemoveTimingLocation(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateTimingLocation(TimingLocation tl)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 37");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 37");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingLocations.UpdateTimingLocation(tl, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 38");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 38");
                return new List<TimingLocation>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimingLocation> output = TimingLocations.GetTimingLocations(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetTimingLocationID(TimingLocation tl)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 39");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 39");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = TimingLocations.GetTimingLocationID(tl, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Segment
         */

        public int AddSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 40");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 40");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                output = Segments.AddSegment(seg, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Segment> AddSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 41");
            List<Segment> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 41");
                return output;
            }
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
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 42");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 42");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Segments.RemoveSegment(seg.Identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveSegment(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 43");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 43");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Segments.RemoveSegment(identifier, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 44");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 44");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void UpdateSegment(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 45");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 45");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Segments.UpdateSegment(seg, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateSegments(List<Segment> segments)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 46");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 46");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Log.D("SQLiteInterface", "Segments count is " + segments.Count);
                foreach (Segment seg in segments)
                {
                    Log.D("SQLiteInterface", "Distance ID " + seg.DistanceId + " Segment Name " + seg.Name + " Segment ID " + seg.Identifier);
                    Segments.UpdateSegment(seg, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public int GetSegmentId(Segment seg)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 47");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 47");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = Segments.GetSegmentId(seg, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Segment> GetSegments(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 48");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 48");
                return new List<Segment>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Segment> output = Segments.GetSegments(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void ResetSegments(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 49");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 49");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Segments.ResetSegments(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public int GetMaxSegments(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 50");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 50");
                return 0;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int output = Segments.GetMaxSegments(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Results
         */

        public void AddTimingResult(TimeResult result)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 51");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 51");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.AddTimingResult(result, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            if (results.Count < 1)
            {
                return;
            }
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 52");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 52");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 53");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 53");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.RemoveTimingResult(tr, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 54");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 54");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting timing results for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetTimingResults(eventId, connection);
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 120");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 120");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting finish times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetFinishTimes(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 137");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 137");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting finish times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetLastSeenResults(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 55");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 55");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting start times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetStartTimes(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 56");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 56");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting segment times for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetSegmentTimes(eventId, segmentId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateTimingResult(TimeResult oldResult, string newTime)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 57");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 57");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.UpdateTimingResult(oldResult, newTime, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 129");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 129");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.SetUploadedTimingResults(results, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 130");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 130");
                return new List<TimeResult>();
            }
            Log.D("SQLiteInterface", "Getting non-uploaded results for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetNonUploadedResults(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 58");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 58");
                return false;
            }
            Log.D("SQLiteInterface", "Checking for unprocessed reads.");
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            long output = ChipReads.UnprocessedReadsExist(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output != 0;
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 59");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 59");
                return false;
            }
            Log.D("SQLiteInterface", "Checking for unprocessed results.");
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            long output = Results.UnprocessedResultsExist(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output != 0;
        }

        /*
         * Reset options for time_results and chipreads
         */

        public void ResetTimingResultsEvent(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 60");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 60");
                return;
            }
            Log.D("SQLiteInterface", "Resetting timing results for event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.ResetTimingResultsEvent(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void ResetTimingResultsPlacements(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 64");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 64");
                return;
            }
            Log.D("SQLiteInterface", "Resetting timing result placements for event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.ResetTimingResultsPlacements(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Database Functions
         */

        public void HardResetDatabase()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 67");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 67");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            DatabaseHelpers.HardResetDatabase(connection);
            connection.Close();
            mutex.ReleaseMutex();
            Initialize();
        }

        public void ResetDatabase()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 68");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 68");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            DatabaseHelpers.ResetDatabase(connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Bib Chip Associations
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 77");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 77");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            BibChips.AddBibChipAssociation(eventId, assoc, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<BibChipAssociation> GetBibChips()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 78");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 78");
                return new List<BibChipAssociation>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<BibChipAssociation> output = BibChips.GetBibChips(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 79");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 79");
                return new List<BibChipAssociation>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<BibChipAssociation> output = BibChips.GetBibChips(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 80");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 80");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            BibChips.RemoveBibChipAssociation(eventId, chip, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        private void RemoveBibChipAssociationInternal(BibChipAssociation assoc, SQLiteConnection connection)
        {
            if (assoc != null) BibChips.RemoveBibChipAssociation(assoc.EventId, assoc.Chip, connection);
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 81");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 81");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            if (assoc != null) BibChips.RemoveBibChipAssociation(assoc.EventId, assoc.Chip, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 82");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 82");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        /*
         * Chip Reads
         */

        public int AddChipRead(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 83");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 83");
                return output;
            }
            Log.D("SQLiteInterface", "Database - Add chip read. Box " + read.Box + " Antenna " + read.Antenna + " Chip " + read.ChipNumber
                + " LogId " + read.LogId + " Time Given " + read.TimeString);
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                output = ChipReads.AddChipRead(read, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> AddChipReads(List<ChipRead> reads)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 84");
            List<ChipRead> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 84");
                return output;
            }
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
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateChipRead(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 85");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 85");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                ChipReads.UpdateChipRead(read, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateChipReads(List<ChipRead> reads)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 86");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 86");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void SetChipReadStatus(ChipRead read)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 87");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 87");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                ChipReads.SetChipReadStatus(read, connection);
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
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 88");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 88");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void DeleteChipReads(List<ChipRead> reads)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 89");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 89");
                return;
            }
            if (reads.Count < 1) return;
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ChipReads.DeleteChipReads(reads, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<ChipRead> GetChipReadsSafemode(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 158");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 158");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetChipReadsSafemode(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetChipReads()
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 90");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 90");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetChipReads(theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 91");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 91");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetChipReads(eventId, theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 92");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 92");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetUsefulChipReads(eventId, theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetAnnouncerChipReads(int eventId)
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 133");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 133");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetAnnouncerChipReads(eventId, theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetAnnouncerUsedChipReads(int eventId)
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 134");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 134");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetAnnouncerUsedChipReads(eventId, theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ChipRead> GetDNSChipReads(int eventId)
        {
            Event theEvent = GetCurrentEvent();
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 136");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 136");
                return new List<ChipRead>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ChipRead> output = ChipReads.GetDNSChipReads(eventId, theEvent, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Settings
         */

        public AppSetting GetAppSetting(string name)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 95");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 95");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AppSetting output = Settings.GetAppSetting(name, connection);
            connection.Close();
            mutex.ReleaseMutex();
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
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 96");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 96");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Settings.SetAppSetting(setting, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        /*
         * Age Group Functions
         */
        public int AddAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 107");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 107");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                output = AgeGroups.AddAgeGroup(group, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 108");
            List<AgeGroup> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 108");
                return output;
            }
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
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 109");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 109");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.UpdateAgeGroup(group, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 110");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 110");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.RemoveAgeGroup(group, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 111");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 111");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.RemoveAgeGroups(eventId, distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 123");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 123");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.RemoveAgeGroups(groups, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 135");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 135");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.ResetAgeGroups(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 112");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 112");
                return new List<AgeGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<AgeGroup> output = AgeGroups.GetAgeGroups(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 122");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 122");
                return new List<AgeGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<AgeGroup> output = AgeGroups.GetAgeGroups(eventId, distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Systems
         */

        public int AddTimingSystem(TimingSystem system)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 113");
            int output = Constants.Timing.TIMINGSYSTEM_UNKNOWN;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 113");
                return output;
            }
            Log.D("SQLiteInterface", "Database - Add Timing System");
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output = TimingSystems.AddTimingSystem(system, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 114");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 114");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingSystems.UpdateTimingSystem(system, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 115");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 115");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingSystems.SetTimingSystems(systems, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveTimingSystem(TimingSystem system)
        {
            RemoveTimingSystem(system.SystemIdentifier);
        }

        public void RemoveTimingSystem(int systemId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 116");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 116");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingSystems.RemoveTimingSystem(systemId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimingSystem> GetTimingSystems()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 117");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 117");
                return new List<TimingSystem>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimingSystem> output = TimingSystems.GetTimingSystems(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 121");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 121");
                return new List<DistanceStat>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<DistanceStat> output = DistanceStats.GetDistanceStats(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
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
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 125");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 125");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int outVal = APIs.AddAPI(anAPI, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return outVal;
        }

        public void UpdateAPI(APIObject anAPI)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 126");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 126");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            APIs.UpdateAPI(anAPI, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAPI(int identifier)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 127");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 127");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            APIs.RemoveAPI(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public APIObject GetAPI(int identifier)
        {
            if (identifier < 0)
            {
                return null;
            }
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 128");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 128");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            APIObject output = APIs.GetAPI(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<APIObject> GetAllAPI()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 129");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 129");
                return new List<APIObject>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<APIObject> output = APIs.GetAllAPI(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Alarm> SaveAlarms(int eventId, List<Alarm> alarms)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 138");
            List<Alarm> output = new();
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 138");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output.AddRange(Alarms.SaveAlarms(eventId, alarms, connection));
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int SaveAlarm(int eventId, Alarm alarm)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 139");
            int output = -1;
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 139");
                return output;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            output = Alarms.SaveAlarm(eventId, alarm, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 141");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 141");
                return new List<Alarm>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Alarm> output = Alarms.GetAlarms(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void DeleteAlarms(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 142");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 142");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Alarms.DeleteAlarms(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void DeleteAlarm(Alarm alarm)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 143");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 143");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Alarms.DeleteAlarm(alarm, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 144");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 144");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoteReaders.AddRemoteReaders(eventId, readers, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 145");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 145");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoteReaders.DeleteRemoteReaders(eventId, readers, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 146");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 146");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            RemoteReaders.DeleteRemoteReader(eventId, reader, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 147");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 147");
                return new List<RemoteReader>();
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<RemoteReader> output = RemoteReaders.GetRemoteReaders(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 148");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 148");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SmsAlerts.AddSmsAlert(eventId, eventspecific_id, segment_id, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<(int, int)> GetSMSAlerts(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 149");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 149");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<(int, int)> output = SmsAlerts.GetSmsAlerts(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void AddEmailAlert(int eventId, int eventspecific_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 156");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 156");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            EmailAlerts.AddEmailAlert(eventId, eventspecific_id, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<int> GetEmailAlerts(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 157");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 157");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<int> output = EmailAlerts.GetEmailAlerts(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void RemoveEmailAlert(int eventId, int eventspecific_id)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 164");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 164");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            EmailAlerts.RemoveEmailAlert(eventId, eventspecific_id, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<string> GetBannedPhones()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 150");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 150");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<string> output = Banned.GetBannedPhones(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void AddBannedPhone(string phone)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 151");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 151");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.AddBannedPhone(phone, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddBannedPhones(List<string> phones)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 152");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 152");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.AddBannedPhones(phones, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBannedPhones(List<string> phones)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 162");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 162");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.RemoveBannedPhones(phones, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<string> GetBannedEmails()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 153");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 153");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<string> output = Banned.GetBannedEmails(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void AddBannedEmail(string email)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 154");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 154");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.AddBannedEmail(email, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddBannedEmails(List<string> emails)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 155");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 155");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.AddBannedEmails(emails, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBannedEmails(List<string> emails)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 163");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 163");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.RemoveBannedEmails(emails, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<APISmsSubscription> GetSmsSubscriptions(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 159");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 159");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<APISmsSubscription> output = SmsSubscriptions.GetSmsSubscriptions(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 160");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 160");
                return;
            }
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
            mutex.ReleaseMutex();
        }

        public void DeleteSmsSubscriptions(int eventId)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 161");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 161");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            SmsSubscriptions.DeleteSmsSubscriptions(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBannedEmail(string email)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 165");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 165");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.RemoveBannedEmail(email, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveBannedPhone(string phone)
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 165");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 165");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.RemoveBannedPhone(phone, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void ClearBannedEmails()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 167");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 167");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.ClearBannedEmails(connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void ClearBannedPhones()
        {
            Log.D("SQLiteInterface", "Attempting to grab Mutex: ID 168");
            if (!mutex.WaitOne(3000))
            {
                Log.D("SQLiteInterface", "Failed to grab Mutex: ID 168");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Banned.ClearBannedPhones(connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void UpdateDivisionsEnabled() { }

        public void UpdateStart() { }
    }
}
