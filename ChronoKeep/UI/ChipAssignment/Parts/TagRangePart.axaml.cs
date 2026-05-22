using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Chronokeep.Helpers;

namespace Chronokeep.UI.ChipAssignment.Parts;

public partial class TagRangePart : UserControl
{
    public string EndBibVal { get => EndBib.Text!; }
    public string EndChipVal { get => EndChip.Text!; }

    private readonly ListBox parent;

    public TagRangePart(ListBox correlationBox)
    {
        InitializeComponent();
        int lastEndBib = 0, lastEndChip = 0;
        parent = correlationBox;
        if (correlationBox.Items.Count > 0)
        {
            TagRangePart? lastItem = correlationBox.Items[^1] as TagRangePart;
            try
            {
                _ = int.TryParse(lastItem!.EndBibVal, out lastEndBib);
                _ = int.TryParse(lastItem.EndChipVal, out lastEndChip);
            }
            catch { }
        }
        StartBib.Text = string.Format("{0}", lastEndBib + 1);
        EndBib.Text = string.Format("{0}", lastEndBib + 1);
        StartChip.Text = string.Format("{0}", lastEndChip + 1);
        EndChip.Text = string.Format("{0}", lastEndChip + 1);
    }

    private void UpdateEndChip()
    {
        _ = int.TryParse(StartBib.Text, out int startBib);
        _ = int.TryParse(EndBib.Text, out int endBib);
        _ = int.TryParse(StartChip.Text, out int startChip);
        int endChip = endBib - startBib + startChip;
        EndChip.Text = endChip.ToString();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        Log.D("UI.ChipAssignment.ChipTool", "Removing an item.");
        try
        {
            parent.Items.Remove(this);
        }
        catch { }
    }

    private void StartBib_TextChanged(object sender, TextChangedEventArgs e)
    {
        string replaceStr = StartBib.Text!.Replace(" ", "");
        if (StartBib.Text.Length != replaceStr.Length)
        {
            StartBib.Text = replaceStr;
        }
        UpdateEndChip();
    }

    private void EndBib_TextChanged(object sender, TextChangedEventArgs e)
    {
        string replaceStr = EndBib.Text!.Replace(" ", "");
        if (EndBib.Text.Length != replaceStr.Length)
        {
            EndBib.Text = replaceStr;
        }
        UpdateEndChip();
    }

    private void StartChip_TextChanged(object sender, TextChangedEventArgs e)
    {
        string replaceStr = StartChip.Text!.Replace(" ", "");
        if (StartChip.Text.Length != replaceStr.Length)
        {
            StartChip.Text = replaceStr;
        }
        _ = (int.TryParse(StartChip.Text, out int startChip));
        if (string.CompareOrdinal(StartChip.Text, startChip.ToString()) != 0)
        {
            StartChip.Text = startChip.ToString();
        }
        UpdateEndChip();
    }

    private void SelectAll(object sender, FocusChangedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }

    private void KeyPressHandler(object sender, KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9) { }
        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { }
        else if (e.Key == Key.Tab) { }
        else
        {
            e.Handled = true;
        }
    }
}