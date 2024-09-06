﻿using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * ChipRead Functions
         */

        public int AddChipRead(ChipRead read)
        {
            Log.D("MemStore", "AddChipRead");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                bibChipLock.AcquireReaderLock(lockTimeout);
                locationsLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireReaderLock(lockTimeout);
                eventLock.AcquireReaderLock(lockTimeout);
                read.ReadId = database.AddChipRead(read);
                DateTime start = DateTime.Now;
                if (theEvent != null)
                {
                    start = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
                }
                read.Start = start;
                if (chipToBibAssociations.TryGetValue(read.ChipNumber, out BibChipAssociation ba))
                {
                    read.ChipBib = ba.Bib;
                }
                else
                {
                    read.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                }
                read.ReadBib ??= Constants.Timing.CHIPREAD_DUMMYBIB;
                if (locations.TryGetValue(read.LocationID, out TimingLocation loc))
                {
                    read.LocationName = loc.Name;
                }
                else
                {
                    read.LocationName = "";
                }
                Dictionary<string, Participant> partDictionary = new Dictionary<string, Participant>();
                foreach (Participant part in participants.Values)
                {
                    if (part.Bib.Length > 0)
                    {
                        partDictionary[part.Bib] = part;
                    }
                }
                if (partDictionary.TryGetValue(read.Bib, out Participant p))
                {
                    read.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                }
                else
                {
                    read.Name = "";
                }
                chipReads[read.ReadId] = read;
                chipReadsLock.ReleaseWriterLock();
                bibChipLock.ReleaseReaderLock();
                locationsLock.ReleaseReaderLock();
                participantsLock.ReleaseReaderLock();
                eventLock.ReleaseReaderLock();
                return read.ReadId;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public List<ChipRead> AddChipReads(List<ChipRead> reads)
        {
            Log.D("MemStore", "AddChipReads");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                bibChipLock.AcquireReaderLock(lockTimeout);
                locationsLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireReaderLock(lockTimeout);
                Dictionary<string, Participant> partDictionary = new Dictionary<string, Participant>();
                foreach (Participant part in participants.Values)
                {
                    if (part.Bib.Length > 0)
                    {
                        partDictionary[part.Bib] = part;
                    }
                }
                DateTime start = DateTime.Now;
                if (theEvent != null)
                {
                    start = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
                }
                List<ChipRead> newReads = database.AddChipReads(reads);
                foreach (ChipRead read in newReads)
                {
                    read.Start = start;
                    chipReads[read.ReadId] = read;
                    if (chipToBibAssociations.TryGetValue(read.ChipNumber, out BibChipAssociation ba))
                    {
                        read.ChipBib = ba.Bib;
                    }
                    else
                    {
                        read.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                    }
                    read.ReadBib ??= Constants.Timing.CHIPREAD_DUMMYBIB;
                    if (locations.TryGetValue(read.LocationID, out TimingLocation loc))
                    {
                        read.LocationName = loc.Name;
                    }
                    else
                    {
                        read.LocationName = "";
                    }
                    if (partDictionary.TryGetValue(read.Bib, out Participant p))
                    {
                        read.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                    }
                    else
                    {
                        read.Name = "";
                    }
                }
                chipReadsLock.ReleaseWriterLock();
                bibChipLock.ReleaseReaderLock();
                locationsLock.ReleaseReaderLock();
                participantsLock.ReleaseReaderLock();
                return newReads;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public void DeleteChipReads(List<ChipRead> reads)
        {
            Log.D("MemStore", "DeleteChipReads");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                database.DeleteChipReads(reads);
                foreach (ChipRead read in reads)
                {
                    chipReads.Remove(read.ReadId);
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public List<ChipRead> GetChipReads()
        {
            Log.D("MemStore", "GetChipReads");
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                output.AddRange(chipReads.Values);
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetAnnouncerChipReads(int eventId)
        {
            Log.D("MemStore", "GetAnnouncerChipReads");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                foreach (ChipRead read in chipReads.Values)
                {
                    if (Constants.Timing.LOCATION_ANNOUNCER == read.LocationID
                        && Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        output.Add(read);
                    }
                }
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetAnnouncerUsedChipReads(int eventId)
        {
            Log.D("MemStore", "GetAnnouncerUsedChipReads");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                foreach (ChipRead read in chipReads.Values)
                {
                    if (Constants.Timing.LOCATION_ANNOUNCER == read.LocationID
                        && Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED == read.Status)
                    {
                        output.Add(read);
                    }
                }
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetChipReads(int eventId)
        {
            Log.D("MemStore", "GetChipReads");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                output.AddRange(chipReads.Values);
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetChipReadsSafemode(int eventId)
        {
            Log.D("MemStore", "GetChipReadsSafemode");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                output.AddRange(chipReads.Values);
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetDNSChipReads(int eventId)
        {
            Log.D("MemStore", "GetDNSChipReads");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                foreach (ChipRead read in chipReads.Values)
                {
                    if (Constants.Timing.CHIPREAD_STATUS_DNS == read.Status)
                    {
                        output.Add(read);
                    }
                }
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public List<ChipRead> GetUsefulChipReads(int eventId)
        {
            Log.D("MemStore", "GetUsefulChipReads");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
            List<ChipRead> output = new();
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                foreach (ChipRead read in chipReads.Values)
                {
                    if (read.IsUseful() && Constants.Timing.LOCATION_ANNOUNCER != read.LocationID)
                    {
                        output.Add(read);
                    }
                }
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
            return output;
        }

        public void SetChipReadStatus(ChipRead read)
        {
            Log.D("MemStore", "SetChipReadStatus");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                database.SetChipReadStatus(read);
                if (chipReads.TryGetValue(read.ReadId, out ChipRead known))
                {
                    known.Status = read.Status;
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public void SetChipReadStatuses(List<ChipRead> reads)
        {
            Log.D("MemStore", "SetChipReadStatuses");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                database.SetChipReadStatuses(reads);
                foreach (ChipRead read in reads)
                {
                    if (chipReads.TryGetValue(read.ReadId, out ChipRead known))
                    {
                        known.Status = read.Status;
                    }
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public void UpdateChipRead(ChipRead read)
        {
            Log.D("MemStore", "UpdateChipRead");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                database.UpdateChipRead(read);
                if (chipReads.TryGetValue(read.ReadId, out ChipRead known))
                {
                    known.Status = read.Status;
                    known.TimeSeconds = read.TimeSeconds;
                    known.TimeMilliseconds = read.TimeMilliseconds;
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public void UpdateChipReads(List<ChipRead> reads)
        {
            Log.D("MemStore", "UpdateChipReads");
            try
            {
                chipReadsLock.AcquireWriterLock(lockTimeout);
                database.UpdateChipReads(reads);
                foreach (ChipRead read in reads)
                {
                    if (chipReads.TryGetValue(read.ReadId, out ChipRead known))
                    {
                        known.Status = read.Status;
                        known.TimeSeconds = read.TimeSeconds;
                        known.TimeMilliseconds = read.TimeMilliseconds;
                    }
                }
                chipReadsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }

        public bool UnprocessedReadsExist(int eventId)
        {
            Log.D("MemStore", "UnprocessedReadsExist");
            bool output = false;
            try
            {
                chipReadsLock.AcquireReaderLock(lockTimeout);
                foreach (ChipRead read in chipReads.Values)
                {
                    if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        output = true;
                        break;
                    }
                }
                chipReadsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring chipReadsLock. " + e.Message);
                throw new MutexLockException("chipReadsLock");
            }
        }
    }
}
