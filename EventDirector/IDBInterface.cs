using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    interface IDBInterface
    {
        void Initialize();

        void AddEvent(Event anEvent);
        void RemoveEvent(int identifier);
        void RemoveEvent(Event anEvent);
        void UpdateEvent(Event anEvent);

        void AddDivision(Division div);
        void RemoveDivision(int identifier);
        void RemoveDivision(Division div);
        void UpdateDivision(Division div);

        void AddTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(int identifier);
        void UpdateTimingPoint(TimingPoint tp);

        void AddParticipant(Participant person);
        void AddParticipants(ArrayList people);
        void RemoveParticipant(int identifier);
        void RemoveParticipant(Participant person);
        void UpdateParticipant(Participant person);

        void AddTimingResult(TimeResult tr);
        void RemoveTimingResult(TimeResult tr);
        void UpdateTimingResult(TimeResult oldResult, TimeResult newResult);

        void CheckInParticipant(int identifier, int checkedIn);
        void CheckInParticipant(Participant person);

        void ResetDatabase();

        ArrayList GetEvents();
        ArrayList GetDivisions();
        ArrayList GetTimingPoints();
        ArrayList GetParticipants();
        ArrayList GetTimingResults();
    }
}
