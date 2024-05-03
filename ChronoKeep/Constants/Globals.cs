using Chronokeep.Network.API;
using Chronokeep.Objects.API;
using Chronokeep.Objects.Notifications;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Twilio;

namespace Chronokeep.Constants
{
    public class Globals
    {
        // keep track of TWILIO credentials
        public static TwilioCredentials TwilioCredentials = new();
        // keep track of banned phones
        public static List<string> BannedPhones = new List<string>();
        // keep track of banned emails
        public static List<string> BannedEmails = new List<string>();

        private static Regex phoneRegex = new Regex("^(?:\\+?1)?\\s*\\-?\\s*(?:\\d{3}|\\(\\d{3}\\))\\s*\\-?\\s*\\d{3}\\s*\\-?\\s*\\d{4}$");
        private static Regex whitespace = new Regex("\\s+");

        public static async void UpdateBannedPhones()
        {
            BannedPhones.Clear();
            GetBannedPhonesResponse phonesResponse = await APIHandlers.GetBannedPhones();
            if (phonesResponse.Phones != null)
            {
                foreach (string phone in phonesResponse.Phones)
                {
                    string p = GetValidPhone(phone);
                    if (p.Length > 0)
                    {
                        BannedPhones.Add(p);
                    }
                }
            }
        }

        public static async void UpdateBannedEmails()
        {
            BannedEmails.Clear();
            GetBannedEmailsResponse emailsResponse = await APIHandlers.GetBannedEmails();
            if (emailsResponse.Emails != null)
            {
                BannedEmails.AddRange(emailsResponse.Emails);
            }
        }

        public static string GetValidPhone(string phone)
        {
            string output = "";
            if (phoneRegex.Match(phone).Success)
            {
                string tmp = whitespace.Replace(phone.Replace("-", "").Replace(")", "").Replace("(", ""), "");
                if (tmp.Length == 10)
                {
                    output = string.Format("+1{0}", tmp);
                }
                else if (tmp.Length == 11)
                {
                    output = string.Format("+{0}", tmp);
                }
                else if (tmp.Length == 12 && tmp.First() == '+')
                {
                    output = tmp;
                }
            }
            return output;
        }

        public static void SetTwilioCredentials(IDBInterface database)
        {
            AppSetting sid = database.GetAppSetting(Settings.TWILIO_ACCOUNT_SID);
            AppSetting auth = database.GetAppSetting(Settings.TWILIO_AUTH_TOKEN);
            AppSetting phone = database.GetAppSetting(Settings.TWILIO_PHONE_NUMBER);
            if (sid != null)
            {
                TwilioCredentials.AccountSID = sid.Value;
            }
            if (auth != null)
            {
                TwilioCredentials.AuthToken = auth.Value;
            }
            if (phone != null)
            {
                TwilioCredentials.PhoneNumber = phone.Value;
            }
            if (TwilioCredentials.AccountSID.Length > 0 && TwilioCredentials.AuthToken.Length > 0)
            {
                TwilioClient.Init(TwilioCredentials.AccountSID, TwilioCredentials.AuthToken);
            }
        }

        public static void SetTwilioCredentials(string accountSID, string authToken, string phoneNumber)
        {
            TwilioCredentials.AccountSID = accountSID;
            TwilioCredentials.AuthToken = authToken;
            TwilioCredentials.PhoneNumber = GetValidPhone(phoneNumber);
            if (TwilioCredentials.AccountSID.Length > 0 && TwilioCredentials.AuthToken.Length > 0)
            {
                TwilioClient.Init(TwilioCredentials.AccountSID, TwilioCredentials.AuthToken);
            }
        }
    }
}
