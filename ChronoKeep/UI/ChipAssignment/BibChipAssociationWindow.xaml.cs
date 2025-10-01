using Chronokeep.Helpers;
using Chronokeep.Interfaces;
using Chronokeep.IO;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for BibChipAssociationWindow.xaml
    /// </summary>
    public partial class BibChipAssociationWindow : FluentWindow
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
            this.MinHeight = 300;
            this.MinWidth = 300;
            this.Height = 300;
            this.Width = 300;
            this.Topmost = true;
            if (importer.Data.Type == ImportData.FileType.EXCEL)
            {
                SheetsContainer.Visibility = Visibility.Visible;
                SheetsBox.ItemsSource = ((ExcelImporter)importer).SheetNames;
                SheetsBox.SelectedIndex = 0;
                init = false;
            }
            else
            {
                SheetsContainer.Visibility = Visibility.Collapsed;
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
            return new(window, importer, database);
        }

        internal List<string> RepeatHeaders()
        {
            Log.D("UI.BibChipAssociationWindow", "Checking for repeat headers in user selection.");
            int[] check = new int[ImportFileWindow.human_fields.Length];
            bool repeat = false;
            List<string> output = [];
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
            Log.D("UI.BibChipAssociationWindow", "You've selected number " + selection);
            ExcelImporter excelImporter = (ExcelImporter)importer;
            excelImporter.ChangeSheet(selection);
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
            Log.D("UI.BibChipAssociationWindow", "Bib Chip Association - Cancel Button clicked.");
            ImportComplete = false;
            Close();
        }

        private async void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.BibChipAssociationWindow", "Bib Chip Association = Done Clicked.");
            List<string> headers = RepeatHeaders();
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
                    Dictionary<string, string> currentAssociations = database.GetBibChips(eventId).ToDictionary(x => x.Chip, x => x.Bib);
                    List<BibChipAssociation> items = [];
                    ImportData data = importer.Data;
                    int numEntries = data.Data.Count;
                    if (extraAssoc)
                    {
                        items.Add(new()
                        {
                            Bib = data.Headers[keys[1]],
                            Chip = data.Headers[keys[2]]
                        });
                    }
                    for (int counter = 0; counter < numEntries; counter++)
                    {
                        try
                        {
                            items.Add(new()
                            {
                                Bib = data.Data[counter][keys[1]],
                                Chip = data.Data[counter][keys[2]]
                            });
                        }
                        catch
                        {
                            Log.E("UI.BibChipAssociationWindow", "One or more values not an integer.");
                        }
                    }
                    // Check new associations against old ones.
                    List<BibChipAssociation> conflicts = [];
                    foreach (BibChipAssociation assoc in items)
                    {
                        // Check to ensure we aren't trying to associate this chip with a different bib
                        // when it already has one associated with it.
                        if (currentAssociations.TryGetValue(assoc.Chip, out string oBib) && !oBib.Equals(assoc.Bib, StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(assoc);
                        }
                    }
                    // if there are conflicts, alter the user to them and verify clobbering
                    if (conflicts.Count > 0)
                    {
                        StringBuilder error = new("There were conflicts found in the import file. Please confirm you want to clobber current values.");
                        foreach (BibChipAssociation assoc in conflicts)
                        {
                            error.Append(string.Format("\nChip {0} - Bib {1}", assoc.Chip, assoc.Bib));
                        }
                        DialogBox.Show(
                            error.ToString(),
                            "Yes",
                            "No",
                            () =>
                            {
                                items.RemoveAll(x => conflicts.Contains(x));
                            }
                            );
                    }
                    database.AddBibChipAssociation(eventId, items);
                });
                Log.D("UI.BibChipAssociationWindow", "All done with bib chip associations.");
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
                DialogBox.Show("Multiple values given for: " + val);
            }
        }
    }
}
