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

        List<Alarm> alarms = new List<Alarm>();

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
            alarms.Sort();
            foreach (Alarm alarm in alarms)
            {
                AlarmsBox.Items.Add(new AnAlarmItem(this, alarm));
            }
        }

        private void SaveAlarms()
        {
            Log.D("UI.Timing.AlarmsPage", "Saving Alarms.");
            alarms.Clear();
            foreach (AnAlarmItem alarm in AlarmsBox.Items)
            {
                alarms.Add(alarm.GetUpdatedAlarm());
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
                SaveAlarms();
            }
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S()
        {
            Log.D("UI.Timing.AlarmsPage", "Ctrl+S pressed.");
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
            SaveAlarms();
            UpdateView();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AlarmsPage", "Add button clicked.");
            AlarmsBox.Items.Add(new AnAlarmItem(this, new Alarm()));
            SaveAlarms();
            UpdateView();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AlarmsPage", "Save button clicked.");
            SaveAlarms();
            UpdateView();
        }

        private class AnAlarmItem : ListBoxItem
        {
            public TextBox BibBox;
            public TextBox ChipBox;
            public TextBox AlertCountBox;
            public Label AlertedCountLabel;
            public ComboBox AlarmSoundBox;
            public System.Windows.Controls.CheckBox EnabledBox;
            public Button RemoveButton;

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
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(175) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(52) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
                BibBox = new TextBox()
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
                ChipBox = new TextBox()
                {
                    Text = alarm.Chip,
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                ChipBox.GotFocus += new RoutedEventHandler(SelectAll);
                theGrid.Children.Add(ChipBox);
                Grid.SetColumn(ChipBox, 1);
                AlertCountBox = new TextBox()
                {
                    Text = alarm.AlertCount < 1 ? "1" : alarm.AlertCount.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AlertCountBox.GotFocus += new RoutedEventHandler(SelectAll);
                AlertCountBox.PreviewTextInput += new TextCompositionEventHandler(NumberValidation);
                theGrid.Children.Add(AlertCountBox);
                Grid.SetColumn(AlertCountBox, 2);
                AlertedCountLabel = new Label()
                {
                    Content = alarm.AlertedCount.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                theGrid.Children.Add(AlertedCountLabel);
                Grid.SetColumn(AlertedCountLabel, 3);
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
                Grid.SetColumn(AlarmSoundBox, 4);
                EnabledBox = new System.Windows.Controls.CheckBox()
                {
                    IsChecked = alarm.Enabled,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                theGrid.Children.Add(EnabledBox);
                Grid.SetColumn(EnabledBox, 5);
                RemoveButton = new Button()
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
                Grid.SetColumn(RemoveButton, 6);
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
                int tmpBib, tmpAlertCount;
                if (int.TryParse(BibBox.Text, out tmpBib) == false)
                {
                    tmpBib = -1;
                }
                if (int.TryParse(AlertCountBox.Text, out tmpAlertCount) == false)
                {
                    tmpAlertCount = 1;
                }
                theAlarm.Bib = tmpBib;
                theAlarm.Chip = ChipBox.Text;
                theAlarm.AlertCount = tmpAlertCount;
                theAlarm.Enabled = EnabledBox.IsChecked == true;
                theAlarm.AlarmSound = AlarmSoundBox.SelectedIndex;
                return theAlarm;
            }
        }
    }
}
 