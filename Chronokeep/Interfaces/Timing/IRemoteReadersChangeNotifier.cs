namespace Chronokeep.Interfaces.Timing
{
    internal interface IRemoteReadersChangeNotifier
    {
        public bool Subscribe(IRemoteReadersChangeSubscriber sub);
        public bool Unsubscribe(IRemoteReadersChangeSubscriber sub);
        public void Notify();
    }
}
