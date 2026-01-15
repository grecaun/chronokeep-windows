using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using static Chronokeep.UI.Timing.ClockControl;

namespace Chronokeep.UI.Timing.Windows;

public partial class ClockControl : Window
{
    private static ClockControl theOne = null;

    private readonly IMainWindow window;
    private readonly IDBInterface database;

    private readonly Dictionary<int, Chronoclock> ClockDict = [];

    private ClockControl(IMainWindow window, IDBInterface database)
    {
        InitializeComponent();
        this.window = window;
        this.database = database;
        this.MinWidth = 10;
        this.MinHeight = 10;
        List<Chronoclock> clocks = database.GetClocks();
        foreach (Chronoclock clock in clocks)
        {
            ClockDict[clock.Identifier] = clock;
        }
        UpdateView();
    }
    public static ClockControl CreateWindow(IMainWindow window, IDBInterface database)
    {
        theOne ??= new(window, database);
        return theOne;
    }

    private void RemoveClock(Chronoclock clock)
    {
        database.RemoveClocks([clock]);
        ClockDict.Remove(clock.Identifier);
        UpdateView();
    }

    private void UpdateView()
    {
        Log.D("UI.Timing.ClockControl", "UpdateView");
        foreach (ClockListItem clItem in clockListView.Items)
        {
            Chronoclock clock = clItem.GetUpdatedClock();
            ClockDict[clock.Identifier] = clock;
        }
        clockListView.Items.Clear();
        foreach (Chronoclock clock in ClockDict.Values)
        {
            clockListView.Items.Add(new ClockListItem(clock, this, database.GetCurrentEvent()));
        }
    }

    private void UpdateTime(string time)
    {
        TimeLabel.Text = string.Format("Clock time is {0}", time);
        TimeLabel.Visibility = Visibility.Visible;
        CurrentTimeLabel.Text = string.Format("System time is {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        CurrentTimeLabel.Visibility = Visibility.Visible;
    }

    private void Window_Closed(object? sender, System.EventArgs e)
    {
        Log.D("UI.Timing.ClockControl", "Window is closed.");
        theOne = null;
        foreach (ClockListItem clItem in clockListView.Items)
        {
            Chronoclock clock = clItem.GetUpdatedClock();
            database.UpdateClock(clock);
        }
        window.WindowFinalize(this);
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl", "Close button clicked.");
        Close();
    }

    private void AddButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Chronoclock newClock = new()
        {
            Name = "New Clock",
            URL = "chronoclock.local",
            Enabled = false,
        };
        newClock.Identifier = database.AddClock(newClock);
        if (newClock.Identifier >= 0)
        {
            ClockDict[newClock.Identifier] = newClock;
        }
        UpdateView();
    }
}