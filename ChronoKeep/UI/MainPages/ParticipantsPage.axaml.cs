using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.IO;
using Chronokeep.Interfaces.UI;
using Chronokeep.IO;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.UI.Import;
using Chronokeep.UI.Participants;
using Chronokeep.UI.Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.UI.MainPages;

public partial class ParticipantsPage : UserControl, IMainPage
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private readonly Event? theEvent;
    private List<Participant> allParticipants = [];
    private readonly List<Participant> conflicts = [];

    private readonly bool loaded = false;

    public ParticipantsPage(IMainWindow mainWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mainWindow;
        this.database = database;
        theEvent = database.GetCurrentEvent();
        SortBox.SelectedIndex = 0;
        loaded = true;
        UpdateDistancesBox();
    }

    public async void UpdateView()
    {
        Log.D("UI.MainPages.ParticipantsPage", "Updating Participants Page.");
        if (theEvent == null || theEvent.Identifier < 0)
        {
            return;
        }
        int distanceId = -1;
        try
        {
            distanceId = Convert.ToInt32((string)((ComboBoxItem)DistanceBox.SelectedItem!).Tag!);
        }
        catch
        {
            distanceId = -1;
        }
        Log.D("PartPage", string.Format("Distance ID is {0}", distanceId));
        List<Participant> newList = [];
        await Task.Run(() =>
        {
            allParticipants = database.GetParticipants(theEvent.Identifier);
            if (distanceId == -1)
            {
                newList.AddRange(allParticipants);
            }
            else
            {
                newList.AddRange(database.GetParticipants(theEvent.Identifier, distanceId));
            }
        });
        Dictionary<string, BibStats> bibStats = [];
        foreach (Participant p in allParticipants)
        {
            if (!bibStats.TryGetValue(p.Distance, out BibStats? bStats))
            {
                bStats = new()
                {
                    With = 0,
                    Without = 0,
                    DistanceName = p.Distance,
                };
                bibStats[p.Distance] = bStats;
            }
            if (p.Bib.Length > 0)
            {
                bStats.With += 1;
            }
            else
            {
                bStats.Without += 1;
            }
        }
        BibStats totals = new()
        {
            With = 0,
            Without = 0,
            DistanceName = "All"
        };
        List<BibStats> listStats = [];
        foreach (BibStats b in bibStats.Values)
        {
            listStats.Add(b);
            totals.With += b.With;
            totals.Without += b.Without;
        }
        if (bibStats.Values.Count > 1)
        {
            listStats.Insert(0, totals);
            ViewPanel.IsVisible = true;
        }
        else
        {
            ViewPanel.IsVisible = false;
        }
        statsListView.ItemsSource = listStats;
        if (totals.Without > 0)
        {
            statsExpander.IsVisible = true;
        }
        else
        {
            statsExpander.IsVisible = false;
        }
        if (SortBox.SelectedItem != null)
        {
            switch (((ComboBoxItem)SortBox.SelectedItem).Content)
            {
                case "Name":
                    newList.Sort(Participant.CompareByName);
                    break;
                case "Bib":
                    newList.Sort(Participant.CompareByBib);
                    break;
                default:
                    newList.Sort();
                    break;
            }
        }
        else
        {
            newList.Sort();
        }
        string search = SearchBox != null && SearchBox.Text != null ? SearchBox.Text.Trim() : "";
        newList.RemoveAll(x => x.IsNotMatch(search));
        ParticipantsList.ItemsSource = newList;
        if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
        {
            apiPanel.IsVisible = true;
        }
        else
        {
            apiPanel.IsVisible = false;
        }
        // Make conflict check HERE
        if (conflicts.Count > 0)
        {
            ConflictsBtn.Content = string.Format("Conflicts - {0}", conflicts.Count);
            ConflictsBtn.IsVisible = true;
        }
        else
        {
            ConflictsBtn.IsVisible = false;
        }
        Log.D("UI.MainPages.ParticipantsPage", "Participants updated.");
    }

    public void UpdateDistancesBox()
    {
        Log.D("UI.MainPages.ParticipantsPage", "Updating distances box.");
        DistanceBox.Items.Clear();
        DistanceBox.Items.Add(new ComboBoxItem()
        {
            Content = "All",
            Tag = "-1"
        });
        if (theEvent == null || theEvent.Identifier < 0)
        {
            return;
        }
        List<Distance> distances = database.GetDistances(theEvent.Identifier);
        distances.Sort();
        foreach (Distance d in distances)
        {
            DistanceBox.Items.Add(new ComboBoxItem()
            {
                Content = d.Name,
                Tag = d.Identifier.ToString()
            });
        }
        DistanceBox.SelectedIndex = 0;
    }

    public static void UpdateDatabase() { }

    public void Keyboard_Ctrl_A()
    {
        Add_Click(null, null);
    }

    public void Keyboard_Ctrl_S() { }

    public void Keyboard_Ctrl_Z() { }

    public void Closing()
    {
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE)!.Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
    }

    public async void DownloadParticipants()
    {
        // Get API to upload.
        if (theEvent!.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
        {
            Download.Content = "Download";
            return;
        }
        APIObject api = database.GetAPI(theEvent.API_ID)!;
        string[] event_ids = theEvent.API_Event_ID.Split(',');
        if (event_ids.Length != 2)
        {
            Download.Content = "Download";
            return;
        }
        try
        {
            int page = 1;
            List<APIPerson> newPersons = [];
            do
            {
                GetParticipantsResponse response = await APIHandlers.GetParticipants(api, event_ids[0], event_ids[1], 50, page);
                newPersons.AddRange(response.Participants);
                Log.D("UI.MainPages.ParticipantsPage", response.Participants.Count.ToString() + " participants downloaded.");
                if (response.Participants.Count != 50)
                {
                    break;
                }
                page++;
            } while (true);
            Log.D("UI.MainPages.ParticipantsPage", newPersons.Count.ToString() + " total participants downloaded.");
            // Key is (First, Last, Birthdate, Distance)
            Dictionary<(string, string, string, string), Participant> partDictionary = [];
            Dictionary<string, Participant> partESDictionary = [];
            Dictionary<string, Distance> distDictionary = [];
            string uniqueID = "";
            AppSetting programID = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER)!;
            if (programID != null)
            {
                uniqueID = string.Format("{0}-", programID.Value);
            }
            foreach (Participant p in database.GetParticipants(theEvent.Identifier))
            {
                partDictionary[(p.FirstName, p.LastName, p.Birthdate, p.Distance.ToLower())] = p;
                partESDictionary[string.Format("{0}{1}", uniqueID, p.EventSpecific.Identifier)] = p;
            }
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                distDictionary[d.Name.ToLower()] = d;
            }
            foreach (APIPerson person in newPersons)
            {
                if (!distDictionary.TryGetValue(person.Distance.ToLower(), out Distance? dist))
                {
                    distDictionary[person.Distance.ToLower()] = new(person.Distance, theEvent.Identifier);
                    database.AddDistance(new(person.Distance, theEvent.Identifier));
                }
            }
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                distDictionary[d.Name.ToLower()] = d;
            }
            conflicts.Clear();
            List<Participant> partsToUpdate = [];
            List<Participant> partsToAdd = [];
            foreach (APIPerson person in newPersons)
            {
                person.Trim();
                person.FormatData();
                if (distDictionary.TryGetValue(person.Distance.ToLower(), out Distance? distance))
                {
                    if (partESDictionary.TryGetValue(person.Identifier, out Participant? old) && old != null && old.IsSimilar(person))
                    {
                        // Only update if a bib exists and it has not been updated in the software since it was uploaded
                        // Uploaded Version should equal Version, Version will be higher if it was updated after upload.
                        if (person.Bib.Length > 0 && old.EventSpecific.UploadedVersion >= old.EventSpecific.Version)
                        {
                            Participant newPart = new(
                                old.Identifier,
                                person.First.Length > 0 ? person.First : old.FirstName,
                                person.Last.Length > 0 ? person.Last : old.LastName,
                                old.Street,
                                old.City,
                                old.State,
                                old.Zip,
                                person.Birthdate,
                                new(
                                    old.EventSpecific.Identifier,
                                    theEvent.Identifier,
                                    distDictionary[person.Distance.ToLower()].Identifier,
                                    distDictionary[person.Distance.ToLower()].Name,
                                    person.Bib,
                                    old.EventSpecific.CheckedIn,
                                    old.EventSpecific.Comments,
                                    old.EventSpecific.Owes,
                                    old.EventSpecific.Other,
                                    old.EventSpecific.Status,
                                    old.EventSpecific.AgeGroupName,
                                    old.EventSpecific.AgeGroupId,
                                    person.Anonymous,
                                    person.SMSEnabled,
                                    person.Apparel,
                                    old.EventSpecific.Division,
                                    old.EventSpecific.Version,
                                    old.EventSpecific.UploadedVersion
                                    ),
                                old.Email,
                                old.Phone,
                                person.Mobile.Length > 0 ? person.Mobile : old.Mobile,
                                old.Parent,
                                old.Country,
                                old.Street2,
                                person.Gender,
                                old.ECName,
                                old.ECPhone
                                );
                            // Check if we've updated the Bib
                            if (old.Bib.Length > 0 && !old.Bib.Equals(person.Bib, StringComparison.OrdinalIgnoreCase))
                            {
                                conflicts.Add(old);
                                conflicts.Add(newPart);
                            }
                            partsToUpdate.Add(newPart);
                        }
                    }
                    else if (partDictionary.TryGetValue((person.First, person.Last, person.Birthdate, person.Distance.ToLower()), out Participant? oldTwo))
                    {
                        // Only update if a bib exists and it has not been updated in the software since it was uploaded
                        // Uploaded Version should equal Version, Version will be higher if it was updated after upload.
                        if (person.Bib.Length > 0 && oldTwo.EventSpecific.UploadedVersion >= oldTwo.EventSpecific.Version)
                        {
                            Participant newPart = new(
                                oldTwo.Identifier,
                                person.First.Length > 0 ? person.First : oldTwo.FirstName,
                                person.Last.Length > 0 ? person.Last : oldTwo.LastName,
                                oldTwo.Street,
                                oldTwo.City,
                                oldTwo.State,
                                oldTwo.Zip,
                                person.Birthdate,
                                new(
                                    oldTwo.EventSpecific.Identifier,
                                    theEvent.Identifier,
                                    distDictionary[person.Distance.ToLower()].Identifier,
                                    distDictionary[person.Distance.ToLower()].Name,
                                    person.Bib,
                                    oldTwo.EventSpecific.CheckedIn,
                                    oldTwo.EventSpecific.Comments,
                                    oldTwo.EventSpecific.Owes,
                                    oldTwo.EventSpecific.Other,
                                    oldTwo.EventSpecific.Status,
                                    oldTwo.EventSpecific.AgeGroupName,
                                    oldTwo.EventSpecific.AgeGroupId,
                                    person.Anonymous,
                                    person.SMSEnabled,
                                    person.Apparel,
                                    oldTwo.EventSpecific.Division,
                                    oldTwo.EventSpecific.Version,
                                    oldTwo.EventSpecific.UploadedVersion
                                    ),
                                oldTwo.Email,
                                oldTwo.Phone,
                                person.Mobile.Length > 0 ? person.Mobile : oldTwo.Mobile,
                                oldTwo.Parent,
                                oldTwo.Country,
                                oldTwo.Street2,
                                person.Gender,
                                oldTwo.ECName,
                                oldTwo.ECPhone
                                );
                            // Check if we've updated the Bib.
                            if (old!.Bib.Length > 0 && !oldTwo.Bib.Equals(person.Bib, StringComparison.OrdinalIgnoreCase))
                            {
                                conflicts.Add(oldTwo);
                                conflicts.Add(newPart);
                            }
                            partsToUpdate.Add(newPart);
                        }
                    }
                    else if (person.First.Length > 0 || person.Last.Length > 0)
                    {
                        partsToAdd.Add(
                            new(
                                person.First,
                                person.Last,
                                "",
                                "",
                                "",
                                "",
                                person.Birthdate,
                                new(
                                    theEvent.Identifier,
                                    distDictionary[person.Distance.ToLower()].Identifier,
                                    distDictionary[person.Distance.ToLower()].Name,
                                    person.Bib,
                                    0,
                                    "",
                                    "",
                                    "",
                                    person.Anonymous,
                                    person.SMSEnabled,
                                    person.Apparel,
                                    ""
                                ),
                                "",
                                "",
                                person.Mobile,
                                "",
                                "",
                                "",
                                person.Gender,
                                "",
                                ""
                            )
                        );
                    }
                }
            }
            if (partsToUpdate.Count > 0)
            {
                database.UpdateParticipants(partsToUpdate);
            }
            if (partsToAdd.Count > 0)
            {
                database.AddParticipants(partsToAdd);
            }
            Dictionary<string, Participant> knownBibs = [];
            // This checks for doubles of bibs in existing information.
            foreach (Participant part in partESDictionary.Values)
            {
                if (part.Bib.Length > 0)
                {
                    if (knownBibs.TryGetValue(part.Bib, out Participant? known))
                    {
                        if (!part.IsSimilar(known))
                        {
                            conflicts.Add(part);
                            conflicts.Add(known);
                        }
                    }
                    else
                    {
                        knownBibs.Add(part.Bib, part);
                    }
                }
            }
            foreach (Participant part in partsToUpdate)
            {
                if (part.Bib.Length > 0)
                {
                    if (knownBibs.TryGetValue(part.Bib, out Participant? known))
                    {
                        if (!part.IsSimilar(known))
                        {
                            conflicts.Add(part);
                            conflicts.Add(known);
                        }
                    }
                    else
                    {
                        knownBibs.Add(part.Bib, part);
                    }
                }
            }
            foreach (Participant part in partsToAdd)
            {
                if (part.Bib.Length > 0)
                {
                    if (knownBibs.TryGetValue(part.Bib, out Participant? known))
                    {
                        if (!part.IsSimilar(known))
                        {
                            conflicts.Add(part);
                            conflicts.Add(known);
                        }
                    }
                    else
                    {
                        knownBibs.Add(part.Bib, part);
                    }
                }
            }
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            Download.Content = "Download";
            return;
        }
        Download.Content = "Download";
        UpdateView();
    }

    private async void UploadParticipants()
    {
        // Get API to upload.
        if (theEvent!.API_ID < 0 || theEvent.API_Event_ID.Length < 1)
        {
            Upload.Content = "Upload";
            return;
        }
        APIObject api = database.GetAPI(theEvent.API_ID)!;
        string[] event_ids = theEvent.API_Event_ID.Split(',');
        if (event_ids.Length != 2)
        {
            Upload.Content = "Upload";
            return;
        }
        // Get results to upload.
        List<Participant> participants = database.GetParticipants(theEvent.Identifier);
        List<BibChipAssociation> bibChips = database.GetBibChips(theEvent.Identifier);
        if (participants.Count < 1)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Nothing to upload.");
            Upload.Content = "Upload";
            return;
        }
        // Change Participant to APIPerson
        List<APIPerson> upParticipants = [];
        List<BibChip> upBibChips = [];
        Log.D("UI.MainPages.ParticipantsPage", "Participants count: " + participants.Count.ToString());
        string uniqueID = "";
        AppSetting programID = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER)!;
        if (programID != null)
        {
            uniqueID = string.Format("{0}-", programID.Value);
        }
        foreach (Participant part in participants)
        {
            upParticipants.Add(new(part, uniqueID));
        }
        Log.D("UI.MainPages.ParticipantsPage", "BibChips count: " + bibChips.Count.ToString());
        foreach (BibChipAssociation bc in bibChips)
        {
            upBibChips.Add(new()
            {
                Bib = bc.Bib,
                Chip = bc.Chip,
            });
        }
        Log.D("UI.MainPages.ParticipantsPage", "Attempting to upload " + upParticipants.Count.ToString() + " participants.");
        int total = 0;
        int loops = upParticipants.Count / Constants.Timing.API_LOOP_COUNT;
        AddResultsResponse response;
        for (int i = 0; i < loops; i += 1)
        {
            try
            {
                response = await APIHandlers.UploadParticipants(api, event_ids[0], event_ids[1], upParticipants.GetRange(i * Constants.Timing.API_LOOP_COUNT, Constants.Timing.API_LOOP_COUNT));
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                Upload.Content = "Upload";
                return;
            }
            if (response != null)
            {
                total += response.Count;
                Log.D("UI.MainPages.ParticipantsPage", "Total: " + total + " Count: " + response.Count);
            }
        }
        int leftovers = upParticipants.Count - (loops * Constants.Timing.API_LOOP_COUNT);
        if (leftovers > 0)
        {
            try
            {
                response = await APIHandlers.UploadParticipants(api, event_ids[0], event_ids[1], upParticipants.GetRange(loops * Constants.Timing.API_LOOP_COUNT, leftovers));
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                Upload.Content = "Upload";
                return;
            }
            if (response != null)
            {
                total += response.Count;
                Log.D("UI.MainPages.TimingPage", "Total: " + total + " Count: " + response.Count);
            }
            Log.D("UI.MainPages.TimingPage", "Upload finished. Count total: " + total);
        }
        foreach (Participant part in participants)
        {
            // record the version number that we uploaded, should default to 0 for anything we haven't touched
            part.EventSpecific.UploadedVersion = part.EventSpecific.Version;
        }
        database.UpdateParticipants(participants);
        Log.D("UI.MainPages.ParticipantsPage", "Attempting to upload " + upBibChips.Count.ToString() + " bibchips.");
        total = 0;
        loops = upBibChips.Count / Constants.Timing.API_LOOP_COUNT;
        for (int i = 0; i < loops; i += 1)
        {
            try
            {
                response = await APIHandlers.UploadBibChips(api, event_ids[0], event_ids[1], upBibChips.GetRange(i * Constants.Timing.API_LOOP_COUNT, Constants.Timing.API_LOOP_COUNT));
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                Upload.Content = "Upload";
                return;
            }
            if (response != null)
            {
                total += response.Count;
                Log.D("UI.MainPages.ParticipantsPage", "Total: " + total + " Count: " + response.Count);
            }
        }
        leftovers = upBibChips.Count - (loops * Constants.Timing.API_LOOP_COUNT);
        if (leftovers > 0)
        {
            try
            {
                response = await APIHandlers.UploadBibChips(api, event_ids[0], event_ids[1], upBibChips.GetRange(loops * Constants.Timing.API_LOOP_COUNT, leftovers));
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                Upload.Content = "Upload";
                return;
            }
            if (response != null)
            {
                total += response.Count;
                Log.D("UI.MainPages.TimingPage", "Total: " + total + " Count: " + response.Count);
            }
            Log.D("UI.MainPages.TimingPage", "Upload finished. Count total: " + total);
        }
        Upload.Content = "Upload";
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Import Excel clicked.");
        var files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = [Utils.ExcelType, FilePickerFileTypes.All],
            AllowMultiple = false,
        });
        if (files.Count > 0)
        {
            string ext = Path.GetExtension(files[0].Name);
            Log.D("UI.MainPages.ParticipantsPage", $"Extension found: {ext}");
            try
            {
                IDataImporter importer;
                if (ext == ".xlsx" || ext == ".xls")
                {
                    importer = new ExcelImporter(files[0].Name);
                }
                else
                {
                    importer = new CSVImporter(files[0].Name);
                }
                importer.FetchHeaders();
                ImportFileWindow importWindow = ImportFileWindow.NewWindow(mWindow, importer, database);
                if (importWindow != null)
                {
                    mWindow.AddWindow(importWindow);
                    _ = importWindow.ShowDialog((Window)mWindow);
                }
            }
            catch (Exception ex)
            {
                DialogBox.Show("There was a problem importing the file.");
                Log.E("UI.MainPages.ParticipantsPage", $"Something went wrong when trying to read the Excel file. {ex.StackTrace}");
            }
        }
    }

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Export clicked.");
        var file = await TopLevel.GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = [Utils.ExcelType],
            SuggestedFileName = string.Format("{0} {1} Entrants.{2}", theEvent!.YearCode, theEvent.Name, "xlsx"),
        });
        if (file is not null)
        {
            if (theEvent != null)
            {
                await Task.Run(() =>
                {
                    Log.D("UI.MainPages.ParticipantsPage", "Event has name " + theEvent.Name + " and date of " + theEvent.Date + " and finally has ID " + theEvent.Identifier);
                    List<Participant> parts = database.GetParticipants(theEvent.Identifier);
                    string[] headers = [
                        "Bib",
                            "Distance",
                            "Status",
                            "First",
                            "Last",
                            "Birthday",
                            "Age",
                            "Age Group",
                            "Division",
                            "Street",
                            "Apartment",
                            "City",
                            "State",
                            "Zip",
                            "Country",
                            "Phone",
                            "Mobile",
                            "Email",
                            "Parent",
                            "Gender",
                            "Comments",
                            "Other",
                            "Owes",
                            "Emergency Contact Name",
                            "Emergency Contact Phone",
                            "Anonymous",
                            "Apparel" // new
                    ];
                    List<object[]> data = [];
                    foreach (Participant p in parts)
                    {
                        data.Add([
                            p.Bib,
                                p.Distance,
                                p.EventSpecific.StatusStr,
                                p.FirstName,
                                p.LastName,
                                p.Birthdate,
                                p.Age(theEvent.Date),
                                p.EventSpecific.AgeGroupName,
                                p.EventSpecific.Division,
                                p.Street,
                                p.Street2,
                                p.City,
                                p.State,
                                p.Zip,
                                p.Country,
                                p.Phone,
                                p.Mobile,
                                p.Email,
                                p.Parent,
                                p.Gender,
                                // Get rid of all the quote and newline characters.
                                p.Comments.Replace('\"', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace('\'', ' '),
                                p.Other.Replace('\"', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace('\'', ' '),
                                p.Owes,
                                p.ECName,
                                p.ECPhone,
                                p.PrettyAnonymous,
                                p.Apparel,
                            ]);
                    }
                    IDataExporter? exporter = null;
                    string extension = Path.GetExtension(file.Name);
                    Log.D("UI.MainPages.ParticipantsPage", string.Format("Extension is '{0}'", extension));
                    if (extension.Contains("xls", StringComparison.CurrentCulture))
                    {
                        exporter = new ExcelExporter();
                    }
                    else
                    {
                        StringBuilder format = new();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            format.Append("\"{");
                            format.Append(i);
                            format.Append("}\",");
                        }
                        format.Remove(format.Length - 1, 1);
                        Log.D("UI.MainPages.ParticipantsPage", string.Format("The format is '{0}'", format.ToString()));
                        exporter = new CSVExporter(format.ToString());
                    }
                    if (exporter != null)
                    {
                        exporter.SetData(headers, data);
                        exporter.ExportData(file.Name);
                    }
                });
                DialogBox.Show("File saved.");
            }
        }
    }

    private void Upload_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Upload clicked.");
        if (Upload.Content!.ToString() != "Working")
        {
            Log.D("UI.MainPages.TimingPage", "Uploading data.");
            Upload.Content = "Working";
            UploadParticipants();
            return;
        }
        Log.D("UI.MainPages.ParticipantsPage", "Already uploading.");
    }

    private void Download_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Download clicked.");
        if (Download.Content!.ToString() != "Working")
        {
            Log.D("UI.MainPages.TimingPage", "Downloading data.");
            Download.Content = "Working";
            DownloadParticipants();
            return;
        }
        Log.D("UI.MainPages.ParticipantsPage", "Already downloading.");
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Delete clicked.");
        if (Delete.Content!.ToString() != "Working")
        {
            Log.D("UI.MainPages.ParticipantsPage", "Deleting uploaded participants data.");
            Delete.Content = "Working";
            APIObject? api = null;
            try
            {
                api = database.GetAPI(theEvent!.API_ID);
                Log.D("UI.MainPages.ParticipantsPage", "API found.");
            }
            catch { }
            // Get the event id values. Exit if not valid.
            string[] event_ids = theEvent!.API_Event_ID.Split(',');
            // Create a bool for checking if we've grabbed the APIController's lock so we release it later
            if (event_ids.Length == 2)
            {
                try
                {
                    Log.D("UI.MainPages.ParticipantsPage", "Deleting participants from API.");
                    await APIHandlers.DeleteParticipants(api!, event_ids[0], event_ids[1]);
                    await APIHandlers.DeleteBibChips(api!, event_ids[0], event_ids[1]);
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                }
            }
            Delete.Content = "Delete Uploaded";
            return;
        }
        Log.D("UI.MainPages.ParticipantsPage", "Already deleting.");
    }

    private void ConflictsBtn_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Conflicts clicked.");
        ParticipantConflicts conflictWindow = ParticipantConflicts.NewWindow(mWindow, conflicts);
        if (conflictWindow != null)
        {
            mWindow.AddWindow(conflictWindow);
            conflictWindow.ShowDialog((Window)mWindow);
        }
    }

    private void DistanceBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "New Distance selected.");
        UpdateView();
    }

    private void SortBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!loaded) return;
        Log.D("UI.MainPages.ParticipantsPage", "Sort style changed.");
        List<Participant> newParts = [.. allParticipants];
        switch (((ComboBoxItem)SortBox.SelectedItem!).Content)
        {
            case "Name":
                newParts.Sort(Participant.CompareByName);
                break;
            case "Bib":
                newParts.Sort(Participant.CompareByBib);
                break;
            default:
                newParts.Sort();
                break;
        }
        ParticipantsList.SelectedItems.Clear();
        ParticipantsList.ItemsSource = newParts;
        Log.D("UI.MainPages.ParticipantsPage", "Done");
    }

    private void Modify_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Modify clicked.");
        List<Participant> selected = [];
        foreach (Participant p in ParticipantsList.SelectedItems)
        {
            selected.Add(p);
        }
        Log.D("UI.MainPages.ParticipantsPage", selected.Count + " participants selected.");
        if (selected.Count > 1)
        {
            ChangeMultiParticipantWindow changeMultiParticipantWindow = new(mWindow, database, selected);
            mWindow.AddWindow(changeMultiParticipantWindow);
            changeMultiParticipantWindow.ShowDialog((Window)mWindow);
            return;
        }
        Participant? part = null;
        foreach (Participant p in selected)
        {
            part = p;
        }
        if (part == null) return;
        ModifyParticipantWindow modifyParticipant = ModifyParticipantWindow.NewWindow(mWindow, database, part);
        if (modifyParticipant != null)
        {
            mWindow.AddWindow(modifyParticipant);
            modifyParticipant.ShowDialog((Window)mWindow);
        }
    }

    private void Add_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Add clicked.");
        ModifyParticipantWindow addParticipant = ModifyParticipantWindow.NewWindow(mWindow, database);
        if (addParticipant != null)
        {
            mWindow.AddWindow(addParticipant);
            addParticipant.ShowDialog((Window)mWindow);
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!loaded) return;
        List<Participant> newList = [.. allParticipants];
        switch (((ComboBoxItem)SortBox.SelectedItem!).Content)
        {
            case "Name":
                newList.Sort(Participant.CompareByName);
                break;
            case "Bib":
                newList.Sort(Participant.CompareByBib);
                break;
            default:
                newList.Sort();
                break;
        }
        string search = SearchBox != null ? SearchBox.Text!.Trim() : "";
        newList.RemoveAll(x => x.IsNotMatch(search));
        ParticipantsList.SelectedItems.Clear();
        ParticipantsList.ItemsSource = newList;
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.ParticipantsPage", "Remove clicked.");
        IList selected = ParticipantsList.SelectedItems;
        List<Participant> parts = [];
        foreach (Participant p in selected)
        {
            parts.Add(p);
        }
        database.RemoveParticipantEntries(parts);
        UpdateView();
    }

    private void ParticipantsList_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ParticipantsList.SelectedItem == null) return;
        ModifyParticipantWindow modifyParticipant = ModifyParticipantWindow.NewWindow(mWindow, database, (Participant)ParticipantsList.SelectedItem);
        if (modifyParticipant != null)
        {
            mWindow.AddWindow(modifyParticipant);
            modifyParticipant.ShowDialog((Window)mWindow);
        }
    }
}