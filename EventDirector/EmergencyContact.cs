﻿using System;
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

        public EmergencyContact() { }

        public static EmergencyContact BlankContact()
        {
            return new EmergencyContact()
            {
                identifier = 0,
                name = "",
                phone = "",
                email = ""
            };
        }

        public EmergencyContact(string n, string p, string e)
        {
            this.name = n ?? "";
            this.phone = p ?? "";
            this.email = e ?? "";
        }

        public EmergencyContact(int id, string n, string p, string e)
        {
            this.identifier = id;
            this.name = n ?? "";
            this.phone = p ?? "";
            this.email = e ?? "";
        }

        internal void Trim()
        {
            name = name.Trim();
            email = email.Trim();
        }

        public string Name { get => name; set => name = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Email { get => email; set => email = value; }
        public int Identifier { get => identifier; set => identifier = value; }
    }
}
