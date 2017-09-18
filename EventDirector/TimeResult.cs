using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class TimeResult
    {
        int eventIdentifier, eventParticipantId, timingPointId, time;

        public int EventSpecificId { get => eventParticipantId; set => eventParticipantId = value; }
        public int TimingPointId { get => timingPointId; set => timingPointId = value; }
        public int Time { get => time; set => time = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
    }
}
