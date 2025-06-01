using Chronokeep.Timing;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Twilio.Rest.Api.V2010.Account;

namespace Chronokeep.Objects
{
    public class TimeResult: IEquatable<TimeResult>
    {
        private int eventId, eventspecificId, locationId, segmentId,
            occurrence, readId, place, agePlace, genderPlace,
            ageGroupId, chipMilliseconds, status, uploaded, type, milliseconds,
            divisionPlace;
        private long chipSeconds, seconds;
        private string time, locationName, segmentName, firstName, lastName, bib,
            distanceName = "", unknownId, chipTime, gender, ageGroupName, splitTime = "", birthday,
            linked_distance_name = "", chip = "", participantId = "", division = "";
        private bool anonymous;
        DateTime systemTime;
        Event theEvent;

        public static readonly Regex timeRegex = new Regex(@"(\d+):(\d{2}):(\d{2})\.(\d{3})");

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
            bool anonymous,
            string participantId,
            Dictionary<int, TimingLocation> locations,
            Dictionary<int, Segment> segments,
            Dictionary<string, Distance> distances,
            Event theEvent,
            string division,
            int divisionPlace
            )
        {
            this.eventId = eventId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.time = time ?? "";
            this.occurrence = occurrence;
            locationName = locations != null ? locations.ContainsKey(locationId) ?
                locations[this.locationId].Name : "Unknown" : "Unknown";
            if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
            {
                segmentName = "Finish ";
            }
            else if (Constants.Timing.SEGMENT_START == this.segmentId)
            {
                segmentName = "Start ";
            }
            else if (segments != null && segments.ContainsKey(this.segmentId))
            {
                segmentName = segments[this.segmentId].Name + " ";
            }
            else
            {
                segmentName = "";
            }
            if (theEvent != null && theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME && Constants.Timing.SEGMENT_FINISH == SegmentId)
            {
                if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
                {
                    if (linked_distance_name.Length > 0
                        && distances.ContainsKey(linked_distance_name)
                        && distances[linked_distance_name].DistanceValue > 0)
                    {
                        segmentName = string.Format("{1:0.##} {2} - Lap {0}",
                            occurrence,
                            distances[linked_distance_name].DistanceValue * occurrence,
                            Constants.Distances.DistanceString(distances[linked_distance_name].DistanceUnit)
                            );
                    }
                    else if (distance.Length > 0
                        && distances.ContainsKey(distance)
                        && distances[distance].DistanceValue > 0)
                    {
                        segmentName = string.Format("{1:0.##} {2} - Lap {0}",
                            occurrence,
                            distances[distance].DistanceValue * occurrence,
                            Constants.Distances.DistanceString(distances[distance].DistanceUnit)
                            );
                    }
                    else
                    {
                        segmentName = string.Format("Lap {0}", occurrence);
                    }
                }
                else if (Constants.Timing.SEGMENT_START != this.segmentId)
                {
                    if (linked_distance_name.Length > 0
                        && segments.ContainsKey(this.segmentId)
                        && segments[this.segmentId].CumulativeDistance > 0)
                    {
                        segmentName = string.Format("{2:0.##} {3} - {0}{1}",
                            segmentName,
                            occurrence,
                            segments[this.segmentId].CumulativeDistance * occurrence,
                            Constants.Distances.DistanceString(segments[this.segmentId].DistanceUnit)
                            );
                    }
                    else
                    {
                        segmentName = string.Format("{0} {1}", segmentName, occurrence);
                    }
                }
            }
            else if (theEvent != null && Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType)
            {
                if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
                {
                    segmentName = string.Format("Loop {0}", (this.occurrence / 2) + 1);
                }
                else if (Constants.Timing.SEGMENT_START == this.segmentId)
                {
                    segmentName = string.Format("Start {0}", (this.occurrence / 2) + 1);
                }
            }
            segmentName = segmentName.Trim();
            firstName = first ?? "";
            lastName = last ?? "";
            distanceName = distance ?? "";
            this.bib = bib ?? "";
            this.unknownId = unknownId ?? "";
            this.readId = readId;
            systemTime = Constants.Timing.RFIDEpochToDate(systemTimeSec).AddMilliseconds(systemTimeMill);
            this.chipTime = chipTime ?? "";
            this.place = place;
            this.agePlace = agePlace;
            this.genderPlace = genderPlace;
            this.gender = gender ?? "";
            this.ageGroupId = ageGroupId;
            this.ageGroupName = ageGroupName ?? "";
            Match chipTimeMatch = timeRegex.Match(chipTime);
            chipSeconds = 0;
            chipMilliseconds = 0;
            if (chipTimeMatch.Success)
            {
                chipSeconds = Convert.ToInt64(chipTimeMatch.Groups[1].Value) * 3600
                   + Convert.ToInt64(chipTimeMatch.Groups[2].Value) * 60
                   + Convert.ToInt64(chipTimeMatch.Groups[3].Value);
                chipMilliseconds = Convert.ToInt32(chipTimeMatch.Groups[4].Value);
            }
            Match timeMatch = timeRegex.Match(time);
            seconds = 0;
            milliseconds = 0;
            if (timeMatch.Success)
            {
                seconds = Convert.ToInt64(timeMatch.Groups[1].Value) * 3600
                   + Convert.ToInt64(timeMatch.Groups[2].Value) * 60
                   + Convert.ToInt64(timeMatch.Groups[3].Value);
                milliseconds = Convert.ToInt32(timeMatch.Groups[4].Value);
            }
            this.status = status;
            splitTime = split ?? "";
            this.uploaded = uploaded;
            this.birthday = birthday ?? "";
            this.type = type;
            this.linked_distance_name = linked_distance_name ?? "";
            this.chip = chip ?? "";
            this.anonymous = anonymous;
            this.participantId = participantId ?? First+Last;
            this.theEvent = theEvent;
            this.division = division ?? "";
            this.divisionPlace = divisionPlace;
        }

        // Used by routines to add new results to the database.
        public TimeResult(
            int eventId,
            int readId,
            int eventspecificId,
            int locationId,
            int segmentId,
            int occurrence,
            long seconds,
            int milliseconds,
            string unknownId,
            long chipSeconds,
            int chipMilliseconds,
            DateTime systemTime,
            string bib,
            int status,
            string division
            )
        {
            this.eventId = eventId;
            this.readId = readId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.occurrence = occurrence;
            this.time = Constants.Timing.TIMERESULT_STATUS_DNF == status ? "DNF" : Constants.Timing.TIMERESULT_STATUS_DNS == status ? "DNS" : Constants.Timing.ToTime(seconds, milliseconds);
            this.unknownId = unknownId ?? "";
            this.chipTime = Constants.Timing.TIMERESULT_STATUS_DNF == status ? "DNF" : Constants.Timing.TIMERESULT_STATUS_DNS == status ? "DNS" : Constants.Timing.ToTime(chipSeconds, chipMilliseconds);
            this.systemTime = systemTime;
            this.bib = bib ?? "";
            place = Constants.Timing.TIMERESULT_DUMMYPLACE;
            agePlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            genderPlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            divisionPlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            this.status = status;
            splitTime = "";
            this.seconds = seconds;
            this.milliseconds = milliseconds;
            this.chipSeconds = chipSeconds;
            this.chipMilliseconds = chipMilliseconds;
            this.division = division ?? "";
        }

        public int EventSpecificId { get => eventspecificId; set => eventspecificId = value; }
        public int LocationId { get => locationId; set => locationId = value; }
        public int EventIdentifier { get => eventId; set => eventId = value; }
        public int SegmentId { get => segmentId; set => segmentId = value; }
        public int Occurrence { get => occurrence; set => occurrence = value; }
        public string Time { get => time; set => time = value ?? ""; }
        public string LocationName { get => locationName; set => locationName = value ?? ""; }
        public string SegmentName { get => segmentName; set => segmentName = value ?? ""; }
        public string First { get => firstName; set => firstName = value ?? ""; }
        public string Last { get => lastName; set => lastName = value ?? ""; }
        public string ParticipantName { get => string.Format("{0} {1}", firstName, lastName).Trim(); }
        public string PrettyParticipantName { get => anonymous ? string.Format("Bib {0}", bib) : string.Format("{0} {1}", firstName, lastName).Trim(); }
        public string DistanceName { get => linked_distance_name == "" ? distanceName : linked_distance_name; }
        internal string LinkedDistanceName { get => linked_distance_name; set => linked_distance_name = value; }
        public string RealDistanceName { get => distanceName; internal set => distanceName = value; }
        public string Bib { get => bib; set => bib = value ?? ""; }
        public int AgeGroupId { get => ageGroupId; set => ageGroupId = value; }
        public string UnknownId { get => unknownId; set => unknownId = value ?? ""; }
        public int ReadId { get => readId; set => readId = value; }
        public int Place { get => place; set => place = value; }
        public string PlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? place < 1 ? "" : place.ToString() : ""; }
        public string PrettyPlaceStr
        {
            get => type == Constants.Timing.DISTANCE_TYPE_EARLY && place > 0 ? string.Format("{0}e", place) :
                type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL && place > 0 ? string.Format("{0}u", place) :
                Finish && place > 0 ? place.ToString() : "";
        }
        public int AgePlace { get => agePlace; set => agePlace = value; }
        public string AgePlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? agePlace < 1 ? "" : agePlace.ToString() : ""; }
        public int GenderPlace { get => genderPlace; set => genderPlace = value; }
        public string GenderPlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? genderPlace < 1 ? "" : genderPlace.ToString() : ""; }
        public string Division { get => division; set => division = value; }
        public int DivisionPlace { get => divisionPlace; set => divisionPlace = value; }
        public string DivisionPlaceStr { get => theEvent != null && theEvent.DisplayPlacements ? divisionPlace < 1 ? "" : divisionPlace.ToString() : ""; }
        public int Type { get => type; set => type = value; }
        public string Identifier { get => unknownId; }
        public string PrettyType
        {
            get => PrettyTypeStr();
        }
        public string PrettyGender
        {
            get => gender == null ? "" : gender == "Man" ? "M" : gender == "Woman" ? "W" : gender == "Non-Binary" ? "X" : gender == "Not Specified" || gender.Equals("ns", StringComparison.OrdinalIgnoreCase) ? "" : gender.Length <= 2 ? gender : gender[..2];
        }
        public int DivisionColWidth { get => theEvent.DivisionsEnabled ? 80 : 0; }
        public int DivisionPlaceColWidth { get => theEvent.DivisionsEnabled ? 40 : 0; }
        public int DivisionMargin { get => theEvent.DivisionsEnabled ? 4 : 0; }

        public string PrettyTypeStr()
        {
            string output = type == Constants.Timing.DISTANCE_TYPE_EARLY ? "E"
                : type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL ? "U"
                : type == Constants.Timing.DISTANCE_TYPE_LATE ? "L"
                : type == Constants.Timing.DISTANCE_TYPE_VIRTUAL ? "V"
                : "";
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
            get => theEvent != null && theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME ? splitTime : chipTime;
        }
        public string ChipTime { get => chipTime; set => chipTime = value ?? ""; }
        public string ChipTimeNoMilliseconds { get => chipTime.Split('.').Length > 0 ? chipTime.Split('.')[0] : chipTime; }
        public string Gender { get => gender; set => gender = value ?? ""; }
        public string AgeGroupName { get => PrettyAgeGroupName(); set => ageGroupName = value ?? ""; }
        public int Status { get => status; set => status = value; }
        public string LapTime { get => splitTime; set => splitTime = value ?? ""; }
        public long ChipSeconds { get => chipSeconds; set => chipSeconds = value; }
        public int ChipMilliseconds { get => chipMilliseconds; set => chipMilliseconds = value; }
        public long Seconds { get => seconds; set => seconds = value; }
        public int Milliseconds { get => milliseconds; set => milliseconds = value; }
        public int Uploaded { get => uploaded; set => uploaded = value == Constants.Timing.TIMERESULT_UPLOADED_FALSE ? Constants.Timing.TIMERESULT_UPLOADED_FALSE : Constants.Timing.TIMERESULT_UPLOADED_TRUE; }
        public string Birthday { get => birthday; set => birthday = value ?? ""; }
        public string Chip { get => chip; set => chip = value ?? ""; }
        public bool Anonymous { get => anonymous; set => anonymous = value; }
        public string AgeGenderString
        {
            get => theEvent != null ? string.Format("{0} {1}", Age(theEvent.Date), PrettyGender) : string.Format("? {0}", PrettyGender);
        }
        public bool Finish { get => segmentId == Constants.Timing.SEGMENT_FINISH; }
        public string ParticipantId { get => participantId; }

        public string PrettyAgeGroupName()
        {
            string[] agSplit = ageGroupName.Split('-');
            int topAge = -1;
            if (agSplit.Length > 1 && agSplit[0] == "0")
            {
                if (int.TryParse(agSplit[1], out topAge) && topAge > 0)
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

        public void SetParticipant(Participant p)
        {
            participantId = p.Identifier.ToString();
            anonymous = p.Anonymous;
            distanceName = p.EventSpecific.DistanceName;
            gender = p.Gender;
            firstName = p.FirstName;
            lastName = p.LastName;
            ageGroupId = p.EventSpecific.AgeGroupId;
            ageGroupName = p.EventSpecific.AgeGroupName;
            birthday = p.Birthdate;
        }

        public void SetBlankParticipant()
        {
            participantId = "";
            anonymous = false;
            distanceName = "";
            gender = "";
            firstName = "";
            lastName = "";
            ageGroupId = -1;
            ageGroupName = "";
            birthday = "";
            linked_distance_name = "";
        }

        public void SetLinkedDistanceName(string linkedDistanceName)
        {
            linked_distance_name = linkedDistanceName ?? "";
        }

        public void SetResultType(int type)
        {
            this.type = type;
        }

        public void SetChipRead(
            string chip,
            string bib,
            long systemTimeSec,
            int systemTimeMill
            )
        {
            this.chip = chip;
            this.bib = bib;
            systemTime = Constants.Timing.RFIDEpochToDate(systemTimeSec).AddMilliseconds(systemTimeMill);
        }

        public void SetFinalValues(
            Dictionary<int, TimingLocation> locations,
            Dictionary<int, Segment> segments,
            Dictionary<string, Distance> distances,
            Event theEvent
            )
        {
            locationName = locations != null ? locations.ContainsKey(locationId) ?
                locations[this.locationId].Name : "Unknown" : "Unknown";
            if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
            {
                segmentName = "Finish ";
            }
            else if (Constants.Timing.SEGMENT_START == this.segmentId)
            {
                segmentName = "Start ";
            }
            else if (segments != null && segments.ContainsKey(this.segmentId))
            {
                segmentName = segments[this.segmentId].Name + " ";
            }
            else
            {
                segmentName = "";
            }
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME && Constants.Timing.SEGMENT_FINISH == SegmentId)
            {
                if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
                {
                    if (linked_distance_name.Length > 0
                        && distances.ContainsKey(linked_distance_name)
                        && distances[linked_distance_name].DistanceValue > 0)
                    {
                        segmentName = string.Format("{1:0.##} {2} - Lap {0}",
                            occurrence,
                            distances[linked_distance_name].DistanceValue * occurrence,
                            Constants.Distances.DistanceString(distances[linked_distance_name].DistanceUnit)
                            );
                    }
                    else if (distanceName.Length > 0
                        && distances.ContainsKey(distanceName)
                        && distances[distanceName].DistanceValue > 0)
                    {
                        segmentName = string.Format("{1:0.##} {2} - Lap {0}",
                            occurrence,
                            distances[distanceName].DistanceValue * occurrence,
                            Constants.Distances.DistanceString(distances[distanceName].DistanceUnit)
                            );
                    }
                    else
                    {
                        segmentName = string.Format("Lap {0}", occurrence);
                    }
                }
                else if (Constants.Timing.SEGMENT_START != this.segmentId)
                {
                    if (linked_distance_name.Length > 0
                        && segments.ContainsKey(this.segmentId)
                        && segments[this.segmentId].CumulativeDistance > 0)
                    {
                        segmentName = string.Format("{2:0.##} {3} - {0}{1}",
                            segmentName,
                            occurrence,
                            segments[this.segmentId].CumulativeDistance * occurrence,
                            Constants.Distances.DistanceString(segments[this.segmentId].DistanceUnit)
                            );
                    }
                    else
                    {
                        segmentName = string.Format("{0} {1}", segmentName, occurrence);
                    }
                }
            }
            segmentName = segmentName.Trim();
            this.theEvent = theEvent;
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
            if (eventDateTime.Month < myDateTime.Month || eventDateTime.Month == myDateTime.Month && eventDateTime.Day < myDateTime.Day)
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
            if (one.Seconds == two.Seconds)
            {
                return one.Milliseconds.CompareTo(two.Milliseconds);
            }
            return one.Seconds.CompareTo(two.Seconds);
        }

        public static int CompareByNetTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.ChipSeconds == two.ChipSeconds)
            {
                return one.ChipMilliseconds.CompareTo(two.ChipMilliseconds);
            }
            return one.ChipSeconds.CompareTo(two.ChipSeconds);
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
            if (chipSeconds == other.chipSeconds)
            {
                return chipMilliseconds.CompareTo(other.chipMilliseconds);
            }
            return chipSeconds.CompareTo(other.chipSeconds);
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
                if (one.Status == Constants.Timing.TIMERESULT_STATUS_DNF && two.Status != Constants.Timing.TIMERESULT_STATUS_DNF)
                {
                    return 1;
                }
                if (one.Status != Constants.Timing.TIMERESULT_STATUS_DNF && two.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
                {
                    return -1;
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
                if (one.Seconds == two.Seconds)
                {
                    return one.Milliseconds.CompareTo(two.Milliseconds);
                }
                return one.seconds.CompareTo(two.Seconds);
            }
            return one.Occurrence.CompareTo(two.Occurrence);
        }

        public static int CompareForBackyardElapsed(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.Occurrence.Equals(two.Occurrence))
            {
                if (one.Seconds == two.Seconds)
                {
                    return one.Milliseconds.CompareTo(two.Milliseconds);
                }
                return one.Seconds.CompareTo(two.Seconds);
            }
            return two.Occurrence.CompareTo(one.Occurrence);
        }

        public static int CompareForBackyardCumulative(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.Occurrence.Equals(two.Occurrence))
            {
                if (one.ChipSeconds == two.ChipSeconds)
                {
                    return one.ChipMilliseconds.CompareTo(two.ChipMilliseconds);
                }
                return one.ChipSeconds.CompareTo(two.ChipSeconds);
            }
            return two.Occurrence.CompareTo(one.Occurrence);
        }

        public static bool IsNotKnown(TimeResult one)
        {
            return one.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON;
        }

        public static bool IsKnown(TimeResult one)
        {
            return one.EventSpecificId != Constants.Timing.TIMERESULT_DUMMYPERSON;
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

        public static bool IsNotFinishOrKnown(TimeResult one)
        {
            return one.SegmentId == Constants.Timing.SEGMENT_START
                || one.EventSpecificId != Constants.Timing.TIMERESULT_DUMMYPERSON
                || one.locationId != Constants.Timing.LOCATION_FINISH;
        }

        public static bool IsNotStartOrKnown(TimeResult one)
        {
            return one.SegmentId != Constants.Timing.SEGMENT_START
                || one.EventSpecificId != Constants.Timing.TIMERESULT_DUMMYPERSON;
        }

        public bool IsNotMatch(string value)
        {
            return !Bib.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase) &&
                ParticipantName.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1;
        }

        public bool SMSCanBeSent(TimingDictionary dictionary)
        {
            if (Constants.Globals.TwilioCredentials.AccountSID.Length < 1 || Constants.Globals.TwilioCredentials.AuthToken.Length < 1)
            {
                return false;
            }
            Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ? dictionary.participantBibDictionary[bib] : null;
            if (part == null || part.EventSpecific.SMSEnabled == false)
            {
                return false;
            }
            string validPhone = Constants.Globals.GetValidPhone(part.Mobile);
            if (validPhone.Length == 0)
            {
                validPhone = Constants.Globals.GetValidPhone(part.Phone);
            }
            // Invalid length. +15555551234 is a valid phone
            if (validPhone.Length != 12 || Constants.Globals.TwilioCredentials.PhoneNumber.Length != 12)
            {
                return false;
            }
            return true;
        }

        public static SMSState SendSMSAlert(string phone, string sms)
        {
            if (Constants.Globals.TwilioCredentials.AccountSID.Length < 1 || Constants.Globals.TwilioCredentials.AuthToken.Length < 1)
            {
                return SMSState.Invalid;
            }
            // Invalid length. +15555551234 is a valid phone
            if (phone.Length != 12 || Constants.Globals.TwilioCredentials.PhoneNumber.Length != 12)
            {
                return SMSState.Invalid;
            }
            // Verify phone number isn't in our list of banned phone numbers (i.e. they've told us to not send texts)
            // return true if it is in the banned list, otherwise try to send it, and return true if we were able to send it
            if (Constants.Globals.BannedPhones.Contains(phone))
            {
                Log.D("Objects.TimeResult", "Phone number is banned.");
                return SMSState.Invalid;
            }
            try
            {
                Log.D("Objects.TimeResult", "sms: '" + sms + "' phone: " + phone);
                var messageOptions = new CreateMessageOptions(
                    new Twilio.Types.PhoneNumber(phone)
                    );
                messageOptions.From = new Twilio.Types.PhoneNumber(Constants.Globals.TwilioCredentials.PhoneNumber);
                messageOptions.Body = sms;
                var message = MessageResource.Create(messageOptions);
                if (message.ErrorMessage != null)
                {
                    return SMSState.AddToBanned;
                }
            }
            catch
            {
                return SMSState.NetworkError;
            }
            return SMSState.Success;
        }

        public bool Equals(TimeResult other)
        {
            return this.EventSpecificId == other.EventSpecificId
                && this.LocationId == other.LocationId
                && this.SegmentId == other.SegmentId
                && this.Occurrence == other.Occurrence;
        }

        public void UpdateEvent(Event newEvent)
        {
            theEvent = newEvent;
        }

        public enum SMSState
        {
            None = 0,
            Success,
            AddToBanned,
            Invalid,
            NetworkError
        }
    }
}
