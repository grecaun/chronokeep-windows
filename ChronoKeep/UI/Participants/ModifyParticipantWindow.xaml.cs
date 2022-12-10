using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.Participants
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

        Dictionary<(int, int), AgeGroup> AgeGroups = AgeGroup.GetAgeGroups();
        Dictionary<int, AgeGroup> LastAgeGroup = AgeGroup.GetLastAgeGroup();

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
                UpdateDistances();
            }
            else
            {
                Add.Click += new RoutedEventHandler(this.Modify_Click);
                UpdateAllFields();
            }
            BibBox.Focus();
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
                UpdateDistances();
            }
            else
            {
                Add.Click += new RoutedEventHandler(this.Modify_Click);
                UpdateAllFields();
            }
            BibBox.IsEnabled = false;
        }

        public static ModifyParticipantWindow NewWindow(IMainWindow window, IDBInterface database, Participant person = null)
        {
            return new ModifyParticipantWindow(window, database, person);
        }

        private void UpdateDistances()
        {
            if (theEvent == null || theEvent.Identifier < 0)
                return;
            List<Distance> divs = database.GetDistances(theEvent.Identifier);
            DistanceBox.Items.Clear();
            divs.Sort();
            foreach (Distance d in divs)
            {
                DistanceBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            DistanceBox.SelectedIndex = 0;
            GenderBox.SelectedIndex = 0;
        }

        private void UpdateAllFields()
        {
            if (person == null || theEvent == null || theEvent.Identifier < 0)
                return;
            List<Distance> divs = database.GetDistances(theEvent.Identifier);
            DistanceBox.Items.Clear();
            divs.Sort();
            ComboBoxItem selected = null;
            foreach (Distance d in divs)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                };
                if (d.Identifier == person.EventSpecific.DistanceIdentifier)
                {
                    selected = item;
                }
                DistanceBox.Items.Add(item);
            }
            DistanceBox.SelectedItem = selected;
            BibBox.Text = person.Bib.ToString();
            FirstBox.Text = person.FirstName;
            LastBox.Text = person.LastName;
            BirthdayBox.Text = person.Birthdate;
            AgeBox.Text = person.Age(theEvent.Date);
            GenderBox.SelectedIndex = person.Gender.Equals("M", StringComparison.OrdinalIgnoreCase) ? 0 : person.Gender.Equals("F", StringComparison.OrdinalIgnoreCase) ? 1 : person.Gender.Equals("NB", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
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
            ECNameBox.Text = person.ECName;
            ECPhoneBox.Text = person.ECPhone;
            Add.Content = "Update";
            Done.Content = "Cancel";
        }

        private void Clear()
        {
            DistanceBox.SelectedItem = 0;
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
            ECNameBox.Text = "";
            ECPhoneBox.Text = "";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Participants.ModifyParticipantWindow", "Add clicked.");
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
            Log.D("UI.Participants.ModifyParticipantWindow", "Modify clicked.");
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
                // bib is taken - person object holds old bib #
                MessageBoxResult result = MessageBox.Show("This bib is already taken. Swap bibs?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (MessageBoxResult.Yes == result)
                {
                    offendingBib.EventSpecific.Bib = person.EventSpecific.Bib;
                    int newBib = newPart.EventSpecific.Bib;
                    newPart.EventSpecific.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
                    database.UpdateParticipant(newPart);
                    database.UpdateParticipant(offendingBib);
                    newPart.EventSpecific.Bib = newBib;
                    database.UpdateParticipant(newPart);
                    ParticipantChanged = true;
                    this.Close();
                }
                else if (MessageBoxResult.No == result)
                {
                    BibBox.Text = person.EventSpecific.Bib.ToString();
                    return;
                }
            }
            if (newPart != null)
            {
                Log.D("UI.Participants.ModifyParticipantWindow", "NewPart not null ---- Should update --- NewPart birthdate ----" + newPart.Birthdate);
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
            int eventSpecificId = -1, participantId = -1;
            if (person != null)
            {
                eventSpecificId = person.EventSpecific.Identifier;
                participantId = person.Identifier;
            }
            int bib = Constants.Timing.CHIPREAD_DUMMYBIB;
            try
            {
                bib = int.Parse(BibBox.Text);
            }
            catch { }
            string gender = "Not Specified";
            if (GenderBox.SelectedItem != null)
            {
                gender = ((ComboBoxItem)GenderBox.SelectedItem).Content.ToString();
            }
            if (gender.Equals("Non-Binary", StringComparison.OrdinalIgnoreCase))
            {
                gender = "NB";
            }
            else if (gender.Equals("Not Specified", StringComparison.OrdinalIgnoreCase))
            {
                gender = "U";
            }
            int checkedin = 0;
            int.TryParse(AgeBox.Text, out int age);
            string birthdate = BirthdayBox.Text;
            if (age != 0 && birthdate.Length < 1)
            {
                int.TryParse(theEvent.Date.Split('/')[2], out int year);
                year = year < 1969 ? DateTime.Now.Year : year;
                birthdate = "1/1/" + (year - age);
            }
            Log.D("UI.Participants.ModifyParticipantWindow", "----- Birthdate -----" + birthdate);
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
                    Convert.ToInt32(((ComboBoxItem)DistanceBox.SelectedItem).Uid),
                    "",
                    bib,
                    checkedin,
                    CommentsBox.Text,
                    "",
                    OtherBox.Text,
                    Constants.Timing.EVENTSPECIFIC_NOSHOW,
                    "0-110",
                    Constants.Timing.TIMERESULT_DUMMYAGEGROUP
                    ),
                EmailBox.Text,
                MobileBox.Text,
                ParentBox.Text,
                CountryBox.Text,
                Street2Box.Text,
                gender,
                ECNameBox.Text,
                ECPhoneBox.Text);
            int agDivId = theEvent.CommonAgeGroups ? Constants.Timing.COMMON_AGEGROUPS_DISTANCEID : output.EventSpecific.DistanceIdentifier;
            if (AgeGroups == null || age < 0)
            {
                output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                output.EventSpecific.AgeGroupName = "0-110";
            }
            else if (AgeGroups.ContainsKey((agDivId, age)))
            {
                AgeGroup group = AgeGroups[(agDivId, age)];
                output.EventSpecific.AgeGroupId = group.GroupId;
                output.EventSpecific.AgeGroupName = string.Format("{0}-{1}", group.StartAge, group.EndAge);
            }
            else if (LastAgeGroup.ContainsKey(agDivId))
            {
                AgeGroup group = LastAgeGroup[agDivId];
                output.EventSpecific.AgeGroupId = group.GroupId;
                output.EventSpecific.AgeGroupName = string.Format("{0}-{1}", group.StartAge, group.EndAge);
            }
            else
            {
                output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                output.EventSpecific.AgeGroupName = "0-110";
            }
            return output;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Participants.ModifyParticipantWindow", "Done clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ParticipantChanged)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                if (window != null)
                {
                    window.NotifyTimingWorker();
                }
                if (tPage != null)
                {
                    tPage.DatasetChanged();
                    tPage.UpdateView();
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
