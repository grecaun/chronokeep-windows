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
    /// Interaction logic for ImportFileWindow.xaml
    /// </summary>
    public partial class ImportFileWindow : Window
    {
        CSVImporter importer;
        MainWindow mainWindow;
        string[] fields = new string[] {
            "First Name",
            "Last Name",
            "Street",
            "Street 2",
            "City",
            "State",
            "Zip",
            "Country",
            "Birthday",
            "Phone",
            "Email",
            "Mobile",
            "Parent",
            "Bib",
            "Chip",
            "Shirt Size",
            "Owes",
            "Comments",
            "Second Shirt",
            "Hat",
            "Other",
            "Division",
            "Emergency Contact Name",
            "Emergency Contact Phone",
            "Emergency Contact Email"
        };

        public ImportFileWindow(MainWindow mainWindow, CSVImporter importer)
        {
            InitializeComponent();
            this.importer = importer;
            this.mainWindow = mainWindow;
            foreach (String s in importer.Data.Headers)
            {
                ListBoxItem newItem = new ListBoxItem();
                Grid theGrid = new Grid();
                newItem.Content = theGrid;
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                Button cancel = new Button();
                Button done = new Button();
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
