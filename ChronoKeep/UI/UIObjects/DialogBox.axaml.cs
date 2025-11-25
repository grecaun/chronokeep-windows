using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

public partial class DialogBox : Window
{
    public DialogBox()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
    }

    private void CopyBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
    }
}