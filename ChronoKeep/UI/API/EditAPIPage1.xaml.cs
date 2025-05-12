using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditAPIPage1.xaml
    /// </summary>
    public partial class EditAPIPage1
    {
        EditAPIWindow window;

        public EditAPIPage1(EditAPIWindow window)
        {
            InitializeComponent();
            this.window = window;
        }

        private void Edit_Event_Click(object sender, RoutedEventArgs e)
        {
            window.GotoEditEvent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }

        private void Edit_Year_Click(object sender, RoutedEventArgs e)
        {
            window.GotoEditYear();
        }
    }
}
