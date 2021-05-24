using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    public class Event
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Website { get; set; }
        public string Image { get; set; }
        public string ContactEmail { get; set; }
        public bool AccessRestricted { get; set; }
    }
}
