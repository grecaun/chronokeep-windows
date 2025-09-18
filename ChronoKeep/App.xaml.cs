using Chronokeep.Helpers;
using Chronokeep.UI;
using Sentry;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            SentrySdk.Init(o =>
            {
                o.Dsn = "https://69f609b68d42089ccea1545e8942c9e6@o4510042186514432.ingest.us.sentry.io/4510042244317184";
                o.IsGlobalModeEnabled = true;
                o.StackTraceMode = StackTraceMode.Enhanced;
#if DEBUG
                o.Environment = "debug";
#else
                o.Environment = "release";
#endif
                string gitVersion = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep." + "version.txt"))
                {
                    using StreamReader reader = new(stream);
                    gitVersion = reader.ReadToEnd();
                }
                o.Release = string.Format("chronokeep-windows@{0}", gitVersion);
            });
            Log.D("UI.MainWindow", "Looking for log directory.");
            string logDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR, "logs");
            if (!Directory.Exists(logDirPath))
            {
                Log.D("UI.MainWindow", "Creating log directory.");
                Directory.CreateDirectory(logDirPath);
            }
            Globals.ErrorLogPath = Path.Combine(logDirPath, string.Format("{0}_error_log.txt", DateTime.Now.ToString("yyyyMMdd")));
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string logDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR, "logs");
            if (!Directory.Exists(logDirPath))
            {
                Log.D("UI.MainWindow", "Creating log directory.");
                Directory.CreateDirectory(logDirPath);
            }
            string date = DateTime.Now.ToString("yyyyMMdd");
            string logPath = Path.Combine(logDirPath, string.Format("{0}_crash_0.txt", date));
            int ix = 0;
            while (File.Exists(logPath))
            {
                ix++;
                logPath = Path.Combine(logDirPath, string.Format("{0}_crash_{1}.txt", date, ix));
            }
            File.WriteAllText(logPath, e.Exception.StackTrace);

            // Let the app crash.
            e.Handled = false;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool safeMode = false;
            foreach (string arg in e.Args)
            {
                Log.D("AppStartup", "Startup arg: " + arg);
                if (arg.Contains("safe", System.StringComparison.OrdinalIgnoreCase))
                {
                    safeMode = true;
                }
            }
            if (safeMode)
            {
                MinWindow minWindow = new();
                minWindow.Show();
            }
            else
            {
                MainWindow mainWindow = new();
                mainWindow.Show();
            }
        }

        public void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled && sender != null)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
