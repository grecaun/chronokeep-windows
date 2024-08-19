using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.MemStore
{
    internal class MemStore : IDBInterface
    {
        internal class MutexLockException : Exception
        {
            public MutexLockException(string message) : base(message) { }
        }

        internal class InvalidEventID : Exception
        {
            public InvalidEventID(string message) : base(message) { }
        }

        private readonly int lockTimeout = 3000;

        // Singleton
        private static MemStore instance;

        // Items that don't rely on a specific Event
        private static ReaderWriterLock settingsLock = new();
        // key == setting name
        private static Dictionary<string, AppSetting> settings = new();

        private static ReaderWriterLock apiLock = new();
        // key == api id
        private static Dictionary<int, APIObject> apis = new();

        private static ReaderWriterLock timingSystemsLock = new();
        // key == system id
        private static Dictionary<int, TimingSystem> timingSystems = new();

        private static ReaderWriterLock bannedLock = new();
        private static HashSet<string> bannedPhones = new();
        private static HashSet<string> bannedEmails = new();

        // Event and the related fields
        private static ReaderWriterLock eventLock = new();
        private static Event theEvent;

        private static ReaderWriterLock distanceLock = new();
        // key == distance id
        private static Dictionary<int, Distance> distances = new();

        private static ReaderWriterLock locationsLock = new();
        // key == location id
        private static Dictionary<int, TimingLocation> locations = new();

        private static ReaderWriterLock segmentLock = new();
        // key == segment id
        private static Dictionary<int, Segment> segments = new();

        private static ReaderWriterLock participantsLock = new();
        // key == event specific id
        private static Dictionary<int, Participant> participants = new();

        private static ReaderWriterLock bibChipLock = new();
        // key == chip
        private static Dictionary<string, BibChipAssociation> chipToBibAssociations = new();

        private static ReaderWriterLock ageGroupLock = new();
        // key == distanceId
        private static Dictionary<int, List<AgeGroup>> ageGroups = new();

        private static ReaderWriterLock alarmLock = new();
        // key == bib
        private static Dictionary<string, Alarm> bibAlarms = new();
        // key == chip
        private static Dictionary<string, Alarm> chipAlarms = new();

        private static ReaderWriterLock remoteReadersLock = new();
        private static List<RemoteReader> remoteReaders = new();

        private static ReaderWriterLock alertsLock = new();
        private static HashSet<(int, int)> smsAlerts = new();
        private static HashSet<int> emailAlerts = new();
        private static List<APISmsSubscription> smsSubscriptions = new();

        // Timing results
        private static ReaderWriterLock resultsLock = new();
        private static List<TimeResult> timingResults = new();

        // Chip Read data
        private static ReaderWriterLock chipReadsLock = new();
        private static List<ChipRead> chipReads = new();

        // Local variables
        private IDBInterface database;

        private MemStore(IMainWindow window, IDBInterface database)
        {
            this.database = database;
        }

        public static MemStore GetMemStore(IMainWindow window, IDBInterface database)
        {
            if (instance == null)
            {
                instance = new MemStore(window, database);
            }
            return instance;
        }

        public void LoadEvent()
        {
            if (theEvent == null)
            {
                return;
            }
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                // load distances
                foreach (Distance dist in database.GetDistances(theEvent.Identifier))
                {
                    distances[dist.Identifier] = dist;
                }
                distanceLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
            try
            {
                locationsLock.AcquireWriterLock(lockTimeout);
                // load locations
                foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
                {
                    locations[loc.Identifier] = loc;
                }
                locationsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                // load segments
                foreach (Segment seg in database.GetSegments(theEvent.Identifier))
                {
                    segments[seg.Identifier] = seg;
                }
                segmentLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                // load participants
                foreach (Participant part in database.GetParticipants(theEvent.Identifier))
                {
                    participants[part.EventSpecific.Identifier] = part;
                }
                participantsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                // load bibchipassociations
                foreach (BibChipAssociation assoc in database.GetBibChips(theEvent.Identifier))
                {
                    chipToBibAssociations[assoc.Chip] = assoc;
                }
                bibChipLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                // load age groups
                getAgeGroupDictionary();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            try
            {
                alarmLock.AcquireWriterLock(lockTimeout);
                // load alarms
                foreach (Alarm alarm in database.GetAlarms(theEvent.Identifier))
                {
                    bibAlarms[alarm.Bib] = alarm;
                    chipAlarms[alarm.Chip] = alarm;
                }
                alarmLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
            try
            {
                remoteReadersLock.AcquireWriterLock(lockTimeout);
                // load remote readers
                foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
                {
                    remoteReaders.Add(reader);
                }
                remoteReadersLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
            try
            {
                alertsLock.AcquireWriterLock(lockTimeout);
                // load sms alerts sent
                foreach ((int, int) alert in database.GetSMSAlerts(theEvent.Identifier))
                {
                    smsAlerts.Add(alert);
                }
                // load email alerts sent
                foreach (int alert in database.GetEmailAlerts(theEvent.Identifier))
                {
                    emailAlerts.Add(alert);
                }
                // load sms subscriptions
                foreach (APISmsSubscription sub in database.GetSmsSubscriptions(theEvent.Identifier))
                {
                    smsSubscriptions.Add(sub);
                }
                alertsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                // load timingresults
                foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
                {
                    timingResults.Add(result);
                }
                resultsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                // load chipreads
                foreach (ChipRead read in database.GetChipReads(theEvent.Identifier))
                {
                    chipReads.Add(read);
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }
        internal void getAgeGroupDictionary()
        {
            ageGroups.Clear();
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                {
                    value = new List<AgeGroup>();
                    ageGroups[group.DistanceId] = value;
                }
                value.Add(group);
            }
        }

        /**
         * Age Group Functions
         */

        public void AddAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "AddAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.AddAgeGroup(group);
                getAgeGroupDictionary();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "AddAgeGroups");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.AddAgeGroups(groups);
                getAgeGroupDictionary();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("MemStore", "GetAgeGroups");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            List<AgeGroup> output = new();
            try
            {
                ageGroupLock.AcquireReaderLock(lockTimeout);
                foreach (List<AgeGroup> groups in ageGroups.Values)
                {
                    output.AddRange(groups);
                }
                getAgeGroupDictionary();
                ageGroupLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetAgeGroups");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            List<AgeGroup> output = new();
            try
            {
                ageGroupLock.AcquireReaderLock(lockTimeout);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> groups))
                {
                    output.AddRange(groups);
                }
                getAgeGroupDictionary();
                ageGroupLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroup(group);
                if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                {
                    list.Remove(group);
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(eventId, distanceId);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> list))
                {
                    list.Clear();
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "RemoveAgeGroups");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(groups);
                foreach (AgeGroup group in groups)
                {
                    if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                    {
                        list.Remove(group);
                    }
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("MemStore", "ResetAgeGroups");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.ResetAgeGroups(eventId);
                ageGroups.Clear();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.UpdateAgeGroup(group);
                if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                {
                    foreach (AgeGroup ageGroup in list)
                    {
                        if (ageGroup.GroupId == group.GroupId)
                        {
                            ageGroup.StartAge = group.StartAge;
                            ageGroup.EndAge = group.EndAge;
                            ageGroup.LastGroup = group.LastGroup;
                            ageGroup.CustomName = group.CustomName;
                        }
                    }
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        /**
         * API functions
         */

        public int AddAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                anAPI.Identifier = database.AddAPI(anAPI);
                apis[anAPI.Identifier] = anAPI;
                apiLock.ReleaseWriterLock();
                return anAPI.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }

        public List<APIObject> GetAllAPI()
        {
            Log.D("MemStore", "GetAllAPI");
            List<APIObject > output = new();
            try
            {
                apiLock.AcquireReaderLock(lockTimeout);
                output.AddRange(apis.Values);
                apiLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
            return output;
        }

        public APIObject GetAPI(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAPI(int identifier)
        {
            Log.D("MemStore", "RemoveAPI");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                database.RemoveAPI(identifier);
                apis.Remove(identifier);
                apiLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }

        public void UpdateAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAPI");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                database.UpdateAPI(anAPI);
                if (apis.TryGetValue(anAPI.Identifier, out APIObject api))
                {
                    api.Type = anAPI.Type;
                    api.URL = anAPI.URL;
                    api.AuthToken = anAPI.AuthToken;
                    api.Nickname = anAPI.Nickname;
                    api.WebURL = anAPI.WebURL;
                }
                else
                {
                    apis[anAPI.Identifier] = anAPI;
                }
                apiLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }

        /**
         * Banned Email/Phone Functions
         */

        // TODO

        public void AddBannedEmail(string email)
        {
            throw new System.NotImplementedException();
        }

        public void AddBannedEmails(List<string> emails)
        {
            throw new System.NotImplementedException();
        }

        public void AddBannedPhone(string phone)
        {
            throw new System.NotImplementedException();
        }

        public void AddBannedPhones(List<string> phones)
        {
            throw new System.NotImplementedException();
        }

        public List<string> GetBannedEmails()
        {
            throw new System.NotImplementedException();
        }

        public List<string> GetBannedPhones()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveBannedEmail(string email)
        {

        }

        public void RemoveBannedPhone(string phone)
        {

        }

        public void ClearBannedEmail()
        {

        }

        public void ClearBannedPhone()
        {

        }

        /**
         * BibChipAssociation Functions
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            throw new System.NotImplementedException();
        }

        public List<BibChipAssociation> GetBibChips()
        {
            throw new System.NotImplementedException();
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            throw new System.NotImplementedException();
        }

        /**
         * ChipRead Functions
         */

        public void AddChipRead(ChipRead read)
        {
            throw new System.NotImplementedException();
        }

        public void AddChipReads(List<ChipRead> reads)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteChipReads(List<ChipRead> reads)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetChipReads()
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetAnnouncerChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetAnnouncerUsedChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetChipReadsSafemode(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetDNSChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void SetChipReadStatus(ChipRead read)
        {
            throw new System.NotImplementedException();
        }

        public void SetChipReadStatuses(List<ChipRead> reads)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateChipRead(ChipRead read)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateChipReads(List<ChipRead> reads)
        {
            throw new System.NotImplementedException();
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Distance Functions
         */

        public void AddDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void AddDistances(List<Distance> distances)
        {
            throw new System.NotImplementedException();
        }

        public Distance GetDistance(int divId)
        {
            throw new System.NotImplementedException();
        }

        public int GetDistanceID(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public List<Distance> GetDistances(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveDistance(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            throw new System.NotImplementedException();
        }

        /**
         * EmailAlert Functions
         */

        public void AddEmailAlert(int eventId, int eventspecific_id)
        {
            throw new System.NotImplementedException();
        }

        public List<int> GetEmailAlerts(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Event Functions
         */

        public void AddEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public Event GetCurrentEvent()
        {
            throw new System.NotImplementedException();
        }

        public void SetCurrentEvent(int eventID)
        {
            throw new System.NotImplementedException();
        }

        public Event GetEvent(int id)
        {
            throw new System.NotImplementedException();
        }

        public int GetEventID(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public List<Event> GetEvents()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEvent(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void SetFinishOptions(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void SetStartWindow(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Participant Functions
         */

        public void AddParticipant(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void AddParticipants(List<Participant> people)
        {
            throw new System.NotImplementedException();
        }

        public Dictionary<int, List<Participant>> GetDistanceParticipantsStatus(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipant(int eventIdentifier, int identifier)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipant(int eventIdentifier, Participant unknown)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipantBib(int eventIdentifier, string bib)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            throw new System.NotImplementedException();
        }

        public int GetParticipantID(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants()
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipant(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            // removes only the eventspecific part from the db
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateParticipant(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateParticipants(List<Participant> participants)
        {
            throw new System.NotImplementedException();
        }

        /**
         * RemoteReader Functions
         */

        public void AddRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            throw new System.NotImplementedException();
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Segment Functions
         */

        public void AddSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void AddSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public int GetSegmentId(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public List<Segment> GetSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegment(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public void ResetSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        /**
         * SMS Functions
         */

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            throw new System.NotImplementedException();
        }

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteSmsSubscriptions(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<(int, int)> GetSMSAlerts(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<APISmsSubscription> GetSmsSubscriptions(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * TimingLocation Functions
         */

        public void AddTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingLocations(List<TimingLocation> locations)
        {
            throw new System.NotImplementedException();
        }

        public int GetTimingLocationID(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingLocation(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        /**
         * TimingResult Functions
         */

        public void AddTimingResult(TimeResult tr)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            throw new System.NotImplementedException();
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void ResetTimingResultsEvent(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void ResetTimingResultsPlacements(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * TimingSystem Functions
         */

        public void AddTimingSystem(TimingSystem system)
        {
            throw new System.NotImplementedException();
        }

        public List<TimingSystem> GetTimingSystems()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingSystem(TimingSystem system)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingSystem(int systemId)
        {
            throw new System.NotImplementedException();
        }

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Alarm Functions
         */

        public void DeleteAlarm(Alarm alarm)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteAlarms(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void SaveAlarm(int eventId, Alarm alarm)
        {
            throw new System.NotImplementedException();
        }

        public void SaveAlarms(int eventId, List<Alarm> alarms)
        {
            throw new System.NotImplementedException();
        }

        /**
         * AppSetting Functions
         */

        public AppSetting GetAppSetting(string name)
        {
            throw new System.NotImplementedException();
        }

        public void SetAppSetting(string name, string value)
        {
            throw new System.NotImplementedException();
        }

        public void SetAppSetting(AppSetting setting)
        {
            throw new System.NotImplementedException();
        }

        /**
         * DistanceStat Functions
         */

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            throw new System.NotImplementedException();
        }

        /**
         * Base Database Functions
         */

        public void HardResetDatabase()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize()
        {
            // Use eventLock to ensure nothing reads from the database.
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                try
                {
                    settingsLock.AcquireReaderLock(lockTimeout);
                    // Load settings
                    settings[Constants.Settings.DEFAULT_TIMING_SYSTEM] = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM);
                    settings[Constants.Settings.COMPANY_NAME] = database.GetAppSetting(Constants.Settings.COMPANY_NAME);
                    settings[Constants.Settings.CONTACT_EMAIL] = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL);
                    settings[Constants.Settings.DEFAULT_EXPORT_DIR] = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR);
                    settings[Constants.Settings.UPDATE_ON_PAGE_CHANGE] = database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE);
                    settings[Constants.Settings.EXIT_NO_PROMPT] = database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT);
                    settings[Constants.Settings.CHECK_UPDATES] = database.GetAppSetting(Constants.Settings.CHECK_UPDATES);
                    settings[Constants.Settings.CURRENT_THEME] = database.GetAppSetting(Constants.Settings.CURRENT_THEME);
                    settings[Constants.Settings.UPLOAD_INTERVAL] = database.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL);
                    settings[Constants.Settings.DOWNLOAD_INTERVAL] = database.GetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL);
                    settings[Constants.Settings.ANNOUNCER_WINDOW] = database.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW);
                    settings[Constants.Settings.ALARM_SOUND] = database.GetAppSetting(Constants.Settings.ALARM_SOUND);
                    settings[Constants.Settings.SERVER_NAME] = database.GetAppSetting(Constants.Settings.SERVER_NAME);
                    settings[Constants.Settings.TWILIO_ACCOUNT_SID] = database.GetAppSetting(Constants.Settings.TWILIO_ACCOUNT_SID);
                    settings[Constants.Settings.TWILIO_AUTH_TOKEN] = database.GetAppSetting(Constants.Settings.TWILIO_AUTH_TOKEN);
                    settings[Constants.Settings.TWILIO_PHONE_NUMBER] = database.GetAppSetting(Constants.Settings.TWILIO_PHONE_NUMBER);
                    settings[Constants.Settings.MAILGUN_FROM_NAME] = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_NAME);
                    settings[Constants.Settings.MAILGUN_FROM_EMAIL] = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL);
                    settings[Constants.Settings.MAILGUN_API_KEY] = database.GetAppSetting(Constants.Settings.MAILGUN_API_KEY);
                    settings[Constants.Settings.MAILGUN_API_URL] = database.GetAppSetting(Constants.Settings.MAILGUN_API_URL);
                    settingsLock.ReleaseWriterLock();
                }
                catch (ApplicationException e)
                {
                    Log.D("MemStore", "Exception acquiring settingsLock. " + e.Message);
                    throw new MutexLockException("settingsLock");
                }
                try
                {
                    apiLock.AcquireWriterLock(lockTimeout);
                    // Load apis
                    foreach (APIObject api in database.GetAllAPI())
                    {
                        apis[api.Identifier] = api;
                    }
                    apiLock.ReleaseWriterLock();
                }
                catch (ApplicationException e)
                {
                    Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                    throw new MutexLockException("apiLock");
                }
                try
                {
                    timingSystemsLock.AcquireWriterLock(lockTimeout);
                    // load timingsystems
                    foreach (TimingSystem system in database.GetTimingSystems())
                    {
                        timingSystems[system.SystemIdentifier] = system;
                    }
                    timingSystemsLock.ReleaseWriterLock();
                }
                catch (ApplicationException e)
                {
                    Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                    throw new MutexLockException("timingSystemsLock");
                }
                try
                {
                    bannedLock.AcquireWriterLock(lockTimeout);
                    // load banned phones
                    foreach (string phone in database.GetBannedPhones())
                    {
                        bannedPhones.Add(phone);
                    }
                    // load banned emails
                    foreach (string email in database.GetBannedEmails())
                    {
                        bannedEmails.Add(email);
                    }
                    bannedLock.ReleaseWriterLock();
                }
                catch (ApplicationException e)
                {
                    Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                    throw new MutexLockException("bannedLock");
                }
                // Load event
                theEvent = database.GetCurrentEvent();
                // Load all data
                LoadEvent();
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void ResetDatabase()
        {
            throw new System.NotImplementedException();
        }
    }
}
