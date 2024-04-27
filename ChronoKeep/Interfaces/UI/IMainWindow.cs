using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using System.Collections.Generic;
using System.Windows;

namespace Chronokeep.Interfaces
{
    public interface IMainWindow : IWindowCallback
    {
        // Window related calls.
        void AddWindow(Window w);
        void SwitchPage(IMainPage iPage);
        void Exit();

        // Networking services related calls.
        void NetworkUpdateResults();
        void NetworkClearResults();
        void StartHttpServer();
        void StopHttpServer();
        bool HttpServerActive();

        // Tools.
        void UpdateStatus();
        void UpdateTimingFromController();
        void UpdateTiming();
        void UpdateAnnouncerWindow();

        // Timing System related calls.
        void ConnectTimingSystem(TimingSystem system);
        void DisconnectTimingSystem(TimingSystem system);
        void TimingSystemDisconnected(TimingSystem system);
        void ShutdownTimingController();
        List<TimingSystem> GetConnectedSystems();
        void NotifyTimingWorker();
        bool InDidNotStartMode();
        bool StartDidNotStartMode();
        bool StopDidNotStartMode();
        void NotifyAlarm(string Bib, string Chip);

        // Announcer related calls.
        bool AnnouncerConnected();
        void AnnouncerClosing();
        bool AnnouncerOpen();
        void StopAnnouncer();

        // API System related calls.
        void StartAPIController();
        bool StopAPIController();
        bool IsAPIControllerRunning();
        int APIErrors();

        // Remote Controller related calls.
        void StartRemote();
        bool StopRemote();
        bool IsRemoteRunning();
        int RemoteErrors();
        void ShowNotificationDialog(string ReaderName, RemoteNotification notification);

        // Theme related calls
        void UpdateTheme(Wpf.Ui.Appearance.ApplicationTheme theme, bool system);

        // Registration related calls
        bool StartRegistration();
        bool StopRegistration();
        bool IsRegistrationRunning();
    }
}
