﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Participant : IEquatable<Participant>, IComparable<Participant>
    {
        private int identifier;
        private long birthdate;
        private string firstName, lastName, street, city, state, zip, phone, email, mobile, parent, country, street2, gender;
        private EmergencyContact emergencyContact;
        private EventSpecific eventSpecific;

        public Participant(
            string first,
            string last,
            string street,
            string city,
            string state,
            string zip,
            string birthday,
            EmergencyContact ec,
            EventSpecific epi,
            string phone,
            string email,
            string mobile,
            string parent,
            string country,
            string street2,
            string gender
            )
        {
            this.birthdate = DateTime.Parse(birthday).Ticks;
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.emergencyContact = ec ?? new EmergencyContact();
            this.eventSpecific = epi;
            this.phone = phone ?? "";
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
        }

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
            string street2,
            string gender
            )
        {
            this.identifier = id;
            this.birthdate = birthday;
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.emergencyContact = ec ?? new EmergencyContact();
            this.eventSpecific = epi;
            this.phone = phone ?? "";
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public long Birthdate { get => birthdate; }
        internal EmergencyContact EmergencyContact { get => emergencyContact; }
        internal EventSpecific EventSpecific { get => eventSpecific; }

        // Event Specific binding stuffs
        public int EventIdentifier { get => eventSpecific.EventIdentifier; }
        public int Bib { get => eventSpecific.Bib; }
        public int Chip { get => eventSpecific.Chip; }
        public string Division { get => eventSpecific.DivisionName; }
        public string CheckedIn { get => eventSpecific.CheckedIn == 0 ? "No" : "Yes"; }
        public string ShirtSize { get => eventSpecific.ShirtSize; }
        public string SecondShirt { get => eventSpecific.SecondShirt; }
        public string Owes { get => eventSpecific.Owes; }
        public string Hat { get => eventSpecific.Hat; }
        public string Other { get => eventSpecific.Other; }
        public string Comments { get => eventSpecific.Comments; }
        public string EarlyStart { get => eventSpecific.EarlyStart == 0 ? "No" : "Yes"; }
        public string BirthdayStr
        {
            get
            {
                return new DateTime(birthdate).ToShortDateString();
            }
        }
        // Emergency Contact binding stuffs
        public int ECID { get => emergencyContact.Identifier; }
        public string ECName { get => emergencyContact.Name; }
        public string ECPhone { get => emergencyContact.Phone; }
        public string ECEmail { get => emergencyContact.Email; }
        
        public string FirstName { get => firstName; }
        public string LastName { get => lastName; }
        public string Street { get => street; }
        public string City { get => city; }
        public string State { get => state; }
        public string Zip { get => zip; }
        public string Phone { get => phone; }
        public string Email { get => email; }
        public string Mobile { get => mobile; }
        public string Parent { get => parent; }
        public string Country { get => country; }
        public string Street2 { get => street2; }
        public string Gender { get => gender; }

        public int CompareTo(Participant other)
        {
            if (other == null) return 1;
            else if (this.EventSpecific.DivisionIdentifier == other.EventSpecific.DivisionIdentifier)
            {
                return this.LastName.CompareTo(other.LastName);
            }
            return this.EventSpecific.DivisionIdentifier.CompareTo(other.EventSpecific.DivisionIdentifier);
        }

        public bool Equals(Participant other)
        {
            if (other == null) return false;
            return this.Identifier == other.Identifier;
        }
    }
}
