using Avalonia.Controls;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.UI.Parts;
using System;
using System.Collections.Generic;

namespace Chronokeep.UI.API.Parts;

public partial class ReaderListItem : UserControl
{
    private readonly RemoteReader reader;
    private readonly APIObject api;
    private readonly IDBInterface database;
    private readonly IMainWindow mWindow;

    public ReaderListItem(
        RemoteReader reader,
        APIObject api,
        Dictionary<(int, string), RemoteReader> savedReaders,
        IDBInterface database,
        IMainWindow mWindow
        )
    {
        this.database = database;
        this.mWindow = mWindow;
        this.api = api;
        this.reader = reader;
        var theEvent = database.GetCurrentEvent();
        if (theEvent == null || theEvent.Identifier < 1)
        {
            return;
        }
        this.reader.EventID = theEvent.Identifier;
        List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
        locations.Insert(0, new(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
        if (!theEvent.CommonStartFinish)
        {
            locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            locations.Insert(0, new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
        }
        else
        {
            locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
        }
        autoFetch.IsChecked = savedReaders.ContainsKey((reader.APIIDentifier, reader.Name));
        nameBlock.Text = reader.Name;
        foreach (TimingLocation loc in locations)
        {
            locationBox.Items.Add(new ComboBoxItem()
            {
                Content = loc.Name,
                Tag = loc.Identifier.ToString(),
                IsSelected = reader.LocationID == loc.Identifier,
            });
        }
        if (locationBox.SelectedItem == null)
        {
            locationBox.SelectedIndex = 0;
        }
        string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
        startDatePicker.Text = dateStr;
        endDatePicker.Text = dateStr;
    }

    public RemoteReader GetUpdatedReader()
    {
        RemoteReader output = new()
        {
            Name = reader.Name,
            EventID = reader.EventID,
            APIIDentifier = api!.Identifier
        };
        if (locationBox.SelectedItem != null && int.TryParse((string)((ComboBoxItem)locationBox.SelectedItem).Tag!, out var locId))
        {
            output.LocationID = locId;
        }
        else
        {
            output.LocationID = Constants.Timing.LOCATION_FINISH;
        }
        return output;
    }

    public bool AutoDownloadReads()
    {
        return autoFetch.IsChecked == true;
    }

    private async void Rewind_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "Rewind clicked.");
        if (!DateTime.TryParse(string.Format("{0} {1}", startDatePicker.Text, startTimeBox.Text!.Replace('_', '0')), out DateTime startDate))
        {
            startDate = DateTime.Now;
        }
        if (!DateTime.TryParse(string.Format("{0} {1}", endDatePicker.Text, endTimeBox.Text!.Replace('_', '0')), out DateTime endDate))
        {
            endDate = DateTime.Now;
        }
        try
        {
            var theEvent = database!.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 1)
            {
                return;
            }
            this.reader.EventID = theEvent.Identifier;
            if (locationBox.SelectedItem == null)
            {
                this.reader.LocationID = Constants.Timing.LOCATION_FINISH;
            }
            else
            {
                this.reader.LocationID = Convert.ToInt32(((ComboBoxItem)locationBox.SelectedItem).Tag);
            }
            (var reads, var note) = await api!.GetReads(this.reader, startDate, endDate);
            this.database.AddChipReads(reads);
            mWindow.UpdateTimingFromController();
            DialogBox.Show("Rewind complete.");
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private void Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "Delete clicked.");
        DialogBox.Show(
            "Warning!\n\nThis will delete every read uploaded to the remote api. That data cannot be recoverred once deleted.",
            "Delete",
            "Cancel",
            async () =>
            {
                Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "User requests deletion.");
                if (!DateTime.TryParse(string.Format("{0} {1}", startDatePicker.Text, startTimeBox.Text!.Replace('_', '0')), out DateTime startDate))
                {
                    startDate = DateTime.Now;
                }
                if (!DateTime.TryParse(string.Format("{0} {1}", endDatePicker.Text, endTimeBox.Text!.Replace('_', '0')), out DateTime endDate))
                {
                    endDate = DateTime.Now;
                }
                try
                {
                    long count = await api.DeleteReads(this.reader, startDate, endDate);
                    mWindow.UpdateTimingFromController();
                    DialogBox.Show(string.Format("Successfully deleted\n\n{0}\n\nreads.", count));
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    return;
                }
            });
    }
}