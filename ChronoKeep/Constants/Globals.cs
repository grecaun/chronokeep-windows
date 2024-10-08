﻿using Chronokeep.Network.API;
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
        public static HashSet<string> BannedPhones = new();
        // keep track of local list of banned phone numbers that we need to add to the api's list
        public static HashSet<string> NewBannedPhones = new();
        // keep track of banned emails
        public static HashSet<string> BannedEmails = new();

        private static readonly Regex phoneRegex = new Regex("^(?:\\+?1)?\\s*\\-?\\s*(?:\\d{3}|\\(\\d{3}\\))\\s*\\-?\\s*\\d{3}\\s*\\-?\\s*\\d{4}$");
        private static readonly Regex whitespace = new Regex("\\s+");

        public static async void UpdateBannedPhones()
        {
            try
            {
                GetBannedPhonesResponse phonesResponse = await APIHandlers.GetBannedPhones();
                BannedPhones.Clear();
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
                    // make sure we've got all our new banned phone numbers in there too
                    foreach (string phone in NewBannedPhones)
                    {
                        string p = GetValidPhone(phone);
                        if (p.Length > 0)
                        {
                            BannedPhones.Add(p);
                        }
                    }
                }
                
            }
            catch
            {
                Log.E("Constants.Globals", "Exception getting banned phones.");
            }
            // attempt to upload all the new phone numbers
            foreach (string phone in NewBannedPhones)
            {
                string p = GetValidPhone(phone);
                if (p.Length > 0)
                {
                    try
                    {
                        await APIHandlers.AddBannedPhone(p);
                        NewBannedPhones.Remove(phone);
                    }
                    catch
                    {
                        Log.E("Constants.Globals", "Exception uploading banned phone number.");
                    }
                }
            }
        }

        public static async void AddBannedPhone(string phone)
        {
            string p = GetValidPhone(phone);
            BannedPhones.Add(p);
            NewBannedPhones.Add(phone);
            try
            {
                await APIHandlers.AddBannedPhone(p);
                NewBannedPhones.Remove(phone);
            }
            catch
            {
                Log.E("Constants.Globals", "Exception uploading banned phone number.");
            }
        }

        public static async void UpdateBannedEmails()
        {
            try
            {
                GetBannedEmailsResponse emailsResponse = await APIHandlers.GetBannedEmails();
                BannedEmails.Clear();
                if (emailsResponse.Emails != null)
                {
                    foreach (string email in emailsResponse.Emails)
                    {
                        BannedEmails.Add(email);
                    }
                }
            }
            catch
            {
                Log.E("Constants.Globals", "Exception getting banned emails.");
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
