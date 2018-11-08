using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
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

namespace EventDirector.UI.Participants
{
    /// <summary>
    /// Interaction logic for ModifyParticipantWindow.xaml
    /// </summary>
    public partial class ModifyParticipantWindow : Window
    {
        IWindowCallback window;
        IDBInterface database;
        Event theEvent;
        Participant person;

        public ModifyParticipantWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            Add.Click += new RoutedEventHandler(this.Add_Click);
            UpdateDivisions();
            BibBox.Focus();
        }

        public ModifyParticipantWindow(IWindowCallback window, IDBInterface database, Participant person)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.person = person;
            theEvent = database.GetCurrentEvent();
            Add.Click += new RoutedEventHandler(this.Modify_Click);
            UpdateAllFields();
            BibBox.Focus();
        }

        public static ModifyParticipantWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.participantWindow != null)
            {
                return null;
            }
            ModifyParticipantWindow output = new ModifyParticipantWindow(window, database);
            StaticEvent.participantWindow = output;
            return output;
        }

        public static ModifyParticipantWindow NewWindow(IWindowCallback window, IDBInterface database, Participant person)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.participantWindow != null)
            {
                return null;
            }
            ModifyParticipantWindow output = new ModifyParticipantWindow(window, database, person);
            StaticEvent.participantWindow = output;
            return output;
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
            Participant newPart = FromFields();
            if (newPart != null)
            {
                database.AddParticipant(newPart);
                window.Update();
            }
            Clear();
            BibBox.Focus();
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Modify clicked.");
            Participant oldPart = FromFields();
            if (oldPart != null)
            {
                database.UpdateParticipant(oldPart);
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
            int bib = -1;
            int.TryParse(BibBox.Text, out bib);
            string gender = "Male";
            if (GenderBox.SelectedItem != null)
            {
                gender = ((ComboBoxItem)GenderBox.SelectedItem).Content.ToString();
            }
            int earlystart = 1, checkedin = 1;
            if (CheckedInBox.IsChecked ?? false)
            {
                checkedin = 0;
            }
            if (EarlyStartBox.IsChecked ?? false)
            {
                earlystart = 0;
            }
            Participant output = new Participant(
                participantId,
                FirstBox.Text,
                LastBox.Text,
                StreetBox.Text,
                CityBox.Text,
                StateBox.Text,
                ZipBox.Text,
                BirthdayBox.Text,
                new EventSpecific(
                    eventSpecificId,
                    theEvent.Identifier,
                    Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid),
                    "",
                    bib,
                    -1,
                    checkedin,
                    CommentsBox.Text,
                    "",
                    OtherBox.Text,
                    earlystart,
                    nextYear),
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
            StaticEvent.participantWindow = null;
            window.WindowFinalize(this);
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
