using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Objects
{
    public class BibGroup
    {
        public BibGroup(int eventId)
        {
            this.EventId = eventId;
            this.Name = "All";
            this.Number = -1;
        }

        public BibGroup() { }

        public int EventId { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
