using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.IO.HtmlTemplates.Printables
{
    public partial class ResultsPrintableOverall
    {
        private Event theEvent;
        private Dictionary<string, List<TimeResult>> distanceResults = new Dictionary<string, List<TimeResult>>();
        private Dictionary<string, List<TimeResult>> dnfResultsDictionary = new Dictionary<string, List<TimeResult>>();

        public ResultsPrintableOverall(Event theEvent,
            Dictionary<string, List<TimeResult>> distanceResults,
            Dictionary<string, List<TimeResult>> dnfResultsDictionary)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
        }
    }

    public partial class ResultsPrintableAgeGroup
    {
        private Event theEvent;
        private Dictionary<string, Dictionary<(int, string), List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
        private Dictionary<string, Dictionary<(int, string), List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
        private Dictionary<int, AgeGroup> ageGroups = new Dictionary<int, AgeGroup>();

        public ResultsPrintableAgeGroup(Event theEvent,
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> distanceResults,
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> dnfResultsDictionary,
            Dictionary<int, AgeGroup> ageGroups)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
            this.ageGroups = ageGroups;
        }
    }

    public partial class ResultsPrintableGender
    {
        private Event theEvent;
        private Dictionary<string, Dictionary<string, List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
        private Dictionary<string, Dictionary<string, List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<string, List<TimeResult>>>();

        public ResultsPrintableGender(Event theEvent,
            Dictionary<string, Dictionary<string, List<TimeResult>>> distanceResults,
            Dictionary<string, Dictionary<string, List<TimeResult>>> dnfResultsDictionary)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
        }
    }
}
