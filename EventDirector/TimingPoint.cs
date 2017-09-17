using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class TimingPoint
    {
        String identifier, name, distance, unit;

        public string Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Distance { get => distance; set => distance = value; }
        public string Unit { get => unit; set => unit = value; }
    }
}
