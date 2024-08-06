using Chronokeep.Interfaces;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.MemStore
{
    public class DatabaseSaver
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private readonly List<Worker> workers = new();

        private static DatabaseSaver instance;

        private static readonly Semaphore semaphore = new(0, 2);
        private static readonly Mutex mutex = new();
        private static readonly Mutex workerMtx = new();
        private static bool QuittingTime = false;

        private DatabaseSaver(IMainWindow window, IDBInterface database)
        {
            this.window = window;
            this.database = database;
        }

        public static DatabaseSaver NewSaver(IMainWindow window, IDBInterface database)
        {
            instance ??= new DatabaseSaver(window, database);
            return instance;
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

        public void AddWorker(Worker worker)
        {
            if (workerMtx.WaitOne(3000))
            {
                worker.SetQueuePosition(workers.Count + 1);
                workers.Add(worker);
                workerMtx.ReleaseMutex();
            }
        }

        public static void Shutdown()
        {
            Log.D("MemStore.DatabaseSaver", "Mutex Wait 01");
            if (mutex.WaitOne(3000))
            {
                QuittingTime = true;
                mutex.ReleaseMutex();
            }
        }

        public static void Notify()
        {
            try
            {
                semaphore.Release();
            }
            catch
            {
                Log.D("MemStore.DatabaseSaver", "Unable to release, release is full.");
            }
        }

        void Run()
        {
            do
            {
                Log.D("MemStore.DatabaseSaver", "Mutex Wait 02");
                semaphore.WaitOne();
                if (mutex.WaitOne(3000))
                {
                    if (QuittingTime)
                    {
                        mutex.ReleaseMutex();
                        break;
                    }
                    mutex.ReleaseMutex();
                    List<Worker> newWorkers = new();
                    if (workerMtx.WaitOne(3000))
                    {
                        newWorkers.AddRange(workers);
                        workers.Clear();
                        workerMtx.ReleaseMutex();
                    }
                    newWorkers.Sort((a, b) => a.GetQueuePosition().CompareTo(b.GetQueuePosition()));
                    foreach (Worker worker in newWorkers)
                    {
                        worker.DoWork(database);
                    }
                }
            } while (true);
            // TODO make sure everything saves
            workerMtx.WaitOne();
            foreach (Worker worker in workers)
            {
                worker.DoWork(database);
            }
            workerMtx.ReleaseMutex();
        }
    }
}
