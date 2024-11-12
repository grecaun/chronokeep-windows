﻿using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Participant Functions
         */

        public Participant AddParticipant(Participant person)
        {
            Log.D("MemStore", "AddParticipant");
            Participant output = null;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent.CommonAgeGroups)
                    {
                        if (currentAgeGroups.TryGetValue(
                            (Constants.Timing.COMMON_AGEGROUPS_DISTANCEID, person.GetAge(theEvent.Date)),
                            out AgeGroup ageGroup))
                        {
                            person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                            person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                        }
                    }
                    else
                    {
                        if (currentAgeGroups.TryGetValue(
                            (person.EventSpecific.DistanceIdentifier, person.GetAge(theEvent.Date)),
                            out AgeGroup ageGroup))
                        {
                            person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                            person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                        }
                    }
                    output = database.AddParticipant(person);
                    if (distances.TryGetValue(output.EventSpecific.DistanceIdentifier, out Distance dist))
                    {
                        output.EventSpecific.DistanceName = dist.Name;
                    }
                    participants[output.EventSpecific.Identifier] = output;
                    if (output.Bib.Length > 0)
                    {
                        foreach (ChipRead read in chipReads.Values)
                        {
                            if (read.Bib.Equals(output.Bib))
                            {
                                read.Name = string.Format("{0} {1}", output.FirstName, output.LastName).Trim();
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
            return output;
        }

        public List<Participant> AddParticipants(List<Participant> people)
        {
            Log.D("MemStore", "AddParticipants");
            List<Participant> output = new();
            Dictionary<string, List<ChipRead>> baseChipReads = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Participant person in people)
                    {
                        if (theEvent.CommonAgeGroups)
                        {
                            if (currentAgeGroups.TryGetValue(
                                (Constants.Timing.COMMON_AGEGROUPS_DISTANCEID, person.GetAge(theEvent.Date)),
                                out AgeGroup ageGroup))
                            {
                                person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                                person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                            }
                        }
                        else
                        {
                            if (currentAgeGroups.TryGetValue(
                                (person.EventSpecific.DistanceIdentifier, person.GetAge(theEvent.Date)),
                                out AgeGroup ageGroup))
                            {
                                person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                                person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                            }
                        }
                    }
                    output.AddRange(database.AddParticipants(people));
                    foreach (ChipRead read in chipReads.Values)
                    {
                        if (!baseChipReads.TryGetValue(read.Bib, out List<ChipRead> chipReadList))
                        {
                            chipReadList = new List<ChipRead>();
                        }
                        chipReadList.Add(read);
                    }
                    foreach (Participant person in output)
                    {
                        if (distances.TryGetValue(person.EventSpecific.DistanceIdentifier, out Distance dist))
                        {
                            person.EventSpecific.DistanceName = dist.Name;
                        }
                        participants[person.EventSpecific.Identifier] = person;
                        if (baseChipReads.TryGetValue(person.Bib, out List<ChipRead> lChipReads))
                        {
                            foreach (ChipRead read in lChipReads)
                            {
                                read.Name = string.Format("{0} {1}", person.FirstName, person.LastName).Trim();
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
            return output;
        }

        public Participant GetParticipant(int eventId, int identifier)
        {
            Log.D("MemStore", "GetParticipant");
            Participant output = null;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (Participant person in participants.Values)
                        {
                            if (person.Identifier == identifier)
                            {
                                output = person;
                                break;
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
            return output;
        }

        public Participant GetParticipant(int eventId, Participant unknown)
        {
            Log.D("MemStore", "GetParticipant");
            Participant output = null;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (Participant person in participants.Values)
                        {
                            if (unknown.Chip.Length > 0 && person.Chip.Equals(unknown.Chip, StringComparison.OrdinalIgnoreCase))
                            {
                                output = person;
                                break;
                            }
                            else if (unknown.FirstName != null && unknown.FirstName.Equals(person.FirstName, StringComparison.OrdinalIgnoreCase)
                                && unknown.LastName != null && unknown.LastName.Equals(person.LastName, StringComparison.OrdinalIgnoreCase)
                                && unknown.Street != null && unknown.Street.Equals(person.Street, StringComparison.OrdinalIgnoreCase)
                                && unknown.City != null && unknown.City.Equals(person.City, StringComparison.OrdinalIgnoreCase)
                                && unknown.State != null && unknown.State.Equals(person.State, StringComparison.OrdinalIgnoreCase)
                                && unknown.Zip != null && unknown.Zip.Equals(person.Zip, StringComparison.OrdinalIgnoreCase)
                                && unknown.Birthdate != null && unknown.Birthdate.Equals(person.Birthdate, StringComparison.OrdinalIgnoreCase))
                            {
                                output = person;
                                break;
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
            return output;
        }

        public Participant GetParticipantBib(int eventId, string bib)
        {
            Log.D("MemStore", "GetParticipantBib");
            Participant output = null;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (Participant person in participants.Values)
                        {
                            if (person.Bib.Equals(bib, StringComparison.OrdinalIgnoreCase))
                            {
                                output = person;
                                break;
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
            return output;
        }

        public Participant GetParticipantEventSpecific(int eventId, int eventSpecificId)
        {
            Log.D("MemStore", "GetParticipantEventSpecific");
            Participant output = null;
            try
            {

                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (Participant person in participants.Values)
                        {
                            if (person.EventSpecific.Identifier == eventSpecificId)
                            {
                                output = person;
                                break;
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
            return output;
        }

        public int GetParticipantID(Participant person)
        {
            Log.D("MemStore", "GetParticipantID");
            int output = -1;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Participant p in participants.Values)
                    {
                        if (p.FirstName.Equals(person.FirstName, StringComparison.OrdinalIgnoreCase)
                            && p.LastName.Equals(person.LastName, StringComparison.OrdinalIgnoreCase)
                            && p.Street.Equals(person.Street, StringComparison.OrdinalIgnoreCase)
                            && p.City.Equals(person.City, StringComparison.OrdinalIgnoreCase)
                            && p.State.Equals(person.State, StringComparison.OrdinalIgnoreCase)
                            && p.Zip.Equals(person.Zip, StringComparison.OrdinalIgnoreCase)
                            && p.Birthdate.Equals(person.Birthdate, StringComparison.OrdinalIgnoreCase))
                        {
                            output = p.Identifier;
                            break;
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
            return output;
        }

        public List<Participant> GetParticipants()
        {
            Log.D("MemStore", "GetParticipants");
            List<Participant> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    output.AddRange(participants.Values);
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

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("MemStore", "GetParticipants");
            List<Participant> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(participants.Values);
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

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetParticipants");
            List<Participant> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (Participant person in participants.Values)
                        {
                            if (person.EventSpecific.DistanceIdentifier == distanceId)
                            {
                                output.Add(person);
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
            return output;
        }

        public void RemoveParticipant(int identifier)
        {
            Log.D("MemStore", "RemoveParticipant");
            database.RemoveParticipant(identifier);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    int eventSpecId = -1;
                    foreach (Participant person in participants.Values)
                    {
                        if (person.Identifier == identifier)
                        {
                            eventSpecId = person.EventSpecific.Identifier;
                            break;
                        }
                    }
                    if (eventSpecId > 0)
                    {
                        participants.Remove(eventSpecId);
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

        public void RemoveParticipantEntries(List<Participant> parts)
        {
            Log.D("MemStore", "RemoveParticipantEntries");
            database.RemoveParticipantEntries(parts);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Participant person in parts)
                    {
                        participants.Remove(person.EventSpecific.Identifier);
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

        public void RemoveParticipantEntry(Participant person)
        {
            Log.D("MemStore", "RemoveParticipantEntries");
            database.RemoveParticipantEntry(person);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    participants.Remove(person.EventSpecific.Identifier);
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void UpdateParticipant(Participant person)
        {
            Log.D("MemStore", "UpdateParticipant");
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent.CommonAgeGroups)
                    {
                        if (currentAgeGroups.TryGetValue(
                            (Constants.Timing.COMMON_AGEGROUPS_DISTANCEID, person.GetAge(theEvent.Date)),
                            out AgeGroup ageGroup))
                        {
                            person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                            person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                        }
                    }
                    else
                    {
                        if (currentAgeGroups.TryGetValue(
                            (person.EventSpecific.DistanceIdentifier, person.GetAge(theEvent.Date)),
                            out AgeGroup ageGroup))
                        {
                            person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                            person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                        }
                    }
                    // This is one of the few functions that has a database call within the ReadWriteLock
                    // This is because we need to update the age group id before putting it in the database
                    database.UpdateParticipant(person);
                    Dictionary<string, List<ChipRead>> baseChipReads = new();
                    foreach (ChipRead read in chipReads.Values)
                    {
                        if (!baseChipReads.TryGetValue(read.Bib, out List<ChipRead> chipReadList))
                        {
                            chipReadList = new List<ChipRead>();
                        }
                        chipReadList.Add(read);
                    }
                    if (participants.TryGetValue(person.EventSpecific.Identifier, out Participant toUpdate))
                    {
                        toUpdate.CopyFrom(person);
                        if (distances.TryGetValue(toUpdate.EventSpecific.DistanceIdentifier, out Distance dist))
                        {
                            toUpdate.EventSpecific.DistanceName = dist.Name;
                        }
                        if (toUpdate.Bib.Length > 0 && baseChipReads.TryGetValue(toUpdate.Bib, out List<ChipRead> lChipReads))
                        {
                            foreach (ChipRead read in lChipReads)
                            {
                                read.Name = string.Format("{0} {1}", toUpdate.FirstName, toUpdate.LastName).Trim();
                            }
                        }
                    }
                    else
                    {
                        participants[person.EventSpecific.Identifier] = person;
                        if (distances.TryGetValue(person.EventSpecific.DistanceIdentifier, out Distance dist))
                        {
                            person.EventSpecific.DistanceName = dist.Name;
                        }
                        if (person.Bib.Length > 0 && baseChipReads.TryGetValue(person.Bib, out List<ChipRead> dChipReads))
                        {
                            foreach (ChipRead read in dChipReads)
                            {
                                read.Name = string.Format("{0} {1}", person.FirstName, person.LastName).Trim();
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

        public void UpdateParticipants(List<Participant> parts)
        {
            Log.D("MemStore", "UpdateParticipants");
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Participant person in parts)
                    {
                        if (theEvent.CommonAgeGroups)
                        {
                            if (currentAgeGroups.TryGetValue(
                                (Constants.Timing.COMMON_AGEGROUPS_DISTANCEID, person.GetAge(theEvent.Date)),
                                out AgeGroup ageGroup))
                            {
                                person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                                person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                            }
                        }
                        else
                        {
                            if (currentAgeGroups.TryGetValue(
                                (person.EventSpecific.DistanceIdentifier, person.GetAge(theEvent.Date)),
                                out AgeGroup ageGroup))
                            {
                                person.EventSpecific.AgeGroupId = ageGroup.GroupId;
                                person.EventSpecific.AgeGroupName = ageGroup.PrettyName();
                            }
                        }
                    }
                    // This is one of the few functions that has a database call within the ReadWriteLock
                    // This is because we need to update the age group id before putting it in the database
                    database.UpdateParticipants(parts);
                    Dictionary<string, List<ChipRead>> baseChipReads = new();
                    foreach (ChipRead read in chipReads.Values)
                    {
                        if (!baseChipReads.TryGetValue(read.Bib, out List<ChipRead> chipReadList))
                        {
                            chipReadList = new List<ChipRead>();
                        }
                        chipReadList.Add(read);
                    }
                    foreach (Participant person in parts)
                    {
                        if (participants.TryGetValue(person.EventSpecific.Identifier, out Participant toUpdate))
                        {
                            toUpdate.CopyFrom(person);
                            if (distances.TryGetValue(toUpdate.EventSpecific.DistanceIdentifier, out Distance dist))
                            {
                                toUpdate.EventSpecific.DistanceName = dist.Name;
                            }
                            if (toUpdate.Bib.Length > 0 && baseChipReads.TryGetValue(toUpdate.Bib, out List<ChipRead> lChipReads))
                            {
                                foreach (ChipRead read in lChipReads)
                                {
                                    read.Name = string.Format("{0} {1}", toUpdate.FirstName, toUpdate.LastName).Trim();
                                }
                            }
                        }
                        else
                        {
                            participants[person.EventSpecific.Identifier] = person;
                            if (distances.TryGetValue(person.EventSpecific.DistanceIdentifier, out Distance dist))
                            {
                                person.EventSpecific.DistanceName = dist.Name;
                            }
                            if (person.Bib.Length > 0 && baseChipReads.TryGetValue(person.Bib, out List<ChipRead> dChipReads))
                            {
                                foreach (ChipRead read in dChipReads)
                                {
                                    read.Name = string.Format("{0} {1}", person.FirstName, person.LastName).Trim();
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
    }
}
