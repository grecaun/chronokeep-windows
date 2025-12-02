using Avalonia.Controls;

namespace Chronokeep.UI.Parts;

public partial class HeaderPart : UserControl
{
    public int Index { get; set; }

    public HeaderPart(string s, int ix)
    {
        InitializeComponent();
        Index = ix;
        HeaderLabel.Text = s;
        HeaderBox.ItemSource = ImportFileWindow.human_fields;
        HeaderBox.SelectedIndex = ImportFileWindow.GetHeaderBoxIndex(s.Trim());
    }
}