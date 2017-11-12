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

        public NextYearSetupPage2(NextYearSetup kiosk, string eventName)
        {
            InitializeComponent();
            string[] parts = eventName.Split();
            int year;
            for (int i = 0; i < parts.Length; i++ )
            {
                int.TryParse(parts[i], out year);
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
            this.kiosk = kiosk;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            String nameString = nameBox.Text.Trim();
            long dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            Log.D("Name given for event: '" + nameString + "' Date Given: " + datePicker.SelectedDate.Value.Date.ToShortDateString() + " Date Value: " + dateVal);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            kiosk.Finish(nameString, dateVal);
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
