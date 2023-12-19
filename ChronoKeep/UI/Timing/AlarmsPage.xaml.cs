using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.IO;
using Chronokeep.IO.HtmlTemplates.Printables;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for AlarmPage.xaml
    /// </summary>
    public partial class AlarmsPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;

        public AlarmsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("UI.Timing.AlarmsPage", "Something went wrong and no proper event was returned.");
                return;
            }
            // get alarms from the database
            // alarms = database.GetAlarms(theEvent.Identifier);
            UpdateView();
        }
        
        public void CancelableUpdateView(CancellationToken token) { }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AlarmsPage", "Done clicked.");
            parent.LoadMainDisplay();
        }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() { }

        public void UpdateView()
        {
            Log.D("UI.Timing.AlarmsPage", "Updating View.");
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            AlarmsBox.Items.Clear();
            // alarms = database.GetAlarms(theEvent.Identifier);
            List<Alarm> alarms = Alarm.GetAlarms();
            alarms.Sort();
            foreach (Alarm alarm in alarms)
            {
                AlarmsBox.Items.Add(new AnAlarmItem(this, alarm));
            }
        }

        private void SaveAlarms()
        {
            Log.D("UI.Timing.AlarmsPage", "Saving Alarms.");
            Alarm.ClearAlarms();
            foreach (AnAlarmItem alarm in AlarmsBox.Items)
            {
                Alarm.AddAlarm(alarm.GetUpdatedAlarm());
            }
            /*
             * database.SaveAlarms(alarms);
             */
        }

        public void Closing()
        {
            Log.D("UI.Timing.AlarmsPage", "Closing Page.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                if (AlarmErrors(true))
                {
                    return;
                }
                SaveAlarms();
            }
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S()
        {
            Log.D("UI.Timing.AlarmsPage", "Ctrl+S pressed.");
            if (AlarmErrors())
            {
                return;
            }
            SaveAlarms();
        }

        public void Keyboard_Ctrl_Z()
        {
            Log.D("UI.Timing.AlarmsPage", "Ctrl+Z pressed.");
            UpdateView();
        }

        private void AlarmsBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Utils.GetScrollViewer(sender as DependencyObject) is ScrollViewer scrollViewer)
            {
                if (e.Delta < 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 35);
                }
                else if (e.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 35);
                }
            }
        }

        private void RemoveAlarm(AnAlarmItem alarm)
        {
            AlarmsBox.Items.Remove(alarm);
        }

        private bool AlarmErrors(bool silent = false)
        {
            // Verify there are no repeating bibs/chips.
            HashSet<int> bibs = new HashSet<int>();
            HashSet<string> chips = new HashSet<string>();
            bool notSetExists = false;
            foreach (AnAlarmItem alarm in AlarmsBox.Items)
            {
                Alarm al = alarm.GetUpdatedAlarm();
                if (al.Bib >= 0 && bibs.Contains(al.Bib))
                {
                    if (!silent)
                    {
                        DialogBox.Show("Unable to continue, multiples of the same bib found.");
                    }
                    return true;
                }
                else
                {
                    bibs.Add(al.Bib);
                }
                if (al.Chip.Length > 0 && chips.Contains(al.Chip))
                {
                    if (!silent)
                    {
                        DialogBox.Show("Unable to continue, multiples of the same chip found.");
                    }
                    return true;
                }
                else
                {
                    chips.Add(al.Chip);
                }
                if (al.Bib < 0 && al.Chip.Length == 0)
                {
                    if (notSetExists)
                    {
                        if (!silent)
                        {
                            DialogBox.Show("Only one alarm without a bib & chip allowed at a time.");
                        }
                        return true;
                    }
                    else
                    {
                        notSetExists = true;
                    }
                }
            }
            return false;
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AlarmsPage", "Add button clicked.");
            AlarmsBox.Items.Add(new AnAlarmItem(this, new Alarm()));
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AlarmsPage", "Save button clicked.");
            if (AlarmErrors())
            {
                return;
            }
            SaveAlarms();
            UpdateView();
        }

        private class AnAlarmItem : ListBoxItem
        {
            public Wpf.Ui.Controls.TextBox BibBox;
            public Wpf.Ui.Controls.TextBox ChipBox;
            public ComboBox AlarmSoundBox;
            public Wpf.Ui.Controls.ToggleSwitch EnabledBox;
            public Wpf.Ui.Controls.Button RemoveButton;

            readonly AlarmsPage page;
            private Alarm theAlarm;

            // Allow only numbers in our bib/alert count boxes.
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public AnAlarmItem(AlarmsPage page, Alarm alarm)
            {
                this.page = page;
                this.theAlarm = alarm;
                Grid theGrid = new Grid()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                this.Content = theGrid;
                this.IsTabStop = false;

                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(120) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(175) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(55) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
                BibBox = new Wpf.Ui.Controls.TextBox()
                {
                    Text = alarm.Bib < 0 ? "" : alarm.Bib.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                BibBox.GotFocus += new RoutedEventHandler(SelectAll);
                BibBox.PreviewTextInput += new TextCompositionEventHandler(NumberValidation);
                theGrid.Children.Add(BibBox);
                Grid.SetColumn(BibBox, 0);
                ChipBox = new Wpf.Ui.Controls.TextBox()
                {
                    Text = alarm.Chip,
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                ChipBox.GotFocus += new RoutedEventHandler(SelectAll);
                theGrid.Children.Add(ChipBox);
                Grid.SetColumn(ChipBox, 1);
                AlarmSoundBox = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Default",
                        Uid = "0",
                    }
                    );
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Sound 1",
                        Uid = "1",
                    }
                    );
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Sound 2",
                        Uid = "2",
                    }
                    );
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Sound 3",
                        Uid = "3",
                    }
                    );
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Sound 4",
                        Uid = "4",
                    }
                    );
                AlarmSoundBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Sound 5",
                        Uid = "5",
                    }
                    );
                AlarmSoundBox.SelectedIndex = alarm.AlarmSound;
                theGrid.Children.Add(AlarmSoundBox);
                Grid.SetColumn(AlarmSoundBox, 2);
                Log.D("UI.Timing.AlarmsPage.AnAlarmItem", "Alarm enabled set to: " + alarm.Enabled.ToString());
                EnabledBox = new Wpf.Ui.Controls.ToggleSwitch()
                {
                    IsChecked = alarm.Enabled,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                theGrid.Children.Add(EnabledBox);
                Grid.SetColumn(EnabledBox, 3);
                RemoveButton = new Wpf.Ui.Controls.Button()
                {
                    Content = "X",
                    FontSize = 16,
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                RemoveButton.Click += new RoutedEventHandler(Remove_Click);
                theGrid.Children.Add(RemoveButton);
                Grid.SetColumn(RemoveButton, 4);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.AlarmsPage", "Removing alarm.");
                page.RemoveAlarm(this);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            public Alarm GetUpdatedAlarm()
            {
                int tmpBib;
                if (int.TryParse(BibBox.Text, out tmpBib) == false)
                {
                    tmpBib = -1;
                }
                theAlarm.Bib = tmpBib;
                if (theAlarm.Bib >= 0)
                {
                    theAlarm.Chip = "";
                }
                else
                {
                    theAlarm.Chip = ChipBox.Text;
                }
                theAlarm.Enabled = EnabledBox.IsChecked == true;
                theAlarm.AlarmSound = AlarmSoundBox.SelectedIndex;
                return theAlarm;
            }
        }
    }
}
 