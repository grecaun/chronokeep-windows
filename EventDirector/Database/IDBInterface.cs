using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public interface IDBInterface
    {
        void Initialize();

        void SetServerName(string name);
        String GetServerName();
        AppSetting GetAppSetting(string name);

        // Event Functions
        void AddEvent(Event anEvent);
        void RemoveEvent(int identifier);
        void RemoveEvent(Event anEvent);
        void UpdateEvent(Event anEvent);
        int GetEventID(Event anEvent);
        void SetEventOptions(int eventId, List<JsonOption> options);
        Event GetEvent(int id);
        List<Event> GetEvents();
        List<JsonOption> GetEventOptions(int eventId);

        // Division Functions
        void AddDivision(Division div);
        void RemoveDivision(int identifier);
        void RemoveDivision(Division div);
        void UpdateDivision(Division div);
        int GetDivisionID(Division div);
        List<Division> GetDivisions(int eventId);

        // Timing Location Functions
        void AddTimingLocation(TimingLocation tp);
        void RemoveTimingLocation(TimingLocation tp);
        void RemoveTimingLocation(int identifier);
        void UpdateTimingLocation(TimingLocation tp);
        int GetTimingLocationID(TimingLocation tp);
        List<TimingLocation> GetTimingLocations(int eventId);

        // Participant Functions
        void AddParticipant(Participant person);
        void AddParticipants(List<Participant> people);
        void RemoveParticipant(int identifier);
        void RemoveParticipant(Participant person);
        void UpdateParticipant(Participant person);
        int GetParticipantID(Participant person);
        void CheckInParticipant(int eventId, int identifier, int checkedIn);
        void CheckInParticipant(Participant person);
        void SetEarlyStartParticipant(int eventId, int identifier, int earlystart);
        void SetEarlyStartParticipant(Participant person);
        List<Participant> GetParticipants();
        List<Participant> GetParticipants(int eventId);
        Participant GetParticipant(int eventIdentifier, int identifier);
        Participant GetParticipant(int eventIdentifier, Participant unknown);

        // Bib Chip Association Functions
        void AddBibChipAssociation(int eventId, List<BibChipAssociation> assoc);

        // Timing Result Functions
        void AddTimingResult(TimeResult tr);
        void RemoveTimingResult(TimeResult tr);
        void UpdateTimingResult(TimeResult oldResult, string newTime);

        List<TimeResult> GetTimingResults(int eventId);

        // Day of Participant / Kiosk Functions
        void AddDayOfParticipant(DayOfParticipant part);
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
        List<BibChipAssociation> GetBibChips();
        List<BibChipAssociation> GetBibChips(int eventId);

        // Chip Read Functions
        void AddChipRead(ChipRead read);
        List<ChipRead> GetChipReads();
        List<ChipRead> GetChipReads(int eventId);

        // Database Functions
        void ResetDatabase();
    }
}
