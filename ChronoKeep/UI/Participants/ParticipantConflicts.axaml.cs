using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

public partial class ParticipantConflicts : Window
{
    public ParticipantConflicts()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
    }

    private void ParticipantsList_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
    }
}