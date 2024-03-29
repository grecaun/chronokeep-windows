﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chronokeep
{
    public class TimeResult
    {
        private int eventId, eventspecificId, locationId, segmentId,
            occurrence, readId, place, agePlace, genderPlace,
            ageGroupId, chipMilliseconds, status, uploaded, type, milliseconds;
        private long chipSeconds, seconds;
        private string time, locationName, segmentName, firstName, lastName, bib,
            distanceName, unknownId, chipTime, gender, ageGroupName, splitTime = "", birthday, linked_distance_name = "", chip = "";
        private bool anonymous;
        DateTime systemTime;

        private static int raceType = Constants.Timing.EVENT_TYPE_DISTANCE;

        public static readonly Regex timeRegex = new Regex(@"(\d+):(\d{2}):(\d{2})\.(\d{3})");

        public static Dictionary<int, TimingLocation> locations = null;
        public static Dictionary<int, Segment> segments = null;
        public static Dictionary<(string, int), TimeResult> RaceResults = null;
        public static Event theEvent = null;

        // database constructor
        public TimeResult(
            int eventId, 
            int eventspecificId, 
            int locationId, 
            int segmentId,
            string time, 
            int occurrence, 
            string first, 
            string last, 
            string distance, 
            string bib,
            int readId, 
            string unknownId, 
            long systemTimeSec, 
            int systemTimeMill, 
            string chipTime, 
            int place,
            int agePlace, 
            int genderPlace, 
            string gender, 
            int status, 
            string split,
            int ageGroupId, 
            string ageGroupName, 
            int uploaded, 
            string birthday, 
            int type, 
            string linked_distance_name, 
            string chip, 
            bool anonymous
            )
        {
            this.eventId = eventId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.time = time;
            this.occurrence = occurrence;
            this.locationName = locations != null ? locations.ContainsKey(this.locationId) ?
                locations[this.locationId].Name : "Unknown" : "Unknown";
            if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
            {
                this.segmentName = "Finish";
            }
            else if (Constants.Timing.SEGMENT_START == this.segmentId)
            {
                this.segmentName = "Start";
            }
            else if (segments != null && segments.ContainsKey(this.segmentId))
            {
                this.segmentName = segments[this.segmentId].Name;
            }
            else
            {
                this.segmentName = "";
            }
            if (raceType == Constants.Timing.EVENT_TYPE_TIME && Constants.Timing.SEGMENT_FINISH == this.SegmentId)
            {
                if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
                {
                    this.segmentName = string.Format("Lap {0}", occurrence);
                }
                else if (Constants.Timing.SEGMENT_START != this.segmentId)
                {
                    this.segmentName = string.Format("{0} {1}", this.segmentName, occurrence);
                }
            }
            this.firstName = first;
            this.lastName = last;
            this.distanceName = distance;
            this.bib = bib;
            this.unknownId = unknownId;
            this.readId = readId;
            this.systemTime = Constants.Timing.RFIDEpochToDate(systemTimeSec).AddMilliseconds(systemTimeMill);
            this.chipTime = chipTime;
            this.place = place;
            this.agePlace = agePlace;
            this.genderPlace = genderPlace;
            this.gender = gender;
            this.ageGroupId = ageGroupId;
            this.ageGroupName = ageGroupName;
            Match chipTimeMatch = timeRegex.Match(chipTime);
            chipSeconds = 0;
            chipMilliseconds = 0;
            if (chipTimeMatch.Success)
            {
                chipSeconds = (Convert.ToInt64(chipTimeMatch.Groups[1].Value) * 3600)
                   + (Convert.ToInt64(chipTimeMatch.Groups[2].Value) * 60)
                   + Convert.ToInt64(chipTimeMatch.Groups[3].Value);
                chipMilliseconds = Convert.ToInt32(chipTimeMatch.Groups[4].Value);
            }
            Match timeMatch = timeRegex.Match(time);
            seconds = 0;
            milliseconds = 0;
            if (timeMatch.Success)
            {
                seconds = (Convert.ToInt64(timeMatch.Groups[1].Value) * 3600)
                   + (Convert.ToInt64(timeMatch.Groups[2].Value) * 60)
                   + Convert.ToInt64(timeMatch.Groups[3].Value);
                milliseconds = Convert.ToInt32(timeMatch.Groups[4].Value);
            }
            this.status = status;
            this.splitTime = split;
            this.uploaded = uploaded;
            this.birthday = birthday;
            this.type = type;
            this.linked_distance_name = linked_distance_name;
            this.chip = chip;
            this.anonymous = anonymous;
        }

        public TimeResult(
            int eventId, 
            int readId, 
            int eventspecificId, 
            int locationId,
            int segmentId, 
            int occurrence, 
            string time, 
            string unknownId, 
            string chipTime,
            DateTime systemTime, 
            string bib, 
            int status
            )
        {
            this.eventId = eventId;
            this.readId = readId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.occurrence = occurrence;
            this.time = time;
            this.unknownId = unknownId;
            this.chipTime = chipTime;
            this.systemTime = systemTime;
            this.bib = bib;
            place = Constants.Timing.TIMERESULT_DUMMYPLACE;
            agePlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            genderPlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            this.status = status;
            this.splitTime = "";
        }

        public static void SetupStaticVariables(IDBInterface database)
        {
            locations = new Dictionary<int, TimingLocation>();
            segments = new Dictionary<int, Segment>();
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            if (!theEvent.CommonStartFinish)
            {
                locations[Constants.Timing.LOCATION_FINISH] = new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
                locations[Constants.Timing.LOCATION_START] = new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow);
            }
            else
            {
                locations[Constants.Timing.LOCATION_FINISH] = new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
            }
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                locations[loc.Identifier] = loc;
            }
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                segments[seg.Identifier] = seg;
            }
            raceType = theEvent.EventType;
        }

        public static void SetupRaceResults(IDBInterface database)
        {
        }

        public int EventSpecificId { get => eventspecificId; set => eventspecificId = value; }
        public int LocationId { get => locationId; set => locationId = value; }
        public int EventIdentifier { get => eventId; set => eventId = value; }
        public int SegmentId { get => segmentId; set => segmentId = value; }
        public int Occurrence { get => occurrence; set => occurrence = value; }
        public string Time { get => time; set => time = value; }
        public string LocationName { get => locationName; set => locationName = value; }
        public string SegmentName { get => segmentName; set => segmentName = value; }
        public string First { get => firstName; set => firstName = value; }
        public string Last { get => lastName; set => lastName = value; }
        public string ParticipantName { get => string.Format("{0} {1}", firstName, lastName).Trim(); }
        public string PrettyParticipantName { get => anonymous ? string.Format("Bib {0}", bib) : string.Format("{0} {1}", firstName, lastName).Trim(); }
        public string DistanceName { get => linked_distance_name == "" ? distanceName : linked_distance_name; }
        public string RealDistanceName { get => distanceName; }
        public string Bib { get => bib; set => bib = value; }
        public int AgeGroupId { get => ageGroupId; set => ageGroupId = value; }
        public string UnknownId { get => unknownId; set => unknownId = value; }
        public int ReadId { get => readId; set => readId = value; }
        public int Place { get => place; set => place = value; }
        public string PlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? (place < 1 ? "" : place.ToString()) : ""; }
        public string PrettyPlaceStr
        {
            get => type == Constants.Timing.DISTANCE_TYPE_EARLY && place > 0 ? string.Format("{0}e", place) :
                type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL && place > 0 ? string.Format("{0}u", place) :
                Finish && place > 0 ? place.ToString() : "";
        }
        public int AgePlace { get => agePlace; set => agePlace = value; }
        public string AgePlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? (agePlace < 1 ? "" : agePlace.ToString()) : ""; }
        public int GenderPlace { get => genderPlace; set => genderPlace = value; }
        public string GenderPlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? (genderPlace < 1 ? "" : genderPlace.ToString()) : ""; }
        public int Type { get => type; set => type = value; }
        public string Identifier { get => unknownId; }
        public string PrettyType
        {
            get => PrettyTypeStr();
        }
        public string PrettyGender
        {
            get => gender == "Man" ? "M" : gender == "Woman" ? "W" : gender == "Non-Binary" ? "NB" : gender == "Not Specified" ? "" : gender.Substring(0, 2);
        }

        public string PrettyTypeStr()
        {
            string output = type == Constants.Timing.DISTANCE_TYPE_EARLY ? "E" : type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL ? "U" : "";
            return anonymous ? "A" + output : output;
        }

        public DateTime SystemTime { get => systemTime; set => systemTime = value; }

        public string SysTime
        {
            get
            {
                return systemTime.ToString("MMM dd HH:mm:ss.fff");
            }
        }

        public string ChipLapTime
        {
            get => raceType == Constants.Timing.EVENT_TYPE_TIME ? splitTime : chipTime;
        }
        public string ChipTime { get => chipTime; set => chipTime = value; }
        public string Gender { get => gender; set => gender = value; }
        public string AgeGroupName { get => PrettyAgeGroupName(); set => ageGroupName = value; }
        public int Status { get => status; set => status = value; }
        public string LapTime { get => splitTime; set => splitTime = value; }
        public long ChipSeconds { get => chipSeconds; set => chipSeconds = value; }
        public int ChipMilliseconds { get => chipMilliseconds; set => chipMilliseconds = value; }
        public long Seconds { get => seconds; set => seconds = value; }
        public int Milliseconds { get => milliseconds; set => milliseconds = value; }
        public int Uploaded { get => uploaded; set => uploaded = (value == Constants.Timing.TIMERESULT_UPLOADED_FALSE ? Constants.Timing.TIMERESULT_UPLOADED_FALSE : Constants.Timing.TIMERESULT_UPLOADED_TRUE); }
        public string Birthday { get => birthday; set => birthday = value; }
        public string Chip { get => chip; set => chip = value; }
        public bool Anonymous { get => anonymous; set => anonymous = value; }
        public string AgeGenderString
        {
            get => theEvent != null ? string.Format("{0} {1}", this.Age(theEvent.Date), this.PrettyGender) : string.Format("? {0}", this.PrettyGender);
        }
        public bool Finish { get => segmentId == Constants.Timing.SEGMENT_FINISH; }

        public string PrettyAgeGroupName()
        {
            string[] agSplit = ageGroupName.Split('-');
            int topAge = -1;
            if (agSplit.Length > 1 && agSplit[0] == "0")
            {
                int.TryParse(agSplit[1], out topAge);
                if (topAge > 0)
                {
                    return string.Format("Under {0}", topAge + 1);
                }
            }
            else if (topAge >= 99)
            {
                return string.Format("Over {0}", agSplit[0]);
            }
            return ageGroupName;
        }

        public static string BibToIdentifier(string iBib)
        {
            return "Bib:" + iBib;
        }
        public static string ChipToIdentifier(string iChip)
        {
            return "Chip:" + iChip;
        }

        public int Age(string eventDate)
        {
            if (birthday.Length < 1)
            {
                return -1;
            }
            DateTime eventDateTime = Convert.ToDateTime(eventDate);
            DateTime myDateTime = Convert.ToDateTime(birthday);
            int numYears = eventDateTime.Year - myDateTime.Year;
            if (eventDateTime.Month < myDateTime.Month || (eventDateTime.Month == myDateTime.Month && eventDateTime.Day < myDateTime.Day))
            {
                numYears--;
            }
            return numYears;
        }

        public bool IsUploaded()
        {
            return uploaded != Constants.Timing.TIMERESULT_UPLOADED_FALSE;
        }

        public bool IsDNF()
        {
            return status == Constants.Timing.TIMERESULT_STATUS_DNF;
        }

        public static int CompareByGunTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            Match oneMatch = timeRegex.Match(one.Time);
            Match twoMatch = timeRegex.Match(two.Time);
            if (oneMatch == null || twoMatch == null) return 1;
            long oneTime = (Convert.ToInt64(oneMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(oneMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(oneMatch.Groups[3].Value);
            long twoTime = (Convert.ToInt64(twoMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(twoMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(twoMatch.Groups[3].Value);
            return oneTime.CompareTo(twoTime);
        }

        public static int CompareByNetTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            Match oneMatch = timeRegex.Match(one.chipTime);
            Match twoMatch = timeRegex.Match(two.chipTime);
            if (oneMatch == null || twoMatch == null) return 1;
            long oneTime = (Convert.ToInt64(oneMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(oneMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(oneMatch.Groups[3].Value);
            long twoTime = (Convert.ToInt64(twoMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(twoMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(twoMatch.Groups[3].Value);
            return oneTime.CompareTo(twoTime);
        }

        public static int CompareByAgeGroup(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                if (one.AgeGroupId == two.AgeGroupId)
                {
                    if (one.Gender.Equals(two.Gender))
                    {
                        if (one.Occurrence.Equals(two.Occurrence))
                        {
                            return one.Place.CompareTo(two.Place);
                        }
                        return one.Occurrence.CompareTo(two.Occurrence);
                    }
                    return one.Gender.CompareTo(two.Gender);
                }
                return one.AgeGroupId.CompareTo(two.AgeGroupId);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareByGender(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                if (one.Gender.Equals(two.Gender))
                {
                    return one.systemTime.CompareTo(two.systemTime);
                }
                return one.Gender.CompareTo(two.Gender);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareBySystemTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            return one.systemTime.CompareTo(two.systemTime);
        }

        public static int CompareByBib(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.Bib == two.Bib)
            {
                return one.systemTime.CompareTo(two.systemTime);
            }
            int bibOne, bibTwo;
            if (int.TryParse(one.Bib, out bibOne) && int.TryParse(two.Bib, out bibTwo))
            {
                return bibOne.CompareTo(bibTwo);
            }
            return one.Bib.CompareTo(two.Bib);
        }

        public static int CompareByDistance(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                return one.systemTime.CompareTo(two.systemTime);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public int CompareChip(TimeResult other)
        {
            if (other == null) return 1;
            if (this.chipSeconds == other.chipSeconds)
            {
                return this.chipMilliseconds.CompareTo(other.chipMilliseconds);
            }
            return this.chipSeconds.CompareTo(other.chipSeconds);
        }

        public static int CompareByDistanceChip(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                if (one.chipSeconds == two.chipSeconds)
                {
                    return one.chipMilliseconds.CompareTo(two.chipMilliseconds);
                }
                return one.chipSeconds.CompareTo(two.chipSeconds);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareByDistancePlace(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                if (one.Occurrence != two.Occurrence)
                {
                    return two.Occurrence.CompareTo(one.Occurrence);
                }
                if (one.status == 3 && two.status != 3)
                {
                    return -1;
                }
                if (one.status != 3 && two.status == 3)
                {
                    return 1;
                }
                if (one.Place == two.Place)
                {
                    return one.SystemTime.CompareTo(two.SystemTime);
                }
                return one.Place.CompareTo(two.Place);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareByDistanceGenderPlace(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                return one.GenderPlace.CompareTo(two.GenderPlace);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareByDistanceAgeGroupPlace(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DistanceName.Equals(two.DistanceName))
            {
                return one.AgePlace.CompareTo(two.AgePlace);
            }
            return one.DistanceName.CompareTo(two.DistanceName);
        }

        public static int CompareByOccurrence(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.Occurrence.Equals(two.Occurrence))
            {
                Match oneMatch = timeRegex.Match(one.Time);
                Match twoMatch = timeRegex.Match(two.Time);
                if (oneMatch == null || twoMatch == null) return 1;
                long oneTime = (Convert.ToInt64(oneMatch.Groups[1].Value) * 3600)
                    + (Convert.ToInt64(oneMatch.Groups[2].Value) * 60)
                    + Convert.ToInt64(oneMatch.Groups[3].Value);
                long twoTime = (Convert.ToInt64(twoMatch.Groups[1].Value) * 3600)
                    + (Convert.ToInt64(twoMatch.Groups[2].Value) * 60)
                    + Convert.ToInt64(twoMatch.Groups[3].Value);
                return oneTime.CompareTo(twoTime);
            }
            return one.Occurrence.CompareTo(two.Occurrence);
        }

        public static bool IsNotKnown(TimeResult one)
        {
            return one.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON;
        }

        public static bool StartTimes(TimeResult one)
        {
            return one.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON || one.SegmentId == Constants.Timing.SEGMENT_START;
        }

        public static bool IsNotStart(TimeResult one)
        {
            return one.SegmentId != Constants.Timing.SEGMENT_START;
        }

        public static bool IsNotFinish(TimeResult one)
        {
            return one.SegmentId != Constants.Timing.SEGMENT_FINISH;
        }

        public bool IsNotMatch(string value)
        {
            return !this.Bib.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase) &&
                this.ParticipantName.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1;
        }
    }
}
