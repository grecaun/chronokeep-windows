using Chronokeep.UI;
using System.Windows;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool safeMode = false;
            foreach (string arg in e.Args)
            {
                Log.D("AppStartup", "Startup arg: " + arg);
                if (arg.IndexOf("safe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    safeMode = true;
                }
            }
            if (safeMode)
            {
                MinWindow minWindow = new MinWindow();
                minWindow.Show();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
    }
}
