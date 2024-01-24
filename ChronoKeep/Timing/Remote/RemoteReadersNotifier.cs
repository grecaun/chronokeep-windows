using Chronokeep.Interfaces.Timing;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Timing.Remote
{
    internal class RemoteReadersNotifier : IRemoteReadersChangeNotifier
    {
        private List<IRemoteReadersChangeSubscriber> subscribed = new();
        private Mutex mtx = new();

        private static RemoteReadersNotifier instance = new();

        public static RemoteReadersNotifier GetRemoteReadersNotifier()
        {
            return instance;
        }

        public bool Subscribe(IRemoteReadersChangeSubscriber sub)
        {
            var output = false;
            if (mtx.WaitOne(3000))
            {
                subscribed.Add(sub);
                output = true;
                mtx.ReleaseMutex();
            }
            return output;
        }

        public bool Unsubscribe(IRemoteReadersChangeSubscriber sub)
        {
            var output = false;
            if (mtx.WaitOne(3000))
            {
                output = subscribed.Remove(sub);
                mtx.ReleaseMutex();
            }
            return output;
        }

        public void Notify()
        {
            if (mtx.WaitOne(3000))
            {
                foreach (IRemoteReadersChangeSubscriber subscriber in subscribed)
                {
                    subscriber.NotifyRemoteReadersChange();
                }
                mtx.ReleaseMutex();
            }
        }
    }
}
