using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class EmergencyContact
    {
        private int identifier;
        private string name, phone, email;

        public EmergencyContact()
        {
            identifier = 0;
            name = "";
            phone = "";
            email = "";
        }

        public EmergencyContact(string n, string p, string e)
        {
            this.name = n;
            this.phone = p;
            this.email = e;
        }

        public EmergencyContact(int id, string n, string p, string e)
        {
            this.identifier = id;
            this.name = n;
            this.phone = p;
            this.email = e;
        }

        public string Name { get => name; }
        public string Phone { get => phone; }
        public string Email { get => email; }
        public int Identifier { get => identifier; set => identifier = value; }
    }
}
