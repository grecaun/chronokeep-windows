using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventDirector.Timing
{
    class TimingWorker
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static TimingWorker worker;

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();
        private static bool QuittingTime = false;

        private TimingWorker(IMainWindow window, IDBInterface database)
        {
            this.window = window;
            this.database = database;
        }

        public static TimingWorker NewWorker(IMainWindow window, IDBInterface database)
        {
            if (worker == null)
            {
                worker = new TimingWorker(window, database);
            }
            return worker;
        }

        public static void Shutdown()
        {
            if (mutex.WaitOne(3000))
            {
                QuittingTime = true;
                mutex.ReleaseMutex();
            }
        }

        public static void Notify()
        {
            semaphore.Release();
        }

        public void Run()
        {
            int counter = 1;
            do
            {
                semaphore.WaitOne();        // Wait for work.
                if (mutex.WaitOne(3000))    // Check if we've been told to quit.
                {                           // Do that here so we don't try to process another loop after being told to quit.
                    if (QuittingTime)
                    {
                        mutex.ReleaseMutex();
                        break;
                    }
                    mutex.ReleaseMutex();
                }
                else
                {
                    break;
                }
                Log.D("Entering loop " + counter++);
                Event theEvent = database.GetCurrentEvent();
                if (theEvent != null && theEvent.Identifier != -1)
                {
                    // Pre-process information we'll need to fully process chip reads
                    // Locations for checking if we're past the maximum number of occurances
                    // Stored in a dictionary based upon the location ID for easier access.
                    Dictionary<int, TimingLocation> locationDictionary = new Dictionary<int, TimingLocation>();
                    foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
                    {
                        if (locationDictionary.ContainsKey(loc.Identifier))
                        {
                            Log.E("Multiples of a location found in location set.");
                        }
                        locationDictionary[loc.Identifier] = loc;
                    }
                    // Segments so we can give a result a segment ID if it's at the right location
                    // and occurance. Stored in a dictionary for obvious reasons.
                    Dictionary<(int, int), Segment> segmentDictionary = new Dictionary<(int, int), Segment>();
                    foreach (Segment seg in database.GetSegments(theEvent.Identifier))
                    {
                        if (segmentDictionary.ContainsKey((seg.LocationId, seg.Occurrence)))
                        {
                            Log.E("Multiples of a segment found in segment set.");
                        }
                        segmentDictionary[(seg.LocationId, seg.Occurrence)] = seg;
                    }
                    // Get all of the Chip Reads we find useful (Unprocessed, and those used as a result.)
                    // and then sort them into groups based upon Bib, Chip, or put them in the ignore pile if
                    // they have no bib or chip.
                    string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff   ");
                    Dictionary<int, List<ChipRead>> bibReadPairs = new Dictionary<int, List<ChipRead>>();
                    Dictionary<string, List<ChipRead>> chipReadPairs = new Dictionary<string, List<ChipRead>>();
                    List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
                    List<ChipRead> setIgnore = new List<ChipRead>();
                    foreach (ChipRead read in allChipReads)
                    {
                        if (read.ChipBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                        {
                            if (!bibReadPairs.ContainsKey(read.ChipBib))
                            {
                                bibReadPairs[read.ChipBib] = new List<ChipRead>();
                            }
                            bibReadPairs[read.ChipBib].Add(read);
                        }
                        else if (read.ReadBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                        {
                            if (!bibReadPairs.ContainsKey(read.ReadBib))
                            {
                                bibReadPairs[read.ReadBib] = new List<ChipRead>();
                            }
                            bibReadPairs[read.ReadBib].Add(read);
                        }
                        else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP)
                        {
                            if (!chipReadPairs.ContainsKey(read.ChipNumber.ToString()))
                            {
                                chipReadPairs[read.ChipNumber.ToString()] = new List<ChipRead>();
                            }
                            chipReadPairs[read.ChipNumber.ToString()].Add(read);
                        }
                        else
                        {
                            setIgnore.Add(read);
                        }
                    }
                    // Go through each chip read for a single person, they *should* be ordered in 
                    // order of occurance. Make sure we keep track of the last occurance for a person
                    // at a specific location.
                    // (Bib, Location), Last Chip Read - (Chip, Location), Last Chip Read
                    Dictionary<(int, int), ChipRead> bibLastReadDictionary = new Dictionary<(int, int), ChipRead>();
                    Dictionary<(int, int), ChipRead> chipLastReadDictionary = new Dictionary<(int, int), ChipRead>();
                    // Write to log test information.
                    string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string outfilepath = System.IO.Path.Combine(database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value, "TimingWorkerTestFile.txt");
                    List<string> messages = new List<string>();
                    messages.Add(String.Format("{0} - {1} : {2}", startTime, endTime, allChipReads.Count));
                    foreach (int BibKey in bibReadPairs.Keys)
                    {
                        foreach (ChipRead read in bibReadPairs[BibKey])
                        {
                            messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", BibKey, read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Bib"));
                        }
                    }
                    foreach (string ChipKey in chipReadPairs.Keys)
                    {
                        foreach (ChipRead read in chipReadPairs[ChipKey])
                        {
                            messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", ChipKey, read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Chip"));
                        }
                    }
                    foreach (ChipRead read in setIgnore)
                    {
                        messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", "", read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Bib"));
                    }
                    Log.WriteFile(outfilepath, messages.ToArray()); //*/
                }
            } while (true);
        }
    }
}
