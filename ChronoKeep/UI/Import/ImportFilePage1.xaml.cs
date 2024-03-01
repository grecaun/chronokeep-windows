using System.Collections.Generic;
using System.Windows.Controls;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for ImportFilePage1.xaml
    /// </summary>
    public partial class ImportFilePage1
    {
        IDataImporter importer;

        public ImportFilePage1(IDataImporter importer)
        {
            InitializeComponent();
            this.importer = importer;
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new HeaderListBoxItem(importer.Data.Headers[i], i));
            }
        }

        internal List<string> RequiredNotFound()
        {
            Log.D("UI.ImportFilePage1", "Checking for required fields.");
            List<string> output = null;
            bool first = false, last = false, age = false, gender = false, bib = false;
            foreach (ListBoxItem item in headerListBox.Items)
            {
                int val = ((HeaderListBoxItem)item).HeaderBox.SelectedIndex;
                if (val == ImportFileWindow.FIRST)
                {
                    first = true;
                }
                else if (val == ImportFileWindow.LAST)
                {
                    last = true;
                }
                else if (val == ImportFileWindow.BIRTHDAY || val == ImportFileWindow.AGE)
                {
                    age = true;
                }
                else if (val == ImportFileWindow.GENDER)
                {
                    gender = true;
                }
                else if (val == ImportFileWindow.BIB)
                {
                    bib = true;
                }
            }
            if (!bib || !first || !last || !age || !gender)
            {
                output = new List<string>();
                if (!bib)
                {
                    output.Add("Bib");
                }
                if (!first)
                {
                    output.Add("First Name");
                }
                if (!last)
                {
                    output.Add("Last Name");
                }
                if (!gender)
                {
                    output.Add("Gender");
                }
                if (!age)
                {
                    output.Add("Birthday or Age");
                }
            }
            return output;
        }

        internal List<string> RepeatHeaders()
        {
            Log.D("UI.ImportFilePage1", "Checking for repeat headers in user selection.");
            int[] check = new int[ImportFileWindow.human_fields.Length];
            bool repeat = false;
            List<string> output = new List<string>();
            foreach (ListBoxItem item in headerListBox.Items)
            {
                int val = ((HeaderListBoxItem)item).HeaderBox.SelectedIndex;
                if (val > 0 && val != ImportFileWindow.APPARELITEM)
                {
                    if (check[val] > 0)
                    {
                        output.Add(((HeaderListBoxItem)item).HeaderBox.SelectedItem.ToString());
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

        internal HeaderListBoxItem[] GetListBoxItems()
        {
            HeaderListBoxItem[] output = new HeaderListBoxItem[headerListBox.Items.Count];
            headerListBox.Items.CopyTo(output, 0);
            return output;
        }

        internal void UpdateSheetNo(int selection)
        {
            Log.D("ImportFilePage1", "Changing sheet to " + selection);
            ExcelImporter excelImporter = (ExcelImporter)importer;
            excelImporter.ChangeSheet(selection);
            excelImporter.FetchHeaders();
            headerListBox.Items.Clear();
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new HeaderListBoxItem(importer.Data.Headers[i], i));
            }
        }
    }
}
