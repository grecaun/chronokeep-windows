using Avalonia.Controls;
using Chronokeep.UI.Import;

namespace Chronokeep.UI.Parts;

public partial class HeaderPart : UserControl
{
    public int Index { get; set; }

    public HeaderPart(string s, int ix)
    {
        InitializeComponent();
        Index = ix;
        HeaderLabel.Text = s;
        foreach (string field in ImportFileWindow.human_fields)
        {
            HeaderBox.Items.Add(field);
        }
        HeaderBox.SelectedIndex = ImportFileWindow.GetHeaderBoxIndex(s.Trim());
    }
}