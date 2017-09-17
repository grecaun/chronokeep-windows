using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class EventParticipantInformation
    {
        private String identifier, divisionIdentifier, shirtSize, eventIdentifier;
        private int bib, chip;
        private Boolean checkedIn = false, shirtPurchase = false;

        public string Identifier { get => identifier; set => identifier = value; }
        public string DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public string ShirtSize { get => shirtSize; set => shirtSize = value; }
        public int Bib { get => bib; set => bib = value; }
        public int Chip { get => chip; set => chip = value; }
        public bool CheckedIn { get => checkedIn; set => checkedIn = value; }
        public bool ShirtPurchase { get => shirtPurchase; set => shirtPurchase = value; }
        public string EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
