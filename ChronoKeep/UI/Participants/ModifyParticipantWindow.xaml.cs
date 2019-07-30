using ChronoKeep.Interfaces;
using ChronoKeep.UI.MainPages;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChronoKeep.UI.Participants
{
    /// <summary>
    /// Interaction logic for ModifyParticipantWindow.xaml
    /// </summary>
    public partial class ModifyParticipantWindow : Window
    {
        IMainWindow window;
        IDBInterface database;
        TimingPage tPage;
        Event theEvent;
        Participant person;

        private bool ParticipantChanged = false;

        public ModifyParticipantWindow(IMainWindow window, IDBInterface database, Participant person)
        {
            InitializeComponent();
            this.window = window;
            this.tPage = null;
            this.database = database;
            this.person = person;
            theEvent = database.GetCurrentEvent();
            if (person == null)
            {
                Add.Click += new RoutedEventHandler(this.Add_Click);
                UpdateDivisions();
            }
            else
            {
                Add.Click += new RoutedEventHandler(this.Modify_Click);
                UpdateAllFields();
            }
            BibBox.Focus();
            if (theEvent.AllowEarlyStart)
            {
                EarlyStartBox.Visibility = Visibility.Visible;
            }
            else
            {
                EarlyStartBox.Visibility = Visibility.Collapsed;
                EarlyStartBox.IsChecked = false;
            }
        }

        public ModifyParticipantWindow(TimingPage tPage, IDBInterface database, int EventSpecificId, int Bib)
        {
            InitializeComponent();
            this.window = null;
            this.tPage = tPage;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            person = database.GetParticipantEventSpecific(theEvent.Identifier, EventSpecificId);
            if (person == null)
            {
                BibBox.Text = Bib.ToString();
                Add.Click += new RoutedEventHandler(this.Add_Click);
                UpdateDivisions();
            }
            else
            {
                Add.Click += new RoutedEventHandler(this.Modify_Click);
                UpdateAllFields();
            }
            BibBox.IsEnabled = false;
            if (theEvent.AllowEarlyStart)
            {
                EarlyStartBox.Visibility = Visibility.Visible;
            }
            else
            {
                EarlyStartBox.Visibility = Visibility.Collapsed;
                EarlyStartBox.IsChecked = false;
            }
        }

        public static ModifyParticipantWindow NewWindow(IMainWindow window, IDBInterface database, Participant person = null)
        {
            return new ModifyParticipantWindow(window, database, person);
        }

        private void UpdateDivisions()
        {
            if (theEvent == null || theEvent.Identifier < 0)
                return;
            List<Division> divs = database.GetDivisions(theEvent.Identifier);
            DivisionBox.Items.Clear();
            divs.Sort();
            foreach (Division d in divs)
            {
                DivisionBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            DivisionBox.SelectedIndex = 0;
            GenderBox.SelectedIndex = 0;
        }

        private void UpdateAllFields()
        {
            if (person == null || theEvent == null || theEvent.Identifier < 0)
                return;
            List<Division> divs = database.GetDivisions(theEvent.Identifier);
            DivisionBox.Items.Clear();
            divs.Sort();
            ComboBoxItem selected = null;
            foreach (Division d in divs)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                };
                if (d.Identifier == person.EventSpecific.DivisionIdentifier)
                {
                    selected = item;
                }
                DivisionBox.Items.Add(item);
            }
            DivisionBox.SelectedItem = selected;
            BibBox.Text = person.Bib.ToString();
            FirstBox.Text = person.FirstName;
            LastBox.Text = person.LastName;
            BirthdayBox.Text = person.Birthdate;
            AgeBox.Text = person.Age(theEvent.Date);
            GenderBox.SelectedIndex = person.Gender.Equals("M", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            StreetBox.Text = person.Street;
            Street2Box.Text = person.Street2;
            CityBox.Text = person.City;
            StateBox.Text = person.State;
            ZipBox.Text = person.Zip;
            CountryBox.Text = person.Country;
            EmailBox.Text = person.Email;
            MobileBox.Text = person.Mobile;
            ParentBox.Text = person.Parent;
            CommentsBox.Text = person.Comments;
            OtherBox.Text = person.Other;
            CheckedInBox.IsChecked = person.IsCheckedIn;
            EarlyStartBox.IsChecked = person.IsEarlyStart;
            ECNameBox.Text = person.ECName;
            ECPhoneBox.Text = person.ECPhone;
            Add.Content = "Update";
            Done.Content = "Cancel";
        }

        private void Clear()
        {
            DivisionBox.SelectedItem = 0;
            BibBox.Text = "";
            FirstBox.Text = "";
            LastBox.Text = "";
            BirthdayBox.Text = "";
            AgeBox.Text = "";
            GenderBox.SelectedIndex = 0;
            StreetBox.Text = "";
            Street2Box.Text = "";
            CityBox.Text = "";
            StateBox.Text = "";
            ZipBox.Text = "";
            CountryBox.Text = "";
            EmailBox.Text = "";
            MobileBox.Text = "";
            ParentBox.Text = "";
            CommentsBox.Text = "";
            OtherBox.Text = "";
            CheckedInBox.IsChecked = false;
            EarlyStartBox.IsChecked = false;
            ECNameBox.Text = "";
            ECPhoneBox.Text = "";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add clicked.");
            if (person != null && person.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                ParticipantChanged = true;
            }
            Participant newPart = FromFields();
            Participant offendingBib = null;
            if (newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                offendingBib = database.GetParticipantBib(theEvent.Identifier, newPart.Bib);
            }
            if (offendingBib != null)
            {
                // bib is taken
                MessageBoxResult result = MessageBox.Show("This bib is already taken. Assign no bib to the previous bib owner?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (MessageBoxResult.Yes == result)
                {
                    offendingBib.EventSpecific.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
                    database.UpdateParticipant(offendingBib);
                }
                else if (MessageBoxResult.No == result)
                {
                    return;
                }
            }
            if (newPart != null)
            {
                if (newPart.FirstName.Trim().Length < 1 || newPart.LastName.Trim().Length < 1)
                {
                    MessageBox.Show("Invalid name given.");
                    return;
                }
                if (newPart.Birthdate.Length < 1)
                {
                    MessageBox.Show("Birthdate or Age not specified.");
                    return;
                }
                database.AddParticipant(newPart);
                if (newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    ParticipantChanged = true;
                }
            }
            Clear();
            BibBox.Focus();
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Modify clicked.");
            if (person != null && person.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                ParticipantChanged = true;
            }
            Participant newPart = FromFields();
            Participant offendingBib = null;
            if (newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                offendingBib = database.GetParticipantBib(theEvent.Identifier, newPart.Bib);
            }
            if (offendingBib != null && newPart.Identifier != offendingBib.Identifier)
            {
                // bib is taken
                Participant oldPart = database.GetParticipant(theEvent.Identifier, newPart);
                MessageBoxResult result = MessageBox.Show("This bib is already taken. Swap bibs?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (MessageBoxResult.Yes == result)
                {
                    offendingBib.EventSpecific.Bib = oldPart.EventSpecific.Bib;
                    database.UpdateParticipant(offendingBib);
                    database.UpdateParticipant(newPart);
                    ParticipantChanged = true;
                    this.Close();
                }
                else if (MessageBoxResult.No == result)
                {
                    BibBox.Text = oldPart.Bib.ToString();
                    return;
                }
            }
            if (newPart != null)
            {
                database.UpdateParticipant(newPart);
                if (newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    ParticipantChanged = true;
                }
                this.Close();
            }
        }

        private Participant FromFields()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return null;
            }
            int eventSpecificId = -1, participantId = -1, nextYear = -1;
            if (person != null)
            {
                eventSpecificId = person.EventSpecific.Identifier;
                participantId = person.Identifier;
                nextYear = person.EventSpecific.NextYear;
            }
            int bib = Constants.Timing.CHIPREAD_DUMMYBIB;
            try
            {
                bib = int.Parse(BibBox.Text);
            }
            catch { }
            string gender = "Male";
            if (GenderBox.SelectedItem != null)
            {
                gender = ((ComboBoxItem)GenderBox.SelectedItem).Content.ToString();
            }
            int earlystart = 0, checkedin = 0;
            if (CheckedInBox.IsChecked ?? false)
            {
                checkedin = 1;
            }
            if (EarlyStartBox.IsChecked ?? false)
            {
                earlystart = 1;
            }
            int.TryParse(AgeBox.Text, out int age);
            string birthdate = BirthdayBox.Text;
            if (age != 0 && birthdate.Length < 1)
            {
                int.TryParse(theEvent.Date.Split('/')[2], out int year);
                year = year < 1969 ? DateTime.Now.Year : year;
                birthdate = "1/1/" + (year - age);
            }
            Participant output = new Participant(
                participantId,
                FirstBox.Text,
                LastBox.Text,
                StreetBox.Text,
                CityBox.Text,
                StateBox.Text,
                ZipBox.Text,
                birthdate,
                new EventSpecific(
                    eventSpecificId,
                    theEvent.Identifier,
                    Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid),
                    "",
                    bib,
                    checkedin,
                    CommentsBox.Text,
                    "",
                    OtherBox.Text,
                    earlystart,
                    nextYear,
                    Constants.Timing.TIMERESULT_DUMMYAGEGROUP,
                    Constants.Timing.EVENTSPECIFIC_NOSHOW),
                EmailBox.Text,
                MobileBox.Text,
                ParentBox.Text,
                CountryBox.Text,
                Street2Box.Text,
                gender,
                ECNameBox.Text,
                ECPhoneBox.Text);
            return output;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ParticipantChanged)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                if (window != null)
                {
                    window.DatasetChanged();
                    window.NotifyRecalculateAgeGroups();
                    window.NotifyTimingWorker();
                }
                if (tPage != null)
                {
                    tPage.DatasetChanged();
                    tPage.UpdateView();
                    tPage.NotifyRecalculateAgeGroups();
                    tPage.NotifyTimingWorker();
                }
            }
            if (window != null) window.WindowFinalize(this);
        }

        private void Box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }
    }
}
