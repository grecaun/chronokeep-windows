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
        private bool linkPart = false;

        public HtmlResultsTemplate(
            Event theEvent,
            List<TimeResult> resultList,
            bool linkPart = false
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
            this.linkPart = linkPart;
        }
    }
    public partial class HtmlParticipantTemplate
    {
        private Event theEvent;
        private List<TimeResult> resultList;
        private TimeResult finish;
        private TimeResult start;
        private string rankingGender = "";

        public HtmlParticipantTemplate(
            Event theEvent,
            List<TimeResult> rList
            )
        {
            this.theEvent = theEvent;
            resultList = rList;
            resultList.Sort(TimeResult.CompareBySystemTime);
            foreach (TimeResult result in resultList)
            {
                if (result.LocationId == Constants.Timing.LOCATION_FINISH)
                {
                    if (finish == null || finish.Occurrence < result.Occurrence)
                    {
                        finish = result;
                    }
                }
                if (result.SegmentId == Constants.Timing.SEGMENT_START)
                {
                    start = result;
                }
            }
            if (finish != null)
            {
                resultList.RemoveAll(r => 
                    (r.Occurrence == finish.Occurrence && r.LocationId == Constants.Timing.LOCATION_FINISH)
                    || (r.SegmentId == Constants.Timing.SEGMENT_START)
                    );
                rankingGender = finish.Gender.ToUpper();
                if (rankingGender == "WOMAN")
                {
                    rankingGender = "Women";
                }
                else if (rankingGender == "MAN")
                {
                    rankingGender = "Men";
                }
                else
                {
                    rankingGender = finish.Gender;
                }
            }
            Log.D("IO.HtmlTemplates.HtmlParticipantTemplate", "Template created.");
        }
    }
}
