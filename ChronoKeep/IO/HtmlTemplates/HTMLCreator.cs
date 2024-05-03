using Chronokeep.Objects;
using System.Collections.Generic;

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

    public partial class HtmlCertificateEmailTemplate
    {
        string eventName;
        string distanceName;
        string participantName;
        string time;
        string certificateUrl;
        string resultsLink;
        string unsubscribe;

        public HtmlCertificateEmailTemplate(
            Event theEvent,
            TimeResult result,
            string email,
            bool singleDist,
            APIObject api
            )
        {
            eventName = string.Format("{0} {1}", theEvent.Year, theEvent.Name);
            distanceName = "";
            if (!singleDist)
            {
                distanceName = string.Format(" {0}", result.DistanceName); 
            }
            participantName = result.First;
            time = result.ChipTimeNoMilliseconds;
            certificateUrl = string.Format("https://api.chronokeep.com/certificate/{0} {1}/{2}{3}/{4}/{5}", result.First, result.Last, eventName, distanceName, time, theEvent.LongDate);
            resultsLink = "";
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            if (api != null && api.Type == Constants.APIConstants.CHRONOKEEP_RESULTS && event_ids.Length == 2)
            {
                resultsLink = string.Format("<p><a href=\"https://www.chronokeep.com/results/{0}/{1}\">Click here for more results.</a></p>", event_ids[0], event_ids[1]);
            }
            unsubscribe = string.Format(" If you don't want to receive these emails <a href=\"https://www.chronokeep.com/unsubscribe/{0}\">click here</a>.", email);
        }
    }
}
