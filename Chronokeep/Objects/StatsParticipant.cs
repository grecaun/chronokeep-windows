namespace Chronokeep.Objects
{
    internal class StatsParticipant
    {
        private readonly Participant Participant;
        public string LastSeen { get; }
        public string LastSeenTime { get; }
        public string Bib { get => Participant.Bib; }
        public string FirstName { get => Participant.FirstName; }
        public string LastName { get => Participant.LastName; }
        public string Gender { get => Participant.Gender; }
        public string Phone { get => Participant.Phone; }
        public string Mobile { get => Participant.Mobile; }
        public string Email { get => Participant.Email; }
        public string CurrentAge { get => Participant.CurrentAge; }

        internal StatsParticipant(Participant participant, string lastSeen, string lastSeenTime)
        {
            Participant = participant;
            LastSeen = lastSeen;
            LastSeenTime = lastSeenTime;
        }

        public Participant GetParticipant()
        {
            return Participant;
        }
    }
}
