using ChronoKeep.Objects;
using System.Collections.Generic;
using System.Windows;

namespace ChronoKeep.Interfaces
{
    public interface IMainWindow : IWindowCallback
    {
        // Window related calls.
        void AddWindow(Window w);
        
        // Networking services related calls.
        bool StartNetworkServices();
        bool StopNetworkServices();
        bool NetworkServicesRunning();
        void NetworkUpdateResults(int eventid, List<TimeResult> results);
        void NetworkAddResults(int eventid, List<TimeResult> results);
        void NetworkClearResults(int eventid);
        void StartHttpServer();
        void StopHttpServer();

        // Tools.
        void UpdateStatus();
        bool ExcelEnabled();
        void UpdateTimingFromController();
        void UpdateTiming();
        bool NewTimingInfo();
        void DatasetChanged();

        // Event related calls.
        void UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice);

        // Timing System related calls.
        void ConnectTimingSystem(TimingSystem system);
        void DisconnectTimingSystem(TimingSystem system);
        void TimingSystemDisconnected(TimingSystem system);
        void ShutdownTimingController();
        List<TimingSystem> GetConnectedSystems();
        void NotifyTimingWorker();
        void NotifyRecalculateAgeGroups();
    }
}
