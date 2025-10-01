using Chronokeep.Database;

namespace Chronokeep.Objects.Notifications
{
    public class MailgunCredentials
    {
        public string Username { get; set; } = "api";
        public string APIKey { get; set; } = "";
        public string FromName { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string Domain { get; set; } = "";

        public bool Valid()
        {
            return APIKey.Length > 0 && Domain.Length > 0 && FromEmail.Length > 0;
        }

        public string From()
        {
            if (FromName.Length  > 0)
            {
                return string.Format("{0} <{1}>", FromName, FromEmail);
            }
            return FromEmail;
        }

        public static MailgunCredentials GetCredentials(IDBInterface database)
        {
            AppSetting APIKey = database.GetAppSetting(Constants.Settings.MAILGUN_API_KEY);
            AppSetting Domain = database.GetAppSetting(Constants.Settings.MAILGUN_API_URL);
            AppSetting FromEmail = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL);
            AppSetting FromName = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_NAME);
            MailgunCredentials output = new();
            if (APIKey != null)
            {
                output.APIKey = APIKey.Value;
            }
            if (Domain != null)
            {
                output.Domain = Domain.Value;
            }
            if (FromEmail != null)
            {
                output.FromEmail = FromEmail.Value;
            }
            if (FromName != null)
            {
                output.FromName = FromName.Value;
            }
            return output;
        }
    }
}
