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
            int output = database.AddEvent(anEvent);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    anEvent.Identifier = output;
                    allEvents.Add(anEvent);
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public Event GetCurrentEvent()
        {
            Log.D("MemStore", "GetCurrentEvent");
            Event output = null;
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    output = theEvent;
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public void SetCurrentEvent(int eventID)
        {
            Log.D("MemStore", "SetCurrentEvent");
            database.SetCurrentEvent(eventID);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    LoadEvent();
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public Event GetEvent(int id)
        {
            Log.D("MemStore", "GetEvent");
            Event output = null;
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    foreach (Event ev in allEvents)
                    {
                        if (ev.Identifier == id)
                        {
                            output = ev;
                            break;
                        }
                    }
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public int GetEventID(Event anEvent)
        {
            Log.D("MemStore", "GetEventID");
            int output = -1;
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    foreach (Event ev in allEvents)
                    {
                        if (ev.Name.Equals(anEvent.Name, StringComparison.OrdinalIgnoreCase) && ev.Date.Equals(anEvent.Date, StringComparison.OrdinalIgnoreCase))
                        {
                            output = ev.Identifier;
                            break;
                        }
                    }
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public List<Event> GetEvents()
        {
            Log.D("MemStore", "GetEventID");
            List<Event> output = new();
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    output.AddRange(allEvents);
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public void RemoveEvent(int identifier)
        {
            Log.D("MemStore", "RemoveEvent");
            database.RemoveEvent(identifier);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    allEvents.RemoveAll(x => x.Identifier == identifier);
                    if (theEvent != null && theEvent.Identifier == identifier)
                    {
                        LoadEvent();
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveEvent(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            database.RemoveEvent(anEvent);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    allEvents.RemoveAll(x => x.Identifier == anEvent.Identifier);
                    if (theEvent != null && theEvent.Identifier == anEvent.Identifier)
                    {
                        LoadEvent();
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void UpdateEvent(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            database.UpdateEvent(anEvent);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == anEvent.Identifier)
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
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void SetFinishOptions(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            database.SetFinishOptions(anEvent);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == anEvent.Identifier)
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
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void SetStartWindow(Event anEvent)
        {
            Log.D("MemStore", "RemoveEvent");
            database.SetStartWindow(anEvent);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == anEvent.Identifier)
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
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }
    }
}
