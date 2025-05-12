namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIErrorPage.xaml
    /// </summary>
    public partial class EditAPIErrorPage
    {
        EditAPIWindow window;
        public EditAPIErrorPage(EditAPIWindow window, bool noAPI)
        {
            InitializeComponent();
            this.window = window;
            if (noAPI)
            {
                errorLabel.Text = "Unable to find linked api/event.";
            }
        }
    }
}
