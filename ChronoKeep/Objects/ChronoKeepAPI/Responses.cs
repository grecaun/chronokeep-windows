using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API responses.
     * 
     */

    // Event specific responses.
    public class GetEventsResponse
    {
        public List<Event> Events { get; set; }
    }

    public class ModifyEventResponse
    {
        public Event Event { get; set; }
    }

    // Event Year specific responses.
    public class GetEventYearsResponse
    {
        public List<EventYear> EventYears { get; set; }
    }

    public class EventYearResponse
    {
        public Event Event { get; set; }
        public EventYear EventYear { get; set; }
    }

    // Results specific responses.
    public class AddResultsResponse
    {
        public int Count { get; set; }
    }
}
