using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoKeep.Timing.Announcer
{
    class AnnouncerThread
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static AnnouncerThread announcer;

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();

        private static bool QuittingTime = false;
        private static List<AnnouncerParticipant> participants = new List<AnnouncerParticipant>();
        private static HashSet<int> bibSeen = new HashSet<int>();

        private AnnouncerThread(IMainWindow window, IDBInterface database)
        {
            this.window = window;
            this.database = database;
        }

        public static AnnouncerThread NewAnnouncer(IMainWindow window, IDBInterface database)
        {
            if (announcer == null)
            {
                announcer = new AnnouncerThread(window, database);
            }
            return announcer;
        }

        public static void Shutdown()
        {
            if (mutex.WaitOne(3000))
            {
                QuittingTime = true;
                mutex.ReleaseMutex();
            }
        }

        public static List<AnnouncerParticipant> GetList()
        {
            List<AnnouncerParticipant> output = new List<AnnouncerParticipant>();
            if (mutex.WaitOne(3000))
            {
                output.AddRange(participants);
                mutex.ReleaseMutex();
            }
            return output;
        }

        public static void Notify()
        {
            try
            {
                semaphore.Release();
            }
            catch
            {
                Log.D("Unable to release, release is most likely full.");
            }
        }

        public void Run()
        {
            while (true)
            {
                semaphore.WaitOne();
                if (mutex.WaitOne(3000))
                {
                    if (QuittingTime)
                    {
                        mutex.ReleaseMutex();
                        Log.D("Exiting announcer thread.");
                        return;
                    }
                    mutex.ReleaseMutex();
                }
                Event theEvent = database.GetCurrentEvent();
                Dictionary<int, Participant> participantBibDictionary = new Dictionary<int, Participant>();
                foreach (Participant part in database.GetParticipants(theEvent.Identifier))
                {
                    if (participantBibDictionary.ContainsKey(part.Bib))
                    {
                        Log.E("Multiples of a Bib found in participants set. " + part.Bib);
                    }
                    participantBibDictionary[part.Bib] = part;
                }
                // Ensure the event exists.
                if (theEvent != null && theEvent.Identifier != -1)
                {
                    bool newParticipants = false;
                    List<ChipRead> announcerReads = database.GetAnnouncerChipReads(theEvent.Identifier);
                    foreach (ChipRead read in announcerReads)
                    {
                        // Check to ensure we know the bib of this person
                        if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                        {
                            // Check if we've already seen the bib.
                            // Only work if we've not seen it before.
                            if (!bibSeen.Contains(read.Bib) && participantBibDictionary.ContainsKey(read.Bib))
                            {
                                newParticipants = true;
                                bibSeen.Add(read.Bib);
                                participants.Add(new AnnouncerParticipant(participantBibDictionary[read.Bib], read.Seconds));
                            }
                        }
                        read.Status = Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_SEEN;
                    }
                    database.UpdateChipReads(announcerReads);
                    // If we've seen new participants update the window.
                    if (newParticipants)
                    {
                        window.UpdateAnnouncerWindow();
                    }
                }
            }
        }
    }
}
