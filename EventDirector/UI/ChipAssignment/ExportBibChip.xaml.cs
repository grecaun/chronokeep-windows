using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
using EventDirector.UI.IO;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace EventDirector.UI.ChipAssignment
{
    /// <summary>
    /// Interaction logic for ExportBibChip.xaml
    /// </summary>
    public partial class ExportBibChip : Window
    {
        IWindowCallback window;
        IDBInterface database;
        String chipsDir = "Chips";

        public ExportBibChip(IWindowCallback window, IDBInterface database, bool ExcelAllowed)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            if (ExcelAllowed)
            {
                Type.Items.Add(new ComboBoxItem()
                {
                    Content = "Excel Spreadsheet (*.xlsx)",
                    Uid = "2"
                });
            }
        }

        public static ExportBibChip NewWindow(IWindowCallback window, IDBInterface database, bool ExcelAllowed)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.chipAssigmentWindow != null)
            {
                return null;
            }
            ExportBibChip output = new ExportBibChip(window, database, ExcelAllowed);
            StaticEvent.chipAssigmentWindow = output;
            return output;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Export clicked.");
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                this.Close();
            }
            AppSetting directorySetting = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR);
            if (directorySetting == null) return;
            String directory = directorySetting.value;
            String fullPath, fileExtension = ".csv";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            directory = System.IO.Path.Combine(directory, chipsDir);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            IDataExporter exporter = null;
            if (Type.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a format to use for exporting the data.");
                return;
            }
            if (Type.SelectedIndex == 0) // CSV
            {
                fileExtension = ".csv";
                exporter = new CSVExporter("\"{0}\",\"{1}\"");
            }
            else if (Type.SelectedIndex == 1) // Excel
            {
                fileExtension = ".xlsx";
                exporter = new ExcelExporter();
            }
            String fileName = theEvent.Name + " Chips";
            fullPath = System.IO.Path.Combine(directory, fileName + fileExtension);
            int number = 1;
            while (File.Exists(fullPath))
            {
                fullPath = System.IO.Path.Combine(directory, fileName + " (" + number++ + ")" + fileExtension);
            }
            List<object[]> data = new List<object[]>();
            List<BibChipAssociation> associations = database.GetBibChips(theEvent.Identifier);
            associations.Sort();
            string[] headers;
            if (!(Header.IsChecked ?? false))
            {
                Log.D("Header is checked");
                BibChipAssociation first = associations[0];
                associations.Remove(first);
                headers = new string[] { first.Bib.ToString(), first.Chip.ToString() };
            }
            else
            {
                headers = new string[] { "Bib", "Chip" };
            }
            foreach (BibChipAssociation bca in associations)
            {
                data.Add(new object[] { bca.Bib, bca.Chip });
            }
            if (exporter != null)
            {
                exporter.SetData(headers, data);
                exporter.ExportData(fullPath);
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window.WindowFinalize(this);
            StaticEvent.chipAssigmentWindow = null;
        }
    }
}
