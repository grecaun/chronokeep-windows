using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class DayOfParticipant
    {
        public DayOfParticipant() { }

        public DayOfParticipant(int eventId, String first, String last, String street, String city, String state, String zip,
            String birthday, String phone, String email, String mobile, String parent, String country, String street2,
            String gender, String comments, String other, String other2, String eName, String ePhone)
        {
            EventIdentifier = eventId;
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

        public DayOfParticipant(int id, int eventId, String first, String last, String street, String city, String state, String zip,
            String birthday, String phone, String email, String mobile, String parent, String country, String street2,
            String gender, String comments, String other, String other2, String eName, String ePhone)
        {
            Identifier = id;
            EventIdentifier = eventId;
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

        int Identifier { get; set; }
        int EventIdentifier { get; set; }
        String First { get; set; }
        String Last { get; set; }
        String Street { get; set; }
        String City { get; set; }
        String State { get; set; }
        String Zip { get; set; }
        String Birthday { get; set; }
        String Phone { get; set; }
        String Email { get; set; }
        String Mobile { get; set; }
        String Parent { get; set; }
        String Country { get; set; }
        String Street2 { get; set; }
        String Gender { get; set; }
        String Comments { get; set; }
        String Other { get; set; }
        String Other2 { get; set; }
        String EmergencyName { get; set; }
        String EmergencyPhone { get; set; }
    }
}
