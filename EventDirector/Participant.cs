using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Participant : IEquatable<Participant>, IComparable<Participant>
    {
        private int identifier;
        private string firstName, lastName, street, city, state, zip, phone, email, mobile, parent, country, street2, gender, birthdate;
        private EmergencyContact emergencyContact;
        private EventSpecific eventSpecific;

        public Participant(
            string first, string last, string street, string city, string state, string zip,
            string birthday, EmergencyContact ec, EventSpecific epi, string phone, string email,
            string mobile, string parent, string country, string street2, string gender
            )
        {
            this.birthdate = birthday;
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.emergencyContact = ec ?? EmergencyContact.BlankContact();
            this.eventSpecific = epi;
            this.phone = phone ?? "";
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
            FormatData();
        }

        public Participant(
            int id, string first, string last, string street, string city, string state, string zip,
            string birthday, EmergencyContact ec, EventSpecific epi, string phone, string email,
            string mobile, string parent, string country, string street2, string gender
            )
        {
            this.identifier = id ;
            this.birthdate = birthday;
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street ?? "";
            this.city = city ?? "";
            this.state = state ?? "";
            this.zip = zip ?? "";
            this.emergencyContact = ec ?? EmergencyContact.BlankContact();
            this.eventSpecific = epi ?? EventSpecific.Blank();
            this.phone = phone ?? "";
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Birthdate { get => birthdate; }
        internal EmergencyContact EmergencyContact { get => emergencyContact; }
        internal EventSpecific EventSpecific { get => eventSpecific; }

        internal void FormatData()
        {
            Log.D("Formatting data.");
            this.firstName = CapitalizeFirst(firstName);
            this.lastName = CapitalizeFirst(lastName);
            this.city = CapitalizeFirst(city);
            string[] addressArray = street.Split(',');
            if (addressArray.Length == 2 && street2.Length == 0)
            {
                this.street = addressArray[0];
                this.street2 = addressArray[1];
            }
            else if (addressArray.Length > 2)
            {
                this.street = addressArray[0];
            }
            int street2Ix = street.IndexOf(street2, StringComparison.OrdinalIgnoreCase);
            if (street2Ix >= 0)
            {
                this.street = street.Remove(street2Ix, street2.Length).Trim();
            }
            if (country.Equals("US", StringComparison.OrdinalIgnoreCase))
            {
                this.country = "USA";
            }
            else if (country.Equals("Canad", StringComparison.OrdinalIgnoreCase) || country.Equals("Canada", StringComparison.OrdinalIgnoreCase))
            {
                this.country = "CA";
            }
            if (state.Length > 2)
            {
                if (state.Equals("Alabama", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AL";
                }
                else if (state.Equals("Alaska", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AK";
                }
                else if (state.Equals("Arizona", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AZ";
                }
                else if (state.Equals("Arkansas", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AR";
                }
                else if (state.Equals("California", StringComparison.OrdinalIgnoreCase))
                {
                    state = "CA";
                }
                else if (state.Equals("Colorado", StringComparison.OrdinalIgnoreCase))
                {
                    state = "CO";
                }
                else if (state.Equals("Connecticut", StringComparison.OrdinalIgnoreCase))
                {
                    state = "CT";
                }
                else if (state.Equals("Delaware", StringComparison.OrdinalIgnoreCase))
                {
                    state = "DE";
                }
                else if (state.Equals("Florida", StringComparison.OrdinalIgnoreCase))
                {
                    state = "FL";
                }
                else if (state.Equals("Georgia", StringComparison.OrdinalIgnoreCase))
                {
                    state = "GA";
                }
                else if (state.Equals("Hawaii", StringComparison.OrdinalIgnoreCase))
                {
                    state = "HI";
                }
                else if (state.Equals("Idaho", StringComparison.OrdinalIgnoreCase))
                {
                    state = "ID";
                }
                else if (state.Equals("Illinois", StringComparison.OrdinalIgnoreCase))
                {
                    state = "IL";
                }
                else if (state.Equals("Indiana", StringComparison.OrdinalIgnoreCase))
                {
                    state = "IN";
                }
                else if (state.Equals("Iowa", StringComparison.OrdinalIgnoreCase))
                {
                    state = "IA";
                }
                else if (state.Equals("Kansas", StringComparison.OrdinalIgnoreCase))
                {
                    state = "KS";
                }
                else if (state.Equals("Kentucky", StringComparison.OrdinalIgnoreCase))
                {
                    state = "KY";
                }
                else if (state.Equals("Louisianna", StringComparison.OrdinalIgnoreCase))
                {
                    state = "LA";
                }
                else if (state.Equals("Maine", StringComparison.OrdinalIgnoreCase))
                {
                    state = "ME";
                }
                else if (state.Equals("Maryland", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MD";
                }
                else if (state.Equals("Massachusetts", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MA";
                }
                else if (state.Equals("Michigan", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MI";
                }
                else if (state.Equals("Minnesota", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MN";
                }
                else if (state.Equals("Mississippi", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MS";
                }
                else if (state.Equals("Missouri", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MO";
                }
                else if (state.Equals("Montana", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MT";
                }
                else if (state.Equals("Nebraska", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NE";
                }
                else if (state.Equals("Nevada", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NV";
                }
                else if (state.Equals("New Hampshire", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NH";
                }
                else if (state.Equals("New Jersey", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NJ";
                }
                else if (state.Equals("New Mexico", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NM";
                }
                else if (state.Equals("New York", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NY";
                }
                else if (state.Equals("North Carolina", StringComparison.OrdinalIgnoreCase))
                {
                    state = "NC";
                }
                else if (state.Equals("North Dakota", StringComparison.OrdinalIgnoreCase))
                {
                    state = "ND";
                }
                else if (state.Equals("Ohio", StringComparison.OrdinalIgnoreCase))
                {
                    state = "OH";
                }
                else if (state.Equals("Oklahoma", StringComparison.OrdinalIgnoreCase))
                {
                    state = "OK";
                }
                else if (state.Equals("Oregon", StringComparison.OrdinalIgnoreCase))
                {
                    state = "OR";
                }
                else if (state.Equals("Pennsylvania", StringComparison.OrdinalIgnoreCase))
                {
                    state = "PA";
                }
                else if (state.Equals("Rhode Island", StringComparison.OrdinalIgnoreCase))
                {
                    state = "RI";
                }
                else if (state.Equals("South Carolina", StringComparison.OrdinalIgnoreCase))
                {
                    state = "SC";
                }
                else if (state.Equals("South Dakota", StringComparison.OrdinalIgnoreCase))
                {
                    state = "SD";
                }
                else if (state.Equals("Tennessee", StringComparison.OrdinalIgnoreCase))
                {
                    state = "TN";
                }
                else if (state.Equals("Texas", StringComparison.OrdinalIgnoreCase))
                {
                    state = "TX";
                }
                else if (state.Equals("Utah", StringComparison.OrdinalIgnoreCase))
                {
                    state = "UT";
                }
                else if (state.Equals("Vermont", StringComparison.OrdinalIgnoreCase))
                {
                    state = "VT";
                }
                else if (state.Equals("Virginia", StringComparison.OrdinalIgnoreCase))
                {
                    state = "VA";
                }
                else if (state.Equals("Washington", StringComparison.OrdinalIgnoreCase))
                {
                    state = "WA";
                }
                else if (state.Equals("West Virginia", StringComparison.OrdinalIgnoreCase))
                {
                    state = "WV";
                }
                else if (state.Equals("Wisconsin", StringComparison.OrdinalIgnoreCase))
                {
                    state = "WI";
                }
                else if (state.Equals("Wyoming", StringComparison.OrdinalIgnoreCase))
                {
                    state = "WY";
                }
                else if (state.Equals("American Samoa", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AS";
                }
                else if (state.Equals("District of Columbia", StringComparison.OrdinalIgnoreCase))
                {
                    state = "DC";
                }
                else if (state.Equals("Federated States of Micronesia", StringComparison.OrdinalIgnoreCase))
                {
                    state = "FM";
                }
                else if (state.Equals("Guam", StringComparison.OrdinalIgnoreCase))
                {
                    state = "GU";
                }
                else if (state.Equals("Marshall Islands", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MH";
                }
                else if (state.Equals("Northern Mariana Islands", StringComparison.OrdinalIgnoreCase))
                {
                    state = "MP";
                }
                else if (state.Equals("Puerto Rico", StringComparison.OrdinalIgnoreCase))
                {
                    state = "PR";
                }
                else if (state.Equals("Palau", StringComparison.OrdinalIgnoreCase))
                {
                    state = "PW";
                }
                else if (state.Equals("Virgin Islands", StringComparison.OrdinalIgnoreCase))
                {
                    state = "VI";
                }
                else if (state.Equals("Armed Forces Africa", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AE";
                }
                else if (state.Equals("Armed Forces Americas", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AA";
                }
                else if (state.Equals("Armed Forces Canada", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AE";
                }
                else if (state.Equals("Armed Forces Europe", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AE";
                }
                else if (state.Equals("Armed Forces Middle East", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AE";
                }
                else if (state.Equals("Armed Forces Pacific", StringComparison.OrdinalIgnoreCase))
                {
                    state = "AP";
                }
            }
            string tmpPhone = phone.Replace("-","").Replace("+","").Replace("(","").Replace(")","").Replace(" ","").Replace(",","").Replace(".","").Trim();
            if (tmpPhone.Length == 10)
            {
                this.phone = tmpPhone.Substring(0, 3) + "-" + tmpPhone.Substring(3, 3) + "-" + tmpPhone.Substring(6, 4);
            }
            else if (tmpPhone.Length == 11)
            {
                this.phone = tmpPhone.Substring(0, 1) + "-" + tmpPhone.Substring(1, 3) + "-" + tmpPhone.Substring(4, 3) + "-" + tmpPhone.Substring(7, 4);
            }
            tmpPhone = mobile.Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", "").Replace(".", "").Trim();
            if (tmpPhone.Length == 10)
            {
                this.mobile = tmpPhone.Substring(0, 3) + "-" + tmpPhone.Substring(3, 3) + "-" + tmpPhone.Substring(6, 4);
            }
            else if (tmpPhone.Length == 11)
            {
                this.mobile = tmpPhone.Substring(0, 1) + "-" + tmpPhone.Substring(1, 3) + "-" + tmpPhone.Substring(4, 3) + "-" + tmpPhone.Substring(7, 4);
            }
            tmpPhone = emergencyContact.Phone.Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", "").Replace(".", "").Trim();
            if (tmpPhone.Length == 10)
            {
                this.emergencyContact.Phone = tmpPhone.Substring(0, 3) + "-" + tmpPhone.Substring(3, 3) + "-" + tmpPhone.Substring(6, 4);
            }
            else if (tmpPhone.Length == 11)
            {
                this.emergencyContact.Phone = tmpPhone.Substring(0, 1) + "-" + tmpPhone.Substring(1, 3) + "-" + tmpPhone.Substring(4, 3) + "-" + tmpPhone.Substring(7, 4);
            }
            if (gender.Length > 1)
            {
                this.gender = gender.Substring(0, 1);
            }
            this.gender = gender.ToUpper();
            try
            {
                DateTime birthDateTime = DateTime.Parse(birthdate);
                birthdate = birthDateTime.ToShortDateString();
            } catch
            {
                birthdate = "01/01/0001";
            }
            Log.D("New data should be First: " + firstName + " Last: " + lastName + " City: " + city + " Street: " + street + " Country: " + country + " Phone: " + phone + " Mobile: " + mobile + " Emergency Contact Phone: " + emergencyContact.Phone + " Gender: " + gender);
        }

        internal Boolean AllCaps(string val)
        {
            return val.Equals(val.ToUpper());
        }

        internal string CapitalizeFirst(String val)
        {
            string outval = val;
            if (AllCaps(val))
            {
                outval = val.ToLower();
            }
            return outval.Substring(0, 1).ToUpper() + outval.Substring(1, outval.Length - 1);
        }

        // Event Specific binding stuffs
        public long EventIdentifier { get => eventSpecific.EventIdentifier; }
        public long Bib { get => eventSpecific.Bib; }
        public string Division { get => eventSpecific.DivisionName; }
        public string CheckedIn { get => eventSpecific.CheckedIn == 0 ? "No" : "Yes"; }
        public string ShirtSize { get => eventSpecific.ShirtSize; }
        public string SecondShirt { get => eventSpecific.SecondShirt; }
        public string Owes { get => eventSpecific.Owes; }
        public string Hat { get => eventSpecific.Hat; }
        public string Other { get => eventSpecific.Other; }
        public string Comments { get => eventSpecific.Comments; }
        public string EarlyStart { get => eventSpecific.EarlyStart == 0 ? "No" : "Yes"; }
        public string Fleece { get => eventSpecific.Fleece; }
        public string NextYear { get => eventSpecific.NextYear == 0 ? "No" : "Yes"; }

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

        public String Age(String eventDate)
        {
            DateTime eventDateTime = Convert.ToDateTime(eventDate);
            DateTime myDateTime = Convert.ToDateTime(birthdate);
            int numYears = eventDateTime.Year - myDateTime.Year;
            if (eventDateTime.Month < myDateTime.Month || (eventDateTime.Month == myDateTime.Month && eventDateTime.Date < myDateTime.Date))
            {
                numYears--;
            }
            return Convert.ToString(numYears);
        }
    }
}
