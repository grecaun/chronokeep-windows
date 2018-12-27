using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class TimeResult
    {
        int eventIdentifier, eventParticipantId, locationId, segment_id, occurrence;
        string time;

        public TimeResult(int ei, int epi, int tpi, int segi, string time, int occurrence)
        {
            this.eventIdentifier = ei;
            this.eventParticipantId = epi;
            this.locationId = tpi;
            this.segment_id = segi;
            this.time = time;
            this.occurrence = occurrence;
        }

        public int EventSpecificId { get => eventParticipantId; set => eventParticipantId = value; }
        public int LocationId { get => locationId; set => locationId = value; }
        public string Time { get => time; set => time = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int SegmentId { get => segment_id; set => segment_id = value; }
        public int Occurrence { get => occurrence; set => occurrence = value; }
    }
}
