using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Participant
    {
        private int identifier;
        private long birthdate;
        private string firstName, lastName, street, city, state, zip, phone, email;
        private EmergencyContact emergencyContact;
        private EventParticipantInformation eventSpecific;

        public int Identifier { get => identifier; set => identifier = value; }
        public long Birthdate { get => birthdate; set => birthdate = value; }
        public string FirstName { get => firstName; set => firstName = value; }
        public string LastName { get => lastName; set => lastName = value; }
        public string Street { get => street; set => street = value; }
        public string City { get => city; set => city = value; }
        public string State { get => state; set => state = value; }
        public string Zip { get => zip; set => zip = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Email { get => email; set => email = value; }
        internal EmergencyContact EmergencyContact { get => emergencyContact; set => emergencyContact = value; }
        internal EventParticipantInformation EventSpecific { get => eventSpecific; set => eventSpecific = value; }
    }
}
