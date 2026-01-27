using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Objects;

namespace Chronokeep.UI.Import;

public partial class ImportFilePageConflicts : UserControl
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