using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class ResultsAPI
    {
        private int id;
        private string type, url, nickname, auth_token;

        public ResultsAPI()
        {
            type = Constants.ResultsAPI.CHRONOKEEP;
            url = Constants.ResultsAPI.CHRONOKEEP_URL;
            auth_token = "";
            nickname = "";
        }

        public ResultsAPI(int id, string type, string url, string nickname, string auth_token)
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
