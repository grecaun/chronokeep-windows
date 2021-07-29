using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, distanceIdentifier, bib,
            checkedIn = 0, earlystart = 0, nextyear = 0, chip = -1,
            status = Constants.Timing.EVENTSPECIFIC_NOSHOW, ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
        private string comments, distanceName, owes, other, ageGroupName = "0-110";

        public EventSpecific() { }

        // Constructor to be used when adding to db
        public EventSpecific(
            int eid,
            int did,
            string distanceName,
            string bib,
            int ci,
            string comments,
            string owes,
            string other,
            int earlystart,
            int nextyear
            )
        {
            this.eventIdentifier = eid;
            this.distanceIdentifier = did;
            this.distanceName = distanceName ?? "";
            this.bib = Int32.TryParse(bib, out int tempBib) ? tempBib : -1;
            this.checkedIn = ci == 0 ? 0 : 1;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart == 0 ? 0 : 1;
            this.nextyear = nextyear;
        }

        // Constructor the database uses
        public EventSpecific(
            int id,
            int eid,
            int did,
            string distanceName,
            int bib,
            int ci,
            string comments,
            string owes,
            string other,
            int earlystart,
            int nextyear,
            int status,
            string ageGroupName,
            int ageGroupId
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.distanceIdentifier = did;
            this.distanceName = distanceName ?? "";
            this.bib = bib;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
            this.nextyear = nextyear;
            this.status = status;
            this.ageGroupName = ageGroupName;
            this.ageGroupId = ageGroupId;
        }

        internal void Trim()
        {
            distanceName = distanceName.Trim();
            owes = owes.Trim();
            other = other.Trim();
            comments = comments.Trim();
        }

        internal EventSpecific Blank()
        {
            return new EventSpecific(-1, -1, -1, "None", -1, 0, "", "", "", 0, 0, 0, "0-110", Constants.Timing.TIMERESULT_DUMMYAGEGROUP);
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DistanceIdentifier { get => distanceIdentifier; set => distanceIdentifier = value; }
        public int Bib { get => bib; set => bib = value; }
        public int Chip { get => chip; set => chip = value; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public string Comments { get => comments; set => comments = value; }
        public string DistanceName { get => distanceName; set => distanceName = value; }
        public string Owes { get => owes; set => owes = value; }
        public string Other { get => other; set => other = value; }
        public int EarlyStart { get => earlystart; set => earlystart = value; }
        public int NextYear { get => nextyear; set => nextyear = value; }
        public int Status { get => status; set => status = value; }
        public string StatusStr { get => Constants.Timing.EVENTSPECIFIC_STATUS_NAMES[status]; }
        public string AgeGroupName { get => ageGroupName; set => ageGroupName = value; }
        public int AgeGroupId { get => ageGroupId; set => ageGroupId = value; }
    }
}
