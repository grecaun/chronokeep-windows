using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace ChronoKeep.IO.HtmlTemplates
{
    public partial class HtmlResultsTemplate
    {
        private Event theEvent;
        private Dictionary<string, List<TimeResult>> divisionResults = new Dictionary<string, List<TimeResult>>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        public HtmlResultsTemplate(IDBInterface database)
        {
            theEvent = database.GetCurrentEvent();
            List<TimeResult> resultList = database.GetFinishTimes(theEvent.Identifier);
            resultList.Sort(TimeResult.CompareByDivisionPlace);
            foreach (TimeResult result in resultList)
            {
                if (!divisionResults.ContainsKey(result.DivisionName))
                {
                    divisionResults[result.DivisionName] = new List<TimeResult>();
                }
                divisionResults[result.DivisionName].Add(result);
            }
            foreach (Participant person in database.GetParticipants(theEvent.Identifier))
            {
                participantDictionary[person.EventSpecific.Identifier] = person;
            }
        }
    }
}
