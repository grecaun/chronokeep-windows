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

        public NewDivisionWindow(MainWindow mWindow)
        {
            InitializeComponent();
            this.mainWindow = mWindow;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            String nameString = nameBox.Text.Trim();
            Log.D("Name given for division: '" + nameString + "'");
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            mainWindow.AddDivision(nameString);
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
