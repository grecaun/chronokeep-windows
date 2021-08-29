using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    class AnnouncerParticipant
    {
        private Participant person;
        private long seconds;

        public AnnouncerParticipant(Participant person, long seconds)
        {
            this.person = person;
            this.seconds = seconds;
        }

        public Participant Person { get => person; }
        public string When { get => Constants.Timing.SecondsToTime(seconds); }

        public int CompareTo(AnnouncerParticipant other)
        {
            if (other == null) return 1;
            return this.seconds.CompareTo(other.seconds);
        }
    }
}
