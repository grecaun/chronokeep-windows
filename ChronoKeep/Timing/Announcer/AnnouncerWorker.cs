﻿using Chronokeep.Interfaces;
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

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();

        private static bool QuittingTime = false;
        private static List<AnnouncerParticipant> participants = new List<AnnouncerParticipant>();
        private static Dictionary<string, DateTime> bibSeen = new();

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
            Log.D("Timing.Announcer.AnnouncerWorker", string.Format("Returning {0} participants to announce.", output.Count));
            return output;
        }

        public static bool Running()
        {
            bool output = false;
            if (mutex.WaitOne(3000))
            {
                output = !QuittingTime;
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
                        && participantBibDictionary.ContainsKey(read.Bib))
                    {
                        newParticipants = true;
                        bibSeen.Add(read.Bib, timeRightNow);
                        participants.Add(new AnnouncerParticipant(participantBibDictionary[read.Bib], read.Seconds));
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
                if (participantBibDictionary.ContainsKey(part.Bib))
                {
                    Log.E("Timing.Announcer.AnnouncerWorker", "Multiples of a Bib found in participants set. " + part.Bib);
                }
                participantBibDictionary[part.Bib] = part;
            }
            // Process any announcer reads that we've already used so we don't announce them later.
            ProcessReads(database.GetAnnouncerUsedChipReads(theEvent.Identifier), participantBibDictionary);
            // Loop while waiting for work.
            while (true)
            {
                bool notified = semaphore.WaitOne(1000 * Constants.Timing.ANNOUNCER_LOOP_TIMER);
                if (mutex.WaitOne(3000))
                {
                    if (QuittingTime)
                    {
                        mutex.ReleaseMutex();
                        Log.D("Timing.Announcer.AnnouncerWorker", "Exiting announcer thread.");
                        return;
                    }
                    mutex.ReleaseMutex();
                }
                if (notified)
                {
                    Log.D("Timing.Announcer.AnnouncerWorker", "New chip reads found!");
                    Event ev2 = database.GetCurrentEvent();
                    // verify that we both ev2 and theevent are not null and they match
                    if (ev2 == null || theEvent == null || ev2.Identifier != theEvent.Identifier)
                    {
                        QuittingTime = true;
                        Log.E("Timing.Announcer.AnnouncerWorker", "The event changed while the announcer window is open.");
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
                    window.UpdateTiming();
                    window.UpdateAnnouncerWindow();
                }
            }
        }
    }
}
