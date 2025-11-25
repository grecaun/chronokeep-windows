using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

public partial class DistancePart : UserControl
{
    public bool PlusWave { get; set; } = true;
    public bool MinusWave { get => !PlusWave; }
    public bool IsMain { get; set; } = true;
    public bool IsLinked { get => !IsMain; }
    public bool DistanceEvent { get; set; } = true;
    public bool NotDistanceEvent { get => !DistanceEvent; }
    public bool NotBackyardEvent { get; set; } = true;

    public DistancePart()
    {
        InitializeComponent();
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void WavePlus_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void TypeBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
    }

    private void SwapWaveType_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void DotValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
    }

    private void AddSub_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}