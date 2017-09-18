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

        public NewEventWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            this.mainWindow = mainWindow;
        }

        private void submit_Click(object sender, RoutedEventArgs e)
        {
            String nameString = nameBox.Text.Trim();
            long dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            Log.D("Name given for event: '" + nameString + "' Date Given: " + datePicker.SelectedDate.Value.Date.ToShortDateString() + " Date Value: " + dateVal);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            mainWindow.AddEvent(nameString, dateVal);
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
