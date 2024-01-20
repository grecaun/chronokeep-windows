using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects
{
    public class APIObject
    {
        private int id;
        private string type, url, nickname, auth_token;

        public APIObject()
        {
            type = Constants.APIConstants.CHRONOKEEP_RESULTS;
            url = Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS];
            auth_token = "";
            nickname = "";
        }

        public APIObject(int id, string type, string url, string nickname, string auth_token)
        {
            this.id = id;
            this.type = type;
            this.url = url;
            this.nickname = nickname;
            this.auth_token = auth_token;
        }

        public int Identifier { get => id; set => id = value; }
        public string Type { get => type; set => type = value; }
        public string URL { get => url; set => url = value; }
        public string Nickname { get => nickname; set => nickname = value; }
        public string AuthToken { get => auth_token; set => auth_token = value; }
    }
}
