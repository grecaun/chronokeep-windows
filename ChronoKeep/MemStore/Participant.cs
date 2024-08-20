using Chronokeep.Objects;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Participant Functions
         */

        public void AddParticipant(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void AddParticipants(List<Participant> people)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipant(int eventIdentifier, int identifier)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipant(int eventIdentifier, Participant unknown)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipantBib(int eventIdentifier, string bib)
        {
            throw new System.NotImplementedException();
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            throw new System.NotImplementedException();
        }

        public int GetParticipantID(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants()
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipant(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntries(List<Participant> participants)
        {
            // removes only the eventspecific part from the db
            throw new System.NotImplementedException();
        }

        public void RemoveParticipantEntry(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateParticipant(Participant person)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateParticipants(List<Participant> participants)
        {
            throw new System.NotImplementedException();
        }
    }
}
