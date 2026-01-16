using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI.Parts;

public partial class AgeGroupPart : UserControl
{
    private readonly AgeGroupsPage page;
    public AgeGroup MyGroup { get; private set; }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex AllowedChars();

    public AgeGroupPart(AgeGroupsPage page, AgeGroup group)
    {
        InitializeComponent();
        this.page = page;
        this.MyGroup = group;
        StartAge.Text = group.StartAge.ToString();
        EndAge.Text = group.EndAge.ToString();
    }

    public AgeGroup GetAgeGroup()
    {
        int start = MyGroup.StartAge, end = MyGroup.EndAge;
        int.TryParse(StartAge.Text, out start);
        int.TryParse(EndAge.Text, out end);
        MyGroup.StartAge = start;
        MyGroup.EndAge = end;
        return MyGroup;
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TextBox src = (TextBox)e.OriginalSource;
        src.SelectAll();
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text);
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.AgeGroupsPage", "Removing.");
        page.RemoveAgeGroup(this);
    }
}