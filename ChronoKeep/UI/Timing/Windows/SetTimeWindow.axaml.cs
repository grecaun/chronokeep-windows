using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.Timing.Windows;

public partial class SetTimeWindow : Window
{
    private readonly ITimingPage parent;
    private readonly TimingSystem timingSystem;

    public SetTimeWindow(ITimingPage parent, TimingSystem timingSystem)
    {
        InitializeComponent();
        this.parent = parent;
        this.timingSystem = timingSystem;
        this.MinHeight = 0;
        this.MinWidth = 0;
        this.Width = 400;
        this.SizeToContent = SizeToContent.Height;
    }

    public bool IsTimingSystem(TimingSystem timingSystem)
    {
        return this.timingSystem.Equals(timingSystem);
    }

    public void UpdateTime()
    {
        TimeLabel.Text = string.Format("Reader time is {0}", timingSystem.SystemTime);
        CurrentTimeLabel.Text = string.Format("System time is {0}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e) { }

    private void Check_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        timingSystem.SystemInterface.GetTime();
    }

    private void Set_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DateTime alternateDate;
        if (DateTime.TryParse(string.Format("{0} {1}", SpecificDateBox.Text, SpecificTimeBox.Text.Replace('_', '0')), out alternateDate) == false)
        {
            alternateDate = DateTime.Now;
        }
        if (SetAllCheckBox.IsChecked == true)
        {
            parent.SetAllTimingSystemsToTime(alternateDate, NowTimeRadioButton.IsChecked == true);
        }
        else if (NowTimeRadioButton.IsChecked == true)
        {
            timingSystem.SystemInterface.SetTime(DateTime.Now);
        }
        else
        {
            timingSystem.SystemInterface.SetTime(alternateDate);
        }
    }

    private void Done_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }
}