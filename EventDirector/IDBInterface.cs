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

        void AddEvent(Event anEvent);
        void RemoveEvent(int identifier);
        void RemoveEvent(Event anEvent);
        void UpdateEvent(Event anEvent);
        int GetEventID(Event anEvent);

        void AddDivision(Division div);
        void RemoveDivision(int identifier);
        void RemoveDivision(Division div);
        void UpdateDivision(Division div);
        int GetDivisionID(Division div);

        void AddTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(int identifier);
        void UpdateTimingPoint(TimingPoint tp);
        int GetTimingPointID(TimingPoint tp);

        void AddParticipant(Participant person);
        void AddParticipants(List<Participant> people);
        void RemoveParticipant(int identifier);
        void RemoveParticipant(Participant person);
        void UpdateParticipant(Participant person);
        int GetParticipantID(Participant person);

        void AddTimingResult(TimeResult tr);
        void RemoveTimingResult(TimeResult tr);
        void UpdateTimingResult(TimeResult oldResult, TimeResult newResult);

        void CheckInParticipant(int identifier, int checkedIn);
        void CheckInParticipant(Participant person);
        void SetEarlyStartParticipant(int identifier, int earlystart);
        void SetEarlyStartParticipant(Participant person);

        void AddChange(Participant newParticipant, Participant oldParticipant);

        void ResetDatabase();

        List<Event> GetEvents();
        List<Division> GetDivisions(int eventId);
        List<TimingPoint> GetTimingPoints(int eventId);
        List<Participant> GetParticipants();
        List<Participant> GetParticipants(int eventId);
        List<TimeResult> GetTimingResults(int eventId);
        List<Change> GetChanges(int eventId);
    }
}
