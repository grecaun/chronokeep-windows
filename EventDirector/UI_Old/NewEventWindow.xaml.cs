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

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewEventWindow : Window
    {
        IMainWindow mainWindow;
        Event theEvent = null;

        public NewEventWindow(IMainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            this.mainWindow = mainWindow;
        }

        public NewEventWindow(MainWindow mW, Event e)
        {
            InitializeComponent();
            nameBox.Text = e.Name;
            datePicker.SelectedDate = DateTime.Parse(e.Date);
            this.mainWindow = mW;
            this.theEvent = e;
            shirtOptional.IsChecked = e.ShirtOptional == 1 ? true : false;
            shirtPriceBox.Text = String.Format("{0}.{1:D2}", e.ShirtPrice / 100, e.ShirtPrice % 100);
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            String nameString = nameBox.Text.Trim();
            int shirtPrice = -1, shirtOptionalVal = shirtOptional.IsChecked == true ? 1 : 0;
            string[] parts = shirtPriceBox.Text.Split('.');
            shirtPrice = 20;
            if (parts.Length > 0)
            {
                int.TryParse(parts[0].Trim(), out shirtPrice);
            }
            shirtPrice = shirtPrice * 100;
            int cents = 0;
            if (parts.Length > 1)
            {
                int.TryParse(parts[1].Trim(), out cents);
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
            if (theEvent == null)
            {
                mainWindow.AddEvent(nameString, dateVal, shirtOptionalVal, shirtPrice);
            }
            else
            {
                mainWindow.UpdateEvent(theEvent.Identifier, nameString, dateVal, theEvent.NextYear, shirtOptionalVal, shirtPrice);
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Keyboard_Up(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowClosed(this);
        }
    }
}
