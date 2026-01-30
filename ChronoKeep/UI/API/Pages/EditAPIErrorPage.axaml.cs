using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.UI.API.Windows;

namespace Chronokeep.UI.API;

public partial class EditAPIErrorPage : UserControl
{
    private readonly EditAPIWindow window;

    public EditAPIErrorPage(EditAPIWindow window, bool noAPI)
    {
        InitializeComponent();
        this.window = window;
        if (noAPI)
        {
            errorLabel.Text = "Unable to find linked api/event.";
        }
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.Close();
    }
}