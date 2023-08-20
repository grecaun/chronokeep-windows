using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.IO.HtmlTemplates
{
    public partial class HtmlResultsTemplate
    {
        private Event theEvent;
        private Dictionary<string, List<TimeResult>> distanceResults = new Dictionary<string, List<TimeResult>>();

        public HtmlResultsTemplate(
            Event theEvent,
            List<TimeResult> resultList
            )
        {
            this.theEvent = theEvent;
            resultList.Sort(TimeResult.CompareByDistancePlace);
            foreach (TimeResult result in resultList)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new List<TimeResult>();
                }
                distanceResults[result.DistanceName].Add(result);
            }
        }
    }
}
