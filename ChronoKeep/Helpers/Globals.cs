using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Helpers
{
    internal class Globals
    {
        public static int UploadInterval = -1;
        public static int DownloadInterval = -1;
        public static int AnnouncerWindow = 45;

        public static void SetupValues(IDBInterface db)
        {
            if (!int.TryParse(db.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL).Value, out UploadInterval))
            {
                DialogBox.Show("Something went wrong trying to get the upload interval.");
            }
            if (!int.TryParse(db.GetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL).Value, out DownloadInterval))
            {
                DialogBox.Show("Something went wrong trying to get the download interval.");
            }
            if (!int.TryParse(db.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW).Value, out AnnouncerWindow))
            {
                DialogBox.Show("Something went wrong trying to get the announcer window.");
            }
        }

        private static readonly List<string> readerMessages = new();
        private static readonly Mutex readerMessageMutex = new();

        public static List<string> GetReaderMessages()
        {
            List<string> output = new();
            if (readerMessageMutex.WaitOne(1000))
            {
                output.AddRange(readerMessages);
                readerMessageMutex.ReleaseMutex();
            }
            return output;
        }

        public static void ClearReaderMessages()
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                readerMessages.Clear();
                readerMessageMutex.ReleaseMutex();
            }
        }

        public static bool AddReaderMessage(string msg)
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                readerMessages.Add(msg);
                readerMessageMutex.ReleaseMutex();
                return true;
            }
            return false;
        }
    }
}
