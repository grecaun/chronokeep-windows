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
using ChronoKeep.Database.SQLite;
using ChronoKeep.Objects;

namespace ChronoKeep
{
    class SQLiteInterface : IDBInterface
    {
        /**
         * HIGHEST MUTEX ID = 132
         * NEXT AVAILABLE   = 133
         */
        private readonly int version = 46;
        readonly string connectionInfo;
        readonly Mutex mutex = new Mutex();

        public SQLiteInterface(string info)
        {
            connectionInfo = info;
        }

        public void Initialize()
        {
            Log.D("Attempting to grab Mutex: ID 0");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 0");
                return;
            }
            Setup.Initialize(version, connectionInfo);
            mutex.ReleaseMutex();
        }

        /*
         * Distances
         */

        public void AddDistance(Distance d)
        {
            Log.D("Attempting to grab Mutex: ID 1");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 1");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distances.AddDistance(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void AddDistances(List<Distance> distances)
        {
            Log.D("Attempting to grab Mutex: ID 117");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 117");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            foreach (Distance dis in distances)
            {
                Distances.AddDistance(dis, connection);
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveDistance(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 2");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 2");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
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
            Log.D("Attempting to grab Mutex: ID 3");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 3");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distances.UpdateDistance(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Distance> GetDistances()
        {
            Log.D("Attempting to grab Mutex: ID 4");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 4");
                return new List<Distance>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Distance> output = Distances.GetDistances(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Distance> GetDistances(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 5");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 5");
                return new List<Distance>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Distance> output = Distances.GetDistances(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public int GetDistanceID(Distance d)
        {
            Log.D("Attempting to grab Mutex: ID 6");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 6");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();

            int output = Distances.GetDistanceID(d, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public Distance GetDistance(int distanceId)
        {
            Log.D("Attempting to grab Mutex: ID 7");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 7");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Distance output = Distances.GetDistance(distanceId, connection);
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
            Distances.SetWaveTimes(eventId, wave, seconds, milliseconds, connection);
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
            Events.AddEvent(anEvent, connection);
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
            Events.RemoveEvent(identifier, connection);
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
            Events.UpdateEvent(anEvent, connection);
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
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Event> output = Events.GetEvents(connection);
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
            Event output = Events.GetEvent(id, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
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
            Events.SetStartWindow(anEvent, connection);
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
            Events.SetFinishOptions(anEvent, connection);
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
                Participants.AddParticipant(person, connection);
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
                    Participants.AddParticipant(person, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
            Participants.RemoveParticipant(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            Log.D("Attempting to grab Mutex: ID 124");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 124");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                Participants.RemoveParticipantEntry(person.Identifier, connection);
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
                    Participants.RemoveParticipantEntry(p.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
            Participants.RemoveEntry(eventId, participantId, connection);
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
                    Participants.RemoveEntry(p.EventIdentifier, p.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
                Participants.UpdateParticipant(person, connection);
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
                    Participants.UpdateParticipant(person, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<Participant> GetParticipants()
        {
            Log.D("Getting all participants for all events.");
            Log.D("Attempting to grab Mutex: ID 29");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 29");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            Log.D("Attempting to grab Mutex: ID 131");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 131");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            Log.D("Getting all participants for event with id of " + eventId);
            Log.D("Attempting to grab Mutex: ID 132");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 132");
                return new List<Participant>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Participant> output = Participants.GetParticipants(eventId, distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
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
            Participant output = Participants.GetParticipantEventSpecific(eventIdentifier, eventSpecificId, connection);
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
            Participant output = Participants.GetParticipantBib(eventIdentifier, bib, connection);
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
            Participant output = Participants.GetParticipant(eventId, identifier, connection);
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
            Participant output = Participants.GetParticipant(eventId, unknown, connection);
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
            int output = Participants.GetParticipantID(person, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Locations
         */

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
            TimingLocations.AddTimingLocation(tl, connection);
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
                TimingLocations.AddTimingLocation(tl, connection);
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
            TimingLocations.RemoveTimingLocation(identifier, connection);
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
            TimingLocations.UpdateTimingLocation(tl, connection);
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
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimingLocation> output = TimingLocations.GetTimingLocations(eventId, connection);
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
            int output = TimingLocations.GetTimingLocationID(tl, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Segment
         */

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
                Segments.AddSegment(seg, connection);
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
                    Segments.AddSegment(seg, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
            Segments.RemoveSegment(seg.Identifier, connection);
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
                Segments.RemoveSegment(identifier, connection);
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
                    Segments.RemoveSegment(seg.Identifier, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
                Segments.UpdateSegment(seg, connection);
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
                    Log.D("Distance ID " + seg.DistanceId + " Segment Name " + seg.Name + " Segment ID " + seg.Identifier);
                    Segments.UpdateSegment(seg, connection);
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
            int output = Segments.GetSegmentId(seg, connection);
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
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<Segment> output = Segments.GetSegments(eventId, connection);
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
            Segments.ResetSegments(eventId, connection);
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
            Log.D("Attempting to grab Mutex: ID 51");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 51");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
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
                    Results.AddTimingResult(result, connection);
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
            Results.RemoveTimingResult(tr, connection);
            connection.Close();
            mutex.ReleaseMutex();
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
            List<TimeResult> output = Results.GetTimingResults(eventId, connection);
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
            List<TimeResult> output = Results.GetFinishTimes(eventId, connection);
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
            List<TimeResult> output = Results.GetStartTimes(eventId, connection);
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
            List<TimeResult> output = Results.GetSegmentTimes(eventId, segmentId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public void UpdateTimingResult(TimeResult oldResult, string newTime)
        {
            Log.D("Attempting to grab Mutex: ID 57");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 57");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.UpdateTimingResult(oldResult, newTime, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            Log.D("Attempting to grab Mutex: ID 129");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 129");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.SetUploadedTimingResults(results, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 130");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 130");
                return new List<TimeResult>();
            }
            Log.D("Getting non-uploaded results for event id of " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimeResult> output = Results.GetNonUploadedResults(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
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
            long output = Results.UnprocessedReadsExist(eventId, connection);
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
            Log.D("Attempting to grab Mutex: ID 60");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 60");
                return;
            }
            Log.D("Resetting timing results for event " + eventId);
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            Results.ResetTimingResultsEvent(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

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
            Results.ResetTimingResultsPlacements(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
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
            SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            DatabaseHelpers.HardResetDatabase(connection);
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
            Log.D("Attempting to grab Mutex: ID 77");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 77");
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
            Log.D("Attempting to grab Mutex: ID 78");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 78");
                return new List<BibChipAssociation>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<BibChipAssociation> output = BibChips.GetBibChips(connection);
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
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<BibChipAssociation> output = BibChips.GetBibChips(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
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
            Log.D("Attempting to grab Mutex: ID 81");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 81");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            if (assoc != null) BibChips.RemoveBibChipAssociation(assoc.EventId, assoc.Chip, connection);
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
                ChipReads.AddChipRead(read, connection);
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
                    ChipReads.AddChipRead(read, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
                ChipReads.UpdateChipRead(read, connection);
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
                    ChipReads.UpdateChipRead(read, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
                    ChipReads.SetChipReadStatus(read, connection);
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
            ChipReads.DeleteChipReads(reads, connection);
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
            List<ChipRead> output = ChipReads.GetChipReads(GetCurrentEvent(), connection);
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
            List<ChipRead> output = ChipReads.GetChipReads(eventId, GetCurrentEvent(), connection);
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
            List<ChipRead> output = ChipReads.GetUsefulChipReads(eventId, GetCurrentEvent(), connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }
        
        /*
         * Settings
         */

        public AppSetting GetAppSetting(string name)
        {
            Log.D("Attempting to grab Mutex: ID 95");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 95");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
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
            Settings.SetAppSetting(setting, connection);
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
                AgeGroups.AddAgeGroup(group, connection);
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
                    AgeGroups.AddAgeGroup(group, connection);
                }
                transaction.Commit();
            }
            connection.Close();
            mutex.ReleaseMutex();
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
            AgeGroups.UpdateAgeGroup(group, connection);
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
            AgeGroups.RemoveAgeGroup(group, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("Attempting to grab Mutex: ID 111");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 111");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            AgeGroups.RemoveAgeGroups(eventId, distanceId, connection);
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
            AgeGroups.RemoveAgeGroups(groups, connection);
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
            List<AgeGroup> output = AgeGroups.GetAgeGroups(eventId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("Attempting to grab Mutex: ID 122");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 122");
                return new List<AgeGroup>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<AgeGroup> output = AgeGroups.GetAgeGroups(eventId, distanceId, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        /*
         * Timing Systems
         */

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
            TimingSystems.AddTimingSystem(system, connection);
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
            TimingSystems.UpdateTimingSystem(system, connection);
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
            Log.D("Attempting to grab Mutex: ID 116");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 116");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            TimingSystems.RemoveTimingSystem(systemId, connection);
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
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<TimingSystem> output = TimingSystems.GetTimingSystems(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            Log.D("Attempting to grab Mutex: ID 121");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 121");
                return new List<DistanceStat>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
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

        public int AddResultsAPI(ResultsAPI anAPI)
        {
            Log.D("Attempting to grab Mutex: ID 125");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 125");
                return -1;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            int outVal = ResultsAPIs.AddResultsAPI(anAPI, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return outVal;
        }

        public void UpdateResultsAPI(ResultsAPI anAPI)
        {
            Log.D("Attempting to grab Mutex: ID 126");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 126");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ResultsAPIs.UpdateResultsAPI(anAPI, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public void RemoveResultsAPI(int identifier)
        {
            Log.D("Attempting to grab Mutex: ID 127");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 127");
                return;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ResultsAPIs.RemoveResultsAPI(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
        }

        public ResultsAPI GetResultsAPI(int identifier)
        {
            if (identifier < 0)
            {
                return null;
            }
            Log.D("Attempting to grab Mutex: ID 128");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 128");
                return null;
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            ResultsAPI output = ResultsAPIs.GetResultsAPI(identifier, connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }

        public List<ResultsAPI> GetAllResultsAPI()
        {
            Log.D("Attempting to grab Mutex: ID 129");
            if (!mutex.WaitOne(3000))
            {
                Log.D("Failed to grab Mutex: ID 129");
                return new List<ResultsAPI>();
            }
            SQLiteConnection connection = new SQLiteConnection(String.Format("Data Source={0};Version=3", connectionInfo));
            connection.Open();
            List<ResultsAPI> output = ResultsAPIs.GetAllResultsAPI(connection);
            connection.Close();
            mutex.ReleaseMutex();
            return output;
        }
    }
}
