using System;
using System.Collections.Generic;

namespace Chronokeep.Constants
{
    public class Timing
    {
        public const int LOCATION_START = -1;
        public const int LOCATION_FINISH = -2;
        public const int LOCATION_ANNOUNCER = -3;
        public const int LOCATION_DUMMY = -12;

        public const int SEGMENT_FINISH = -1;
        public const int SEGMENT_START = -2;
        public const int SEGMENT_NONE = -3;

        public const int CHIPREAD_STATUS_NONE = 0; // If this value changes, SQLiteInterface version must be updated to reflect said change.
        public const int CHIPREAD_STATUS_UNUSEDSTART = 1;
        public const int CHIPREAD_STATUS_IGNORE = 2;
        public const int CHIPREAD_STATUS_PRESTART = 3;
        public const int CHIPREAD_STATUS_USED = 4;
        public const int CHIPREAD_STATUS_WITHINIGN = 5;  // Within the ignore period
        public const int CHIPREAD_STATUS_OVERMAX = 6; // over max occurrences
        public const int CHIPREAD_STATUS_UNKNOWN = 7; // Unknown chip read
        public const int CHIPREAD_STATUS_STARTTIME = 8;
        public const int CHIPREAD_STATUS_DNF = 9;
        public const int CHIPREAD_STATUS_ANNOUNCER_SEEN = 10;
        public const int CHIPREAD_STATUS_ANNOUNCER_USED = 11;
        public const int CHIPREAD_STATUS_DNS = 12;
        public const int CHIPREAD_STATUS_AFTER_DNS = 13;
        public const int CHIPREAD_STATUS_DNF_IGNORE = 14;
        public const int CHIPREAD_STATUS_DNS_IGNORE = 15;

        public const int TIMERESULT_STATUS_NONE = 0;
        public const int TIMERESULT_STATUS_DNF = 1;
        public const int TIMERESULT_STATUS_DNS = 2;
        public const int TIMERESULT_STATUS_PROCESSED = 3;

        public const int TIMERESULT_UPLOADED_FALSE = 0;
        public const int TIMERESULT_UPLOADED_TRUE = 1;

        public const int CHIPREAD_TYPE_MANUAL = 1;
        public const int CHIPREAD_TYPE_CHIP = 0;  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public const string CHIPREAD_DUMMYCHIP = "-1";
        public const string CHIPREAD_DUMMYBIB = "-1";  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public const int TIMERESULT_DUMMYPERSON = -1;
        public const int TIMERESULT_DUMMYPLACE = -1;
        public const int TIMERESULT_GENDER_MALE = 1;
        public const int TIMERESULT_GENDER_FEMALE = 2;
        public const int TIMERESULT_GENDER_UNKNOWN = 3;
        public const int TIMERESULT_GENDER_NON_BINARY = 4;
        public const int TIMERESULT_DUMMYAGEGROUP = -1;
        public const int TIMERESULT_DUMMYREAD = -1;

        public const int EVENTSPECIFIC_UNKNOWN = 0;
        public const int EVENTSPECIFIC_STARTED = 1;
        public const int EVENTSPECIFIC_FINISHED = 2;
        public const int EVENTSPECIFIC_DNF = 3;
        public const int EVENTSPECIFIC_DNS = 4;

        public const int COMMON_SEGMENTS_DISTANCEID = -1;
        public const int COMMON_AGEGROUPS_DISTANCEID = -1;

        public const int AGEGROUPS_CUSTOM_DISTANCEID = -8;
        public const int AGEGROUPS_LASTGROUP_TRUE = 1;
        public const int AGEGROUPS_LASTGROUP_FALSE = 0;

        public const int EVENT_TYPE_DISTANCE = 0;
        public const int EVENT_TYPE_TIME = 1;
        public const int EVENT_TYPE_BACKYARD_ULTRA = 2;

        public const int TIMINGSYSTEM_UNKNOWN = -1;

        public const int PARTICIPANT_DUMMYIDENTIFIER = -1;
        public const int DISTANCE_DUMMYIDENTIFIER = -1;
        public const int DISTANCE_NO_LINKED_ID = -1;

        // These values are what are sent in the API Result and indicate the type of the result.
        public const int DISTANCE_TYPE_NORMAL = 0;
        public const int DISTANCE_TYPE_EARLY = 11;
        public const int DISTANCE_TYPE_UNOFFICIAL = 12;
        public const int DISTANCE_TYPE_VIRTUAL = 13;
        public const int DISTANCE_TYPE_LATE = 14;
        public const int API_TYPE_DNF = 30;
        public const int API_TYPE_DNS = 31;

        // API Upload Count
        public const int API_LOOP_COUNT = 20;

        // Announcer variables
        public const int ANNOUNCER_LOOP_TIMER = 2;

        private static DateTime UnixDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static DateTime RFIDDateTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long RFIDDateToEpoch(DateTime date)
        {
            var ticks = date.Ticks - RFIDDateTime.Ticks;
            return ticks / TimeSpan.TicksPerSecond;
        }

        public static DateTime RFIDEpochToDate(long date)
        {
            return RFIDDateTime.AddTicks(date * TimeSpan.TicksPerSecond);
        }

        public static long UnixDateToEpoch(DateTime date)
        {
            var ticks = date.Ticks - UnixDateTime.Ticks;
            return ticks / TimeSpan.TicksPerSecond;
        }

        public static DateTime UnixEpochToDate(long date)
        {
            return UnixDateTime.AddTicks(date * TimeSpan.TicksPerSecond);
        }

        public static DateTime UTCToLocalDate(long seconds, int milliseconds)
        {
            return UnixEpochToDate(seconds).ToLocalTime().AddMilliseconds(milliseconds);
        }

        public static long UTCSecondsToRFIDSeconds(long seconds)
        {
            return RFIDDateToEpoch(UnixEpochToDate(seconds).ToLocalTime());
        }

        public static readonly Dictionary<int, string> EVENTSPECIFIC_STATUS_NAMES = new Dictionary<int, string>()
        {
            { EVENTSPECIFIC_UNKNOWN, "Unknown" },
            { EVENTSPECIFIC_STARTED, "Started" },
            { EVENTSPECIFIC_FINISHED, "Finished" },
            { EVENTSPECIFIC_DNF, "DNF" },
            { EVENTSPECIFIC_DNS, "DNS" },
        };

        public static string SecondsToTime(long seconds)
        {
            return string.Format("{0}:{1:D2}:{2:D2}", seconds / 3600, (seconds % 3600) / 60, seconds % 60);
        }

        public static string ToTime(long seconds, int milliseconds)
        {
            return string.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", seconds / 3600, seconds % 3600 / 60, seconds % 60, milliseconds);
        }

        public static string ToTimeOfDay(long seconds, int milliseconds)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", seconds / 3600, seconds % 3600 / 60, seconds % 60, milliseconds);
        }
    }
}
