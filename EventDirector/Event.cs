using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Event
    {
        private int identifier, nextYear = -1, shirtOptional = 1, shirtPrice = 2000;
        private string name, date;

        public Event() { }

        public Event(string n, long d, int so, int price)
        {
            this.shirtOptional = so;
            this.date = new DateTime(d).ToShortDateString();
            this.name = n;
            this.shirtPrice = price;
        }

        public Event(int id, string n, long d, int ny, int so, int price)
        {
            this.nextYear = ny;
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = new DateTime(d).ToShortDateString();
            this.shirtPrice = price;
        }

        public Event(int id, string n, string d, int ny, int so, int price)
        {
            this.nextYear = ny;
            this.identifier = id;
            this.shirtOptional = so;
            this.name = n;
            this.date = d;
            this.shirtPrice = price;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int NextYear { get => nextYear; set => nextYear = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
        public int ShirtOptional { get => shirtOptional; set => shirtOptional = value; }
        public int ShirtPrice { get => shirtPrice; set => shirtPrice = value; }
    }
}
