using Avalonia.Controls;
using Chronokeep.UI.API.Windows;

namespace Chronokeep.UI.API;

public partial class APIErrorPage : UserControl
{
    private readonly APIWindow window;

    public APIErrorPage(APIWindow window, bool noAPI)
    {
        InitializeComponent();
        this.window = window;
        if (noAPI)
        {
            errorLabel.Text = "An API must be set up before you can use this tool.";
        }
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.Close();
    }
}