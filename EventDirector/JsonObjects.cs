using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    /*
     *
     * Classes for dealing with client queries.
     *
     */
    public class JsonClientList
    {
        public String Command { get; set; }
    }

    public class JsonClientEvent
    {
        public String Command { get; set; }
        public int EventId { get; set; }
    }

    public class JsonClientParticipants
    {
        public String Command { get; set; }
        public int EventId { get; set; }
    }

    public class JsonClientResults
    {
        public String Command { get; set; }
        public int EventId { get; set; }
    }

    public class JsonClientParticipantUpdate
    {
        public String Command { get; set; }
        public int EventId { get; set; }
        public JsonParticipant Participant { get; set; }
    }

    public class JsonClientParticipantAdd
    {
        public String Command { get; set; }
        public int EventId { get; set; }
        public JsonParticipant Participant { get; set; }
    }

    public class JsonClientParticipantSet
    {
        public String Command { get; set; }
        public int EventId { get; set; }
        public int ParticipantId { get; set; }
        public JsonOption Value { get; set; }
    }

    /**
     *
     * Classes for generating responses to client queries.
     *
     */
    public class JsonServerEventList
    {
        public String Command = "server_eventlist";
        public List<Event> Events {  get; set; }
        public List<JsonOption> Options { get; set; }
    }

    public class JsonOption
    {
        public String Name { get; set; }
        public String Value { get; set; }
    }

    public class JsonServerEvent
    {
        public String Command = "server_event";
        public Event Event;
        public List<Division> Divisions { get; set; }
        public List<TimingPoint> TimingPoints { get; set; }
        public List<JsonOption> EventOptions { get; set; }
    }

    public class JsonServerParticipants
    {
        public String Command = "server_participants";
        public int EventId { get; set; }
        public List<JsonParticipant> Participants { get; set; }
    }

    public class JsonParticipant
    {
        public JsonParticipant(Participant p)
        {
            Id = p.Identifier;
            Birthday = p.Birthdate;
            First = p.FirstName;
            Last = p.LastName;
            Street = p.Street;
            Street2 = p.Street2;
            City = p.City;
            Zip = p.Zip;
            Country = p.Country;
            Phone = p.Phone;
            Mobile = p.Mobile;
            Email = p.Email;
            Parent = p.Parent;
            Gender = p.Gender;
            EmergencyContact = p.EmergencyContact;
            Specific = p.EventSpecific;
        }

        public int Id { get; set; }
        public String Birthday { get; set; }
        public String First { get; set; }
        public String Last { get; set; }
        public String Street { get; set; }
        public String Street2 { get; set; }
        public String City { get; set; }
        public String Zip { get; set; }
        public String Country { get; set; }
        public String Phone { get; set; }
        public String Mobile { get; set; }
        public String Email { get; set; }
        public String Parent { get; set; }
        public String Gender { get; set; }
        public EmergencyContact EmergencyContact { get; set; }
        public EventSpecific Specific { get; set; }
    }

    public class JsonServerResults
    {
        public String Command = "server_results";
        public int EventId { get; set; }
        public List<TimeResult> Results { get; set; }
    }

    public class JsonServerUpdateParticipant
    {
        public String Command = "server_participant_update";
        public int EventId { get; set; }
        public JsonParticipant Participant { get; set; }
    }

    public class JsonServerSetParticipant
    {
        public String Command = "server_participant_set";
        public int EventId { get; set; }
        public int ParticipantId { get; set; }
        public JsonOption Value { get; set; }
    }

    public class JsonServerAddParticipant
    {
        public String Command = "server_participant_add";
        public int EventId { get; set; }
        public JsonParticipant Participant { get; set; }
    }

    public class JsonServerEventUpdate
    {
        public String Command = "server_event_update";
        public Event Event { get; set; }
        public List<Division> Divisions { get; set; }
        public List<TimingPoint> TimingPoints { get; set; }
    }

    public class JsonServerResultUpdate
    {
        public String Command = "server_result_update";
        public int EventId { get; set; }
        public TimeResult Result { get; set; }
    }

    public class JsonServerResultAdd
    {
        public String Command = "server_result_add";
        public int EventId { get; set; }
        public TimeResult Result { get; set; }
    }
}
