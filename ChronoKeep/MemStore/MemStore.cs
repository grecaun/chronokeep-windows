using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.MemStore
{
    internal class MemStore : IDBInterface
    {
        internal class MutexLockException : System.Exception
        {
            public MutexLockException(string message) : base(message) { }
        }

        // Singleton
        private static MemStore instance;

        // Items that don't rely on a specific Event
        private static Mutex settingsMtx = new();
        // key == setting name
        private static Dictionary<string, AppSetting> settings = new();

        private static Mutex apiMtx = new();
        // key == api id
        private static Dictionary<int, APIObject> apis = new();

        private static Mutex timingSystemsMtx = new();
        // key == system id
        private static Dictionary<int, TimingSystem> timingSystems = new();

        private static Mutex bannedMtx = new();
        private static HashSet<string> bannedPhones = new();
        private static HashSet<string> bannedEmails = new();

        // Event and the related fields
        private static Mutex eventMtx = new();
        private static Event theEvent;

        private static Mutex distanceMtx = new();
        // key == distance id
        private static Dictionary<int, Distance> distances = new();

        private static Mutex locationsMtx = new();
        // key == location id
        private static Dictionary<int, TimingLocation> locations = new();

        private static Mutex segmentMtx = new();
        // key == segment id
        private static Dictionary<int, Segment> segments = new();

        private static Mutex participantsMtx = new();
        // key == event specific id
        private static Dictionary<int, Participant> participants = new();

        private static Mutex bibChipMtx = new();
        // key == chip
        private static Dictionary<string, BibChipAssociation> chipToBibAssociations = new();
        // key == bib
        private static Dictionary<string, BibChipAssociation> bibToChipAssociations = new();

        private static Mutex ageGroupMtx = new();
        // key == distanceId
        private static Dictionary<int, List<AgeGroup>> ageGroups = new();

        private static Mutex alarmMtx = new();
        // key == bib
        private static Dictionary<string, Alarm> bibAlarms = new();
        // key == chip
        private static Dictionary<string, Alarm> chipAlarms = new();

        private static Mutex remoteReadersMtx = new();
        private static List<RemoteReader> remoteReaders = new();

        private static Mutex alertsMtx = new();
        private static HashSet<(int, int)> smsAlerts = new();
        private static HashSet<int> emailAlerts = new();
        private static List<APISmsSubscription> smsSubscriptions = new();

        // Timing results
        private static Mutex resultsMtx = new();
        private static List<TimeResult> unprocessedTimingResults = new();
        private static List<TimeResult> notUploadedTimingResults = new();
        private static List<TimeResult> uploadedTimingResults = new();
        // key == bib or chip
        private static Dictionary<string, TimeResult> lastSeenResults = new();
        // key == bib or chip
        private static Dictionary<string, TimeResult> startTimes = new();
        // key == bib or chip
        private static Dictionary<string, TimeResult> finishTimes = new();
        // key == segmentId
        private static Dictionary<int, List<TimeResult>> segmentTimes = new();

        // Chip Read data
        private static Mutex chipReadsMtx = new();
        // Unprocessed == all reads that haven't been looked at but aren't announcer reads
        private static List<ChipRead> unprocessedChipReads = new();
        // Useful == all chip reads that are used for TimingResults
        private static List<ChipRead> usefulChipReads = new();
        // notUseful == all chip reads that were processed but not used for TimingResults
        private static List<ChipRead> notUsefulChipReads = new();
        // announcerChipReads == unprocessed announcer chipreads
        private static List<ChipRead> announcerChipReads = new();
        // announcerUsedChipReads == processed chipreads
        private static List<ChipRead> announerUsedChipReads = new();
        // dns == DidNotStart chipreads
        private static List<ChipRead> dnsChipReads = new();

        // Local variables
        private IDBInterface database;
        private DatabaseSaver databaseSaver;

        private MemStore(IMainWindow window, IDBInterface database)
        {
            this.database = database;
            databaseSaver = DatabaseSaver.NewSaver(window, database);
            // Load settings
            if (!settingsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Settings Mutex");
            }
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
            settingsMtx.ReleaseMutex();
            if (!apiMtx.WaitOne(3000))
            {
                throw new MutexLockException("API Mutex");
            }
            // Load apis
            foreach (APIObject api in database.GetAllAPI())
            {
                apis[api.Identifier] = api;
            }
            apiMtx.ReleaseMutex();
            if (!timingSystemsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Timing Systems Mutex");
            }
            // load timingsystems
            foreach (TimingSystem system in database.GetTimingSystems())
            {
                timingSystems[system.SystemIdentifier] = system;
            }
            timingSystemsMtx.ReleaseMutex();
            if (!bannedMtx.WaitOne(3000))
            {
                throw new MutexLockException("Banned Mutex");
            }
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
            bannedMtx.ReleaseMutex();
            if (!eventMtx.WaitOne(3000))
            {
                throw new MutexLockException("Event Mutex");
            }
            // Load event
            theEvent = database.GetCurrentEvent();
            // Load all data
            LoadEvent();
            eventMtx.ReleaseMutex();
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
            if (!distanceMtx.WaitOne(3000))
            {
                throw new MutexLockException("Distance Mutex");
            }
            // load distances
            foreach (Distance dist in database.GetDistances(theEvent.Identifier))
            {
                distances[dist.Identifier] = dist;
            }
            distanceMtx.ReleaseMutex();
            if (!locationsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Locations Mutex");
            }
            // load locations
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                locations[loc.Identifier] = loc;
            }
            locationsMtx.ReleaseMutex();
            if (!segmentMtx.WaitOne(3000))
            {
                throw new MutexLockException("Segment Mutex");
            }
            // load segments
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                segments[seg.Identifier] = seg;
            }
            segmentMtx.ReleaseMutex();
            if (!participantsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Participants Mutex");
            }
            // load participants
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                participants[part.EventSpecific.Identifier] = part;
            }
            participantsMtx.ReleaseMutex();
            if (!bibChipMtx.WaitOne(3000))
            {
                throw new MutexLockException("Bib Chip Mutex");
            }
            // load bibchipassociations
            foreach (BibChipAssociation assoc in database.GetBibChips(theEvent.Identifier))
            {
                chipToBibAssociations[assoc.Chip] = assoc;
                bibToChipAssociations[assoc.Bib] = assoc;
            }
            bibChipMtx.ReleaseMutex();
            if (!ageGroupMtx.WaitOne(3000))
            {
                throw new MutexLockException("Age Group Mutex");
            }
            // load age groups
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                {
                    value = new List<AgeGroup>();
                    ageGroups[group.DistanceId] = value;
                }
                value.Add(group);
            }
            ageGroupMtx.ReleaseMutex();
            if (!alarmMtx.WaitOne(3000))
            {
                throw new MutexLockException("Alarms Mutex");
            }
            // load alarms
            foreach (Alarm alarm in database.GetAlarms(theEvent.Identifier))
            {
                bibAlarms[alarm.Bib] = alarm;
                chipAlarms[alarm.Chip] = alarm;
            }
            alarmMtx.ReleaseMutex();
            if (!remoteReadersMtx.WaitOne(3000))
            {
                throw new MutexLockException("Remote Readers Mutex");
            }
            // load remote readers
            foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
            {
                remoteReaders.Add(reader);
            }
            remoteReadersMtx.ReleaseMutex();
            if (!alertsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Alerts Mutex");
            }
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
            alertsMtx.ReleaseMutex();
            if (!resultsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Timing Results Mutex");
            }
            // load timingresults
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                // split results into not-processed
                // then processed and uploaded and processed but not uploaded
                if (result.Status == Constants.Timing.TIMERESULT_STATUS_NONE)
                {
                    unprocessedTimingResults.Add(result);
                }
                else if (result.Uploaded == Constants.Timing.TIMERESULT_UPLOADED_TRUE)
                {
                    uploadedTimingResults.Add(result);
                }
                else
                {
                    notUploadedTimingResults.Add(result);
                }
                // last seen    // key == bib or chip
                if (result.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    if (lastSeenResults.TryGetValue(result.Bib, out TimeResult old))
                    {
                        if (result.Seconds > old.Seconds)
                        {
                            lastSeenResults[result.Bib] = result;
                        }
                    }
                    else
                    {
                        lastSeenResults[result.Bib] = result;
                    }
                }
                else if (result.Chip != Constants.Timing.CHIPREAD_DUMMYCHIP)
                {
                    if (lastSeenResults.TryGetValue(result.Chip, out TimeResult old))
                    {
                        if (result.Seconds > old.Seconds)
                        {
                            lastSeenResults[result.Chip] = result;
                        }
                    }
                    else
                    {
                        lastSeenResults[result.Chip] = result;
                    }
                }
                // start        // key == bib or chip
                if (result.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    if (result.SegmentId == Constants.Timing.SEGMENT_START)
                    {
                        startTimes[result.Bib] = result;
                    }
                }
                else if (result.Chip != Constants.Timing.CHIPREAD_DUMMYCHIP)
                {
                    if (result.SegmentId == Constants.Timing.SEGMENT_START)
                    {
                        startTimes[result.Chip] = result;
                    }
                }
                // finish       // key == bib or chip
                if (result.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                    {
                        startTimes[result.Bib] = result;
                    }
                }
                else if (result.Chip != Constants.Timing.CHIPREAD_DUMMYCHIP)
                {
                    if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                    {
                        startTimes[result.Chip] = result;
                    }
                }
                // segment      // key == segmentID
                if (!segmentTimes.TryGetValue(result.SegmentId, out List<TimeResult> segTimeList))
                {
                    segTimeList = new();
                    segmentTimes[result.SegmentId] = segTimeList;
                }
                segTimeList.Add(result);
            }
            resultsMtx.ReleaseMutex();
            if (!chipReadsMtx.WaitOne(3000))
            {
                throw new MutexLockException("Chip Reads Mutex");
            }
            // load chipreads
            foreach (ChipRead read in database.GetChipReads(theEvent.Identifier))
            {
                // Unprocessed == all reads that haven't been looked at but aren't announcer reads
                if (read.Type == Constants.Timing.CHIPREAD_STATUS_NONE)
                {
                    unprocessedChipReads.Add(read);
                }
                // Useful == all chip reads that are used for TimingResults
                if (read.Type == Constants.Timing.CHIPREAD_STATUS_USED
                    || read.Type == Constants.Timing.CHIPREAD_STATUS_NONE
                    || read.Type == Constants.Timing.CHIPREAD_STATUS_STARTTIME
                    || read.Type == Constants.Timing.CHIPREAD_STATUS_DNF
                    || read.Type == Constants.Timing.CHIPREAD_STATUS_DNS
                    )
                {
                    usefulChipReads.Add(read);
                }
                // notUseful == all chip reads that were processed but not used for TimingResults
                else
                {
                    notUsefulChipReads.Add(read);
                }
                // announcerChipReads == unprocessed announcer chipreads
                if (read.LocationID == Constants.Timing.LOCATION_ANNOUNCER)
                {
                    announcerChipReads.Add(read);
                }
                // announcerUsedChipReads == processed chipreads
                if (read.Type == Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED)
                {
                    announcerChipReads.Add(read);
                }
                // dns == DidNotStart chipreads
                if (read.Type == Constants.Timing.CHIPREAD_STATUS_DNS)
                {
                    dnsChipReads.Add(read);
                }
            }
            chipReadsMtx.ReleaseMutex();
        }

        public void AddAgeGroup(AgeGroup group)
        {
            throw new System.NotImplementedException();
        }

        public void AddAgeGroups(List<AgeGroup> groups)
        {
            throw new System.NotImplementedException();
        }

        public int AddAPI(APIObject anAPI)
        {
            throw new System.NotImplementedException();
        }

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

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            throw new System.NotImplementedException();
        }

        public void AddChipRead(ChipRead read)
        {
            throw new System.NotImplementedException();
        }

        public void AddChipReads(List<ChipRead> reads)
        {
            throw new System.NotImplementedException();
        }

        public void AddDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void AddDistances(List<Distance> distances)
        {
            throw new System.NotImplementedException();
        }

        public void AddEmailAlert(int eventId, int eventspecific_id)
        {
            throw new System.NotImplementedException();
        }

        public void AddEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void AddParticipant(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void AddParticipants(List<Participant> people)
        {
            throw new System.NotImplementedException();
        }

        public void AddRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            throw new System.NotImplementedException();
        }

        public void AddSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void AddSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            throw new System.NotImplementedException();
        }

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingLocations(List<TimingLocation> locations)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingResult(TimeResult tr)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public void AddTimingSystem(TimingSystem system)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteAlarm(Alarm alarm)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteAlarms(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteChipReads(List<ChipRead> reads)
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

        public void DeleteSmsSubscriptions(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<APIObject> GetAllAPI()
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

        public APIObject GetAPI(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public AppSetting GetAppSetting(string name)
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

        public List<BibChipAssociation> GetBibChips()
        {
            throw new System.NotImplementedException();
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetChipReads()
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

        public Event GetCurrentEvent()
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

        public Dictionary<int, List<Participant>> GetDistanceParticipantsStatus(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public List<Distance> GetDistances(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetDNSChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<int> GetEmailAlerts(int eventId)
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

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
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

        public List<RemoteReader> GetRemoteReaders(int eventId)
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

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
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

        public List<TimeResult> GetStartTimes(int eventId)
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

        public List<TimeResult> GetTimingResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimingSystem> GetTimingSystems()
        {
            throw new System.NotImplementedException();
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void HardResetDatabase()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAPI(int identifier)
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

        public void RemoveDistance(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEntries(List<Participant> people)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEntry(int eventSpecificId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEntry(Participant person)
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

        public void RemoveParticipant(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntry(Participant person)
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

        public void RemoveTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingLocation(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingResult(TimeResult tr)
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

        public void ResetAgeGroups(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void ResetDatabase()
        {
            throw new System.NotImplementedException();
        }

        public void ResetSegments(int eventId)
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

        public void SaveAlarm(int eventId, Alarm alarm)
        {
            throw new System.NotImplementedException();
        }

        public void SaveAlarms(int eventId, List<Alarm> alarms)
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

        public void SetChipReadStatus(ChipRead read)
        {
            throw new System.NotImplementedException();
        }

        public void SetChipReadStatuses(List<ChipRead> reads)
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

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            throw new System.NotImplementedException();
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            throw new System.NotImplementedException();
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateAPI(APIObject anAPI)
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

        public void UpdateDistance(Distance div)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateEvent(Event anEvent)
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

        public void UpdateSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateTimingLocation(TimingLocation tp)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            throw new System.NotImplementedException();
        }
    }
}
