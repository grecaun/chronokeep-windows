using Chronokeep.Helpers;
using Chronokeep.IO;
using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Chronokeep.UI.Timing.Import
{
    /// <summary>
    /// Interaction logic for ImportLogPage2.xaml
    /// </summary>
    public partial class ImportLogPage2
    {
        internal static string[] human_fields =
        [
            "",
            "Chip",
            "Time"
        ];
        private static readonly int CHIP = 1;
        private static readonly int TIME = 2;

        readonly ImportLogWindow parent;

        public ImportLogPage2(ImportLogWindow parent, LogImporter importer)
        {
            InitializeComponent();
            this.parent = parent;
            for (int i=1; i < importer.Data.GetNumHeaders(); i++)
            {
                itemListBox.Items.Add(new LogListBoxItem(importer.Data.Headers[i], i, human_fields, 0));
            }
        }
        
        internal List<string> RepeatHeaders()
        {
            Log.D("UI.Timing.ImportLog", "Checking for repeat headers in user selection.");
            int[] check = new int[human_fields.Length];
            bool repeat = false;
            List<string> output = [];
            foreach (LogListBoxItem item in itemListBox.Items)
            {
                int val = item.HeaderBox.SelectedIndex;
                if (val > 0)
                {
                    if (check[val] > 0)
                    {
                        output.Add(item.HeaderBox.SelectedItem.ToString());
                        repeat = true;
                    }
                    else
                    {
                        check[val] = 1;
                    }
                }
            }
            return repeat == true ? output : null;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ImportLog", "Import clicked.");
            List<string> repeats = RepeatHeaders();
            if (repeats != null)
            {
                StringBuilder message = new("The following are repeats:\n");
                foreach (string s in repeats)
                {
                    message.Append(s);
                    message.Append('\n');
                }
                DialogBox.Show(message.ToString());
                return;
            }
            int chip = 0, time = 0;
            foreach (LogListBoxItem item in itemListBox.Items)
            {
                if (CHIP == item.HeaderBox.SelectedIndex)
                {
                    chip = item.Index;
                }
                else if (TIME == item.HeaderBox.SelectedIndex)
                {
                    time = item.Index;
                }
            }
            if (chip == 0 || time == 0)
            {
                DialogBox.Show("Both Chip and Time must be chosen.");
                return;
            }
            parent.Import(LogImporter.Type.CUSTOM, Constants.Timing.LOCATION_DUMMY, chip, time);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ImportLog", "Cancel clicked.");
            parent.Cancel();
        }
    }
}
