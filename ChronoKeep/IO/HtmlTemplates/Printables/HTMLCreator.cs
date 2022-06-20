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
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        public ResultsPrintableOverall(Event theEvent,
            Dictionary<string, List<TimeResult>> distanceResults,
            Dictionary<string, List<TimeResult>> dnfResultsDictionary,
            Dictionary<int, Participant> participantDictionary)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
            this.participantDictionary = participantDictionary;
        }
    }

    public partial class ResultsPrintableAgeGroup
    {
        private Event theEvent;
        private Dictionary<string, Dictionary<(int, string), List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
        private Dictionary<string, Dictionary<(int, string), List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
        private Dictionary<int, AgeGroup> ageGroups = new Dictionary<int, AgeGroup>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        public ResultsPrintableAgeGroup(Event theEvent,
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> distanceResults,
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> dnfResultsDictionary,
            Dictionary<int, AgeGroup> ageGroups,
            Dictionary<int, Participant> participantDictionary)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
            this.ageGroups = ageGroups;
            this.participantDictionary = participantDictionary;
        }
    }

    public partial class ResultsPrintableGender
    {
        private Event theEvent;
        private Dictionary<string, Dictionary<string, List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
        private Dictionary<string, Dictionary<string, List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        public ResultsPrintableGender(Event theEvent,
            Dictionary<string, Dictionary<string, List<TimeResult>>> distanceResults,
            Dictionary<string, Dictionary<string, List<TimeResult>>> dnfResultsDictionary,
            Dictionary<int, Participant> participantDictionary)
        {
            this.theEvent = theEvent;
            this.distanceResults = distanceResults;
            this.dnfResultsDictionary = dnfResultsDictionary;
            this.participantDictionary = participantDictionary;
        }
    }
}
