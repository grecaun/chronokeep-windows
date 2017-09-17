using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Participant
    {
        private String identifier, firstName, lastName, birthdate;
        private Address address;
        private EmergencyContact emergencyContact;
        private EventParticipantInformation info;

        public string Birthdate { get => birthdate; set => birthdate = value; }
        public string LastName { get => lastName; set => lastName = value; }
        public string FirstName { get => firstName; set => firstName = value; }
        public string Identifier { get => identifier; set => identifier = value; }
        internal EmergencyContact EmergencyContact { get => emergencyContact; set => emergencyContact = value; }
        internal Address Address { get => address; set => address = value; }
        internal EventParticipantInformation Info { get => info; set => info = value; }
    }
}
