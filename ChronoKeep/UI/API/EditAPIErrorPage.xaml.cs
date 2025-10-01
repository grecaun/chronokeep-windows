using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditAPIErrorPage.xaml
    /// </summary>
    public partial class EditAPIErrorPage
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
