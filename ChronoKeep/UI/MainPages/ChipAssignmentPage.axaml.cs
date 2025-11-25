using Avalonia.Controls;
using System.Collections.Generic;

namespace AvaloniaApp;

public partial class ChipAssignmentPage : UserControl
{
    private List<ChipAssoc> EventChips { get; } = [];
    private List<ChipAssoc> GlobalChips { get; } = [];

    public ChipAssignmentPage()
    {
        InitializeComponent();
    }

    private void Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void Clear_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void DeleteIgnored_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void ClearIgnored_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void KeyPressHandlerSingle(object? sender, Avalonia.Input.KeyEventArgs e)
    {
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
    }

    private void ChipValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
    }

    private void SaveSingleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void FileImport_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void UseTool_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void Export_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void KeyPressHandlerRange(object? sender, Avalonia.Input.KeyEventArgs e)
    {
    }

    private void UpdateEndChip(object? sender, TextChangedEventArgs e)
    {
    }

    private void SaveRangeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void Copy_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void KeyPressHandlerIgnored(object? sender, Avalonia.Input.KeyEventArgs e)
    {
    }

    private void SaveIgnored_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}

class ChipAssoc
{
    string Bib { get; set; } = "";
    string Chip { get; set; } = "";
}