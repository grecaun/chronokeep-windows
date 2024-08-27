using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Event Functions
         */

        public int AddEvent(Event anEvent)
        {
            Log.D("MemStore", "AddEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                int output = database.AddEvent(anEvent);
                anEvent.Identifier = output;
                allEvents.Add(anEvent);
                eventLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public Event GetCurrentEvent()
        {
            Log.D("MemStore", "GetCurrentEvent");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                Event output = theEvent;
                eventLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void SetCurrentEvent(int eventID)
        {
            Log.D("MemStore", "SetCurrentEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.SetCurrentEvent(eventID);
                LoadEvent();
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public Event GetEvent(int id)
        {
            Log.D("MemStore", "GetEvent");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                Event output = null;
                foreach (Event ev in allEvents)
                {
                    if (ev.Identifier == id)
                    {
                        output = ev;
                        break;
                    }
                }
                eventLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public int GetEventID(Event anEvent)
        {
            Log.D("MemStore", "GetEventID");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                int output = -1;
                foreach (Event ev in allEvents)
                {
                    if (ev.Name.Equals(anEvent.Name, StringComparison.OrdinalIgnoreCase) && ev.Date.Equals(anEvent.Date, StringComparison.OrdinalIgnoreCase))
                    {
                        output = ev.Identifier;
                        break;
                    }
                }
                eventLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public List<Event> GetEvents()
        {
            Log.D("MemStore", "GetEventID");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                List<Event> output = new();
                output.AddRange(allEvents);
                eventLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void RemoveEvent(int identifier)
        {
            Log.D("MemStore", "RemoveEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.RemoveEvent(identifier);
                allEvents.RemoveAll(x => x.Identifier == identifier);
                if (theEvent.Identifier == identifier)
                {
                    LoadEvent();
                }
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void RemoveEvent(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.RemoveEvent(anEvent);
                allEvents.RemoveAll(x => x.Identifier == anEvent.Identifier);
                if (theEvent.Identifier == anEvent.Identifier)
                {
                    LoadEvent();
                }
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void UpdateEvent(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.UpdateEvent(anEvent);
                if (theEvent.Identifier == anEvent.Identifier)
                {
                    theEvent.CopyFrom(anEvent);
                }
                foreach (Event ev in allEvents)
                {
                    if (ev.Identifier == anEvent.Identifier)
                    {
                        ev.CopyFrom(anEvent);
                        break;
                    }
                }
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void SetFinishOptions(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.SetFinishOptions(anEvent);
                if (theEvent.Identifier == anEvent.Identifier)
                {
                    theEvent.FinishIgnoreWithin = anEvent.FinishIgnoreWithin;
                    theEvent.FinishMaxOccurrences = anEvent.FinishMaxOccurrences;
                }
                foreach (Event ev in allEvents)
                {
                    if (ev.Identifier == anEvent.Identifier)
                    {
                        ev.FinishIgnoreWithin = anEvent.FinishIgnoreWithin;
                        ev.FinishMaxOccurrences = anEvent.FinishMaxOccurrences;
                        break;
                    }
                }
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }

        public void SetStartWindow(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            try
            {
                eventLock.AcquireWriterLock(lockTimeout);
                database.SetStartWindow(anEvent);
                if (theEvent.Identifier == anEvent.Identifier)
                {
                    theEvent.StartWindow = anEvent.StartWindow;
                }
                foreach (Event ev in allEvents)
                {
                    if (ev.Identifier == anEvent.Identifier)
                    {
                        ev.StartWindow = anEvent.StartWindow;
                        break;
                    }
                }
                eventLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
        }
    }
}
