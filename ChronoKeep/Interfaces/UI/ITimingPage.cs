using Chronokeep.Objects;
using System;

namespace Chronokeep.Interfaces.UI
{
    public interface ITimingPage
    {
        public string GetSearchValue();
        public string GetLocation();
        public string GetReader();
        public SortType GetSortType();
        public PeopleType GetPeopleType();
        public void LoadMainDisplay();
        public void NotifyTimingWorker();
        public void UpdateView();
        public void SetAllTimingSystemsToTime(DateTime date, bool now);
        public void OpenRewindWindow(TimingSystem reader);
        public void OpenTimeWindow(TimingSystem reader);
        public bool ConnectSystem(TimingSystem reader);
        public bool DisconnectSystem(TimingSystem reader);
        public void RemoveSystem(TimingSystem reader);
        public void SetReaders(string[] readers, bool visible);
    }
}
