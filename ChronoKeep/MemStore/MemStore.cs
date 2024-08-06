using Chronokeep.Database.SQLite;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepRemote;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal class MemStore
    {
        // Singleton
        private static MemStore instance;

        // Items that don't rely on a specific Event
        // key == setting name
        private static Dictionary<string, AppSetting> settings = new();
        // key == api id
        private static Dictionary<int, APIObject> apis = new();
        // key == system id
        private static Dictionary<int, TimingSystem> timingSystems = new();
        private static HashSet<string> bannedPhones = new();
        private static HashSet<string> bannedEmails = new();

        // Event and the related fields
        private static Event theEvent;
        // key == distance id
        private static Dictionary<int, Distance> distances = new();
        // key == location id
        private static Dictionary<int, TimingLocation> locations = new();
        // key == segment id
        private static Dictionary<int, Segment> segments = new();
        // key == event specific id
        private static Dictionary<int, Participant> participants = new();
        // key == chip
        private static Dictionary<string, BibChipAssociation> chipToBibAssociations = new();
        // key == bib
        private static Dictionary<string, BibChipAssociation> bibToChipAssociations = new();

        // key == distanceId
        private static Dictionary<int, List<AgeGroup>> ageGroups = new();
        // key == bib
        private static Dictionary<string, Alarm> bibAlarms = new();
        // key == chip
        private static Dictionary<string, Alarm> chipAlarms = new();
        private static List<RemoteReader> remoteReaders = new();

        private static HashSet<(int, int)> smsAlerts = new();
        private static HashSet<int> emailAlerts = new();
        private static List<APISmsSubscription> smsSubscriptions = new();

        // Timing results
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

        private MemStore(IDBInterface database)
        {
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
            // Load apis
            foreach (APIObject api in database.GetAllAPI())
            {
                apis[api.Identifier] = api;
            }
            // load timingsystems
            foreach (TimingSystem system in database.GetTimingSystems())
            {
                timingSystems[system.SystemIdentifier] = system;
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
            // Load event
            theEvent = database.GetCurrentEvent();
            // Load all data if theEvent isn't null.
            if (theEvent == null)
            {
                return;
            }
            // load distances
            foreach (Distance dist in database.GetDistances(theEvent.Identifier))
            {
                distances[dist.Identifier] = dist;
            }
            // load locations
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                locations[loc.Identifier] = loc;
            }
            // load segments
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                segments[seg.Identifier] = seg;
            }
            // load participants
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                participants[part.EventSpecific.Identifier] = part;
            }
            // load bibchipassociations
            foreach (BibChipAssociation assoc in database.GetBibChips(theEvent.Identifier))
            {
                chipToBibAssociations[assoc.Chip] = assoc;
                bibToChipAssociations[assoc.Bib] = assoc;
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
            // load alarms
            foreach (Alarm alarm in database.GetAlarms(theEvent.Identifier))
            {
                bibAlarms[alarm.Bib] = alarm;
                chipAlarms[alarm.Chip] = alarm;
            }
            // load remote readers
            foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
            {
                remoteReaders.Add(reader);
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
        }

        public static MemStore GetMemStore(IDBInterface database)
        {
            if (instance == null)
            {
                instance = new MemStore(database);
            }
            return instance;
        }
    }
}
