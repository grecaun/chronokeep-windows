using ChronoKeep.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public interface IDBInterface
    {
        // Database Functions
        void Initialize();
        void ResetDatabase();
        void HardResetDatabase();

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
        void SetEventOptions(int eventId, List<JsonOption> options);
        void SetStartWindow(Event anEvent);
        void SetFinishOptions(Event anEvent);
        Event GetCurrentEvent();
        Event GetEvent(int id);
        List<Event> GetEvents();
        List<JsonOption> GetEventOptions(int eventId);

        // Division Functions
        void AddDivision(Division div);
        void AddDivisions(List<Division> divisions);
        void RemoveDivision(int identifier);
        void RemoveDivision(Division div);
        void UpdateDivision(Division div);
        int GetDivisionID(Division div);
        List<Division> GetDivisions(int eventId);
        Division GetDivision(int divId);
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
        void CheckInParticipant(int eventId, int identifier, int checkedIn);
        void CheckInParticipant(Participant person);
        void SetEarlyStartParticipant(int eventId, int identifier, int earlystart);
        void SetEarlyStartParticipant(Participant person);
        List<Participant> GetParticipants();
        List<Participant> GetParticipants(int eventId);
        List<Participant> GetParticipants(int eventId, int divisionId);
        Participant GetParticipant(int eventIdentifier, int identifier);
        Participant GetParticipant(int eventIdentifier, Participant unknown);
        Participant GetParticipantEventSpecific(int eventIdentifier, int eventSpecificId);
        Participant GetParticipantBib(int eventIdentifier, int bib);

        // Bib Chip Association Functions
        void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc);
        List<BibChipAssociation> GetBibChips();
        List<BibChipAssociation> GetBibChips(int eventId);
        void RemoveBibChipAssociation(int eventId, string chip);
        void RemoveBibChipAssociation(BibChipAssociation assoc);
        void RemoveBibChipAssociations(List<BibChipAssociation> assocs);

        // Bib Functions
        void AddBibs(int eventId, int group, List<int> bibs);
        void AddBibs(int eventId, List<AvailableBib> bibs);
        void AddBib(int eventId, int group, int bib);
        List<AvailableBib> GetBibs(int eventId);
        void RemoveBib(int eventId, int bib);
        void RemoveBibs(List<AvailableBib> bibs);
        int LargestBib(int eventId);

        // Bib Group Functions
        void AddBibGroup(int eventId, BibGroup group);
        List<BibGroup> GetBibGroups(int eventId);
        void RemoveBibGroup(BibGroup group);

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
        List<DivisionStats> GetDivisionStats(int eventId);
        Dictionary<int, List<Participant>> GetDivisionParticipantsStatus(int eventId, int divisionId);

        // Reset functions for ChipReads/TimeResults
        void ResetTimingResultsEvent(int eventId);                      // event based reset
        void ResetTimingResultsPlacements(int eventId);

        // Day of Participant / Kiosk Functions
        void AddDayOfParticipant(DayOfParticipant part);
        void AddDayOfParticipants(List<DayOfParticipant> participants);
        DayOfParticipant GetDayOfParticipant(DayOfParticipant part);
        List<DayOfParticipant> GetDayOfParticipants(int eventId);
        List<DayOfParticipant> GetDayOfParticipants();
        bool ApproveDayOfParticipant(int eventId, int identifier, int Bib, int earlystart);
        bool ApproveDayOfParticipant(DayOfParticipant part, int Bib, int earlystart);
        void SetLiabilityWaiver(int eventId, String waiver);
        String GetLiabilityWaiver(int eventId);
        void SetPrintOption(int eventId, int print);
        int GetPrintOption(int eventId);

        // Change Functions
        void AddChange(Participant newParticipant, Participant oldParticipant);
        List<Change> GetChanges();

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

        // Age Group Functions
        void AddAgeGroup(AgeGroup group);
        void AddAgeGroups(List<AgeGroup> groups);
        void UpdateAgeGroup(AgeGroup group);
        void RemoveAgeGroup(AgeGroup group);
        void RemoveAgeGroups(int eventId, int divisionId);
        void RemoveAgeGroups(List<AgeGroup> groups);
        List<AgeGroup> GetAgeGroups(int eventId);
        List<AgeGroup> GetAgeGroups(int eventId, int divisionId);

        // Timing Systems
        void AddTimingSystem(TimingSystem system);
        void UpdateTimingSystem(TimingSystem system);
        void SetTimingSystems(List<TimingSystem> systems);
        void RemoveTimingSystem(TimingSystem system);
        void RemoveTimingSystem(int systemId);
        List<TimingSystem> GetTimingSystems();
    }
}
