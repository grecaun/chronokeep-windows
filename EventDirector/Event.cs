using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Event
    {
        private int identifier, date;
        private string name;

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int Date { get => date; set => date = value; }
    }
}
