using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class EventSpecific
    {
        private int identifier, eventIdentifier, divisionIdentifier, bib, chip, checkedIn = 0, shirtPurchase = 0, earlystart = 0;
        private String shirtSize, comments, divisionName, secondShirt, owes, hat, other;

        public EventSpecific(
            int eid,
            int did,
            string divName,
            string bib,
            string chip,
            int ci,
            string size,
            string comments,
            string secondshirt,
            string owes,
            string hat,
            string other,
            int earlystart
            )
        {
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = Int32.TryParse(bib, out int tempBib) ? tempBib : -1;
            this.chip = Int32.TryParse(chip, out int tempChip) ? tempChip : -1;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.shirtSize = size ?? "";
            this.comments = comments ?? "";
            this.secondShirt = secondshirt ?? "";
            this.owes = owes ?? "";
            this.hat = hat ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
        }

        public EventSpecific(
            int id,
            int eid,
            int did,
            string divName,
            int bib,
            int chip,
            int ci,
            string size,
            string comments,
            string secondshirt,
            string owes,
            string hat,
            string other,
            int earlystart
            )
        {
            this.identifier = id;
            this.eventIdentifier = eid;
            this.divisionIdentifier = did;
            this.divisionName = divName ?? "";
            this.bib = bib;
            this.chip = chip;
            this.checkedIn = ci != 0 ? 1 : 0;
            this.shirtSize = size ?? "";
            this.comments = comments ?? "";
            this.secondShirt = secondshirt ?? "";
            this.owes = owes ?? "";
            this.hat = hat ?? "";
            this.other = other ?? "";
            this.earlystart = earlystart != 0 ? 1 : 0;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; }
        public int DivisionIdentifier { get => divisionIdentifier; }
        public int Bib { get => bib; }
        public int Chip { get => chip; }
        public int CheckedIn { get => checkedIn; }
        public string ShirtSize { get => shirtSize; }
        public string Comments { get => comments; }
        public string DivisionName { get => divisionName; }
        public string SecondShirt { get => secondShirt; }
        public string Owes { get => owes; }
        public string Hat { get => hat; }
        public string Other { get => other; }
        public int EarlyStart { get => earlystart; }
    }
}
