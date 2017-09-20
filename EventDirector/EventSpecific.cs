using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, divisionIdentifier, bib, chip, checkedIn = 0, shirtPurchase = 0;
        private String shirtSize, comments, divisionName;

        public EventSpecific(int id, int eid, int did, string divName, int bib, int chip, int ci, int sp, string size, string comments)
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName;
            this.bib = bib;
            this.chip = chip;
            this.checkedIn = ci;
            this.shirtPurchase = sp;
            this.shirtSize = size;
            this.comments = comments;
        }

        public EventSpecific(int id, int eid, int did, int bib, int chip, int ci, int sp, string size, string comments)
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.bib = bib;
            this.chip = chip;
            this.checkedIn = ci;
            this.shirtPurchase = sp;
            this.shirtSize = size;
            this.comments = comments;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public string DivisionName { get => divisionName; set => divisionName = value; }
        public int Bib { get => bib; set => bib = value; }
        public int Chip { get => chip; set => chip = value; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public int ShirtPurchase { get => shirtPurchase; set => shirtPurchase = value; }
        public string ShirtSize { get => shirtSize; set => shirtSize = value; }
        public string Comments { get => comments; set => comments = value; }
    }
}
