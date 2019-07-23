using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class DivisionStats
    {
        public string DivisionName { get; set; }
        public int Total { get; set; }
        public int DNF { get; set; }
        public int DNS { get; set; }
        public int Finished { get; set; }
        public int Active { get; set; }

        public void CalculateAll()
        {
            Total = DNF + DNS + Finished + Active;
        }
    }
}
