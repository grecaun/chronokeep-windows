using Chronokeep.UI.UIObjects;

namespace Chronokeep.Helpers
{
    internal class Globals
    {
        public static int UploadInterval = -1;
        public static int AnnouncerWindow = 45;

        public static void SetupValues(IDBInterface db)
        {
            if (!int.TryParse(db.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL).Value, out UploadInterval))
            {
                DialogBox.Show("Something went wrong trying to get the upload interval.");
            }
            if (!int.TryParse(db.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW).Value, out AnnouncerWindow))
            {
                DialogBox.Show("Something went wrong trying to get the announcer window.");
            }
        }
    }
}
