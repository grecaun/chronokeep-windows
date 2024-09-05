using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * BibChipAssociation Functions
         */

        public void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc)
        {
            Log.D("MemStore", "AddBibChipAssociation");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                database.AddBibChipAssociation(eventId, assoc);
                foreach (BibChipAssociation bc in assoc)
                {
                    chipToBibAssociations[bc.Chip] = bc;
                    bibToChipAssociations[bc.Bib] = bc;
                }
                bibChipLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
        }

        public List<BibChipAssociation> GetBibChips()
        {
            Log.D("MemStore", "GetBibChips");
            List<BibChipAssociation> output = new();
            try
            {
                bibChipLock.AcquireReaderLock(lockTimeout);
                output.AddRange(chipToBibAssociations.Values);
                bibChipLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("MemStore", "GetBibChips");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            List<BibChipAssociation> output = new();
            try
            {
                bibChipLock.AcquireReaderLock(lockTimeout);
                output.AddRange(chipToBibAssociations.Values);
                bibChipLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                database.RemoveBibChipAssociation(eventId, chip);
                chipToBibAssociations.Remove(chip);
                string bib = "";
                foreach (BibChipAssociation b in chipToBibAssociations.Values)
                {
                    if (b.Chip == chip)
                    {
                        bib = b.Bib;
                        break;
                    }
                }
                if (bib.Length > 0)
                {
                    bibToChipAssociations.Remove(bib);
                }
                bibChipLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                database.RemoveBibChipAssociation(assoc);
                chipToBibAssociations.Remove(assoc.Chip);
                bibToChipAssociations.Remove(assoc.Bib);
                bibChipLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            try
            {
                bibChipLock.AcquireWriterLock(lockTimeout);
                database.RemoveBibChipAssociations(assocs);
                foreach (BibChipAssociation assoc in assocs)
                {
                    chipToBibAssociations.Remove(assoc.Chip);
                    bibToChipAssociations.Remove(assoc.Bib);
                }
                bibChipLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring bibChipLock. " + e.Message);
                throw new MutexLockException("bibChipLock");
            }
        }
    }
}
