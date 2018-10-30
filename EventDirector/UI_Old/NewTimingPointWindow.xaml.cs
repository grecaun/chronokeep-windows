using System;
using System.Collections;
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
    /// Interaction logic for NewTimingPointWindow.xaml
    /// </summary>
    public partial class NewTimingPointWindow : Window
    {
        MainWindow mainWindow;
        int timingPointIdentifier = -1;

        public NewTimingPointWindow(MainWindow mWindow)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
        }

        public NewTimingPointWindow(MainWindow mWindow, int id, string name)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
            this.timingPointIdentifier = id;
            nameBox.Text = name;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            String nameString = nameBox.Text.Trim();
            Log.D("Name given for timing point: '" + nameString);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            if (timingPointIdentifier == -1)
            {
                mainWindow.AddTimingPoint(nameString);
            }
            else
            {
                mainWindow.UpdateTimingPoint(timingPointIdentifier, nameString);
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
