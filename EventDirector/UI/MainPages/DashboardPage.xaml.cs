using EventDirector.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        public DashboardPage(INewMainWindow mainWindow, IDBInterface db)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = db;

            int eventId = Convert.ToInt32(database.GetAppSetting(Constants.Settings.CURRENT_EVENT).value);
            theEvent = database.GetEvent(eventId);
            Update();
        }

        private void Update()
        {

        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Button Clicked.");
        }

        private async void StartAppService_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Start App Service Button Clicked.");
            if (startAppService.Content.ToString() == "Start Network Service")
            {
                SetNetworkStuffs(1);
                bool worked = await mWindow.StartNetworkServices(); 
                if (worked)
                {
                    SetNetworkStuffs(2);
                }
                else
                {
                    SetNetworkStuffs(0);
                }
            }
            else if (startAppService.Content.ToString() == "Stop Network Service")
            {
                SetNetworkStuffs(1);
                bool worked = await mWindow.StopNetworkServices();
                if (!worked)
                {
                    SetNetworkStuffs(2);
                }
                else
                {
                    SetNetworkStuffs(0);
                }
            }
        }

        private void SetNetworkStuffs(int which)
        {
            if (which == 1)
            {
                startAppService.Content = "Working...";
            }
            else if (which == 2)
            {
                startAppService.Content = "Stop Network Service";
                startCheckIn.Visibility = Visibility.Visible;
                openResults.Visibility = Visibility.Visible;
                setupNextYear.Visibility = Visibility.Visible;
                setupKiosk.Visibility = Visibility.Visible;
            }
            else
            {
                startAppService.Content = "Start Network Service";
                startCheckIn.Visibility = Visibility.Collapsed;
                openResults.Visibility = Visibility.Collapsed;
                setupNextYear.Visibility = Visibility.Collapsed;
                setupKiosk.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupKiosk_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Setup Kiosk Button Clicked.");
        }

        private void StartCheckIn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenResults_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetupNextYear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NewEvent_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ImportEvent_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ChangeEvent_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
