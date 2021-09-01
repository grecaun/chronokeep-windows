using ChronoKeep.Objects;
using System.Collections.Generic;
using System.Windows;

namespace ChronoKeep.Interfaces
{
    public interface IMainWindow : IWindowCallback
    {
        // Window related calls.
        void AddWindow(Window w);
        void SwitchPage(IMainPage iPage, bool IsMainPage);
        
        // Networking services related calls.
        void NetworkClearResults(int eventid);
        void StartHttpServer();
        void StopHttpServer();
        bool HttpServerActive();

        // Tools.
        void UpdateStatus();
        bool ExcelEnabled();
        void UpdateTimingFromController();
        void UpdateTiming();
        bool NewTimingInfo();
        void UpdateAnnouncerWindow();

        // Timing System related calls.
        void ConnectTimingSystem(TimingSystem system);
        void DisconnectTimingSystem(TimingSystem system);
        void TimingSystemDisconnected(TimingSystem system);
        void ShutdownTimingController();
        List<TimingSystem> GetConnectedSystems();
        void NotifyTimingWorker();

        // Announcer related calls.
        bool AnnouncerConnected();

        // API System related calls.
        void StartAPIController();
        bool StopAPIController();
        bool IsAPIControllerRunning();
    }
}
