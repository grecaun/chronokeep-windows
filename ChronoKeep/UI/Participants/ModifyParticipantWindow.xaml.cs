using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Participants
{
    /// <summary>
    /// Interaction logic for ModifyParticipantWindow.xaml
    /// </summary>
    public partial class ModifyParticipantWindow : FluentWindow
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
            if (theEvent == null)
            {
                DialogBox.Show("Unable to get event.");
                this.Close();
            }
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

        public ModifyParticipantWindow(TimingPage tPage, IDBInterface database, int EventSpecificId, string Bib)
        {
            InitializeComponent();
            this.window = null;
            this.tPage = tPage;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null)
            {
                DialogBox.Show("Unable to get event.");
                this.Close();
            }
            person = database.GetParticipantEventSpecific(theEvent.Identifier, EventSpecificId);
            if (person == null)
            {
                BibBox.Text = Bib;
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
            {
                return;
            }
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
            bool genderFound = false;
            foreach (ComboBoxItem item in GenderBox.Items)
            {
                if (person.Gender.Equals(item.Content.ToString()))
                {
                    GenderBox.SelectedItem = item;
                    genderFound = true;
                }
            }
            if (person.Gender.Equals("NS", StringComparison.OrdinalIgnoreCase))
            {
                GenderBox.SelectedIndex = 4;
                genderFound = true;
            }
            if (!genderFound)
            {
                GenderBox.SelectedIndex = 3;
                otherGenderBox.Text = person.Gender;
                ShowOtherGender();
            }
            else
            {
                DismissOtherGender();
            }
            StreetBox.Text = person.Street;
            Street2Box.Text = person.Street2;
            CityBox.Text = person.City;
            StateBox.Text = person.State;
            ZipBox.Text = person.Zip;
            CountryBox.Text = person.Country;
            EmailBox.Text = person.Email;
            PhoneBox.Text = person.Phone;
            MobileBox.Text = person.Mobile;
            ParentBox.Text = person.Parent;
            CommentsBox.Text = person.Comments;
            ECNameBox.Text = person.ECName;
            ECPhoneBox.Text = person.ECPhone;
            AnonymousBox.IsChecked = person.Anonymous;
            ApparelBox.Text = person.EventSpecific.Apparel;
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
            otherGenderBox.Text = "";
            StreetBox.Text = "";
            Street2Box.Text = "";
            CityBox.Text = "";
            StateBox.Text = "";
            ZipBox.Text = "";
            CountryBox.Text = "";
            EmailBox.Text = "";
            PhoneBox.Text = "";
            MobileBox.Text = "";
            ParentBox.Text = "";
            CommentsBox.Text = "";
            ECNameBox.Text = "";
            ECPhoneBox.Text = "";
            AnonymousBox.IsChecked = false;
            ApparelBox.Text = "";
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
            // If bib isn't empty and isn't the dummybib, offer to remove bib from old participant.
            if (newPart.Bib.Length > 0 && newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                offendingBib = database.GetParticipantBib(theEvent.Identifier, newPart.Bib);
            }
            if (offendingBib != null)
            {
                // bib is taken
                DialogBox.Show(
                    "This bib is already taken. Assign no bib to the previous bib owner?",
                    "Yes",
                    "No",
                    () =>
                    {
                        offendingBib.EventSpecific.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
                        database.UpdateParticipant(offendingBib);
                        if (newPart != null)
                        {
                            if (newPart.FirstName.Trim().Length < 1 && newPart.LastName.Trim().Length < 1)
                            {
                                DialogBox.Show("Invalid name given.");
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
                    });
            }
            else
            {
                if (newPart != null)
                {
                    if (newPart.FirstName.Trim().Length < 1 && newPart.LastName.Trim().Length < 1)
                    {
                        DialogBox.Show("Invalid name given.");
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
        }

        private void ShowOtherGender()
        {
            otherGenderBox.Visibility = Visibility.Visible;
        }

        private void DismissOtherGender()
        {
            otherGenderBox.Visibility = Visibility.Collapsed;
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
            // If bib isn't empty and isn't the dummybib, offer to swap bibs.
            if (newPart.Bib.Length > 0 && newPart.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
            {
                offendingBib = database.GetParticipantBib(theEvent.Identifier, newPart.Bib);
            }
            if (offendingBib != null && newPart.Identifier != offendingBib.Identifier)
            {
                // bib is taken - person object holds old bib #
                bool ModifyBibs = false;
                DialogBox.Show(
                    "This bib is already taken. Swap bibs?",
                    "Yes",
                    "No",
                    () =>
                    {
                        ModifyBibs = true;
                        offendingBib.EventSpecific.Bib = person.EventSpecific.Bib;
                        string newBib = newPart.EventSpecific.Bib;
                        newPart.EventSpecific.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
                        database.UpdateParticipant(newPart);
                        database.UpdateParticipant(offendingBib);
                        newPart.EventSpecific.Bib = newBib;
                        database.UpdateParticipant(newPart);
                        ParticipantChanged = true;
                        this.Close();
                    });
                if (!ModifyBibs)
                {
                    BibBox.Text = person.EventSpecific.Bib.ToString();
                }
            }
            else
            {
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
            string gender = "Not Specified";
            if (GenderBox.SelectedItem != null)
            {
                gender = ((ComboBoxItem)GenderBox.SelectedItem).Content.ToString();
            }
            if (gender.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                gender = otherGenderBox.Text;
                if (gender.Length < 1)
                {
                    gender = "Not Specified";
                }
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
                    BibBox.Text,
                    checkedin,
                    CommentsBox.Text,
                    "",
                    "",
                    Constants.Timing.EVENTSPECIFIC_UNKNOWN,
                    "",
                    Constants.Timing.TIMERESULT_DUMMYAGEGROUP,
                    AnonymousBox.IsChecked == true,
                    false,
                    ApparelBox.Text
                    ),
                EmailBox.Text,
                PhoneBox.Text,
                MobileBox.Text,
                ParentBox.Text,
                CountryBox.Text,
                Street2Box.Text,
                gender,
                ECNameBox.Text,
                ECPhoneBox.Text,
                "" // placeholder chip value
                );
            age = output.GetAge(theEvent.Date);
            Dictionary<(int, int), AgeGroup> AgeGroups = AgeGroup.GetAgeGroups();
            Dictionary<int, AgeGroup> LastAgeGroup = AgeGroup.GetLastAgeGroup();
            int agDivId = theEvent.CommonAgeGroups ? Constants.Timing.COMMON_AGEGROUPS_DISTANCEID : output.EventSpecific.DistanceIdentifier;
            if (AgeGroups == null || age < 0)
            {
                Log.D("UI.Participants.ModifyParticipantWindow", "Age Groups not found or Age is less than 0.");
                output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                output.EventSpecific.AgeGroupName = "";
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
                Log.D("UI.Participants.ModifyParticipantWindow", "Age Group not found.");
                output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                output.EventSpecific.AgeGroupName = "";
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
                Add.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            }
        }

        private void GenderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedGender = ((ComboBoxItem)GenderBox.SelectedItem).Content.ToString();
            if (selectedGender.Equals("Other", StringComparison.OrdinalIgnoreCase)) {
                ShowOtherGender();
            }
            else
            {
                DismissOtherGender();
            }
        }
    }
}
