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

namespace ChronoKeep.UI.Import
{
    /// <summary>
    /// Interaction logic for ImportPageMultiples.xaml
    /// </summary>
    public partial class ImportFilePageConflicts : Page
    {
        public ImportFilePageConflicts(List<Participant> conflicts, Event theEvent)
        {
            InitializeComponent();
            foreach (Participant part in conflicts)
            {
                multiplesListBox.Items.Add(new MultipleEntryListBoxItem(part, theEvent));
            }
        }

        public List<Participant> GetParticipantsToRemove()
        {
            List<Participant> output = new List<Participant>();
            foreach (MultipleEntryListBoxItem item in multiplesListBox.Items)
            {
                if (item.Keep.IsChecked == false)
                {
                    output.Add(item.Part);
                }
            }
            return output;
        }
    }
}
