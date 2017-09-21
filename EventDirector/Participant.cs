using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Participant
    {
        private int identifier;
        private long birthdate;
        private string firstName, lastName, street, city, state, zip, phone, email, mobile, parent, country, street2;
        private EmergencyContact emergencyContact;
        private EventSpecific eventSpecific;

        public Participant(
            int id,
            string first,
            string last,
            string street,
            string city,
            string state,
            string zip,
            long birthday,
            EmergencyContact ec,
            EventSpecific epi,
            string phone,
            string email,
            string mobile,
            string parent,
            string country,
            string street2
            )
        {
            this.identifier = id;
            this.birthdate = birthday;
            this.firstName = first;
            this.lastName = last;
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.emergencyContact = ec;
            this.eventSpecific = epi;
            this.phone = phone;
            this.email = email;
            this.mobile = mobile;
            this.parent = parent;
            this.country = country;
            this.street2 = street2;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public long Birthdate { get => birthdate; set => birthdate = value; }
        internal EmergencyContact EmergencyContact { get => emergencyContact; set => emergencyContact = value; }
        internal EventSpecific EventSpecific { get => eventSpecific; set => eventSpecific = value; }

        // Event Specific binding stuffs
        public int EventIdentifier { get => eventSpecific.EventIdentifier; }
        public int Bib { get => eventSpecific.Bib; }
        public int Chip { get => eventSpecific.Chip; }
        public string Division { get => eventSpecific.DivisionName; }
        public string CheckedIn { get => eventSpecific.CheckedIn == 0 ? "No" : "Yes"; }
        public string ShirtPurchase { get => eventSpecific.ShirtPurchase == 0 ? "No" : "Yes"; }
        public string ShirtSize { get => eventSpecific.ShirtSize; }
        public string SecondShirt { get => eventSpecific.SecondShirt; }
        public string Owes { get => eventSpecific.Owes; }
        public string Hat { get => eventSpecific.Hat; }
        public string Other { get => eventSpecific.Other; }
        // Emergency Contact binding stuffs
        public int ECID { get => emergencyContact.Identifier; }
        public string ECName { get => emergencyContact.Name; }
        public string ECPhone { get => emergencyContact.Phone; }
        public string ECEmail { get => emergencyContact.Email; }
        
        public string FirstName { get => firstName; set => firstName = value; }
        public string LastName { get => lastName; set => lastName = value; }
        public string Street { get => street; set => street = value; }
        public string City { get => city; set => city = value; }
        public string State { get => state; set => state = value; }
        public string Zip { get => zip; set => zip = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Email { get => email; set => email = value; }
        public string Mobile { get => mobile; set => mobile = value; }
        public string Parent { get => parent; set => parent = value; }
        public string Country { get => country; set => country = value; }
        public string Street2 { get => street2; set => street2 = value; }
    }
}
