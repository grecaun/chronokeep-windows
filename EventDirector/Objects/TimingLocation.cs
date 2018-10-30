using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class TimingLocation
    {
        private int identifier, eventIdentifier;
        private string name;

        public TimingLocation() { }

        public TimingLocation(int eventIdentifier, string nameString)
        {
            this.eventIdentifier = eventIdentifier;
            this.name = nameString;
        }

        public TimingLocation(int identifier, int eventIdentifier, string nameString)
        {
            this.identifier = identifier;
            this.eventIdentifier = eventIdentifier;
            this.name = nameString;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public string Name { get => name; set => name = value; }
    }
}
