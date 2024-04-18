using Chronokeep.Objects.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Constants
{
    public class Globals
    {
        // keep track of TWILIO credentials
        public static Twilio TwilioCredentials = new();
        // keep track of banned phones
        public static List<string> BannedPhones = new List<string>();
        // keep track of banned emails
        public static List<string> BannedEmails = new List<string>();
    }
}
