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
        public int DivisionID { get; set; }
        public int Total { get => DNF + DNS + Finished + Active; }
        public int DNF { get; set; }
        public int DNS { get; set; }
        public int Finished { get; set; }
        public int Active { get; set; }
    }
}
