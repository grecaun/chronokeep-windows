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
    /// Interaction logic for newDivision.xaml
    /// </summary>
    public partial class NewDivisionWindow : Window
    {
        MainWindow mainWindow;
        int divisionIdentifier = -1;
        int eventId = -1;

        public NewDivisionWindow(MainWindow mWindow, int eventId)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
            this.eventId = eventId;
        }

        public NewDivisionWindow(MainWindow mWindow, int eventId, int divId, string name)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
            this.divisionIdentifier = divId;
            this.eventId = eventId;
            nameBox.Text = name;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            String nameString = nameBox.Text.Trim();
            Log.D("Name given for division: '" + nameString + "'");
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            if (divisionIdentifier != -1)
            {
                mainWindow.UpdateDivision(eventId, divisionIdentifier, nameString);
            }
            else
            {
                mainWindow.AddDivision(eventId, nameString);
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
