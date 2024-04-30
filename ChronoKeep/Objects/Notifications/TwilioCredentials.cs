﻿using Twilio;

namespace Chronokeep.Objects.Notifications
{
    public class TwilioCredentials
    {
        public TwilioClient Client { get; set; }
        public string AccountSID { get; set; }
        public string AuthToken { get; set; }
        public string PhoneNumber { get; set; }
    }
}
