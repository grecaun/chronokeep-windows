namespace Chronokeep
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, distanceIdentifier,
            checkedIn = 0,
            status = Constants.Timing.EVENTSPECIFIC_UNKNOWN,
            ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
        private string comments, distanceName, owes, other, ageGroupName = "", bib, apparel, division = "";
        private bool anonymous, sms_enabled;

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
            bool anonymous,
            bool sms_enabled,
            string apparel,
            string division
            )
        {
            this.eventIdentifier = eid;
            this.distanceIdentifier = did;
            this.distanceName = distanceName ?? "";
            this.bib = bib ?? "";
            this.checkedIn = ci == 0 ? 0 : 1;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.anonymous = anonymous;
            this.sms_enabled = sms_enabled;
            this.apparel = apparel ?? "";
            this.division = division ?? "";
        }

        // Constructor the database uses
        public EventSpecific(
            int id,
            int eid,
            int did,
            string distanceName,
            string bib,
            int ci,
            string comments,
            string owes,
            string other,
            int status,
            string ageGroupName,
            int ageGroupId,
            bool anonymous,
            bool sms_enabled,
            string apparel,
            string division
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.distanceIdentifier = did;
            this.distanceName = distanceName ?? "";
            this.bib = bib ?? "";
            this.checkedIn = ci != 0 ? 1 : 0;
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.comments = comments ?? "";
            this.status = status;
            this.ageGroupName = ageGroupName ?? "";
            this.ageGroupId = ageGroupId;
            this.anonymous = anonymous;
            this.sms_enabled = sms_enabled;
            this.apparel = apparel ?? "";
            this.division = division ?? "";
        }

        internal void Trim()
        {
            distanceName = distanceName.Trim();
            bib = bib.Trim();
            owes = owes.Trim();
            other = other.Trim();
            comments = comments.Trim();
            ageGroupName = ageGroupName.Trim();
            apparel = apparel.Trim();
            division = division.Trim();
        }

        internal EventSpecific Blank()
        {
            return new EventSpecific(-1, -1, -1, "None", "", 0, "", "", "", 0, "", Constants.Timing.TIMERESULT_DUMMYAGEGROUP, false, false, "", "");
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DistanceIdentifier { get => distanceIdentifier; set => distanceIdentifier = value; }
        public string Bib { get => bib; set => bib = value ?? ""; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public string Comments { get => comments; set => comments = value ?? ""; }
        public string DistanceName { get => distanceName; set => distanceName = value ?? ""; }
        public string Owes { get => owes; set => owes = value ?? ""; }
        public string Other { get => other; set => other = value ?? ""; }
        public int Status { get => status; set => status = value; }
        public string StatusStr { get => Constants.Timing.EVENTSPECIFIC_STATUS_NAMES[status]; }
        public string AgeGroupName { get => ageGroupName; set => ageGroupName = value ?? ""; }
        public int AgeGroupId { get => ageGroupId; set => ageGroupId = value; }
        public bool Anonymous { get => anonymous; set => anonymous = value; }
        public bool SMSEnabled { get => sms_enabled; set => sms_enabled = value; }
        public string Apparel { get => apparel; set => apparel = value ?? ""; }
        public string Division { get => division; set => division = value ?? ""; }

        public void CopyFrom(EventSpecific other)
        {
            this.EventIdentifier = other.EventIdentifier;
            this.DistanceIdentifier = other.DistanceIdentifier;
            this.Bib = other.Bib;
            this.CheckedIn = other.CheckedIn;
            this.Comments = other.Comments;
            this.DistanceName = other.DistanceName;
            this.Owes = other.Owes;
            this.Other = other.Other;
            this.Status = other.Status;
            this.AgeGroupName = other.AgeGroupName;
            this.AgeGroupId = other.AgeGroupId;
            this.Anonymous = other.Anonymous;
            this.SMSEnabled = other.SMSEnabled;
            this.Apparel = other.Apparel;
            this.Division = other.Division;
        }
    }
}
