using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Event
    {
        private int identifier, shirtOptional;
        private string name, date;

        public Event() { }

        public Event(string n, long d, int so)
        {
            this.shirtOptional = so;
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
        }

        public Event(int id, string n, long d, int so)
        {
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = new DateTime(d).ToShortDateString();
        }

        public Event(int id, string n, string d, int so)
        {
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = d;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
        public int ShirtOptional { get => shirtOptional; set => shirtOptional = value; }
    }
}
