using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Helpers
{
    internal class Globals
    {
        public static int UploadInterval = -1;
        public static int DownloadInterval = -1;
        public static int AnnouncerWindow = 45;
        public static string ErrorLogPath = "";

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

        public class ReaderMessage : IComparable
        {
            public SeverityLevel Severity;
            public RemoteNotification Message;
            public bool Notified = false;
            public string SystemName = "";
            public string Address = "";

            public enum SeverityLevel
            {
                High,
                Moderate,
                Low
            }

            public string Who { get => SystemName; }
            public string Where { get => Address; }
            public string When { get => Message.When; }
            public string Information { get => PortalNotification.GetRemoteNotificationMessage(Message.Type); }
            public string DialogBoxString { get => PortalNotification.GetRemoteNotificationMessage(SystemName, Address, Message); }
            public string SeverityString { get => Severity == SeverityLevel.High ? "High" : Severity == SeverityLevel.Moderate ? "Moderate" : "Low"; }
            public string Background { get => Severity == SeverityLevel.High ? "#3FFF0000" : Severity == SeverityLevel.Moderate ? "#4FF75605" : "#3FF7CF05"; }

            public int CompareTo(object other)
            {
                if (other is not ReaderMessage) return -1;
                if (DateTime.TryParse(Message.When, out DateTime thisWhen)
                    && DateTime.TryParse(((ReaderMessage)other).Message.When, out DateTime otherWhen))
                {
                    return thisWhen.CompareTo(otherWhen);
                }
                return Message.Type.CompareTo(((ReaderMessage)other).Message.Type);
            }

            public bool Equals(ReaderMessage other)
            {
                return Severity == other.Severity && When.Equals(other.When, StringComparison.Ordinal) && Address.Equals(other.Address, StringComparison.Ordinal) && Message.Type.Equals(other.Message.Type, StringComparison.Ordinal);
            }
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
                    if (readerMessages.TryGetValue((m.SystemName, m.Message), out ReaderMessage found))
                    {
                        found.Notified = m.Notified;
                    }
                }
                readerMessageMutex.ReleaseMutex();
            }
        }

        public static void UpdateReaderMessage(ReaderMessage msg)
        {
            if (readerMessageMutex.WaitOne(1000))
            {
                if (readerMessages.TryGetValue((msg.SystemName, msg.Message), out ReaderMessage found))
                {
                    found.Notified = msg.Notified;
                }
                readerMessageMutex.ReleaseMutex();
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
                readerMessages.Add((msg.SystemName, msg.Message), msg);
                readerMessageMutex.ReleaseMutex();
                return true;
            }
            return false;
        }
    }
}
