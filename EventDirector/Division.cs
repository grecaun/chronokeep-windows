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

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
