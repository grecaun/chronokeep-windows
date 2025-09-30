using Chronokeep.Interfaces;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Timing.Announcer
{
    public class AnnouncerWorker
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static AnnouncerWorker announcer;

        private static readonly Semaphore semaphore = new(0, 2);
        private static readonly Lock anLock = new();

        private static bool QuittingTime = false;
        private static readonly List<AnnouncerParticipant> participants = [];
        private static readonly Dictionary<string, DateTime> bibSeen = [];

        private static readonly int seenWindow = 5; // minutes

        private AnnouncerWorker(IMainWindow window, IDBInterface database)
        {
            this.window = window;
            this.database = database;
        }

        public static AnnouncerWorker NewAnnouncer(IMainWindow window, IDBInterface database)
        {
            if (announcer == null)
            {
                announcer = new AnnouncerWorker(window, database);
            }
            QuittingTime = false;
            return announcer;
        }

        public static void Shutdown()
        {
            if (anLock.TryEnter(3000))
            {
                try
                {
                    QuittingTime = true;
                }
                finally
                {
                    anLock.Exit();
                }
            }
        }

        public static List<AnnouncerParticipant> GetList()
        {
            List<AnnouncerParticipant> output = [];
            if (anLock.TryEnter(3000))
            {
                try
                {
                    output.AddRange(participants);
                }
                finally
                {
                    anLock.Exit();
                }
            }
            Log.D("Timing.Announcer.AnnouncerWorker", string.Format("Returning {0} participants to announce.", output.Count));
            return output;
        }

        public static bool Running()
        {
            bool output = false;
            if (anLock.TryEnter(3000))
            {
                try
                {
                    output = !QuittingTime;
                }
                finally
                {
                    anLock.Exit();
                }
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
                Log.D("Timing.Announcer.AnnouncerWorker", "Unable to release, release is most likely full.");
            }
        }

        private bool ProcessReads(List<ChipRead> announcerReads, Dictionary<string, Participant> participantBibDictionary)
        {
            Log.D("Timing.Announcer.AnnouncerWorker", "Processing chip reads.");
            bool newParticipants = false;
            DateTime timeRightNow = DateTime.Now;
            foreach (ChipRead read in announcerReads)
            {
                // Check to ensure we know the bib of this person
                if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    // Check if we've already seen the bib (or we haven't seen the bib in seenWindow minutes).
                    // Only work if we've not seen it before.
                    if ((!bibSeen.TryGetValue(read.Bib, out DateTime lastSeen)
                        || lastSeen.AddMinutes(seenWindow).CompareTo(timeRightNow) < 0)
                        && participantBibDictionary.TryGetValue(read.Bib, out Participant part))
                    {
                        newParticipants = true;
                        bibSeen.Add(read.Bib, timeRightNow);
                        participants.Add(new AnnouncerParticipant(part, read.Seconds));
                        // Mark this chipread as USED
                        read.Status = Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED;
                    }
                }
                // Don't clobber over ANNOUNCER_USED statuses.
                if (read.Status != Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_SEEN;
                }
            }
            database.UpdateChipReads(announcerReads);
            return newParticipants;
        }

        public void Run()
        {
            // Get the event we're looking at and fill the participant bib dictionary.
            Event theEvent = database.GetCurrentEvent();
            Dictionary<string, Participant> participantBibDictionary = new Dictionary<string, Participant>();
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                if (!participantBibDictionary.TryAdd(part.Bib, part))
                {
                    Log.D("Timing.Announcer.AnnouncerWorker", "Multiples of a Bib found in participants set. " + part.Bib);
                }
            }
            // Process any announcer reads that we've already used so we don't announce them later.
            ProcessReads(database.GetAnnouncerUsedChipReads(theEvent.Identifier), participantBibDictionary);
            // Loop while waiting for work.
            while (true)
            {
                try
                {
                    bool notified = semaphore.WaitOne(1000 * Constants.Timing.ANNOUNCER_LOOP_TIMER);
                    if (anLock.TryEnter(3000))
                    {
                        try
                        {
                            if (QuittingTime)
                            {
                                Log.D("Timing.Announcer.AnnouncerWorker", "Exiting announcer thread.");
                                return;
                            }
                        }
                        finally
                        {
                            anLock.Exit();
                        }
                    }
                    if (notified)
                    {
                        Log.D("Timing.Announcer.AnnouncerWorker", "New chip reads found!");
                        Event ev2 = database.GetCurrentEvent();
                        // verify that we both ev2 and theevent are not null and they match
                        if (ev2 == null || theEvent == null || ev2.Identifier != theEvent.Identifier)
                        {
                            QuittingTime = true;
                            Log.D("Timing.Announcer.AnnouncerWorker", "The event changed while the announcer window is open.");
                            return;
                        }
                        // Ensure the event exists.
                        if (theEvent.Identifier != -1)
                        {
                            // If we've seen new participants update the window.
                            if (ProcessReads(database.GetAnnouncerChipReads(theEvent.Identifier), participantBibDictionary))
                            {
                                Log.D("Timing.Announcer.AnnouncerWorker", "There are people to announce.");
                                window.UpdateAnnouncerWindow();
                            }
                        }
                    }
                    else
                    {
                        Log.D("Timing.Announcer.AnnouncerWorker", "Update window expired.");
                        window.UpdateAnnouncerWindow();
                    }
                }
                catch (Exception e)
                {
                    Log.E("AnnouncerWindow", string.Format("Error processing announcer reads. {0}", e.ToString()));
                }
            }
        }
    }
}
