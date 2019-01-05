using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Objects
{
    public class TimingSystem
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int LocationID { get; set; } = -1;
        public bool Connected { get; set; } = false;
    }
}
