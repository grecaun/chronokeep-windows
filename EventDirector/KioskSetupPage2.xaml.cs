using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for KioskSettupPage2.xaml
    /// </summary>
    public partial class KioskSetupPage2 : Page
    {
        KioskSetup kiosk;

        public KioskSetupPage2(KioskSetup kiosk, IDBInterface database)
        {
            InitializeComponent();
            this.kiosk = kiosk;

            List<Event> eventList = database.GetEvents();
            events.Items.Clear();
            ComboBoxItem boxItem;
            foreach (Event e in eventList)
            {
                boxItem = new ComboBoxItem
                {
                    Content = e.Name,
                    Uid = e.Identifier.ToString()
                };
                events.Items.Add(boxItem);
            }
            events.SelectedIndex = 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kiosk.Close();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            kiosk.GotoPage3(Convert.ToInt32(((ComboBoxItem)events.SelectedItem).Uid), (yesRadio.IsChecked == true ? 1 : 0));
        }
    }
}
