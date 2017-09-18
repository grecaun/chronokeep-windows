using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class TimingPoint
    {
        private int identifier, eventIdentifier;
        private string name, distance, unit;

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Distance { get => distance; set => distance = value; }
        public string Unit { get => unit; set => unit = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
