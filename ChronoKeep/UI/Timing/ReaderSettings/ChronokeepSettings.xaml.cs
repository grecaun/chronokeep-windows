using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.UIObjects;
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
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing.ReaderSettings
{
    /// <summary>
    /// Interaction logic for ChronokeepSettings.xaml
    /// </summary>
    public partial class ChronokeepSettings : UiWindow
    {
        private ChronokeepInterface reader = null;
        private IDBInterface database = null;

        private bool saving = false;

        internal ChronokeepSettings(ChronokeepInterface reader, IDBInterface database)
        {
            InitializeComponent();
            this.MinWidth = 100;
            this.MinHeight = 100;
            this.reader = reader;
            this.database = database;
            reader.SendGetSettings();
        }

        private void uploadParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Upload participants button clicked.");
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            try
            {
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                List<PortalParticipant> uploadParticipants = new List<PortalParticipant>();
                foreach (Participant participant in participants)
                {
                    uploadParticipants.Add(new PortalParticipant
                    {
                        Bib = participant.Bib,
                        First = participant.FirstName,
                        Last = participant.LastName,
                        Age = participant.GetAge(theEvent.Date),
                        Gender = participant.Gender,
                        AgeGroup = participant.EventSpecific.AgeGroupName,
                        Distance = participant.Distance,
                        Chip = participant.Chip,
                        Anonymous = participant.Anonymous,
                    });
                }
                reader.SendUploadParticipants(uploadParticipants);
                DialogBox.Show("Participants successfully uploaded.");
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.ReaderSettings.ChronokeepSettings", string.Format("something went wrong trying to upload participants: " + ex.Message));
                DialogBox.Show("Something went wrong uploading participants.");
            }
        }

        private void removeParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Remove participants button clicked.");
            DialogBox.Show(
                "This is not reversable, are you sure you want to do this?",
                "Yes",
                "No",
                () =>
                {
                    reader.SendRemoveParticipants();
                }
                );
        }

        private void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Stop button clicked.");
            DialogBox.Show(
                "This will stop the portal software. Do you want to proceed?",
                "Yes",
                "No",
                () =>
                {
                    // send stop command
                    reader.SendStop();
                }
                );
        }

        private void shutdownServerButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Shutdown button clicked.");
            DialogBox.Show(
                "This will shutdown the entire computer the portal software is running on. Do you want to proceed?",
                "Yes",
                "No",
                () =>
                    {
                        // send shutdown command
                        reader.SendShutdown();
                    }
                );
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Save button clicked.");
            saving = true;
            int window = 0;
            string[] split = sightingPeriodBox.Text.Split(':');
            if (split.Length > 0)
            {
                switch (split.Length)
                {
                    case 1:
                        int.TryParse(split[0], out window);
                        break;
                    case 2:
                        if (split[1].Length == 2)
                        {
                            int minutes, seconds;
                            if (int.TryParse(split[0], out minutes) &&
                                int.TryParse(split[1], out seconds)) {
                                window = seconds + (minutes * 60);
                            }
                        }
                        break;
                    case 3:
                        if (split[1].Length == 2 && split[2].Length == 2)
                        {
                            int hours, minutes, seconds;
                            if (int.TryParse(split[0], out hours) &&
                                int.TryParse(split[1], out minutes) &&
                                int.TryParse(split[2], out seconds))
                            {
                                window = seconds + (minutes * 60) + (hours * 3600);
                            }
                        }
                        break;
                }
            }
            try
            {
                AllPortalSettings sett = new AllPortalSettings
                {
                    Name = nameBox.Text.Trim(),
                    SightingPeriod = window,
                    ReadWindow = int.Parse(readWindowBox.Text.Trim()),
                    ChipType = chipTypeBox.SelectedIndex == 0 ? AllPortalSettings.ChipTypeEnum.DEC : AllPortalSettings.ChipTypeEnum.HEX,
                    Volume = volumeSlider.Value / 10,
                    PlaySound = soundBox.IsChecked == true,
                };
                reader.SendSetSettings(sett);
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.ReaderSettings.ChronokeepSettings", "Error saving settings: " + ex.Message);
                DialogBox.Show("Error saving settings.");
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Close button clicked.");
            this.Close();
        }

        private void swapButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Swap button clicked.");
            if (loadingPanel.Visibility == Visibility.Collapsed)
            {
                loadingPanel.Visibility = Visibility.Visible;
                settingsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                loadingPanel.Visibility = Visibility.Collapsed;
                settingsPanel.Visibility = Visibility.Visible;
            }
        }

        internal void UpdateView(AllPortalSettings allSettings, bool settings, bool readers, bool apis)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "UpdateView.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (saving)
                {
                    this.Close();
                }
                settingsPanel.Visibility = Visibility.Visible;
                loadingPanel.Visibility = Visibility.Collapsed;
                if (settings)
                {
                    nameBox.Text = allSettings.Name;
                    if (allSettings.SightingPeriod > 3600)
                    {
                        sightingPeriodBox.Text = Constants.Timing.SecondsToTime(allSettings.SightingPeriod);
                    }
                    else
                    {
                        sightingPeriodBox.Text = string.Format("{0}:{1:D2}", allSettings.SightingPeriod / 60, allSettings.SightingPeriod % 60);
                    }
                    readWindowBox.Text = allSettings.ReadWindow.ToString();
                    chipTypeBox.SelectedIndex = allSettings.ChipType == AllPortalSettings.ChipTypeEnum.DEC ? 0 : 1;
                    volumeSlider.Value = allSettings.Volume * 10;
                    soundBox.IsChecked = allSettings.PlaySound;
                }
                // add readers and apis to views
                if (readers)
                {

                }
                if (apis)
                {

                }
            }));
        }

        public void CloseWindow()
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "CloseWindow.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                Close();
            }));
        }

        private void UiWindow_Closed(object sender, EventArgs e)
        {
            reader.SettingsWindowFinalize();
        }
    }
}
