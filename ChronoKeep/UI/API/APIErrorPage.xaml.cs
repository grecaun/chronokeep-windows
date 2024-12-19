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
                errorLabel.Content = "An API must be set up before you can use this tool.";
            }
        }
    }
}
