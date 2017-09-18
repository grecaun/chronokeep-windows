using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class EventParticipantInformation
    {
        private int identifier, eventIdentifier, divisionIdentifier, bib, chip, checkedIn = 0, shirtPurchase = 0;
        private String shirtSize;

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public int Bib { get => bib; set => bib = value; }
        public int Chip { get => chip; set => chip = value; }
        public int CheckedIn { get => checkedIn; set => checkedIn = value; }
        public int ShirtPurchase { get => shirtPurchase; set => shirtPurchase = value; }
        public string ShirtSize { get => shirtSize; set => shirtSize = value; }
    }
}
