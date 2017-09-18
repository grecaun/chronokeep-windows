using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Division
    {
        private String name;
        private int identifier, eventIdentifier;

        public Division(string name, int eventIdentifier)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
        }

        public Division(int identifier, string name, int eventIdentifier)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
