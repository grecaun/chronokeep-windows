using Chronokeep.Objects;
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
            foreach (BibChipAssociation bc in assoc)
            {
                bc.TrimFields();
            }
            database.AddBibChipAssociation(eventId, assoc);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (BibChipAssociation bc in assoc)
                        {
                            chipToBibAssociations[bc.Chip] = bc;
                            bibToChipAssociations[bc.Bib] = bc;
                        }
                        Dictionary<string, Participant> bibPartDict = new();
                        foreach (Participant part in participants.Values)
                        {
                            part.Chip = "";
                            if (bibToChipAssociations.TryGetValue(part.Bib, out BibChipAssociation bc))
                            {
                                part.Chip = bc.Chip;
                            }
                            bibPartDict[part.Bib] = part;
                        }
                        foreach (ChipRead cr in chipReads.Values)
                        {
                            cr.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                            cr.Name = "";
                            if (chipToBibAssociations.TryGetValue(cr.ChipNumber, out BibChipAssociation bc))
                            {
                                cr.ChipBib = bc.Bib;
                                if (bibPartDict.TryGetValue(bc.Bib, out Participant p))
                                {
                                    cr.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                                }
                            }
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<BibChipAssociation> GetBibChips()
        {
            Log.D("MemStore", "GetBibChips");
            List<BibChipAssociation> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    output.AddRange(chipToBibAssociations.Values);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("MemStore", "GetBibChips");
            List<BibChipAssociation> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(chipToBibAssociations.Values);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociation(eventId, chip);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
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
                        Dictionary<string, Participant> bibPartDict = new();
                        foreach (Participant part in participants.Values)
                        {
                            part.Chip = "";
                            if (bibToChipAssociations.TryGetValue(part.Bib, out BibChipAssociation bc))
                            {
                                part.Chip = bc.Chip;
                            }
                            bibPartDict[part.Bib] = part;
                        }
                        foreach (ChipRead cr in chipReads.Values)
                        {
                            cr.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                            cr.Name = "";
                            if (chipToBibAssociations.TryGetValue(cr.ChipNumber, out BibChipAssociation bc))
                            {
                                cr.ChipBib = bc.Bib;
                                if (bibPartDict.TryGetValue(bc.Bib, out Participant p))
                                {
                                    cr.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                                }
                            }
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociation(assoc);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    chipToBibAssociations.Remove(assoc.Chip);
                    bibToChipAssociations.Remove(assoc.Bib);
                    Dictionary<string, Participant> bibPartDict = new();
                    foreach (Participant part in participants.Values)
                    {
                        part.Chip = "";
                        if (bibToChipAssociations.TryGetValue(part.Bib, out BibChipAssociation bc))
                        {
                            part.Chip = bc.Chip;
                        }
                        bibPartDict[part.Bib] = part;
                    }
                    foreach (ChipRead cr in chipReads.Values)
                    {
                        cr.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                        cr.Name = "";
                        if (chipToBibAssociations.TryGetValue(cr.ChipNumber, out BibChipAssociation bc))
                        {
                            cr.ChipBib = bc.Bib;
                            if (bibPartDict.TryGetValue(bc.Bib, out Participant p))
                            {
                                cr.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                            }
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociations(assocs);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (BibChipAssociation assoc in assocs)
                    {
                        chipToBibAssociations.Remove(assoc.Chip);
                        bibToChipAssociations.Remove(assoc.Bib);
                    }
                    Dictionary<string, Participant> bibPartDict = new();
                    foreach (Participant part in participants.Values)
                    {
                        part.Chip = "";
                        if (bibToChipAssociations.TryGetValue(part.Bib, out BibChipAssociation bc))
                        {
                            part.Chip = bc.Chip;
                        }
                        bibPartDict[part.Bib] = part;
                    }
                    foreach (ChipRead cr in chipReads.Values)
                    {
                        cr.ChipBib = Constants.Timing.CHIPREAD_DUMMYBIB;
                        cr.Name = "";
                        if (chipToBibAssociations.TryGetValue(cr.ChipNumber, out BibChipAssociation bc))
                        {
                            cr.ChipBib = bc.Bib;
                            if (bibPartDict.TryGetValue(bc.Bib, out Participant p))
                            {
                                cr.Name = string.Format("{0} {1}", p.FirstName, p.LastName).Trim();
                            }
                        }
                    }
                    memStoreLock.ReleaseMutex();
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
