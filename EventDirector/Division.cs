using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Division
    {
        String identifier, name, eventIdentifier;

        public string Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
