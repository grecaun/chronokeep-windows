using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Chronokeep.IO.HtmlTemplates
{
    public partial class HtmlResultsTemplate
    {
        private Event theEvent;
        private Dictionary<string, List<TimeResult>> distanceResults = new Dictionary<string, List<TimeResult>>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        public HtmlResultsTemplate(Event theEvent,
            List<TimeResult> resultList,
            Dictionary<int, Participant> participantDictionary)
        {
            this.theEvent = theEvent;
            resultList.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            resultList.Sort(TimeResult.CompareByDistancePlace);
            foreach (TimeResult result in resultList)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new List<TimeResult>();
                }
                distanceResults[result.DistanceName].Add(result);
            }
            this.participantDictionary = participantDictionary;
        }
    }

    public partial class HtmlResultsTemplateTime
    {
        private Event theEvent;
        private Dictionary<string, List<TimeResult>> distanceResults = new Dictionary<string, List<TimeResult>>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
        private Dictionary<string, int> maxLoops = new Dictionary<string, int>();
        private Dictionary<(int, int), TimeResult> LoopResults = new Dictionary<(int, int), TimeResult>();
        private Dictionary<int, int> RunnerLoopsCompleted = new Dictionary<int, int>();
        private Dictionary<string, double> DivisionDistancePerLoop = new Dictionary<string, double>();
        private Dictionary<string, string> DivisionDistanceType = new Dictionary<string, string>();

        public HtmlResultsTemplateTime(Event theEvent,
            List<TimeResult> finalLoopList,
            Dictionary<int, Participant> participantDictionary,
            Dictionary<string, int> maxLoops,
            Dictionary<(int,int), TimeResult> LoopResults,
            Dictionary<int,int> RunnerLoopsCompleted,
            Dictionary<string,double> DivisionDistancePerLoop,
            Dictionary<string,string> DivisionDistanceType
            )
        {
            this.maxLoops = maxLoops;
            this.theEvent = theEvent;
            finalLoopList.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            finalLoopList.RemoveAll(x => !RunnerLoopsCompleted.ContainsKey(x.EventSpecificId) || RunnerLoopsCompleted[x.EventSpecificId] != x.Occurrence);
            finalLoopList.Sort(TimeResult.CompareByDistancePlace);
            foreach (TimeResult result in finalLoopList)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new List<TimeResult>();
                }
                distanceResults[result.DistanceName].Add(result);
            }
            this.LoopResults = LoopResults;
            this.participantDictionary = participantDictionary;
            this.RunnerLoopsCompleted = RunnerLoopsCompleted;
            this.DivisionDistancePerLoop = DivisionDistancePerLoop;
            this.DivisionDistanceType = DivisionDistanceType;
        }
    }
}
