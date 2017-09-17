using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Event
    {
        String identifier, name, date;

        public string Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
    }
}
