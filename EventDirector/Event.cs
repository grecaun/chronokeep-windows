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
        private long date;
        private string name;

        public Event(string n, long d)
        {
            this.date = d;
            this.name = n;
        }

        public Event(int id, string n, long d)
        {
            this.identifier = id;
            this.name = n;
            this.date = d;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public long Date { get => date; set => date = value; }
        public string DateStr
        {
            get
            {
                return new DateTime(date).ToShortDateString();
            }
        }
    }
}
