using Avalonia.Controls;
using Chronokeep.Objects;
using Chronokeep.UI.Parts;
using System.Collections.Generic;
using System.Linq;

namespace Chronokeep.UI.Import;

public partial class ImportFilePageConflicts : UserControl
{
    public ImportFilePageConflicts(List<Participant> conflicts, Event theEvent)
    {
        InitializeComponent();
        foreach (Participant part in conflicts)
        {
            multiplesListBox.Items.Add(new MultipleEntryPart(part, theEvent));
        }
    }

    public List<Participant> GetParticipantsToRemove()
    {
        List<Participant> output = [];
        foreach (MultipleEntryPart item in multiplesListBox.Items.Cast<MultipleEntryPart>())
        {
            if (item.Keep.IsChecked == false)
            {
                output.Add(item.Part);
            }
        }
        return output;
    }
}