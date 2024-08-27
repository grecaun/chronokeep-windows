using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
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
        private static List<Event> allEvents = new();
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
        // key == (bib, chip)
        private static Dictionary<(string, string), Alarm> alarms = new();

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
            // Load event
            theEvent = database.GetCurrentEvent();
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
                foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
                {
                    if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                    {
                        value = new List<AgeGroup>();
                        ageGroups[group.DistanceId] = value;
                    }
                    value.Add(group);
                }
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
                    alarms[(alarm.Bib, alarm.Chip)] = alarm;
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

        /**
         * Base Database Functions
         */

        internal void ResetVariables()
        {
            // app setting
            try
            {
                settingsLock.AcquireWriterLock(lockTimeout);
                settings.Clear();
                settingsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring settingsLock. " + e.Message);
                throw new MutexLockException("settingsLock");
            }
            // api
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                apis.Clear();
                apiLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
            // timing system
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                timingSystems.Clear();
                timingSystemsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
            // banned
            try
            {
                bannedLock.AcquireWriterLock(lockTimeout);
                bannedEmails.Clear();
                bannedPhones.Clear();
                bannedLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring bannedLock. " + e.Message);
                throw new MutexLockException("bannedLock");
            }
            // distances
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                distances.Clear();
                distanceLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
            // locations
            try
            {
                locationsLock.AcquireWriterLock(lockTimeout);
                locations.Clear();
                locationsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
            // segments
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                segments.Clear();
                segmentLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            // participants
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                participants.Clear();
                participantsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
            // age groups
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                ageGroups.Clear();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            // alarms
            try
            {
                alarmLock.AcquireWriterLock(lockTimeout);
                alarms.Clear();
                alarmLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
            // alerts
            try
            {
                alertsLock.AcquireWriterLock(lockTimeout);
                emailAlerts.Clear();
                smsAlerts.Clear();
                alertsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
            // bib chips
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                chipToBibAssociations.Clear();
                bibChipLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
            // remote reader
            try
            {
                remoteReadersLock.AcquireWriterLock(lockTimeout);
                remoteReaders.Clear();
                remoteReadersLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
            // results
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                timingResults.Clear();
                resultsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
            // chipread
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                chipReads.Clear();
                chipReadsLock.ReleaseWriterLock();
            }
            catch (ApplicationException e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            // event
            theEvent = null;
        }

        public void HardResetDatabase()
        {
            Log.D("MemStore", "HardResetDatabase");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.HardResetDatabase();
                ResetVariables();
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void Initialize()
        {
            // Use eventLock to ensure nothing reads from the database.
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.Initialize();
                try
                {
                    settingsLock.AcquireWriterLock(lockTimeout);
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
                // Load events
                allEvents.Clear();
                allEvents.AddRange(database.GetEvents());
                // Load event data
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
            Log.D("MemStore", "ResetDatabase");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.ResetDatabase();
                ResetVariables();
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }
    }
}
