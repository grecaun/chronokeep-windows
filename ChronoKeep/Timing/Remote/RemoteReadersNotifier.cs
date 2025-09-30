using Chronokeep.Interfaces.Timing;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Timing.Remote
{
    internal class RemoteReadersNotifier : IRemoteReadersChangeNotifier
    {
        private readonly List<IRemoteReadersChangeSubscriber> subscribed = [];
        private readonly Lock rrLock = new();

        private static readonly RemoteReadersNotifier instance = new();

        public static RemoteReadersNotifier GetRemoteReadersNotifier()
        {
            return instance;
        }

        public bool Subscribe(IRemoteReadersChangeSubscriber sub)
        {
            var output = false;
            if (rrLock.TryEnter(3000))
            {
                try
                {
                    subscribed.Add(sub);
                    output = true;
                }
                finally
                {
                    rrLock.Exit();
                }
            }
            return output;
        }

        public bool Unsubscribe(IRemoteReadersChangeSubscriber sub)
        {
            var output = false;
            if (rrLock.TryEnter(3000))
            {
                try
                {
                    output = subscribed.Remove(sub);
                }
                finally
                {
                    rrLock.Exit();
                }
            }
            return output;
        }

        public void Notify()
        {
            if (rrLock.TryEnter(3000))
            {
                try
                {
                    foreach (IRemoteReadersChangeSubscriber subscriber in subscribed)
                    {
                        subscriber.NotifyRemoteReadersChange();
                    }
                }
                finally
                {
                    rrLock.Exit();
                }
            }
        }
    }
}
