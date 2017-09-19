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
    /// Interaction logic for NewTimingPointWindow.xaml
    /// </summary>
    public partial class NewTimingPointWindow : Window
    {
        MainWindow mainWindow;

        public NewTimingPointWindow(MainWindow mWindow)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
        }

        private void submit_Click(object sender, RoutedEventArgs e)
        {
            submit();
        }

        private void submit()
        {
            String nameString = nameBox.Text.Trim();
            String distanceStr = distanceBox.Text.Trim();
            try
            {
                Convert.ToInt32(distanceStr);
#pragma warning disable CS0168 // Variable is declared but never used
            }
            catch (Exception exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                Log.D("User is trying to fool me precious.");
                distanceStr = "";
            }
            String unitString = unitBox.Text;
            Log.D("Name given for timing point: '" + nameString + "' Distance: " + distanceStr + " Unit: " + unitString);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            mainWindow.AddTimingPoint(nameString, distanceStr, unitString);
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Keyboard_Up(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                submit();
            }
        }
    }
}
