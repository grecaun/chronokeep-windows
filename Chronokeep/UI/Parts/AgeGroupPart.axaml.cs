using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using System.Text.RegularExpressions;

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

    private void SelectAll(object? sender, FocusChangedEventArgs e)
    {
        TextBox? src = (TextBox?)e.Source;
        src?.SelectAll();
    }

    private void NumberValidation(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null)
        {
            e.Handled = AllowedChars().IsMatch(e.Text);
        }
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.AgeGroupsPage", "Removing.");
        page.RemoveAgeGroup(this);
    }
}