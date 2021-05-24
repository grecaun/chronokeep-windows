using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API requests.
     * 
     */

    // General request with only the key
    public class GeneralRequest
    {
        public string Key { get; set; }
    }

    // Event specific requests
    public class GetEventRequest
    {
        public string Key { get; set; }
        public string Slug { get; set; }
    }

    public class ModifyEventRequest
    {
        public string Key { get; set; }
        public Event Event { get; set; }
    }

    // Event Year specific requests.
    public class GetEventYearRequest
    {
        public string Key { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
    }

    public class ModifyEventYearRequest
    {
        public string Key { get; set; }
        public string Slug { get; set; }
        public EventYear Year { get; set; }
    }

    // Result specific requests
    public class GetResultsRequest
    {
        public string Key { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
    }

    public class AddResultsRequest
    {
        public string Key { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
        public List<Result> Results { get; set; }
    }
}
