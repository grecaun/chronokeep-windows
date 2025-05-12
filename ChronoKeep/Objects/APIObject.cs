using Chronokeep.Network.Remote;
using Chronokeep.Objects.ChronokeepRemote;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chronokeep.Objects
{
    public class APIObject
    {
        private int id;
        private string type, url, nickname, auth_token, web_url;

        public APIObject()
        {
            type = Constants.APIConstants.CHRONOKEEP_RESULTS;
            url = Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS];
            auth_token = "";
            nickname = "";
            web_url = "";
        }

        public APIObject(int id, string type, string url, string nickname, string auth_token, string web_url)
        {
            this.id = id;
            this.type = type;
            this.url = url;
            this.nickname = nickname;
            this.auth_token = auth_token;
            this.web_url = web_url;
        }

        public int Identifier { get => id; set => id = value; }
        public string Type { get => type; set => type = value; }
        public string URL { get => url; set => url = value; }
        public string Nickname { get => nickname; set => nickname = value; }
        public string AuthToken { get => auth_token; set => auth_token = value; }
        public string WebURL { get => web_url; set => web_url = value; }

        public async Task<List<RemoteReader>> GetReaders()
        {
            if (type != Constants.APIConstants.CHRONOKEEP_REMOTE && type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF)
            {
                throw new Exception("not a valid reader type");
            }
            var response = await RemoteHandlers.GetReaders(this);
            return response.Readers;
        }

        public async Task<(List<ChipRead>, RemoteNotification)> GetReads(RemoteReader reader, DateTime start, DateTime end)
        {
            if (type != Constants.APIConstants.CHRONOKEEP_REMOTE && type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF)
            {
                throw new Exception("not a valid reader type");
            }
            var result = await RemoteHandlers.GetReads(
                    this,
                    reader.Name,
                    Constants.Timing.UnixDateToEpoch(start.ToUniversalTime()),
                    Constants.Timing.UnixDateToEpoch(end.ToUniversalTime())
                );
            List<ChipRead> output = new List<ChipRead>();
            if (result.Reads == null)
            {
                return (output, result.Notification);
            }
            foreach (RemoteRead read in result.Reads)
            {
                output.Add(read.ConvertToChipRead(reader.EventID, reader.LocationID));
            }
            return (output, result.Notification);
        }

        public async Task<long> DeleteReads(RemoteReader reader, DateTime start, DateTime end)
        {
            if (type != Constants.APIConstants.CHRONOKEEP_REMOTE && type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF)
            {
                throw new Exception("not a valid reader type");
            }
            var result = await RemoteHandlers.DeleteReads(
                    this,
                    reader.Name,
                    Constants.Timing.UnixDateToEpoch(start.ToUniversalTime()),
                    Constants.Timing.UnixDateToEpoch(end.ToUniversalTime())
                );
            return result.Count;
        }
    }
}
