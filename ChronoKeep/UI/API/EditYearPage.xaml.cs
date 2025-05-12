using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditYearPage.xaml
    /// </summary>
    public partial class EditYearPage
    {
        EditAPIWindow window;
        IDBInterface database;

        public EditYearPage(EditAPIWindow window, IDBInterface database)
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
