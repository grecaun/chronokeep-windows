using Chronokeep.Objects;
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
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                distanceLock.AcquireReaderLock(lockTimeout);
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
                Participant output = database.AddParticipant(person);
                if (distances.TryGetValue(output.EventSpecific.DistanceIdentifier, out Distance dist))
                {
                    output.EventSpecific.DistanceName = dist.Name;
                }
                participants[output.EventSpecific.Identifier] = output;
                participantsLock.ReleaseWriterLock();
                distanceLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<Participant> AddParticipants(List<Participant> people)
        {
            Log.D("MemStore", "AddParticipants");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                distanceLock.AcquireReaderLock(lockTimeout);
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
                List<Participant> output = database.AddParticipants(people);
                foreach (Participant person in output)
                {
                    if (distances.TryGetValue(person.EventSpecific.DistanceIdentifier, out Distance dist))
                    {
                        person.EventSpecific.DistanceName = dist.Name;
                    }
                    participants[person.EventSpecific.Identifier] = person;
                }
                participantsLock.ReleaseWriterLock();
                distanceLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public Participant GetParticipant(int eventIdentifier, int identifier)
        {
            Log.D("MemStore", "GetParticipant");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                Participant output = null;
                foreach (Participant person in participants.Values)
                {
                    if (person.Identifier == identifier)
                    {
                        output = person;
                        break;
                    }
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public Participant GetParticipant(int eventIdentifier, Participant unknown)
        {
            Log.D("MemStore", "GetParticipant");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                Participant output = null;
                foreach (Participant person in participants.Values)
                {
                    if (unknown.Chip.Length > 0 && person.Chip.Equals(unknown.Chip, StringComparison.OrdinalIgnoreCase))
                    {
                        output = person;
                        break;
                    }
                    else if (unknown.FirstName.Equals(person.FirstName, StringComparison.OrdinalIgnoreCase)
                        && unknown.LastName.Equals(person.LastName, StringComparison.OrdinalIgnoreCase)
                        && unknown.Street.Equals(person.Street, StringComparison.OrdinalIgnoreCase)
                        && unknown.City.Equals(person.City, StringComparison.OrdinalIgnoreCase)
                        && unknown.State.Equals(person.State, StringComparison.OrdinalIgnoreCase)
                        && unknown.Zip.Equals(person.Zip, StringComparison.OrdinalIgnoreCase)
                        && unknown.Birthdate.Equals(person.Birthdate, StringComparison.OrdinalIgnoreCase))
                    {
                        output = person;
                        break;
                    }
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public Participant GetParticipantBib(int eventIdentifier, string bib)
        {
            Log.D("MemStore", "GetParticipantBib");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                Participant output = null;
                foreach (Participant person in participants.Values)
                {
                    if (person.Bib.Equals(bib, StringComparison.OrdinalIgnoreCase))
                    {
                        output = person;
                        break;
                    }
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId)
        {
            Log.D("MemStore", "GetParticipantEventSpecific");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                Participant output = null;
                foreach (Participant person in participants.Values)
                {
                    if (person.EventSpecific.Identifier == eventSpecificId)
                    {
                        output = person;
                        break;
                    }
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public int GetParticipantID(Participant person)
        {
            Log.D("MemStore", "GetParticipantID");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                int output = -1;
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
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<Participant> GetParticipants()
        {
            Log.D("MemStore", "GetParticipants");
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                List<Participant> output = new();
                output.AddRange(participants.Values);
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<Participant> GetParticipants(int eventId)
        {
            Log.D("MemStore", "GetParticipants");
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
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                List<Participant> output = new();
                output.AddRange(participants.Values);
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<Participant> GetParticipants(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetParticipants");
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
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                List<Participant> output = new();
                foreach (Participant person in participants.Values)
                {
                    if (person.EventSpecific.DistanceIdentifier == distanceId)
                    {
                        output.Add(person);
                    }
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveParticipant(int identifier)
        {
            Log.D("MemStore", "RemoveParticipant");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveParticipant(identifier);
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
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveParticipantEntries(List<Participant> parts)
        {
            Log.D("MemStore", "RemoveParticipantEntries");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveParticipantEntries(parts);
                foreach (Participant person in parts)
                {
                    participants.Remove(person.EventSpecific.Identifier);
                }
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveParticipantEntry(Participant person)
        {
            Log.D("MemStore", "RemoveParticipantEntries");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveParticipantEntry(person);
                participants.Remove(person.EventSpecific.Identifier);
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void UpdateParticipant(Participant person)
        {
            Log.D("MemStore", "UpdateParticipant");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
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
                database.UpdateParticipant(person);
                if (participants.TryGetValue(person.EventSpecific.Identifier, out Participant toUpdate))
                {
                    toUpdate.CopyFrom(person);
                }
                else
                {
                    participants[person.EventSpecific.Identifier] = person;
                }
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void UpdateParticipants(List<Participant> parts)
        {
            Log.D("MemStore", "UpdateParticipants");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
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
                database.UpdateParticipants(parts);
                foreach (Participant p in parts)
                {
                    if (participants.TryGetValue(p.EventSpecific.Identifier, out Participant toUpdate))
                    {
                        toUpdate.CopyFrom(p);
                    }
                    else
                    {
                        participants[p.EventSpecific.Identifier] = p;
                    }
                }
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }
    }
}
