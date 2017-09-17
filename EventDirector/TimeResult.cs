using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class TimeResult
    {
        String eventParticipantId, timingPointId, time;

        public string EventParticipantId { get => eventParticipantId; set => eventParticipantId = value; }
        public string TimingPointId { get => timingPointId; set => timingPointId = value; }
        public string Time { get => time; set => time = value; }
    }
}
