using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepRemote
{
    public class RemoteReader
    {
        public RemoteReader()
        {
            Name = string.Empty;
            APIIDentifier = -1;
            LocationID = Constants.Timing.LOCATION_DUMMY;
            EventID = -1;
        }

        public RemoteReader(string name, int api_id, int location_id, int event_id)
        {
            this.Name = name;
            this.APIIDentifier = api_id;
            this.LocationID = location_id;
            this.EventID = event_id;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        public int APIIDentifier { get; set; }
        public int LocationID { get; set; }
        public int EventID { get; set; }
    }
}
