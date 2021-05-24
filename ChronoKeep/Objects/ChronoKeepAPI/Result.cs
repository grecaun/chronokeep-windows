using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    public class Result
    {
        public string Bib { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string AgeGroup { get; set; }
        public string Distance { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }
        public string Segment { get; set; }
        public string Location { get; set; }
        public int Occurence { get; set; }
        public int Ranking { get; set; }
        public int AgeRanking { get; set; }
        public int GenderRanking { get; set; }
        public bool Finish { get; set; }
    }
}
