using Avalonia.Controls;
using System;

namespace Chronokeep.UI.Parts;

public partial class BibChipHeaderPart : UserControl
{
    public int Index { get; set; }
    public static string[] human_fields =
    {
        "",
        "Bib",
        "Chip"
    };

    public BibChipHeaderPart(string s, int ix)
    {
        InitializeComponent();
        Index = ix;
        HeaderLabel.Text = s;
        HeaderBox.ItemSource = human_fields;
        HeaderBox.SelectedIndex = GetHeaderBoxIndex(s.Trim());
    }

    internal static int GetHeaderBoxIndex(string s)
    {
        if (string.Equals(s, "bib", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        else if (string.Equals(s, "chip", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }
        return 0;
    }
}