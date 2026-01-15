using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Constants;
using Chronokeep.Database.SQLite;
using Chronokeep.Objects;
using Chronokeep.UI.Parts;

namespace Chronokeep.UI.Timing.Windows;

public partial class RewindWindow : Window
{
    private readonly TimingSystem system;

    public RewindWindow(TimingSystem system)
    {
        InitializeComponent();
        this.MinWidth = 0;
        this.MinHeight = 0;
        this.SizeToContent = SizeToContent.Height;
        this.Width = 400;
        this.system = system;
        string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
        FromDate.Text = dateStr;
        ToDate.Text = dateStr;
        FromTime.Text = "00:00:00";
        ToTime.Text = "23:59:59";
        if (system.Type == Constants.Readers.SYSTEM_IPICO || system.Type == Constants.Readers.SYSTEM_IPICO_LITE)
        {
            Reader1.Visibility = Visibility.Visible;
            Reader2.Visibility = Visibility.Visible;
        }
    }

    public bool IsTimingSystem(TimingSystem timingSystem)
    {
        return this.system.Equals(timingSystem);
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e) { }

    private void SetYesterday_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string dateStr = DateTime.Now.AddDays(-1).ToString("MM/dd/yyyy");
        FromDate.Text = dateStr;
        ToDate.Text = dateStr;
        FromTime.Text = "00:00:00";
        ToTime.Text = "23:59:59";
    }

    private void SetToday_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
        FromDate.Text = dateStr;
        ToDate.Text = dateStr;
        FromTime.Text = "00:00:00";
        ToTime.Text = "23:59:59";
    }

    private void SetTomorrow_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string dateStr = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
        FromDate.Text = dateStr;
        ToDate.Text = dateStr;
        FromTime.Text = "00:00:00";
        ToTime.Text = "23:59:59";
    }

    private void Rewind_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!DateTime.TryParse(string.Format("{0} {1}", FromDate.Text, FromTime.Text.Replace('_', '0')), out DateTime from))
        {
            from = DateTime.Now;
        }
        if (!DateTime.TryParse(string.Format("{0} {1}", ToDate.Text, ToTime.Text.Replace('_', '0')), out DateTime to))
        {
            to = DateTime.Now;
        }
        if (system.Type == Constants.Readers.SYSTEM_IPICO || system.Type == Constants.Readers.SYSTEM_IPICO_LITE)
        {
            DialogBox.Show(
                "This process can take up to 3 minutes to complete. There is no guarantee that other processes will work properly while this is occuring. Are you sure you wish to proceed?",
                "Yes",
                "No",
                () =>
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (o, ea) =>
                    {
                        system.SystemInterface.Rewind(from, to, Reader1.IsChecked == true ? 1 : 2);
                        ((IpicoInterface)system.SystemInterface).GetRewind();
                    };
                    worker.RunWorkerCompleted += (o, ea) =>
                    {
                        busyIndicator.IsBusy = false;
                    };
                    busyIndicator.IsBusy = true;
                    worker.RunWorkerAsync();
                });
        }
        else
        {
            system.SystemInterface.Rewind(from, to);
        }
        this.Close();
    }

    private void Done_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }
}