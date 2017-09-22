using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class TimeResult
    {
        int eventIdentifier, eventParticipantId, timingPointId, time;

        public TimeResult(int ei, int epi, int tpi, int time)
        {
            this.eventIdentifier = ei;
            this.eventParticipantId = epi;
            this.timingPointId = tpi;
            this.time = time;
        }

        public int EventSpecificId { get => eventParticipantId; }
        public int TimingPointId { get => timingPointId; }
        public int Time { get => time;  }
        public int EventIdentifier { get => eventIdentifier; }
    }
}
