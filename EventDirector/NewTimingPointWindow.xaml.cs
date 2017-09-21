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

        public NewTimingPointWindow(MainWindow mWindow, ArrayList divisions)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
            foreach (Division d in divisions)
            {
                divBox.Items.Add(new ComboBoxItem {
                    Uid = d.Identifier.ToString(),
                    Content = d.Name
                });
            }
            divBox.SelectedIndex = 0;
        }

        public NewTimingPointWindow(MainWindow mWindow, ArrayList divisions, int id, string name, string distance, string unit)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
            foreach (Division d in divisions)
            {
                divBox.Items.Add(new ComboBoxItem
                {
                    Uid = d.Identifier.ToString(),
                    Content = d.Name
                });
            }
            divBox.SelectedIndex = 0;
            this.timingPointIdentifier = id;
            nameBox.Text = name;
            distanceBox.Text = distance;
            if (unit == "MI")
            {
                unitBox.SelectedIndex = 1;
            }
            else if (unit == "KM")
            {
                unitBox.SelectedIndex = 2;
            }
            else
            {
                unitBox.SelectedIndex = 0;
            }
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
            }
            catch
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
            int divisionId = Convert.ToInt32(((ComboBoxItem)divBox.SelectedItem).Uid);
            if (timingPointIdentifier == -1)
            {
                mainWindow.AddTimingPoint(nameString, distanceStr, unitString, divisionId);
            }
            else
            {
                mainWindow.UpdateTimingPoint(timingPointIdentifier, nameString, distanceStr, unitString, divisionId);
            }
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
