using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Division
    {
        private String name;
        private int identifier, eventIdentifier, cost;

        public Division() { }

        public Division(string name, int eventIdentifier, int cost)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
        }

        public Division(int identifier, string name, int eventIdentifier, int cost)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int Cost { get => cost; set => cost = value; }
    }
}
