using Avalonia.Controls;

namespace Chronokeep.UI.Export.Parts;

public partial class HeaderPart : UserControl
{
    public string NameValue { get => HeaderName.Text!; }
    public bool IsIncluded { get => Include.IsChecked == true; }

    public HeaderPart(string name)
    {
        InitializeComponent();
        HeaderName.Text = name;
    }
}