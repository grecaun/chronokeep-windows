using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class TimingPoint
    {
        private int identifier, eventIdentifier, divisionIdentifier;
        private string name, distance, unit;

        public TimingPoint(int eventIdentifier, int divisionIdentifier, string nameString, string distanceStr, string unitString)
        {
            this.eventIdentifier = eventIdentifier;
            this.divisionIdentifier = divisionIdentifier;
            this.name = nameString;
            this.distance = distanceStr;
            this.unit = unitString;
        }

        public TimingPoint(int identifier, int eventIdentifier, int divisionIdentifier, string nameString, string distanceStr, string unitString)
        {
            this.identifier = identifier;
            this.eventIdentifier = eventIdentifier;
            this.divisionIdentifier = divisionIdentifier;
            this.name = nameString;
            this.distance = distanceStr;
            this.unit = unitString;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int DivisionIdentifier { get => divisionIdentifier; set => divisionIdentifier = value; }
        public string Name { get => name; set => name = value; }
        public string Distance { get => distance; set => distance = value; }
        public string Unit { get => unit; set => unit = value; }
    }
}
