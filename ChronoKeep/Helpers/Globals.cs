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

        private static readonly List<BibChipAssociation> ignoredChips = [];
        private static readonly Lock ignoredChipsLock = new();

        public static List<BibChipAssociation> GetIgnoredChips()
        {
            List<BibChipAssociation> output = [];
            if (ignoredChipsLock.TryEnter(1000))
            {
                try
                {
                    output.AddRange(ignoredChips);
                }
                finally
                {
                    ignoredChipsLock.Exit();
                }
            }
            return output;
        }

        public static void UpdateIgnoredChips(IDBInterface database)
        {
            if (ignoredChipsLock.TryEnter(1000))
            {
                try
                {
                    ignoredChips.Clear();
                    List<BibChipAssociation> tmp = database.GetBibChips(-1);
                    ignoredChips.AddRange(database.GetBibChips(-1));
                }
                finally
                {
                    ignoredChipsLock.Exit();
                }
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
        private static readonly Lock readerMessageLock = new();

        public static List<ReaderMessage> GetReaderMessages()
        {
            List<ReaderMessage> output = [];
            if (readerMessageLock.TryEnter(1000))
            {
                try
                {
                    output.AddRange(readerMessages.Values);
                }
                finally
                {
                    readerMessageLock.Exit();
                }
            }
            return output;
        }

        public static void UpdateReaderMessages(List<ReaderMessage> msgs)
        {
            if (readerMessageLock.TryEnter(1000))
            {
                try
                {
                    foreach (ReaderMessage m in msgs)
                    {
                        if (readerMessages.TryGetValue((m.SystemName, m.Message), out ReaderMessage found))
                        {
                            found.Notified = m.Notified;
                        }
                    }
                }
                finally
                {
                    readerMessageLock.Exit();
                }
            }
        }

        public static void UpdateReaderMessage(ReaderMessage msg)
        {
            if (readerMessageLock.TryEnter(1000))
            {
                try
                {
                    if (readerMessages.TryGetValue((msg.SystemName, msg.Message), out ReaderMessage found))
                    {
                        found.Notified = msg.Notified;
                    }
                }
                finally
                {
                    readerMessageLock.Exit();
                }
            }
        }

        public static void ClearReaderMessages()
        {
            if (readerMessageLock.TryEnter(1000))
            {
                try
                {
                    readerMessages.Clear();
                }
                finally
                {
                    readerMessageLock.Exit();
                }
            }
        }

        public static bool AddReaderMessage(ReaderMessage msg)
        {
            if (readerMessageLock.TryEnter(1000))
            {
                try
                {
                    readerMessages.Add((msg.SystemName, msg.Message), msg);
                }
                finally
                {
                    readerMessageLock.Exit();
                }
                return true;
            }
            return false;
        }
    }
}
