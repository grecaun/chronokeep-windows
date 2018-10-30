using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Division
    {
        private String name;
        private int identifier, eventIdentifier, cost;
        private double distance;
        private int distance_unit = 0, finish_location = -1, finish_occurance = 1, start_location=-2, start_within = 5;

        public Division() { }

        public Division(string name, int eventIdentifier, int cost)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
        }

        public Division(int identifier, string name, int eventIdentifier, int cost)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
        }

        public Division(string name, int eventIdentifier, int cost, double distance, int dunit, int finloc, int finocc, int startloc, int startwith)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
            this.distance = distance;
            this.distance_unit = dunit;
            this.finish_location = finloc;
            this.finish_occurance = finocc;
            this.start_location = startloc;
            this.start_within = startwith;
        }

        public Division(int identifier, string name, int eventIdentifier, int cost, double distance, int dunit, int finloc, int finocc, int startloc, int startwith)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
            this.distance = distance;
            this.distance_unit = dunit;
            this.finish_location = finloc;
            this.finish_occurance = finocc;
            this.start_location = startloc;
            this.start_within = startwith;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int Cost { get => cost; set => cost = value; }
        public double Distance { get => distance; set => distance = value; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public int FinishLocation { get => finish_location; set => finish_location = value; }
        public int FinishOccurance { get => finish_occurance; set => finish_occurance = value; }
        public int StartLocation { get => start_location; set => start_location = value; }
        public int StartWithin { get => start_within; set => start_within = value; }
    }
}
