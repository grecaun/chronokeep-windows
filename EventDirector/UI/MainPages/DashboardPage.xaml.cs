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
    public partial class DashboardPage : Page, IMainPage
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        public DashboardPage(INewMainWindow mainWindow, IDBInterface db)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = db;
            Update();
        }

        public void Update()
        {
            theEvent = database.GetCurrentEvent();
            eventNameTextBox.Text = theEvent.Name;
            eventYearCodeTextBox.Text = theEvent.YearCode;
            eventDatePicker.Text = theEvent.Date;
            rankByGunCheckBox.IsChecked = theEvent.RankByGun == 1;
            commonAgeCheckBox.IsChecked = theEvent.CommonAgeGroups == 1;
            commonStartCheckBox.IsChecked = theEvent.CommonStartFinish == 1;
            segmentCheckBox.IsChecked = theEvent.DivisionSpecificSegments == 1;
            if (theEvent.AllowEarlyStart == 1)
            {
                earlyCheckBox.IsChecked = true;
                earlyTimePanel.Visibility = Visibility.Visible;
                earlyTimeTextBox.Text = theEvent.GetEarlyStartString();
            }
            else
            {
                earlyCheckBox.IsChecked = false;
                earlyTimePanel.Visibility = Visibility.Collapsed;
                earlyTimeTextBox.Text = theEvent.GetEarlyStartString();
            }
            List<JsonOption> options = database.GetEventOptions(theEvent.Identifier);
            foreach (JsonOption opt in options)
            {
                if (opt.Name == Constants.JsonOptions.RESULTS)
                {
                    if (opt.Value == Constants.JsonOptions.TRUE)
                    {
                        openResults.Content = Constants.DashboardLabels.CLOSE_RESULTS;
                    }
                    else
                    {
                        openResults.Content = Constants.DashboardLabels.OPEN_RESULTS;
                    }
                }
                else if (opt.Name == Constants.JsonOptions.REGISTRATION)
                {
                    if (opt.Value == Constants.JsonOptions.TRUE)
                    {
                        startCheckIn.Content = Constants.DashboardLabels.STOP_CHECKIN;
                    }
                    else
                    {
                        startCheckIn.Content = Constants.DashboardLabels.START_CHECKIN;
                    }
                }
                else if (opt.Name == Constants.JsonOptions.KIOSK)
                {
                    if (opt.Value == Constants.JsonOptions.TRUE)
                    {
                        setupKiosk.Content = Constants.DashboardLabels.CANCEL_KIOSK;
                    }
                    else
                    {
                        setupKiosk.Content = Constants.DashboardLabels.SETUP_KIOSK;
                    }
                }
            }
            if (theEvent.NextYear != -1)
            {
                setupNextYear.Content = Constants.DashboardLabels.CANCEL_NEXT_YEAR;
            }
            else
            {
                setupNextYear.Content = Constants.DashboardLabels.SETUP_NEXT_YEAR;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Button Clicked.");
            if (editButton.Content.ToString() == Constants.DashboardLabels.EDIT)
            {
                Log.D("Editing.");
                editButton.Content = Constants.DashboardLabels.WORKING;
                EnableEditableFields();
                editButton.Content = Constants.DashboardLabels.SAVE;
                cancelButton.Visibility = Visibility.Visible;
            }
            else if (editButton.Content.ToString() == Constants.DashboardLabels.SAVE)
            {
                Log.D("Saving");
                editButton.Content = Constants.DashboardLabels.WORKING;
                DisableEditableFields();
                theEvent.Name = eventNameTextBox.Text;
                theEvent.YearCode = eventYearCodeTextBox.Text;
                theEvent.Date = eventDatePicker.Text;
                theEvent.RankByGun = (rankByGunCheckBox.IsChecked ?? false) ? 1 : 0;
                theEvent.CommonAgeGroups = (commonAgeCheckBox.IsChecked ?? false) ? 1 : 0;
                theEvent.CommonStartFinish = (commonStartCheckBox.IsChecked ?? false) ? 1 : 0;
                theEvent.DivisionSpecificSegments = (segmentCheckBox.IsChecked ?? false) ? 1 : 0;
                theEvent.AllowEarlyStart = (earlyCheckBox.IsChecked ?? false) ? 1 : 0;
                UpdateEarlyTimeTextBox();
                string[] nums = earlyTimeTextBox.Text.Split(':');
                if (nums.Length == 3)
                {
                    theEvent.EarlyStartDifference = (Convert.ToInt32(nums[0]) * 3600) + (Convert.ToInt32(nums[1]) * 60) + Convert.ToInt32(nums[2]);
                }
                database.UpdateEvent(theEvent);
                try
                {
                    mWindow.UpdateEvent(theEvent.Identifier, "", 0, 0, 0, 0);
                }
                catch
                {
                    Log.D("Unable to update event with mainwindow. TCP Server error or wrong main window.");
                }
                editButton.Content = Constants.DashboardLabels.EDIT;
            }
            else
            {
                Log.D("Crying.");
            }
        }

        public void DisableEditableFields()
        {
            eventNameTextBox.IsEnabled = false;
            eventYearCodeTextBox.IsEnabled = false;
            eventDatePicker.IsEnabled = false;
            rankByGunCheckBox.IsEnabled = false;
            commonAgeCheckBox.IsEnabled = false;
            commonStartCheckBox.IsEnabled = false;
            segmentCheckBox.IsEnabled = false;
            earlyCheckBox.IsEnabled = false;
            earlyTimeTextBox.IsEnabled = false;
        }

        public void EnableEditableFields()
        {
            eventNameTextBox.IsEnabled = true;
            eventYearCodeTextBox.IsEnabled = true;
            eventDatePicker.IsEnabled = true;
            rankByGunCheckBox.IsEnabled = true;
            commonAgeCheckBox.IsEnabled = true;
            commonStartCheckBox.IsEnabled = true;
            segmentCheckBox.IsEnabled = true;
            earlyCheckBox.IsEnabled = true;
            earlyTimeTextBox.IsEnabled = true;
        }

        private void UpdateEarlyTimeTextBox()
        {
            String startTimeValue = earlyTimeTextBox.Text.Replace('_', '0');
            earlyTimeTextBox.Text = startTimeValue;
        }

        private async void StartAppService_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Start App Service Button Clicked.");
            if (startAppService.Content.ToString() == Constants.DashboardLabels.START_NETWORK)
            {
                startAppService.Content = Constants.DashboardLabels.WORKING;
                bool worked = false;
                await Task.Run(() =>
                {
                    worked = mWindow.StartNetworkServices();
                });
                if (worked)
                {
                    SetNetworkWorking();
                }
                else
                {
                    SetNetworkStopped();
                }
            }
            else if (startAppService.Content.ToString() == Constants.DashboardLabels.STOP_NETWORK)
            {
                startAppService.Content = Constants.DashboardLabels.WORKING;
                bool worked = false;
                await Task.Run(() =>
                {
                    worked = mWindow.StopNetworkServices();
                });
                if (!worked)
                {
                    SetNetworkWorking();
                }
                else
                {
                    SetNetworkStopped();
                }
            }
        }

        private void SetNetworkStopped()
        {
            startAppService.Content = Constants.DashboardLabels.START_NETWORK;
            startCheckIn.Visibility = Visibility.Collapsed;
            openResults.Visibility = Visibility.Collapsed;
            setupNextYear.Visibility = Visibility.Collapsed;
            setupKiosk.Visibility = Visibility.Collapsed;
        }

        private void SetNetworkWorking()
        {
            startAppService.Content = Constants.DashboardLabels.STOP_NETWORK;
            startCheckIn.Visibility = Visibility.Visible;
            openResults.Visibility = Visibility.Visible;
            setupNextYear.Visibility = Visibility.Visible;
            setupKiosk.Visibility = Visibility.Visible;
        }

        private void SetupKiosk_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Setup Kiosk Button Clicked.");
        }

        private void StartCheckIn_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Start checkin clicked.");
            bool value = false;
            if (startCheckIn.Content.ToString() == Constants.DashboardLabels.START_CHECKIN)
            {
                value = true;
                startCheckIn.Content = Constants.DashboardLabels.STOP_CHECKIN;
            }
            else
            {
                startCheckIn.Content = Constants.DashboardLabels.START_CHECKIN;
            }
            List<JsonOption> list = database.GetEventOptions(theEvent.Identifier);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == Constants.JsonOptions.REGISTRATION)
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(theEvent.Identifier, list);
            try
            {
                mWindow.UpdateEvent(theEvent.Identifier, "", 0, 0, 0, 0);
            }
            catch
            {
                Log.D("Main window failed updating the event. Either the TCP Server isn't running, or someone is using the wrong Main Window.");
            }
        }

        private void OpenResults_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Open Results clicked.");
            bool value = false;
            if (openResults.Content.ToString() == Constants.DashboardLabels.OPEN_RESULTS)
            {
                value = true;
                openResults.Content = Constants.DashboardLabels.CLOSE_RESULTS;
            }
            else
            {
                openResults.Content = Constants.DashboardLabels.OPEN_RESULTS;
            }
            List<JsonOption> list = database.GetEventOptions(theEvent.Identifier);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == Constants.JsonOptions.RESULTS)
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(theEvent.Identifier, list);
            try
            {
                mWindow.UpdateEvent(theEvent.Identifier, "", 0, 0, 0, 0);
            }
            catch
            {
                Log.D("Main window failed updating the event. Either the TCP Server isn't running, or someone is using the wrong Main Window.");
            }
        }

        private void SetupNextYear_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Setup Next Year Mode clicked.");
            if (setupNextYear.Content.ToString() == Constants.DashboardLabels.SETUP_NEXT_YEAR)
            {
                setupNextYear.Content = Constants.DashboardLabels.WORKING;
                NextYearSetup nysetup = new NextYearSetup(mWindow, database, theEvent);
                nysetup.Show();
            }
            else if (setupNextYear.Content.ToString() == Constants.DashboardLabels.CANCEL_NEXT_YEAR)
            {
                setupNextYear.Content = Constants.DashboardLabels.WORKING;
                theEvent.NextYear = -1;
                database.UpdateEvent(theEvent);
                Update();
            }
        }

        private void NewEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("New event clicked.");
        }

        private void ImportEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import event clicked.");
        }

        private void ChangeEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Change event clicked.");
        }

        private void EarlyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Allow early start clicked.");
            if (earlyCheckBox.IsChecked == true)
            {
                earlyTimePanel.Visibility = Visibility.Visible;
            }
            else
            {
                earlyTimePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Delete event clicked.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            DisableEditableFields();
            Update();
            editButton.Content = Constants.DashboardLabels.EDIT;
            cancelButton.Visibility = Visibility.Collapsed;
        }
    }
}
