using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, divisionIdentifier, bib, checkedIn = 0, earlystart = 0, nextyear = 0;
        private String shirtSize, comments, divisionName, secondShirt, owes, hat, other, fleece;

        public EventSpecific() { }

        public EventSpecific(
            int eid,
            int did,
            string divName,
            string bib,
            int ci,
            string size,
            string comments,
            string secondshirt,
            string owes,
            string hat,
            string other,
            int earlystart,
            string fleece,
            int nextyear
            )
        {
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = Int32.TryParse(bib, out int tempBib) ? tempBib : -1;
            this.checkedIn = ci == 0 ? 0 : 1;
            this.shirtSize = size ?? "";
            this.comments = comments ?? "";
            this.secondShirt = secondshirt ?? "";
            this.owes = owes ?? "";
            this.hat = hat ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart == 0 ? 0 : 1;
            this.fleece = fleece;
            this.nextyear = nextyear;
        }

        public EventSpecific(
            int id,
            int eid,
            int did,
            string divName,
            int bib,
            int ci,
            string size,
            string comments,
            string secondshirt,
            string owes,
            string hat,
            string other,
            int earlystart,
            string fleece,
            int nextyear
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = bib;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.shirtSize = size ?? "";
            this.comments = comments ?? "";
            this.secondShirt = secondshirt ?? "";
            this.owes = owes ?? "";
            this.hat = hat ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
            this.fleece = fleece;
            this.nextyear = nextyear;
        }

        public EventSpecific(EventSpecific that)
        {
            this.identifier = that.identifier;
            this.eventIdentifier = that.eventIdentifier;
            this.divisionIdentifier = that.divisionIdentifier;
            this.divisionName = that.divisionName;
            this.bib = that.bib;
            this.checkedIn = that.checkedIn;
            this.shirtSize = that.shirtSize;
            this.comments = that.comments;
            this.secondShirt = that.secondShirt;
            this.owes = that.owes;
            this.hat = that.hat;
            this.other = that.other;
            this.earlystart = that.earlystart;
            this.fleece = that.fleece;
            this.nextyear = that.nextyear;
        }

        internal EventSpecific Blank()
        {
            return new EventSpecific(-1, -1, -1, "None", -1, 0, "", "", "", "", "", "", 0, "", 0);
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public int Bib { get => bib; set => bib = value; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public string ShirtSize { get => shirtSize; set => shirtSize = value; }
        public string Comments { get => comments; set => comments = value; }
        public string DivisionName { get => divisionName; set => divisionName = value; }
        public string SecondShirt { get => secondShirt; set => secondShirt = value; }
        public string Owes { get => owes; set => owes = value; }
        public string Hat { get => hat; set => hat = value; }
        public string Other { get => other; set => other = value; }
        public int EarlyStart { get => earlystart; set => earlystart = value; }
        public string Fleece { get => fleece; set => fleece = value; }
        public int NextYear { get => nextyear; set => nextyear = value; }
    }
}
