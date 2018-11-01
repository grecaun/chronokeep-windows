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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ImportFilePage1.xaml
    /// </summary>
    public partial class ImportFilePage1 : Page
    {
        IDataImporter importer;

        public ImportFilePage1(IDataImporter importer)
        {
            InitializeComponent();
            this.importer = importer;
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new HListBoxItem(importer.Data.Headers[i], i));
            }
        }

        internal List<String> RepeatHeaders()
        {
            Log.D("Checking for repeat headers in user selection.");
            int[] check = new int[ImportFileWindow.human_fields.Length];
            bool repeat = false;
            List<String> output = new List<String>();
            foreach (ListBoxItem item in headerListBox.Items)
            {
                int val = ((HListBoxItem)item).HeaderBox.SelectedIndex;
                if (val > 0 && val != ImportFileWindow.APPARELITEM)
                {
                    if (check[val] > 0)
                    {
                        output.Add(((HListBoxItem)item).HeaderBox.SelectedItem.ToString());
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

        internal HListBoxItem[] GetListBoxItems()
        {
            HListBoxItem[] output = new HListBoxItem[headerListBox.Items.Count];
            headerListBox.Items.CopyTo(output, 0);
            return output;
        }

        internal void UpdateSheetNo(int selection)
        {
            ExcelImporter excelImporter = (ExcelImporter)importer;
            excelImporter.ChangeSheet(selection + 1);
            excelImporter.FetchHeaders();
            headerListBox.Items.Clear();
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new HListBoxItem(importer.Data.Headers[i], i));
            }
        }
    }
}
