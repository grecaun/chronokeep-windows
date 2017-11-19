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

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for NextYearSetupPage2.xaml
    /// </summary>
    public partial class NextYearSetupPage2 : Page
    {
        NextYearSetup kiosk;

        public NextYearSetupPage2(NextYearSetup kiosk, Event oldEvent)
        {
            InitializeComponent();
            string[] parts = oldEvent.Name.Split();
            for (int i = 0; i < parts.Length; i++)
            {
                int.TryParse(parts[i], out int year);
                if (year != 0 && year > 1999)
                {
                    parts[i] = String.Format("{0}", year + 1);
                    break;
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (string s in parts)
            {
                sb.Append(s + " ");
            }
            nameBox.Text = sb.ToString().Trim();
            datePicker.SelectedDate = DateTime.Today;
            shirtOptionalBox.IsChecked = oldEvent.ShirtOptional == 1 ? true : false;
            shirtPriceBox.Text = String.Format("{0}.{1:D2}", oldEvent.ShirtPrice / 100, oldEvent.ShirtPrice % 100);
            this.kiosk = kiosk;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            String nameString = nameBox.Text.Trim();
            int shirtOptional = 1, shirtPrice = 0;
            string[] shirtVals = shirtPriceBox.Text.Split('.');
            shirtPrice = 20;
            if (shirtVals.Length > 0)
            {
                int.TryParse(shirtVals[0].Trim(), out shirtPrice);
            }
            shirtPrice = shirtPrice * 100;
            int cents = 0;
            if (shirtVals.Length > 1)
            {
                int.TryParse(shirtVals[1].Trim(), out cents);
            }
            while (cents > 100)
            {
                cents = cents / 100;
            }
            shirtPrice += cents;
            long dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            Log.D("Name given for event: '" + nameString + "' Date Given: " + datePicker.SelectedDate.Value.Date.ToShortDateString() + " Date Value: " + dateVal);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            kiosk.GoToPage3(nameString, dateVal, shirtOptional, shirtPrice);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kiosk.Close();
        }

        private void Keyboard_Up(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit();
            }
        }
    }
}
