using Chronokeep.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
