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
        MainWindow mainWindow;
        int eventIdentifier = -1;

        public NewEventWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            this.mainWindow = mainWindow;
        }

        public NewEventWindow(MainWindow mW, int id, string name, string date)
        {
            InitializeComponent();
            nameBox.Text = name;
            datePicker.SelectedDate = DateTime.Parse(date);
            this.mainWindow = mW;
            this.eventIdentifier = id;
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
            if (eventIdentifier == -1)
            {
                mainWindow.AddEvent(nameString, dateVal);
            }
            else
            {
                mainWindow.UpdateEvent(eventIdentifier, nameString, dateVal);
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
