using Avalonia.Controls;

namespace Chronokeep.UI.Parts;

public partial class LogPart : UserControl
{
    public int Index { get; set; }

    public LogPart(string s, int ix, string[] human_fields, int selectedIx)
    {
        InitializeComponent();
        Index = ix;
        HeaderLabel.Text = s;
        HeaderBox.ItemSource = human_fields;
        HeaderBox.SelectedIndex = selectedIx;
    }
}