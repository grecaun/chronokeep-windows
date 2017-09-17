using System;
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
        void RemoveEvent(String identifier);
        void RemoveEvent(Event anEvent);
        void UpdateEvent(Event anEvent);

        void AddDivision(Division div);
        void RemoveDivision(String identifier);
        void RemoveDivision(Division div);
        void UpdateDivision(Division div);

        void AddTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(TimingPoint tp);
        void RemoveTimingPoint(String identifier);
        void UpdateTimingPoint(TimingPoint tp);

        void AddParticipant(Participant person);
        void RemoveParticipant(String identifier);
        void RemoveParticipant(Participant person);
        void UpdateParticipant(Participant person);

        void AddTimingResult(TimeResult tr);
        void RemoveTimingResult(TimeResult tr);
        void UpdateTimingResult(TimeResult oldResult, TimeResult newResult);

        void ConnectionInformation(String info);
    }
}
