using System;

namespace Chronokeep.Objects
{
    public class AnnouncerParticipant(Participant person, long seconds)
    {
        private readonly long seconds = seconds;
        private static Event tEvent = null;

        public static Event TheEvent { get => tEvent; set => tEvent = value; }
        public Participant Person { get => person; }
        public DateTime When { get => Constants.Timing.RFIDEpochToDate(seconds); }
        public string AnnouncerWhen { get => this.When.ToString("HH:mm:ss"); }
        public string Distance { get => person.Distance; }
        public string Bib { get => person.Bib.ToString(); }
        public string ParticipantName { get => string.Format("{0} {1}", person.FirstName, person.LastName); }
        public string CityState { get => string.Format("{0} {1}", person.City, person.State); }
        public string AgeGender { get => TheEvent == null ? string.Format("? {0}", person.Gender) : string.Format("{0} {1}", person.Age(TheEvent.Date), person.Gender); }
        public string Comments { get => person.Comments; }

        public int CompareTo(AnnouncerParticipant other)
        {
            if (other == null) return 1;
            return this.seconds.CompareTo(other.seconds);
        }
    }
}
