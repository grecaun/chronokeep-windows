using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class EmergencyContact
    {
        private int identifier;
        private string name, phone, email;
        public string Name { get => name; set => name = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Email { get => email; set => email = value; }
        public int Identifier { get => identifier; set => identifier = value; }
    }
}
