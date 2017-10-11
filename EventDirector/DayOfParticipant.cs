using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class DayOfParticipant : IEquatable<DayOfParticipant>, IComparable<DayOfParticipant>
    {
        public DayOfParticipant() { }

        public DayOfParticipant(int eventId, int divisionId, String first, String last, String street, String city, String state, String zip,
            String birthday, String phone, String email, String mobile, String parent, String country, String street2,
            String gender, String comments, String other, String other2, String eName, String ePhone)
        {
            EventIdentifier = eventId;
            DivisionIdentifier = divisionId;
            First = first;
            Last = last;
            Street = street;
            City = city;
            State = state;
            Zip = zip;
            Birthday = birthday;
            Phone = phone;
            Email = email;
            Mobile = mobile;
            Parent = parent;
            Country = country;
            Street2 = street2;
            Gender = gender;
            Comments = comments;
            Other = other;
            Other2 = other2;
            EmergencyName = eName;
            EmergencyPhone = ePhone;
        }

        public DayOfParticipant(int id, int eventId, int divisionId, String first, String last, String street, String city, String state, String zip,
            String birthday, String phone, String email, String mobile, String parent, String country, String street2,
            String gender, String comments, String other, String other2, String eName, String ePhone)
        {
            Identifier = id;
            EventIdentifier = eventId;
            DivisionIdentifier = divisionId;
            First = first;
            Last = last;
            Street = street;
            City = city;
            State = state;
            Zip = zip;
            Birthday = birthday;
            Phone = phone;
            Email = email;
            Mobile = mobile;
            Parent = parent;
            Country = country;
            Street2 = street2;
            Gender = gender;
            Comments = comments;
            Other = other;
            Other2 = other2;
            EmergencyName = eName;
            EmergencyPhone = ePhone;
        }

        public int Identifier { get; set; }
        public int EventIdentifier { get; set; }
        public int DivisionIdentifier { get;set; }
        public String First { get; set; }
        public String Last { get; set; }
        public String Street { get; set; }
        public String City { get; set; }
        public String State { get; set; }
        public String Zip { get; set; }
        public String Birthday { get; set; }
        public String Phone { get; set; }
        public String Email { get; set; }
        public String Mobile { get; set; }
        public String Parent { get; set; }
        public String Country { get; set; }
        public String Street2 { get; set; }
        public String Gender { get; set; }
        public String Comments { get; set; }
        public String Other { get; set; }
        public String Other2 { get; set; }
        public String EmergencyName { get; set; }
        public String EmergencyPhone { get; set; }

        public int CompareTo(DayOfParticipant other)
        {
            if (other == null) return 1;
            else if (this.DivisionIdentifier == other.DivisionIdentifier)
            {
                return this.Last.CompareTo(other.Last);
            }
            return this.DivisionIdentifier.CompareTo(other.DivisionIdentifier);
        }

        public bool Equals(DayOfParticipant other)
        {
            if (other == null) return false;
            return this.Identifier == other.Identifier;
        }
    }
}
