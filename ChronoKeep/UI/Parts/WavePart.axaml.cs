using Avalonia.Controls;
using Chronokeep.Helpers;
using System;

namespace Chronokeep.UI.Parts;

public partial class WavePart : UserControl
{
    public int Wave { get; private set; }
    public bool PlusWave { get; set; }
    public bool MinusWave { get => !PlusWave; }

    private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

    public WavePart(int num, long startSeconds, int startMilliseconds)
    {
        InitializeComponent();
        Wave = num;
        if (startSeconds >= 0)
        {
            PlusWave = true;
        }
        else
        {
            PlusWave = false;
            startSeconds = startSeconds * -1;
        }
        if (startMilliseconds < 0)
        {
            startMilliseconds = startMilliseconds * -1;
        }
        StartOffset.Text = string.Format(TimeFormat, startSeconds / 3600,
            (startSeconds % 3600) / 60, startSeconds % 60,
            startMilliseconds);
    }

    public void SetTime(long seconds, int milliseconds)
    {
        StartOffset.Text = string.Format(TimeFormat, seconds / 3600,
            (seconds % 3600) / 60, seconds % 60,
            milliseconds);
    }

    public int GetWave()
    {
        return Wave;
    }

    public (int, long, int) GetValues()
    {
        string[] firstparts = StartOffset.Text!.Replace('_', '0').Split(':');
        string[] secondparts = firstparts[2].Split('.');
        try
        {
            int hours = Convert.ToInt32(firstparts[0]),
                minutes = Convert.ToInt32(firstparts[1]),
                seconds = Convert.ToInt32(secondparts[0]),
                milliseconds = Convert.ToInt32(secondparts[1]);
            seconds = (hours * 3600) + (minutes * 60) + seconds;
            if (MinusWave)
            {
                Log.D("UI.Timing.WaveWindow", "Negative wave, setting values to match.");
                seconds *= -1;
                milliseconds *= -1;
            }
            return (Wave, seconds, milliseconds);
        }
        catch
        {
            Log.D("UI.Timing.WaveWindow", "Error evaluating values.");
        }
        return (Wave, 0, 0);
    }

    private void SelectAll(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }

    private void SwapWaveType_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.WaveWindow", "Plus/Minus sign clicked. WaveType is: " + (PlusWave ? "+" : "-"));
        PlusWave = !PlusWave;
    }
}