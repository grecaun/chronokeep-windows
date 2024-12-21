using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Objects.ChronokeepRemote;
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

        public class ReaderMessage
        {
            public SeverityLevel Severity;
            public RemoteNotification Message;
            public bool Notified = false;
            public string ReaderName = "";

            public enum SeverityLevel
            {
                High,
                Moderate,
                Low
            }

            public string When { get => Message.When; }
            public string Where { get => ReaderName; }
            public string Information { get => PortalNotification.GetRemoteNotificationMessage(Message.Type); }
            public string DialogBoxString { get => PortalNotification.GetRemoteNotificationMessage(ReaderName, Message.Type); }
        }

        private static readonly Dictionary<(string, RemoteNotification), ReaderMessage> readerMessages = new();
        private static readonly Mutex readerMessageMutex = new();

        public static List<ReaderMessage> GetReaderMessages()
        {
            List<ReaderMessage> output = [];
            if (readerMessageMutex.WaitOne(1000))
            {
                output.AddRange(readerMessages.Values);
                readerMessageMutex.ReleaseMutex();
            }
            return output;
        }

        public static void UpdateReaderMessages(List<ReaderMessage> msgs)
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                foreach (ReaderMessage m in msgs)
                {
                    if (readerMessages.TryGetValue((m.ReaderName, m.Message), out ReaderMessage found))
                    {
                        found.Notified = m.Notified;
                    }
                }
            }
        }

        public static void UpdateReaderMessage(ReaderMessage msg)
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                if (readerMessages.TryGetValue((msg.ReaderName, msg.Message), out ReaderMessage found))
                {
                    found.Notified = msg.Notified;
                }
            }
        }

        public static void ClearReaderMessages()
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                readerMessages.Clear();
                readerMessageMutex.ReleaseMutex();
            }
        }

        public static bool AddReaderMessage(ReaderMessage msg)
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                readerMessages.Add((msg.ReaderName, msg.Message), msg);
                readerMessageMutex.ReleaseMutex();
                return true;
            }
            return false;
        }
    }
}
