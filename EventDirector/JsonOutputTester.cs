using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventDirector
{
    class JsonOutputTester
    {
        private JsonHandler handler;
        private IDBInterface database;

        public JsonOutputTester(IDBInterface database)
        {
            this.database = database;
            handler = new JsonHandler(database);
        }
        
        public void Test()
        {
            // JsonServerEventList
            Log.D("About to grab events.");
            Log.D("JsonServerEventList is '" + handler.GetJsonServerEventList() + "'");

            // JsonServerEvent
            Event oneEvent = database.GetEvents()[0];
            Log.D("About to grab event info.");
            Log.D("JsonServerEvent is '" + handler.GetJsonServerEvent(oneEvent.Identifier) + "'");

            // JsonServerParticipants
            Log.D("About to grab participants.");
            Log.D("JsonServerParticipants is '" + handler.GetJsonServerParticipants(oneEvent.Identifier) + "'");

            // JsonServerResults


            // JsonServerUpdateParticipant


            // JsonServerSetParticipant


            // JsonServerAddParticipant


            // JsonServerEventUpdate


            // JsonServerResultUpdate


            // JsonServerResultAdd
        }
    }
}
