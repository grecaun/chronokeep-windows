using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class EmergencyContact
    {
        private string name, phone;

        public EmergencyContact() { }

        public static EmergencyContact BlankContact()
        {
            return new EmergencyContact()
            {
                name = "",
                phone = "",
            };
        }

        public EmergencyContact(string n, string p)
        {
            this.name = n ?? "";
            this.phone = p ?? "";
        }

        internal void Trim()
        {
            name = name.Trim();
        }

        public string Name { get => name; set => name = value; }
        public string Phone { get => phone; set => phone = value; }
    }
}
