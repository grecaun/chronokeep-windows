using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.IO;
using Chronokeep.IO;
using Chronokeep.UI.Parts;
using System.Collections.Generic;
using System.Linq;

namespace Chronokeep.UI.Import;

public partial class ImportFilePage1 : UserControl
{
    private readonly IDataImporter importer;

    public ImportFilePage1(IDataImporter importer)
    {
        InitializeComponent();
        this.importer = importer;
        for (int i = 1; i < importer.Data!.GetNumHeaders(); i++)
        {
            headerListBox.Items.Add(new HeaderPart(importer.Data.Headers[i], i));
        }
    }

    internal List<string> RequiredNotFound()
    {
        Log.D("UI.ImportFilePage1", "Checking for required fields.");
        List<string> output = [];
        bool first = false, last = false;
        foreach (HeaderPart item in headerListBox.Items.Cast<HeaderPart>())
        {
            int val = item.HeaderBox.SelectedIndex;
            if (val == ImportFileWindow.FIRST)
            {
                first = true;
            }
            else if (val == ImportFileWindow.LAST)
            {
                last = true;
            }
        }
        if (!first && !last)
        {
            output = ["First and/or Last Name"];
        }
        return output;
    }

    internal List<string> RepeatHeaders()
    {
        Log.D("UI.ImportFilePage1", "Checking for repeat headers in user selection.");
        int[] check = new int[ImportFileWindow.human_fields.Length];
        bool repeat = false;
        List<string> output = [];
        foreach (HeaderPart item in headerListBox.Items.Cast<HeaderPart>())
        {
            int val = item.HeaderBox.SelectedIndex;
            if (val > 0)
            {
                if (check[val] > 0)
                {
                    output.Add(item.HeaderBox.SelectedItem!.ToString()!);
                    repeat = true;
                }
                else
                {
                    check[val] = 1;
                }
            }
        }
        return repeat == true ? output : [];
    }

    internal HeaderPart[] GetListBoxItems()
    {
        HeaderPart[] output = new HeaderPart[headerListBox.Items.Count];
        for (int i=0; i<headerListBox.Items.Count; i++)
        {
            output[i] = (HeaderPart)headerListBox.Items[i]!;
        }
        return output;
    }

    internal void UpdateSheetNo(int selection)
    {
        Log.D("ImportFilePage1", "Changing sheet to " + selection);
        ExcelImporter excelImporter = (ExcelImporter)importer;
        excelImporter.ChangeSheet(selection);
        excelImporter.FetchHeaders();
        headerListBox.Items.Clear();
        for (int i = 1; i < importer.Data!.GetNumHeaders(); i++)
        {
            headerListBox.Items.Add(new HeaderPart(importer.Data.Headers[i], i));
        }
    }
}