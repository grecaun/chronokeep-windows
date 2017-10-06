using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class ChangeParticipant : Participant, IEquatable<ChangeParticipant>, IComparable<ChangeParticipant>
    {
        public int ChangeIdentifier { get; set; }
        public String Which { get; set; }

        public ChangeParticipant(int changeId, String which, Participant p)
            : base(p.Identifier, p.FirstName, p.LastName, p.Street, p.City,
                p.State, p.Zip, p.Birthdate, p.EmergencyContact, p.EventSpecific, p.Phone, p.Email,
                p.Mobile, p.Parent, p.Country, p.Street2, p.Gender)
        {
            ChangeIdentifier = changeId;
            Which = which;
        }

        public int CompareTo(ChangeParticipant other)
        {
            if (ChangeIdentifier == other.ChangeIdentifier)
            {
                return other.Which.CompareTo(Which);
            }
            return ChangeIdentifier.CompareTo(other.ChangeIdentifier);
        }

        public bool Equals(ChangeParticipant other)
        {
            return ChangeIdentifier == other.ChangeIdentifier && Which == other.Which;
        }
    }
}
