using ChronoKeep.Interfaces;
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

namespace ChronoKeep
{
    /// <summary>
    /// Interaction logic for BibChipAssociationWindow.xaml
    /// </summary>
    public partial class BibChipAssociationWindow : Window
    {
        IDataImporter importer;
        IWindowCallback window = null;
        IDBInterface database;
        Boolean init = true;
        int[] keys;

        public bool ImportComplete = false;

        private BibChipAssociationWindow(IWindowCallback window, IDataImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.importer = importer;
            this.window = window;
            this.database = database;
            if (importer.Data.Type == ImportData.FileType.EXCEL)
            {
                SheetsLabel.Visibility = Visibility.Visible;
                SheetsBox.Visibility = Visibility.Visible;
                SheetsBox.ItemsSource = ((ExcelImporter)importer).SheetNames;
                SheetsBox.SelectedIndex = 0;
                init = false;
            }
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new BibChipHeaderListBoxItem(importer.Data.Headers[i], i));
            }
            EventHolder.Visibility = Visibility.Collapsed;
            TopRow.Height = new GridLength(0);
        }

        public static BibChipAssociationWindow NewWindow(IWindowCallback window, IDataImporter importer, IDBInterface database)
        {
            return new BibChipAssociationWindow(window, importer, database);
        }

        internal List<String> RepeatHeaders()
        {
            Log.D("Checking for repeat headers in user selection.");
            int[] check = new int[ImportFileWindow.human_fields.Length];
            bool repeat = false;
            List<String> output = new List<String>();
            foreach (ListBoxItem item in headerListBox.Items)
            {
                int val = ((BibChipHeaderListBoxItem)item).HeaderBox.SelectedIndex;
                if (val > 0)
                {
                    if (check[val] > 0)
                    {
                        output.Add(((BibChipHeaderListBoxItem)item).HeaderBox.SelectedItem.ToString());
                        repeat = true;
                    }
                    else
                    {
                        check[val] = 1;
                    }
                }
            }
            return repeat ? output : null;
        }

        internal BibChipHeaderListBoxItem[] GetListBoxItems()
        {
            BibChipHeaderListBoxItem[] output = new BibChipHeaderListBoxItem[headerListBox.Items.Count];
            headerListBox.Items.CopyTo(output, 0);
            return output;
        }

        private void SheetsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (init) { return; }
            int selection = ((ComboBox)sender).SelectedIndex;
            Log.D("You've selected number " + selection);
            ExcelImporter excelImporter = (ExcelImporter)importer;
            excelImporter.ChangeSheet(selection + 1);
            excelImporter.FetchHeaders();
            headerListBox.Items.Clear();
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new BibChipHeaderListBoxItem(importer.Data.Headers[i], i));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            importer.Finish();
            Log.D("Bib Chip Association - Cancel Button clicked.");
            ImportComplete = false;
        }

        private async void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Bib Chip Association = Done Clicked.");
            List<String> headers = RepeatHeaders();
            int eventId = -1;
            eventId = database.GetCurrentEvent().Identifier;
            if (headers == null)
            {
                importer.FetchData();
                keys = new int[3];
                foreach (BibChipHeaderListBoxItem item in headerListBox.Items)
                {
                    if (item.HeaderBox.SelectedIndex != 0)
                    {
                        keys[item.HeaderBox.SelectedIndex] = item.Index;
                    }
                }
                bool extraAssoc = Headers.IsChecked == false;
                await Task.Run(() =>
                {
                    ImportData data = importer.Data;
                    int numEntries = data.Data.Count;
                    List<BibChipAssociation> items = new List<BibChipAssociation>();
                    if (extraAssoc)
                    {
                        items.Add(new BibChipAssociation
                        {
                            Bib = int.Parse(data.Headers[keys[1]]),
                            Chip = data.Headers[keys[2]]
                        });
                    }
                    for (int counter = 0; counter < numEntries; counter++)
                    {
                        try
                        {
                            items.Add(new BibChipAssociation
                            {
                                Bib = int.Parse(data.Data[counter][keys[1]]),
                                Chip = data.Data[counter][keys[2]]
                            });
                        }
                        catch
                        {
                            Log.E("One or more values not an integer.");
                        }
                    }
                    database.AddBibChipAssociation(eventId, items);
                });
                Log.D("All done with bib chip associations.");
                ImportComplete = true;
                this.Close();
            }
            else
            {
                string val = "";
                foreach (string str in headers)
                {
                    val = " " + str;
                }
                MessageBox.Show("Multiple values given for:" + val);
            }
        }
    }
}
