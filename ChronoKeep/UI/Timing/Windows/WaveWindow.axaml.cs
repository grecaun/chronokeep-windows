using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.Timing.Windows;

public partial class WaveWindow : Window
{
    private readonly IMainWindow window;
    private readonly IDBInterface database;
    private readonly Event theEvent;
    private readonly Dictionary<int, Distance> distanceDictionary = [];
    private readonly Dictionary<int, (long seconds, int milliseconds)> waveTimes = [];
    private readonly HashSet<int> waves = [];

    private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

    public WaveWindow(IMainWindow window, IDBInterface database)
    {
        InitializeComponent();
        this.MinHeight = 300;
        this.MinWidth = 230;
        this.Width = 300;
        this.window = window;
        this.database = database;
        theEvent = database.GetCurrentEvent();
        if (theEvent == null || theEvent.Identifier == -1) return;
        foreach (Distance div in database.GetDistances(theEvent.Identifier))
        {
            distanceDictionary[div.Identifier] = div;
            waves.Add(div.Wave);
            waveTimes[div.Wave] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
        }
        List<int> sortedWaves = [.. waves];
        sortedWaves.Sort();
        foreach (int waveNum in sortedWaves)
        {
            long seconds = waveTimes[waveNum].seconds;
            int milliseconds = waveTimes[waveNum].milliseconds;
            Log.D("UI.Timing.WaveWindow", string.Format("Seconds {0} - Milliseconds {1}", seconds, milliseconds));
            WaveList.Items.Add(new AWave(waveNum, waveTimes[waveNum].seconds, waveTimes[waveNum].milliseconds));
        }
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        window?.WindowFinalize(this);
    }

    private void NetTimeButton_Checked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.WaveWindow", "Net Time Selected.");
        foreach (AWave wave in WaveList.Items)
        {
            int waveId = wave.GetWave();
            wave.SetTime(waveTimes[waveId].seconds, waveTimes[waveId].milliseconds);
        }
    }

    private void TimeofDayButton_Checked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.WaveWindow", "Time of day selected.");
        foreach (AWave wave in WaveList.Items)
        {
            int waveId = wave.GetWave();
            wave.SetTime(waveTimes[waveId].seconds + theEvent.StartSeconds, waveTimes[waveId].milliseconds + theEvent.StartMilliseconds);
        }
    }

    private void SetButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.WaveWindow", "Aye aye! Updating!");
        foreach (AWave wave in WaveList.Items)
        {
            (int waveNo, long seconds, int milliseconds) = wave.GetValues();
            if (TimeofDayButton.IsChecked == true)
            {
                seconds = seconds - theEvent.StartSeconds;
                milliseconds = milliseconds - theEvent.StartMilliseconds;
                if (milliseconds < 0)
                {
                    seconds -= 1;
                    milliseconds = 1000 - milliseconds;
                }
                if (seconds < 0)
                {
                    seconds = 0;
                    milliseconds = 0;
                }
            }
            database.SetWaveTimes(theEvent.Identifier, waveNo, seconds, milliseconds);
        }
        List<Distance> newDistances = database.GetDistances(theEvent.Identifier);
        bool update = false;
        foreach (Distance div in newDistances)
        {
            if (!distanceDictionary.TryGetValue(div.Identifier, out Distance oDist)
                || oDist.StartOffsetSeconds != div.StartOffsetSeconds
                || oDist.StartOffsetMilliseconds != div.StartOffsetMilliseconds)
            {
                update = true;
            }
        }
        if (update)
        {
            database.ResetTimingResultsEvent(theEvent.Identifier);
            window.UpdateTiming();
            window.NotifyTimingWorker();
        }
        this.Close();
    }

    private void DoneButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.WaveWindow", "We don't really want to set the wave times.");
        this.Close();
    }
}