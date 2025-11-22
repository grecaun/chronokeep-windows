using Chronokeep.Database;
using Chronokeep.Helpers;
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
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            foreach (BibChipAssociation bc in assoc)
                            {
                                chipToBibAssociations[bc.Chip] = bc;
                                if (!bibToChipAssociations.TryGetValue(bc.Bib, out Dictionary<string, BibChipAssociation> chipDict))
                                {
                                    bibToChipAssociations[bc.Bib] = [];
                                }
                                bibToChipAssociations[bc.Bib][bc.Chip] = bc;
                            }
                            Dictionary<string, Participant> bibPartDict = [];
                            foreach (Participant part in participants.Values)
                            {
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
                        else if (eventId == -1)
                        {
                            foreach (BibChipAssociation soc in assoc)
                            {
                                ignoredChips[soc.Chip] = soc;
                            }
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.E("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public List<BibChipAssociation> GetBibChips()
        {
            Log.D("MemStore", "GetBibChips");
            List<BibChipAssociation> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        output.AddRange(chipToBibAssociations.Values);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public List<BibChipAssociation> GetBibChips(int eventId)
        {
            Log.D("MemStore", "GetBibChips");
            List<BibChipAssociation> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            output.AddRange(chipToBibAssociations.Values);
                        }
                        else if (eventId == -1)
                        {
                            output.AddRange(ignoredChips.Values);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public void RemoveBibChipAssociation(int eventId, string chip)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociation(eventId, chip);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
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
                        else if (eventId == -1)
                        {
                            ignoredChips.Remove(chip);
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public void RemoveBibChipAssociation(BibChipAssociation assoc)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociation(assoc);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == assoc.EventId)
                        {
                            chipToBibAssociations.Remove(assoc.Chip);
                            bibToChipAssociations.Remove(assoc.Bib);
                        }
                        else if (assoc.EventId == -1)
                        {
                            ignoredChips.Remove(assoc.Chip);
                        }
                        Dictionary<string, Participant> bibPartDict = new();
                        foreach (Participant part in participants.Values)
                        {
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
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public void RemoveBibChipAssociations(List<BibChipAssociation> assocs)
        {
            Log.D("MemStore", "RemoveBibChipAssociation");
            database.RemoveBibChipAssociations(assocs);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (BibChipAssociation assoc in assocs)
                        {
                            if (theEvent != null && theEvent.Identifier == assoc.EventId)
                            {
                                chipToBibAssociations.Remove(assoc.Chip);
                                bibToChipAssociations.Remove(assoc.Bib);
                            }
                            else if (assoc.EventId == -1)
                            {
                                ignoredChips.Remove(assoc.Chip);
                            }
                        }
                        Dictionary<string, Participant> bibPartDict = new();
                        foreach (Participant part in participants.Values)
                        {
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
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }
    }
}
