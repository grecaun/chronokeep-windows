using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIErrorPage.xaml
    /// </summary>
    public partial class APIErrorPage
    {
        APIWindow window;

        public APIErrorPage(APIWindow window, bool noAPI)
        {
            InitializeComponent();
            this.window = window;
            if (noAPI)
            {
                errorLabel.Text = "An API must be set up before you can use this tool.";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
