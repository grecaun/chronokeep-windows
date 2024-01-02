using Chronokeep.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep
{
    public interface IDBInterface
    {
        // Database Functions
        void Initialize();
        void ResetDatabase();
        void HardResetDatabase();

        // Settings functions
        AppSetting GetAppSetting(string name);
        void SetAppSetting(string name, string value);
        void SetAppSetting(AppSetting setting);

        // Results API Functions
        int AddResultsAPI(ResultsAPI anAPI);
        void UpdateResultsAPI(ResultsAPI anAPI);
        void RemoveResultsAPI(int identifier);
        ResultsAPI GetResultsAPI(int identifier);
        List<ResultsAPI> GetAllResultsAPI();

        // Event Functions
        void AddEvent(Event anEvent);
        void RemoveEvent(int identifier);
        void RemoveEvent(Event anEvent);
        void UpdateEvent(Event anEvent);
        int GetEventID(Event anEvent);
        void SetStartWindow(Event anEvent);
        void SetFinishOptions(Event anEvent);
        Event GetCurrentEvent();
        Event GetEvent(int id);
        List<Event> GetEvents();

        // Distance Functions
        void AddDistance(Distance div);
        void AddDistances(List<Distance> distances);
        void RemoveDistance(int identifier);
        void RemoveDistance(Distance div);
        void UpdateDistance(Distance div);
        int GetDistanceID(Distance div);
        List<Distance> GetDistances(int eventId);
        Distance GetDistance(int divId);
        void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds);

        // Timing Location Functions
        void AddTimingLocation(TimingLocation tp);
        void AddTimingLocations(List<TimingLocation> locations);
        void RemoveTimingLocation(TimingLocation tp);
        void RemoveTimingLocation(int identifier);
        void UpdateTimingLocation(TimingLocation tp);
        int GetTimingLocationID(TimingLocation tp);
        List<TimingLocation> GetTimingLocations(int eventId);

        // Segment Functions
        void AddSegment(Segment seg);
        void AddSegments(List<Segment> segments);
        void RemoveSegment(Segment seg);
        void RemoveSegment(int identifier);
        void RemoveSegments(List<Segment> segments);
        void UpdateSegment(Segment seg);
        void UpdateSegments(List<Segment> segments);
        int GetSegmentId(Segment seg);
        List<Segment> GetSegments(int eventId);
        void ResetSegments(int eventId);
        int GetMaxSegments(int eventId);

        // Participant Functions
        void AddParticipant(Participant person);
        void AddParticipants(List<Participant> people);
        void RemoveEntry(int eventId, int participantId);
        void RemoveEntry(Participant person);
        void RemoveEntries(List<Participant> people);
        void RemoveParticipant(int identifier);
        void RemoveParticipantEntry(Participant person);
        void RemoveParticipantEntries(List<Participant> participants);
        void UpdateParticipant(Participant person);
        void UpdateParticipants(List<Participant> participants);
        int GetParticipantID(Participant person);
        List<Participant> GetParticipants();
        List<Participant> GetParticipants(int eventId);
        List<Participant> GetParticipants(int eventId, int distanceId);
        Participant GetParticipant(int eventIdentifier, int identifier);
        Participant GetParticipant(int eventIdentifier, Participant unknown);
        Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId);
        Participant GetParticipantBib(int eventIdentifier, string bib);

        // Bib Chip Association Functions
        void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc);
        List<BibChipAssociation> GetBibChips();
        List<BibChipAssociation> GetBibChips(int eventId);
        void RemoveBibChipAssociation(int eventId, string chip);
        void RemoveBibChipAssociation(BibChipAssociation assoc);
        void RemoveBibChipAssociations(List<BibChipAssociation> assocs);

        // Timing Result Functions
        void AddTimingResult(TimeResult tr);
        void AddTimingResults(List<TimeResult> results);
        void RemoveTimingResult(TimeResult tr);
        List<TimeResult> GetTimingResults(int eventId);
        List<TimeResult> GetStartTimes(int eventId);
        List<TimeResult> GetFinishTimes(int eventId);
        List<TimeResult> GetSegmentTimes(int eventId, int segmentId);
        bool UnprocessedReadsExist(int eventId);
        bool UnprocessedResultsExist(int eventId);
        void SetUploadedTimingResults(List<TimeResult> results);
        List<TimeResult> GetNonUploadedResults(int eventId);

        // Timing analytics... sort of
        List<DistanceStat> GetDistanceStats(int eventId);
        Dictionary<int, List<Participant>> GetDistanceParticipantsStatus(int eventId, int distanceId);

        // Reset functions for ChipReads/TimeResults
        void ResetTimingResultsEvent(int eventId);                      // event based reset
        void ResetTimingResultsPlacements(int eventId);

        // Chip Read Functions
        void AddChipRead(ChipRead read);
        void AddChipReads(List<ChipRead> reads);
        void UpdateChipRead(ChipRead read);
        void UpdateChipReads(List<ChipRead> reads);
        void SetChipReadStatus(ChipRead read);
        void SetChipReadStatuses(List<ChipRead> reads);
        void DeleteChipReads(List<ChipRead> reads);
        List<ChipRead> GetChipReads();
        List<ChipRead> GetChipReads(int eventId);
        List<ChipRead> GetUsefulChipReads(int eventId);
        List<ChipRead> GetAnnouncerChipReads(int eventId);
        List<ChipRead> GetAnnouncerUsedChipReads(int eventId);
        List<ChipRead> GetDNSChipReads(int eventId);

        // Age Group Functions
        void AddAgeGroup(AgeGroup group);
        void AddAgeGroups(List<AgeGroup> groups);
        void UpdateAgeGroup(AgeGroup group);
        void RemoveAgeGroup(AgeGroup group);
        void RemoveAgeGroups(int eventId, int distanceId);
        void RemoveAgeGroups(List<AgeGroup> groups);
        void ResetAgeGroups(int eventId);
        List<AgeGroup> GetAgeGroups(int eventId);
        List<AgeGroup> GetAgeGroups(int eventId, int distanceId);

        // Timing Systems
        void AddTimingSystem(TimingSystem system);
        void UpdateTimingSystem(TimingSystem system);
        void SetTimingSystems(List<TimingSystem> systems);
        void RemoveTimingSystem(TimingSystem system);
        void RemoveTimingSystem(int systemId);
        List<TimingSystem> GetTimingSystems();
    }
}
