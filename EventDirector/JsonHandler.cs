using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class JsonHandler
    {
        public static readonly int CLIENT_UNKNOWN = 0;
        public static readonly int CLIENT_EVENT = 1;
        public static readonly int CLIENT_PARTICIPANTS = 2;
        public static readonly int CLIENT_RESULTS = 3;
        public static readonly int CLIENT_PARTICIPANT_UPDATE = 4;
        public static readonly int CLIENT_PARTICIPANT_ADD = 5;
        public static readonly int CLIENT_PARTICIPANT_SET = 6;

        IDBInterface database;

        public JsonHandler(IDBInterface database)
        {
            this.database = database;
        }

        public void HandleJsonClientParticipantSet(JsonClientParticipantSet participantSet)
        {
            if (participantSet.Value.Name == "checked_in")
            {
                Log.D("Checked in set value found.");
                database.CheckInParticipant(participantSet.EventId, participantSet.ParticipantId, Convert.ToInt32(participantSet.Value.Value));
            }
            else if (participantSet.Value.Name == "early_start")
            {
                Log.D("Early start set value found.");
                database.SetEarlyStartParticipant(participantSet.EventId, participantSet.ParticipantId, Convert.ToInt32(participantSet.Value.Value));
            }
        }

        public List<JObject> ParseJsonMessage(String message)
        {
            List<JObject> output = new List<JObject>();
            try
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(message))
                {
                    SupportMultipleContent = true
                };
                while (reader.Read())
                {
                    output.Add((JObject) JObject.ReadFrom(reader));
                }
            }
            catch
            {
                return output;
            }
            Log.D("Number of objects found is...." + output.Count);
            return output;
        }

        public String GetJsonServerEventList()
        {
            List<JsonOption> serverOptions = new List<JsonOption>
            {
                new JsonOption()
                {
                    Name = "authenticate_open",
                    Value = "true"
                }
            };
            JsonServerEventList jsonServerEventList = new JsonServerEventList()
            {
                Events = database.GetEvents(),
                Options = serverOptions
            };
            return JsonConvert.SerializeObject(jsonServerEventList);
        }

        public String GetJsonServerEvent(int eventId)
        {
            Log.D("Getting JsonServerEvent");
            Event oneEvent = database.GetEvent(eventId);
            List<JsonOption> eventOptions = database.GetEventOptions(eventId);
            JsonServerEvent serverEvent = new JsonServerEvent()
            {
                Event = oneEvent,
                Divisions = database.GetDivisions(oneEvent.Identifier),
                TimingPoints = database.GetTimingPoints(oneEvent.Identifier),
                EventOptions = eventOptions
            };
            return JsonConvert.SerializeObject(serverEvent);
        }

        public String GetJsonServerParticipants(int id)
        {
            List<JsonParticipant> parts = new List<JsonParticipant>();
            foreach (Participant p in database.GetParticipants(id))
            {
                parts.Add(new JsonParticipant(p));
            }
            JsonServerParticipants jsonParts = new JsonServerParticipants()
            {
                EventId = id,
                Participants = parts
            };
            return JsonConvert.SerializeObject(jsonParts);
        }

        // JsonServerResults
        public String GetJsonServerResults(int id)
        {
            JsonServerResults res = new JsonServerResults()
            {
                EventId = id,
                Results = database.GetTimingResults(id)
            };
            return JsonConvert.SerializeObject(res);
        }


        // JsonServerUpdateParticipant
        public String GetJsonServerUpdateParticipant(int id, Participant p)
        {
            JsonServerUpdateParticipant update = new JsonServerUpdateParticipant()
            {
                EventId = id,
                Participant = new JsonParticipant(p)
            };
            return JsonConvert.SerializeObject(update);
        }

        // JsonServerSetParticipant
        public String GetJsonServerSetParticipant(int eventid, int partid, JsonOption option)
        {
            JsonServerSetParticipant update = new JsonServerSetParticipant()
            {
                EventId = eventid,
                ParticipantId = partid,
                Value = option
            };
            return JsonConvert.SerializeObject(update);
        }

        // JsonServerAddParticipant
        public String GetJsonServerAddParticipant(int eventid, Participant p)
        {
            JsonServerAddParticipant update = new JsonServerAddParticipant()
            {
                EventId = eventid,
                Participant = new JsonParticipant(p)
            };
            return JsonConvert.SerializeObject(update);
        }


        // JsonServerEventUpdate
        public String GetJsonServerEventUpdate(Event e)
        {
            JsonServerEventUpdate update = new JsonServerEventUpdate()
            {
                Event = e,
                Divisions = database.GetDivisions(e.Identifier),
                TimingPoints = database.GetTimingPoints(e.Identifier)
            };
            return JsonConvert.SerializeObject(update);
        }

        // JsonServerResultUpdate
        public String GetJsonServerResultUpdate(int id, TimeResult result)
        {
            JsonServerResultUpdate update = new JsonServerResultUpdate()
            {
                EventId = id,
                Result = result
            };
            return JsonConvert.SerializeObject(update);
        }

        // JsonServerResultAdd
        public String GetJsonServerResultAdd(int id, TimeResult restult)
        {
            JsonServerResultAdd update = new JsonServerResultAdd()
            {
                EventId = id,
                Result = restult
            };
            return JsonConvert.SerializeObject(update);
        }
    }
}
