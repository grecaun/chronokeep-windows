using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Event
    {
        private int identifier;
        private string name, date;

        public Event(string n, long d)
        {
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
        }

        public Event(int id, string n, long d)
        {
            this.identifier = id;
            this.name = n;
            this.date = new DateTime(d).ToShortDateString();
        }

        public Event(int id, string n, string d)
        {
            this.identifier = id;
            this.name = n;
            this.date = d;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; }
        public string Date { get => date; }
    }
}
