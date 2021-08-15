using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class Participant : IEquatable<Participant>, IComparable<Participant>
    {
        private int identifier = Constants.Timing.PARTICIPANT_DUMMYIDENTIFIER;
        private string firstName, lastName, street, city, state, zip, email,
            mobile, parent, country, street2, gender, birthdate, emergencyName,
            emergencyPhone;
        private EventSpecific eventSpecific;

        public Participant()
        {
            eventSpecific = new EventSpecific();
        }

        public Participant(
            string first, string last, string street, string city, string state, string zip,
            string birthday, EventSpecific epi, string email,
            string mobile, string parent, string country, string street2, string gender,
            string ecName, string ecPhone
            )
        {
            this.birthdate = birthday.Trim();
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.eventSpecific = epi;
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
            this.emergencyName = ecName ?? "Emergency Services";
            this.emergencyPhone = ecPhone ?? "911";
            Trim();
            FormatData();
        }

        public Participant(
            int id, string first, string last, string street, string city, string state, string zip,
            string birthday, EventSpecific epi, string email,
            string mobile, string parent, string country, string street2, string gender,
            string ecName, string ecPhone
            )
        {
            this.identifier = id;
            this.birthdate = birthday;
            this.firstName = first ?? "Unknown";
            this.lastName = last ?? "Unknown";
            this.street = street ?? "";
            this.city = city ?? "";
            this.state = state ?? "";
            this.zip = zip ?? "";
            this.eventSpecific = epi ?? EventSpecific.Blank();
            this.email = email ?? "";
            this.mobile = mobile ?? "";
            this.parent = parent ?? "";
            this.country = country ?? "";
            this.street2 = street2 ?? "";
            this.gender = gender ?? "";
            this.emergencyName = ecName ?? "Emergency Services";
            this.emergencyPhone = ecPhone ?? "911";
            Trim();
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Birthdate { get => birthdate; }
        internal EventSpecific EventSpecific { get => eventSpecific; }

        internal void Trim()
        {
            birthdate = birthdate.Trim();
            firstName = firstName.Trim();
            lastName = lastName.Trim();
            street = (street ?? "").Trim();
            street2 = (street2 ?? "").Trim();
            city = (city ?? "").Trim();
            state = (state ?? "").Trim();
            zip = (zip ?? "").Trim();
            eventSpecific.Trim();
            email = (email ?? "").Trim();
            mobile = (mobile ?? "").Trim();
            parent = (parent ?? "").Trim();
            country = (country ?? "").Trim();
            gender = (gender ?? "").Trim();
            emergencyName = (emergencyName ?? "").Trim();
            emergencyPhone = (emergencyPhone ?? "").Trim();
        }

        internal void FormatData()
        {
            if (firstName != null && firstName.Length > 0)
            {
                this.firstName = CapitalizeFirst(firstName);
            }
            if (lastName != null && lastName.Length > 0)
            {
                this.lastName = CapitalizeFirst(lastName);
            }
            if (city != null && city.Length > 0)
            {
                this.city = CapitalizeFirst(city);
            }
            if (street != null && street.Length > 0) {
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
            }
            if (country != null && country.Length > 0)
            {
                if (country.Equals("US", StringComparison.OrdinalIgnoreCase) || country.Equals("United States of America", StringComparison.OrdinalIgnoreCase) || country.Equals("United States", StringComparison.OrdinalIgnoreCase))
                {
                    this.country = "USA";
                }
                else if (country.Equals("Canad", StringComparison.OrdinalIgnoreCase) || country.Equals("Canada", StringComparison.OrdinalIgnoreCase))
                {
                    this.country = "CA";
                }
            }
            if (state != null && state.Length > 0)
            {
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
                    else if (state.Equals("British Columbia", StringComparison.OrdinalIgnoreCase))
                    {
                        state = "BC";
                    }
                }
                else
                {
                    state = state.ToUpper();
                }
            }
            string tmpPhone;
            if (mobile != null && mobile.Length > 0)
            {
                tmpPhone = mobile.Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", "").Replace(".", "").Trim();
                if (tmpPhone.Length == 10)
                {
                    this.mobile = tmpPhone.Substring(0, 3) + "-" + tmpPhone.Substring(3, 3) + "-" + tmpPhone.Substring(6, 4);
                }
                else if (tmpPhone.Length == 11)
                {
                    this.mobile = tmpPhone.Substring(0, 1) + "-" + tmpPhone.Substring(1, 3) + "-" + tmpPhone.Substring(4, 3) + "-" + tmpPhone.Substring(7, 4);
                }
            }
            if (emergencyPhone != null && emergencyPhone.Length > 0)
            {
                tmpPhone = emergencyPhone.Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace(",", "").Replace(".", "").Trim();
                if (tmpPhone.Length == 10)
                {
                    this.emergencyPhone = tmpPhone.Substring(0, 3) + "-" + tmpPhone.Substring(3, 3) + "-" + tmpPhone.Substring(6, 4);
                }
                else if (tmpPhone.Length == 11)
                {
                    this.emergencyPhone = tmpPhone.Substring(0, 1) + "-" + tmpPhone.Substring(1, 3) + "-" + tmpPhone.Substring(4, 3) + "-" + tmpPhone.Substring(7, 4);
                }
            }
            if (gender != null && gender.Length > 0)
            {
                if (gender.Length > 1)
                {
                    this.gender = gender.Substring(0, 1);
                }
                this.gender = gender.ToUpper();
                if (!gender.Equals("M") && !gender.Equals("F"))
                {
                    if (gender.Equals("W"))
                    {
                        this.gender = "F";
                    }
                    else
                    {
                        this.gender = "U";
                    }
                }
            }
            else
            {
                this.gender = "U";
            }
            try
            {
                DateTime birthDateTime = DateTime.Parse(birthdate);
                birthdate = birthDateTime.ToShortDateString();
            } catch
            {
                birthdate = "01/01/0001";
            }
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
            if (outval.Length < 1)
            {
                return outval;
            }
            else if (outval.Length == 1)
            {
                return outval.ToUpper();
            }
            return outval.Substring(0, 1).ToUpper() + outval.Substring(1, outval.Length - 1);
        }

        internal string CapitalizeFirstAll(String val)
        {
            string[] tmp = val.Split(' ');
            StringBuilder output = new StringBuilder();
            foreach (string s in tmp)
            {
                output.Append(CapitalizeFirst(s.Trim()) + " ");
            }
            return output.ToString().Trim();
        }

        // Event Specific binding stuffs
        public int EventIdentifier { get => eventSpecific.EventIdentifier; }
        public int Bib { get => eventSpecific.Bib; }
        public string Distance { get => eventSpecific.DistanceName; }
        public string CheckedIn { get => eventSpecific.CheckedIn == 0 ? "No" : "Yes"; }
        public bool IsCheckedIn { get => EventSpecific.CheckedIn == 1; }
        public string Owes { get => eventSpecific.Owes; }
        public string Other { get => eventSpecific.Other; }
        public string Comments { get => eventSpecific.Comments; }
        public int Status { get => eventSpecific.Status; set => eventSpecific.Status = value; }

        // Emergency Contact binding stuffs
        public string ECName { get => emergencyName; }
        public string ECPhone { get => emergencyPhone; }
        
        public string FirstName { get => firstName; }
        public string LastName { get => lastName; }
        public string Street { get => street; }
        public string City { get => city; }
        public string State { get => state; }
        public string Zip { get => zip; }
        public string Email { get => email; }
        public string Mobile { get => mobile; }
        public string Parent { get => parent; }
        public string Country { get => country; }
        public string Street2 { get => street2; }
        public string Gender { get => gender; }

        public int CompareTo(Participant other)
        {
            if (other == null) return 1;
            if (this.EventSpecific.DistanceIdentifier == other.EventSpecific.DistanceIdentifier)
            {
                if (this.LastName == other.LastName)
                {
                    return this.FirstName.CompareTo(other.FirstName);
                }
                return this.LastName.CompareTo(other.LastName);
            }
            return this.EventSpecific.DistanceName.CompareTo(other.EventSpecific.DistanceName);
        }

        public bool Equals(Participant other)
        {
            if (other == null) return false;
            return this.Identifier == other.Identifier
                && this.Bib == other.Bib
                && this.EventIdentifier == other.EventIdentifier
                && this.FirstName.Equals(other.FirstName, StringComparison.OrdinalIgnoreCase)
                && this.LastName.Equals(other.LastName, StringComparison.OrdinalIgnoreCase)
                && this.Street.Equals(other.Street, StringComparison.OrdinalIgnoreCase)
                && this.Zip.Equals(other.Zip, StringComparison.OrdinalIgnoreCase)
                && this.Birthdate.Equals(other.Birthdate, StringComparison.OrdinalIgnoreCase);
        }

        public bool Is(Participant other)
        {
            if (other == null) return false;
            return this.FirstName.Equals(other.FirstName, StringComparison.OrdinalIgnoreCase)
                && this.LastName.Equals(other.LastName, StringComparison.OrdinalIgnoreCase)
                && this.Street.Equals(other.Street, StringComparison.OrdinalIgnoreCase)
                && this.Zip.Equals(other.Zip, StringComparison.OrdinalIgnoreCase)
                && this.Birthdate.Equals(other.Birthdate, StringComparison.OrdinalIgnoreCase);
        }

        public String Age(String eventDate)
        {
            DateTime eventDateTime = Convert.ToDateTime(eventDate);
            DateTime myDateTime = Convert.ToDateTime(birthdate);
            int numYears = eventDateTime.Year - myDateTime.Year;
            if (eventDateTime.Month < myDateTime.Month || (eventDateTime.Month == myDateTime.Month && eventDateTime.Day < myDateTime.Day))
            {
                numYears--;
            }
            return Convert.ToString(numYears);
        }

        public int GetAge(String eventDate)
        {
            DateTime eventDateTime = Convert.ToDateTime(eventDate);
            DateTime myDateTime = Convert.ToDateTime(birthdate);
            int numYears = eventDateTime.Year - myDateTime.Year;
            if (eventDateTime.Month < myDateTime.Month || (eventDateTime.Month == myDateTime.Month && eventDateTime.Day < myDateTime.Day))
            {
                numYears--;
            }
            return numYears;
        }

        public static int CompareByDistance(Participant one, Participant two)
        {
            if (two == null || one == null) return 1;
            return one.CompareTo(two);
        }

        public static int CompareByBib(Participant one, Participant two)
        {
            if (two == null || one == null) return 1;
            return one.Bib.CompareTo(two.Bib);
        }

        public static int CompareByName(Participant one, Participant two)
        {
            if (two == null || one == null) return 1;
            if (one.LastName == two.LastName)
            {
                return one.FirstName.CompareTo(two.FirstName);
            }
            return one.LastName.CompareTo(two.LastName);
        }

        public bool IsNotMatch(string value)
        {
            return this.EventSpecific.Bib.ToString().IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1
                && this.FirstName.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1
                && this.LastName.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1;
        }
    }
}
