using Chronokeep.Objects;
using System.Collections.Generic;

namespace Chronokeep.UI.Import
{
    /// <summary>
    /// Interaction logic for ImportPageMultiples.xaml
    /// </summary>
    public partial class ImportFilePageConflicts
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
            List<Participant> output = [];
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
