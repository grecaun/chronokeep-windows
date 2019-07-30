using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, divisionIdentifier, bib,
            checkedIn = 0, earlystart = 0, nextyear = 0, chip = -1,
            ageGroup = Constants.Timing.TIMERESULT_DUMMYAGEGROUP,
            status = Constants.Timing.EVENTSPECIFIC_NOSHOW;
        private String comments, divisionName, owes, other;
        private List<Apparel> apparel;

        public EventSpecific() { }

        // Constructor to be used when adding to db
        public EventSpecific(
            int eid,
            int did,
            string divName,
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
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = Int32.TryParse(bib, out int tempBib) ? tempBib : -1;
            this.checkedIn = ci == 0 ? 0 : 1;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart == 0 ? 0 : 1;
            this.nextyear = nextyear;
            this.apparel = new List<Apparel>();
        }

        // Constructor the database uses
        public EventSpecific(
            int id,
            int eid,
            int did,
            string divName,
            int bib,
            int ci,
            string comments,
            string owes,
            string other,
            int earlystart,
            int nextyear,
            int ageGroup,
            int status
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = bib;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
            this.nextyear = nextyear;
            this.apparel = new List<Apparel>();
            this.ageGroup = ageGroup;
            this.status = status;
        }

        public EventSpecific(
            int id,
            int eid,
            int did,
            string divName,
            int bib,
            int chip,
            int ci,
            string comments,
            string owes,
            string other,
            int earlystart,
            int nextyear,
            int ageGroup
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = bib;
            this.chip = chip;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.comments = comments ?? "";
            this.owes = owes ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
            this.nextyear = nextyear;
            this.apparel = new List<Apparel>();
            this.ageGroup = ageGroup;
        }

        public EventSpecific(EventSpecific that)
        {
            this.identifier = that.identifier;
            this.eventIdentifier = that.eventIdentifier;
            this.divisionIdentifier = that.divisionIdentifier;
            this.divisionName = that.divisionName;
            this.bib = that.bib;
            this.checkedIn = that.checkedIn;
            this.comments = that.comments;
            this.owes = that.owes;
            this.other = that.other;
            this.earlystart = that.earlystart;
            this.nextyear = that.nextyear;
            this.apparel = that.apparel;
            this.status = that.status;
        }

        internal void Trim()
        {
            divisionName = divisionName.Trim();
            owes = owes.Trim();
            other = other.Trim();
            comments = comments.Trim();
        }

        internal EventSpecific Blank()
        {
            return new EventSpecific(-1, -1, -1, "None", -1, 0, "", "", "", 0, 0, Constants.Timing.TIMERESULT_DUMMYAGEGROUP, 0);
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public int Bib { get => bib; set => bib = value; }
        public int Chip { get => chip; set => chip = value; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public string Comments { get => comments; set => comments = value; }
        public string DivisionName { get => divisionName; set => divisionName = value; }
        public string Owes { get => owes; set => owes = value; }
        public string Other { get => other; set => other = value; }
        public int EarlyStart { get => earlystart; set => earlystart = value; }
        public int NextYear { get => nextyear; set => nextyear = value; }
        public int NumApparel { get => apparel.Count; }
        public int AgeGroup { get => ageGroup; set => ageGroup = value; }
        public int Status { get => status; set => status = value; }

        public void SetApparel(List<Apparel> incoming)
        {
            this.apparel = incoming;
        }
    }
}
