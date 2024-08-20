using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Event Functions
         */

        public void AddEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public Event GetCurrentEvent()
        {
            throw new System.NotImplementedException();
        }

        public void SetCurrentEvent(int eventID)
        {
            throw new System.NotImplementedException();
        }

        public Event GetEvent(int id)
        {
            throw new System.NotImplementedException();
        }

        public int GetEventID(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public List<Event> GetEvents()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEvent(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateEvent(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void SetFinishOptions(Event anEvent)
        {
            throw new System.NotImplementedException();
        }

        public void SetStartWindow(Event anEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}
