using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chronokeep.Helpers;
using Chronokeep.UI;
using Microsoft.Extensions.Hosting.Internal;
using Sentry;
using System;
using System.IO;
using System.Reflection;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public override void Initialize()
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = "https://69f609b68d42089ccea1545e8942c9e6@o4510042186514432.ingest.us.sentry.io/4510042244317184";
                options.IsGlobalModeEnabled = true;
                options.StackTraceMode = StackTraceMode.Enhanced;
                options.SendDefaultPii = false;
#if DEBUG
                options.Environment = "debug";
#else
                options.Environment = "release";
#endif
                string gitVersion = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep." + "version.txt"))
                {
                    using StreamReader reader = new(stream);
                    gitVersion = reader.ReadToEnd();
                }
                options.Release = string.Format("chronokeep-windows@{0}", gitVersion);
            });
            Log.D("UI.MainWindow", "Looking for log directory.");
            string logDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR, "logs");
            if (!Directory.Exists(logDirPath))
            {
                Log.D("UI.MainWindow", "Creating log directory.");
                Directory.CreateDirectory(logDirPath);
            }
            Globals.ErrorLogPath = Path.Combine(logDirPath, string.Format("{0}_error_log.txt", DateTime.Now.ToString("yyyyMMdd")));
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                bool safeMode = false;
                foreach (string arg in desktop.Args)
                {
                    Log.D("AppStartup", "Startup arg: " + arg);
                    if (arg.Contains("safe", System.StringComparison.OrdinalIgnoreCase))
                    {
                        safeMode = true;
                    }
                }
                if (safeMode)
                {
                    desktop.MainWindow = new MinWindow();
                }
                else
                {
                    desktop.MainWindow = new MinWindow();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        static void CaptureException(Exception ex)
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
            File.WriteAllText(logPath, ex.StackTrace);
            SentrySdk.CaptureException(ex);
        }
    }
}
