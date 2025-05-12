using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditEventPage.xaml
    /// </summary>
    public partial class EditEventPage
    {
        EditAPIWindow window;
        IDBInterface database;

        public EditEventPage(EditAPIWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
