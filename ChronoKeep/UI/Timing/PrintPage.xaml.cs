using ChronoKeep.Interfaces;
using ChronoKeep.IO;
using ChronoKeep.Objects;
using ChronoKeep.UI.MainPages;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigraDoc.Rendering.Printing;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChronoKeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for PrintPage.xaml
    /// </summary>
    public partial class PrintPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        public PrintPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();

            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }

            if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
            {
                ReportType.Items.Clear();
                ReportType.Items.Add(new ListBoxItem
                {
                    Content = "Total Time",
                    IsSelected = true
                });
                ReportType.Items.Add(new ListBoxItem
                {
                    Content = "Lap Times"
                });
            }

            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            divisions.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            foreach (Division d in divisions)
            {
                DivisionsBox.Items.Add(new ListBoxItem()
                {
                    Content = d.Name
                });
            }
        }

        public void UpdateView() { }

        private enum ValuesType { FINISHONLY, STARTFINISH, ALL, TIME_ALL, TIME_TOTAL }

        private Document GetOverallPrintableDocumentTime(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            // TODO - Make anonymous entries possible.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResults = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResults.ContainsKey(result.DivisionName))
                {
                    divisionResults[result.DivisionName] = new List<TimeResult>();
                }
                divisionResults[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            int maxLoops = 0;

            foreach (string divName in divisionResults.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                int LoopStart = 7;
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.TIME_ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Overall Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Create a tabel to display the results.
                Table table = new Table();
                table.Borders.Width = 0.0;
                table.Rows.Alignment = RowAlignment.Center;
                // Create the rows we're displaying
                table.AddColumn(Unit.FromCentimeter(1));   // place
                table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                table.AddColumn(Unit.FromCentimeter(5));   // name
                table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                table.AddColumn(Unit.FromCentimeter(0.6)); // age
                table.AddColumn(Unit.FromCentimeter(0.6)); // AG Place
                table.AddColumn(Unit.FromCentimeter(1.2)); // AG Place
                // Check if we're a time based event and they want all the lap times.
                int max = 0;
                if (type == ValuesType.TIME_ALL)
                {
                    foreach (TimeResult result in divisionResults[divName])
                    {
                        if (result.LocationId == Constants.Timing.LOCATION_FINISH && max < result.Occurrence)
                        {
                            max = result.Occurrence;
                            maxLoops = max > maxLoops ? max : maxLoops;
                        }
                    }
                    for (int i = 0; i < max; i++)
                    {
                        table.AddColumn(Unit.FromCentimeter(1.8));
                    }
                    if (max > 7)
                    {
                        document.DefaultPageSetup.PageHeight = Unit.FromCentimeter(19 + (maxLoops * 1.8));
                    }
                }
                table.AddColumn(Unit.FromCentimeter(1));   // Loops
                table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                           // add the header row
                Row row = table.AddRow();
                row.Style = "ResultsHeader";
                row.Cells[0].AddParagraph("Place");
                row.Cells[1].AddParagraph("Bib");
                row.Cells[2].AddParagraph("Name");
                row.Cells[2].Style = "ResultsHeaderName";
                row.Cells[3].AddParagraph("G");
                row.Cells[4].AddParagraph("Age");
                row.Cells[5].AddParagraph("AG Place");
                row.Cells[5].MergeRight = 1;
                if (type == ValuesType.TIME_ALL)
                {
                    for (int i = 0; i < max; i++)
                    {
                        row.Cells[LoopStart + i].AddParagraph(string.Format("Loop {0}", i + 1));
                    }
                }
                row.Cells[LoopStart + max].AddParagraph("Loops");
                row.Cells[LoopStart + max + 1].AddParagraph("Ellapsed (Gun)");
                row.Cells[LoopStart + max + 2].AddParagraph("Ellapsed (Chip)");

                // We need a dictionary of everyone's start times
                Dictionary<string, TimeResult> personStartTimeDictionary = new Dictionary<string, TimeResult>();
                // And a dictionary of a list of all their finish times
                Dictionary<(string, int), TimeResult> personFinishResultDictionary = new Dictionary<(string, int), TimeResult>();
                // and their final loop result;
                Dictionary<string, TimeResult> personFinalLoopDictionary = new Dictionary<string, TimeResult>();
                foreach (TimeResult result in divisionResults[divName])
                {
                    if (result.SegmentId == Constants.Timing.SEGMENT_START)
                    {
                        personStartTimeDictionary[result.Identifier] = result;
                    }
                    else if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                    {
                        personFinishResultDictionary[(result.Identifier, result.Occurrence - 1)] = result;
                        if (!personFinalLoopDictionary.ContainsKey(result.Identifier) || personFinalLoopDictionary[result.Identifier].Occurrence < result.Occurrence)
                        {
                            personFinalLoopDictionary[result.Identifier] = result;
                        }
                    }
                }
                List<TimeResult> finalResults = new List<TimeResult>(personFinalLoopDictionary.Values);
                finalResults.Sort(TimeResult.CompareByDivisionPlace);
                foreach (TimeResult result in finalResults)
                {
                    row = table.AddRow();
                    row.Style = "ResultsRow";
                    row.Cells[0].AddParagraph(result.Place.ToString());
                    row.Cells[1].AddParagraph(result.Bib.ToString());
                    row.Cells[2].AddParagraph(result.ParticipantName);
                    row.Cells[2].Style = "ResultsRowName";
                    row.Cells[3].AddParagraph(result.Gender);
                    row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                    row.Cells[5].AddParagraph(result.AgePlaceStr);
                    row.Cells[6].AddParagraph(result.AgeGroupName);
                    if (type == ValuesType.TIME_ALL)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                : "";
                            value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                            row.Cells[LoopStart + i].AddParagraph(value);
                        }
                    }
                    row.Cells[LoopStart + max].AddParagraph(result.Occurrence.ToString());
                    row.Cells[LoopStart + max + 1].AddParagraph(result.Time.Substring(0, result.Time.Length - 2));
                    row.Cells[LoopStart + max + 2].AddParagraph(result.ChipTime.Substring(0, result.ChipTime.Length - 2));
                }
                if (type == ValuesType.TIME_ALL)
                {
                    HashSet<string> DidNotFinish = new HashSet<string>(personStartTimeDictionary.Keys);
                    foreach ((string val, int x) in personFinishResultDictionary.Keys)
                    {
                        DidNotFinish.Add(val);
                    }
                    DidNotFinish.RemoveWhere(x => personFinalLoopDictionary.ContainsKey(x));
                    foreach (string identifier in DidNotFinish)
                    {
                        TimeResult result = personStartTimeDictionary.ContainsKey(identifier) ? personStartTimeDictionary[identifier] : null;
                        if (result == null)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                result = personFinishResultDictionary.ContainsKey((identifier, i)) ? personFinishResultDictionary[(identifier, i)] : null;
                            }
                        }
                        row = table.AddRow();
                        row.Style = "ResultsRow";
                        row.Cells[0].AddParagraph("");
                        row.Cells[1].AddParagraph(result.Bib.ToString());
                        row.Cells[2].AddParagraph(result.ParticipantName);
                        row.Cells[2].Style = "ResultsRowName";
                        row.Cells[3].AddParagraph(result.Gender);
                        row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                        row.Cells[5].AddParagraph("");
                        row.Cells[6].AddParagraph("");
                        if (type == ValuesType.TIME_ALL)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                    ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[LoopStart + i].AddParagraph(value);
                            }
                        }
                        row.Cells[LoopStart + max].AddParagraph("");
                        row.Cells[LoopStart + max + 1].AddParagraph("");
                        row.Cells[LoopStart + max + 2].AddParagraph("");
                    }
                }
                section.Add(table);
            }
            return document;
        }

        private Document GetGenderPrintableDocumentTime(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResult = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResult.ContainsKey(result.DivisionName))
                {
                    divisionResult[result.DivisionName] = new List<TimeResult>();
                }
                divisionResult[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            int maxLoops = 0;

            foreach (string divName in divisionResult.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.TIME_ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Gender Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Separate each gender into their own little world.
                Dictionary<string, List<TimeResult>> genderResultDictionary = new Dictionary<string, List<TimeResult>>();
                foreach (TimeResult result in divisionResult[divName])
                {
                    if (!genderResultDictionary.ContainsKey(result.Gender))
                    {
                        genderResultDictionary[result.Gender] = new List<TimeResult>();
                    }
                    genderResultDictionary[result.Gender].Add(result);
                }
                foreach (string gender in genderResultDictionary.Keys.OrderBy(i => i))
                {
                    int LoopStart = 6;
                    section.AddParagraph(gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female", "SubHeading");
                    // Create a tabel to display the results.
                    Table table = new Table();
                    table.Borders.Width = 0.0;
                    table.Rows.Alignment = RowAlignment.Center;
                    // Create the rows we're displaying
                    table.AddColumn(Unit.FromCentimeter(1));   // place
                    table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                    table.AddColumn(Unit.FromCentimeter(5));   // name
                    table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                    table.AddColumn(Unit.FromCentimeter(0.6)); // age
                    table.AddColumn(Unit.FromCentimeter(1.3)); // Overall
                    int max = 0;
                    if (type == ValuesType.TIME_ALL)
                    {
                        foreach (TimeResult result in genderResultDictionary[gender])
                        {
                            if (result.LocationId == Constants.Timing.LOCATION_FINISH && max < result.Occurrence)
                            {
                                max = result.Occurrence;
                                maxLoops = max > maxLoops ? max : maxLoops;
                            }
                        }
                        for (int i = 0; i < max; i++)
                        {
                            table.AddColumn(Unit.FromCentimeter(1.8));
                        }
                        if (max > 7)
                        {
                            document.DefaultPageSetup.PageHeight = Unit.FromCentimeter(19 + (maxLoops * 1.8));
                        }
                    }
                    table.AddColumn(Unit.FromCentimeter(1));   // Loops
                    table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                    table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                               // add the header row

                    Row row = table.AddRow();
                    row.Style = "ResultsHeader";
                    row.Cells[0].AddParagraph("Place");
                    row.Cells[1].AddParagraph("Bib");
                    row.Cells[2].AddParagraph("Name");
                    row.Cells[2].Style = "ResultsHeaderName";
                    row.Cells[3].AddParagraph("G");
                    row.Cells[4].AddParagraph("Age");
                    row.Cells[5].AddParagraph("Overall");
                    if (type == ValuesType.TIME_ALL)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            row.Cells[LoopStart + i].AddParagraph(string.Format("Loop {0}", i + 1));
                        }
                    }
                    row.Cells[LoopStart + max].AddParagraph("Loops");
                    row.Cells[LoopStart + max + 1].AddParagraph("Ellapsed (Gun)");
                    row.Cells[LoopStart + max + 2].AddParagraph("Ellapsed (Chip)");

                    // We need a dictionary of everyone's start times
                    Dictionary<string, TimeResult> personStartTimeDictionary = new Dictionary<string, TimeResult>();
                    // And a dictionary of a list of all their finish times
                    Dictionary<(string, int), TimeResult> personFinishResultDictionary = new Dictionary<(string, int), TimeResult>();
                    // and their final loop result;
                    Dictionary<string, TimeResult> personFinalLoopDictionary = new Dictionary<string, TimeResult>();
                    foreach (TimeResult result in genderResultDictionary[gender])
                    {
                        if (result.SegmentId == Constants.Timing.SEGMENT_START)
                        {
                            personStartTimeDictionary[result.Identifier] = result;
                        }
                        else if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                        {
                            personFinishResultDictionary[(result.Identifier, result.Occurrence - 1)] = result;
                            if (!personFinalLoopDictionary.ContainsKey(result.Identifier) || personFinalLoopDictionary[result.Identifier].Occurrence < result.Occurrence)
                            {
                                personFinalLoopDictionary[result.Identifier] = result;
                            }
                        }
                    }
                    List<TimeResult> finalResults = new List<TimeResult>(personFinalLoopDictionary.Values);
                    finalResults.Sort(TimeResult.CompareByDivisionPlace);
                    foreach (TimeResult result in finalResults)
                    {
                        row = table.AddRow();
                        row.Style = "ResultsRow";
                        row.Cells[0].AddParagraph(result.GenderPlace.ToString());
                        row.Cells[1].AddParagraph(result.Bib.ToString());
                        row.Cells[2].AddParagraph(result.ParticipantName);
                        row.Cells[2].Style = "ResultsRowName";
                        row.Cells[3].AddParagraph(result.Gender);
                        row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                        row.Cells[5].AddParagraph(result.Place.ToString());
                        if (type == ValuesType.TIME_ALL)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                    ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[LoopStart + i].AddParagraph(value);
                            }
                        }
                        row.Cells[LoopStart + max].AddParagraph(result.Occurrence.ToString());
                        row.Cells[LoopStart + max + 1].AddParagraph(result.Time.Substring(0, result.Time.Length - 2));
                        row.Cells[LoopStart + max + 2].AddParagraph(result.ChipTime.Substring(0, result.ChipTime.Length - 2));
                    }
                    if (type == ValuesType.TIME_ALL)
                    {
                        HashSet<string> DidNotFinish = new HashSet<string>(personStartTimeDictionary.Keys);
                        foreach ((string val, int x) in personFinishResultDictionary.Keys)
                        {
                            DidNotFinish.Add(val);
                        }
                        DidNotFinish.RemoveWhere(x => personFinalLoopDictionary.ContainsKey(x));
                        foreach (string identifier in DidNotFinish)
                        {
                            TimeResult result = personStartTimeDictionary.ContainsKey(identifier) ? personStartTimeDictionary[identifier] : null;
                            if (result == null)
                            {
                                for (int i = 0; i < max; i++)
                                {
                                    result = personFinishResultDictionary.ContainsKey((identifier, i)) ? personFinishResultDictionary[(identifier, i)] : null;
                                }
                            }
                            row = table.AddRow();
                            row.Style = "ResultsRow";
                            row.Cells[0].AddParagraph("");
                            row.Cells[1].AddParagraph(result.Bib.ToString());
                            row.Cells[2].AddParagraph(result.ParticipantName);
                            row.Cells[2].Style = "ResultsRowName";
                            row.Cells[3].AddParagraph(result.Gender);
                            row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                            row.Cells[5].AddParagraph("");
                            row.Cells[6].AddParagraph("");
                            if (type == ValuesType.TIME_ALL)
                            {
                                for (int i = 0; i < max; i++)
                                {
                                    string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                        ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                        : "";
                                    value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                    row.Cells[LoopStart + i].AddParagraph(value);
                                }
                            }
                            row.Cells[LoopStart + max].AddParagraph("");
                            row.Cells[LoopStart + max + 1].AddParagraph("");
                            row.Cells[LoopStart + max + 2].AddParagraph("");
                        }
                    }
                    section.Add(table);
                }
            }
            return document;
        }

        private Document GetAgeGroupPrintableDocumentTime(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all of the age groups for the race
            Dictionary<int, AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier).ToDictionary(x => x.GroupId, x=> x);
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResult = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResult.ContainsKey(result.DivisionName))
                {
                    divisionResult[result.DivisionName] = new List<TimeResult>();
                }
                divisionResult[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            int maxLoops = 0;

            foreach (string divName in divisionResult.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.TIME_ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Age Group Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Separate each age group into their own little world
                Dictionary<(int, string), List<TimeResult>> ageGroupResultsDictionary = new Dictionary<(int, string), List<TimeResult>>();
                foreach (TimeResult result in divisionResult[divName])
                {
                    if (!ageGroupResultsDictionary.ContainsKey((result.AgeGroupId, result.Gender)))
                    {
                        ageGroupResultsDictionary[(result.AgeGroupId, result.Gender)] = new List<TimeResult>();
                    }
                    ageGroupResultsDictionary[(result.AgeGroupId, result.Gender)].Add(result);
                }
                foreach ((int AgeGroupID, string gender) in ageGroupResultsDictionary.Keys
                    .OrderBy(c => c.Item2).ThenBy(i => ageGroups[i.Item1].StartAge))
                {
                    int LoopStart = 6;
                    section.AddParagraph(string.Format("{0} {1} - {2}",
                        gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                        ageGroups[AgeGroupID].StartAge,
                        ageGroups[AgeGroupID].EndAge), "SubHeading");
                    // Create a tabel to display the results.
                    Table table = new Table();
                    table.Borders.Width = 0.0;
                    table.Rows.Alignment = RowAlignment.Center;
                    // Create the rows we're displaying
                    table.AddColumn(Unit.FromCentimeter(1));   // place
                    table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                    table.AddColumn(Unit.FromCentimeter(5));   // name
                    table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                    table.AddColumn(Unit.FromCentimeter(0.6)); // age
                    table.AddColumn(Unit.FromCentimeter(1.3)); // Overall
                    int max = 0;
                    if (type == ValuesType.TIME_ALL)
                    {
                        foreach (TimeResult result in ageGroupResultsDictionary[(AgeGroupID, gender)])
                        {
                            if (result.LocationId == Constants.Timing.LOCATION_FINISH && max < result.Occurrence)
                            {
                                max = result.Occurrence;
                                maxLoops = max > maxLoops ? max : maxLoops;
                            }
                        }
                        for (int i = 0; i < max; i++)
                        {
                            table.AddColumn(Unit.FromCentimeter(1.8));
                        }
                        if (max > 7)
                        {
                            document.DefaultPageSetup.PageHeight = Unit.FromCentimeter(19 + (maxLoops * 1.8));
                        }
                    }
                    table.AddColumn(Unit.FromCentimeter(1));   // Loops
                    table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                    table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                               // add the header row
                    Row row = table.AddRow();
                    row.Style = "ResultsHeader";
                    row.Cells[0].AddParagraph("Place");
                    row.Cells[1].AddParagraph("Bib");
                    row.Cells[2].AddParagraph("Name");
                    row.Cells[2].Style = "ResultsHeaderName";
                    row.Cells[3].AddParagraph("G");
                    row.Cells[4].AddParagraph("Age");
                    row.Cells[5].AddParagraph("Overall");
                    if (type == ValuesType.TIME_ALL)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            row.Cells[LoopStart + i].AddParagraph(string.Format("Loop {0}", i + 1));
                        }
                    }
                    row.Cells[LoopStart + max].AddParagraph("Loops");
                    row.Cells[LoopStart + max + 1].AddParagraph("Ellapsed (Gun)");
                    row.Cells[LoopStart + max + 2].AddParagraph("Ellapsed (Chip)");

                    // We need a dictionary of everyone's start times
                    Dictionary<string, TimeResult> personStartTimeDictionary = new Dictionary<string, TimeResult>();
                    // And a dictionary of a list of all their finish times
                    Dictionary<(string, int), TimeResult> personFinishResultDictionary = new Dictionary<(string, int), TimeResult>();
                    // and their final loop result;
                    Dictionary<string, TimeResult> personFinalLoopDictionary = new Dictionary<string, TimeResult>();
                    foreach (TimeResult result in ageGroupResultsDictionary[(AgeGroupID, gender)])
                    {
                        if (result.SegmentId == Constants.Timing.SEGMENT_START)
                        {
                            personStartTimeDictionary[result.Identifier] = result;
                        }
                        else if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                        {
                            personFinishResultDictionary[(result.Identifier, result.Occurrence - 1)] = result;
                            if (!personFinalLoopDictionary.ContainsKey(result.Identifier) || personFinalLoopDictionary[result.Identifier].Occurrence < result.Occurrence)
                            {
                                personFinalLoopDictionary[result.Identifier] = result;
                            }
                        }
                    }
                    List<TimeResult> finalResults = new List<TimeResult>(personFinalLoopDictionary.Values);
                    finalResults.Sort(TimeResult.CompareByDivisionPlace);
                    foreach (TimeResult result in finalResults)
                    {
                        row = table.AddRow();
                        row.Style = "ResultsRow";
                        row.Cells[0].AddParagraph(result.AgePlace.ToString());
                        row.Cells[1].AddParagraph(result.Bib.ToString());
                        row.Cells[2].AddParagraph(result.ParticipantName);
                        row.Cells[2].Style = "ResultsRowName";
                        row.Cells[3].AddParagraph(result.Gender);
                        row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                        row.Cells[5].AddParagraph(result.Place.ToString());
                        if (type == ValuesType.TIME_ALL)
                        {
                            for (int i = 0; i < max; i++)
                            {
                                string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                    ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[LoopStart + i].AddParagraph(value);
                            }
                        }
                        row.Cells[LoopStart + max].AddParagraph(result.Occurrence.ToString());
                        row.Cells[LoopStart + max + 1].AddParagraph(result.Time.Substring(0, result.Time.Length - 2));
                        row.Cells[LoopStart + max + 2].AddParagraph(result.ChipTime.Substring(0, result.ChipTime.Length - 2));
                    }
                    if (type == ValuesType.TIME_ALL)
                    {
                        HashSet<string> DidNotFinish = new HashSet<string>(personStartTimeDictionary.Keys);
                        foreach ((string val, int x) in personFinishResultDictionary.Keys)
                        {
                            DidNotFinish.Add(val);
                        }
                        DidNotFinish.RemoveWhere(x => personFinalLoopDictionary.ContainsKey(x));
                        foreach (string identifier in DidNotFinish)
                        {
                            TimeResult result = personStartTimeDictionary.ContainsKey(identifier) ? personStartTimeDictionary[identifier] : null;
                            if (result == null)
                            {
                                for (int i = 0; i < max; i++)
                                {
                                    result = personFinishResultDictionary.ContainsKey((identifier, i)) ? personFinishResultDictionary[(identifier, i)] : null;
                                }
                            }
                            row = table.AddRow();
                            row.Style = "ResultsRow";
                            row.Cells[0].AddParagraph("");
                            row.Cells[1].AddParagraph(result.Bib.ToString());
                            row.Cells[2].AddParagraph(result.ParticipantName);
                            row.Cells[2].Style = "ResultsRowName";
                            row.Cells[3].AddParagraph(result.Gender);
                            row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                            row.Cells[5].AddParagraph("");
                            row.Cells[6].AddParagraph("");
                            if (type == ValuesType.TIME_ALL)
                            {
                                for (int i = 0; i < max; i++)
                                {
                                    string value = personFinishResultDictionary.ContainsKey((result.Identifier, i))
                                        ? personFinishResultDictionary[(result.Identifier, i)].LapTime
                                        : "";
                                    value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                    row.Cells[LoopStart + i].AddParagraph(value);
                                }
                            }
                            row.Cells[LoopStart + max].AddParagraph("");
                            row.Cells[LoopStart + max + 1].AddParagraph("");
                            row.Cells[LoopStart + max + 2].AddParagraph("");
                        }
                    }
                    row = table.AddRow();

                    if (type == ValuesType.TIME_ALL || personFinalLoopDictionary.Keys.Count > 0)
                    {
                        section.Add(table);
                    }
                }
            }
            return document;
        }

        private Document GetOverallPrintableDocument(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            // TODO - Make anonymous entries possible.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResults = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResults.ContainsKey(result.DivisionName))
                {
                    divisionResults[result.DivisionName] = new List<TimeResult>();
                }
                divisionResults[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            foreach (string divName in divisionResults.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                int FinishIndex = 7;
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Overall Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Create a tabel to display the results.
                Table table = new Table();
                table.Borders.Width = 0.0;
                table.Rows.Alignment = RowAlignment.Center;
                // Create the rows we're displaying
                table.AddColumn(Unit.FromCentimeter(1));   // place
                table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                table.AddColumn(Unit.FromCentimeter(5));   // name
                table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                table.AddColumn(Unit.FromCentimeter(0.6)); // age
                table.AddColumn(Unit.FromCentimeter(0.6)); // AG Place
                table.AddColumn(Unit.FromCentimeter(1.2)); // AG Place
                if (type != ValuesType.FINISHONLY)
                {
                    table.AddColumn(Unit.FromCentimeter(2.3)); // start
                    FinishIndex++;
                }
                // Check if we're doing all of the segments.
                bool distanceSegments = type == ValuesType.ALL && segmentsDictionary.ContainsKey(divisionDictionary[divName]);
                if (distanceSegments)
                {
                    foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                    {
                        segmentIndexDictionary[s.Identifier] = FinishIndex++;
                        table.AddColumn(Unit.FromCentimeter(2.3));
                    }
                }
                table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                           // add the header row
                Row row = table.AddRow();
                row.Style = "ResultsHeader";
                row.Cells[0].AddParagraph("Place");
                row.Cells[1].AddParagraph("Bib");
                row.Cells[2].AddParagraph("Name");
                row.Cells[2].Style = "ResultsHeaderName";
                row.Cells[3].AddParagraph("G");
                row.Cells[4].AddParagraph("Age");
                row.Cells[5].AddParagraph("AG Place");
                row.Cells[5].MergeRight = 1;
                if (type != ValuesType.FINISHONLY)
                {
                    row.Cells[7].AddParagraph("Start");
                }
                if (distanceSegments)
                {
                    foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                    {
                        row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(s.Name);
                    }
                }
                row.Cells[FinishIndex].AddParagraph("Finish Gun");
                row.Cells[FinishIndex + 1].AddParagraph("Finish Chip");
                List<TimeResult> finishTimes = new List<TimeResult>();
                finishTimes.AddRange(divisionResults[divName]);
                finishTimes.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                finishTimes.Sort(TimeResult.CompareByDivisionPlace);
                // The key is (EVENTSPECIFICID, SEGMENTID) and is used to identify segment results
                // such as the start chip read and any other reads there may be other than finish.
                Dictionary<(int, int), TimeResult> personSegmentResultDictionary = new Dictionary<(int, int), TimeResult>();
                foreach (TimeResult result in divisionResults[divName])
                {
                    personSegmentResultDictionary[(result.EventSpecificId, result.SegmentId)] = result;
                }
                foreach (TimeResult result in finishTimes)
                {
                    row = table.AddRow();
                    row.Style = "ResultsRow";
                    row.Cells[0].AddParagraph(result.Place.ToString());
                    row.Cells[1].AddParagraph(result.Bib.ToString());
                    row.Cells[2].AddParagraph(result.ParticipantName);
                    row.Cells[2].Style = "ResultsRowName";
                    row.Cells[3].AddParagraph(result.Gender);
                    row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                    row.Cells[5].AddParagraph(result.AgePlaceStr);
                    row.Cells[6].AddParagraph(result.AgeGroupName);
                    if (type != ValuesType.FINISHONLY)
                    {
                        string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                            ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                            : "";
                        value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                        row.Cells[7].AddParagraph(value);
                    }
                    if (distanceSegments)
                    {
                        foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                        {
                            string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                : "";
                            value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                            row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                        }
                    }
                    row.Cells[FinishIndex].AddParagraph(result.Time.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                    row.Cells[FinishIndex+1].AddParagraph(result.ChipTime.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                }
                if (type != ValuesType.FINISHONLY)
                {
                    HashSet<int> DidNotFinish = new HashSet<int>(); // Event Specific ID
                    foreach ((int val, int x) in personSegmentResultDictionary.Keys)
                    {
                        DidNotFinish.Add(val);
                    }
                    foreach (TimeResult result in finishTimes)
                    {
                        DidNotFinish.Remove(result.EventSpecificId);
                    }
                    foreach (int identifier in DidNotFinish)
                    {
                        TimeResult result = personSegmentResultDictionary.ContainsKey((identifier, Constants.Timing.SEGMENT_START))
                            ? personSegmentResultDictionary[(identifier, Constants.Timing.SEGMENT_START)] : null;
                        if (result == null)
                        {
                            foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                            {
                                result = personSegmentResultDictionary.ContainsKey((identifier, s.Identifier))
                              ? personSegmentResultDictionary[(identifier, s.Identifier)] : null;
                            }
                        }
                        row = table.AddRow();
                        row.Style = "ResultsRow";
                        row.Cells[0].AddParagraph("");
                        row.Cells[1].AddParagraph(result.Bib.ToString());
                        row.Cells[2].AddParagraph(result.ParticipantName);
                        row.Cells[2].Style = "ResultsRowName";
                        row.Cells[3].AddParagraph(result.Gender);
                        row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                        row.Cells[5].AddParagraph("");
                        row.Cells[6].AddParagraph("");
                        if (type != ValuesType.FINISHONLY)
                        {
                            string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                                ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                                : "";
                            value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                            row.Cells[7].AddParagraph(value);
                        }
                        if (distanceSegments)
                        {
                            foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                            {
                                string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                    ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                            }
                        }
                        row.Cells[FinishIndex].AddParagraph("");
                        row.Cells[FinishIndex + 1].AddParagraph("");
                    }
                }
                section.Add(table);
            }
            return document;
        }

        private Document GetGenderPrintableDocument(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResult = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResult.ContainsKey(result.DivisionName))
                {
                    divisionResult[result.DivisionName] = new List<TimeResult>();
                }
                divisionResult[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            foreach (string divName in divisionResult.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Gender Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Separate each gender into their own little world.
                Dictionary<string, List<TimeResult>> genderResultDictionary = new Dictionary<string, List<TimeResult>>();
                foreach (TimeResult result in divisionResult[divName])
                {
                    if (!genderResultDictionary.ContainsKey(result.Gender))
                    {
                        genderResultDictionary[result.Gender] = new List<TimeResult>();
                    }
                    genderResultDictionary[result.Gender].Add(result);
                }
                foreach (string gender in genderResultDictionary.Keys.OrderBy(i=>i))
                {
                    int FinishIndex = 6;
                    section.AddParagraph(gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female", "SubHeading");
                    // Create a tabel to display the results.
                    Table table = new Table();
                    table.Borders.Width = 0.0;
                    table.Rows.Alignment = RowAlignment.Center;
                    // Create the rows we're displaying
                    table.AddColumn(Unit.FromCentimeter(1));   // place
                    table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                    table.AddColumn(Unit.FromCentimeter(5));   // name
                    table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                    table.AddColumn(Unit.FromCentimeter(0.6)); // age
                    table.AddColumn(Unit.FromCentimeter(1.3)); // Overall
                    if (type != ValuesType.FINISHONLY)
                    {
                        table.AddColumn(Unit.FromCentimeter(2.3)); // start
                        FinishIndex++;
                    }
                    bool distanceSegments = type == ValuesType.ALL && segmentsDictionary.ContainsKey(divisionDictionary[divName]);
                    if (distanceSegments)
                    {
                        foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                        {
                            segmentIndexDictionary[s.Identifier] = FinishIndex++;
                            table.AddColumn(Unit.FromCentimeter(2.3));
                        }
                    }
                    table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                    table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                               // add the header row
                    Row row = table.AddRow();
                    row.Style = "ResultsHeader";
                    row.Cells[0].AddParagraph("Place");
                    row.Cells[1].AddParagraph("Bib");
                    row.Cells[2].AddParagraph("Name");
                    row.Cells[2].Style = "ResultsHeaderName";
                    row.Cells[3].AddParagraph("G");
                    row.Cells[4].AddParagraph("Age");
                    row.Cells[5].AddParagraph("Overall");
                    if (type != ValuesType.FINISHONLY)
                    {
                        row.Cells[6].AddParagraph("Start");
                    }
                    if (distanceSegments)
                    {
                        foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                        {
                            row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(s.Name);
                        }
                    }
                    row.Cells[FinishIndex].AddParagraph("Finish Gun");
                    int numColumns = table.Columns.Count;
                    row.Cells[FinishIndex + 1].AddParagraph("Finish Chip");
                    List<TimeResult> finishTimes = new List<TimeResult>();
                    finishTimes.AddRange(genderResultDictionary[gender]);
                    finishTimes.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                    finishTimes.Sort(TimeResult.CompareByDivisionGenderPlace);
                    // The key is (EVENTSPECIFICID, SEGMENTID) and is used to identify segment results
                    // such as the start chip read and any other reads there may be other than finish.
                    Dictionary<(int, int), TimeResult> personSegmentResultDictionary = new Dictionary<(int, int), TimeResult>();
                    foreach (TimeResult result in genderResultDictionary[gender])
                    {
                        personSegmentResultDictionary[(result.EventSpecificId, result.SegmentId)] = result;
                    }
                    foreach (TimeResult result in finishTimes)
                    {
                        row = table.AddRow();
                        row.Style = "ResultsRow";
                        row.Cells[0].AddParagraph(result.GenderPlace.ToString());
                        row.Cells[1].AddParagraph(result.Bib.ToString());
                        row.Cells[2].AddParagraph(result.ParticipantName);
                        row.Cells[2].Style = "ResultsRowName";
                        row.Cells[3].AddParagraph(result.Gender);
                        row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                        row.Cells[5].AddParagraph(result.Place.ToString());
                        if (type != ValuesType.FINISHONLY)
                        {
                            string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                                ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                                : "";
                            value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                            row.Cells[6].AddParagraph(value);
                        }
                        if (distanceSegments)
                        {
                            foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                            {
                                string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                    ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                            }
                        }
                        row.Cells[FinishIndex].AddParagraph(result.Time.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                        row.Cells[FinishIndex + 1].AddParagraph(result.ChipTime.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                    }
                    if (type != ValuesType.FINISHONLY)
                    {
                        HashSet<int> DidNotFinish = new HashSet<int>(); // Event Specific ID
                        foreach ((int val, int x) in personSegmentResultDictionary.Keys)
                        {
                            DidNotFinish.Add(val);
                        }
                        foreach (TimeResult result in finishTimes)
                        {
                            DidNotFinish.Remove(result.EventSpecificId);
                        }
                        foreach (int identifier in DidNotFinish)
                        {
                            TimeResult result = personSegmentResultDictionary.ContainsKey((identifier, Constants.Timing.SEGMENT_START))
                                ? personSegmentResultDictionary[(identifier, Constants.Timing.SEGMENT_START)] : null;
                            if (result == null)
                            {
                                foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                                {
                                    result = personSegmentResultDictionary.ContainsKey((identifier, s.Identifier))
                                  ? personSegmentResultDictionary[(identifier, s.Identifier)] : null;
                                }
                            }
                            row = table.AddRow();
                            row.Style = "ResultsRow";
                            row.Cells[0].AddParagraph("");
                            row.Cells[1].AddParagraph(result.Bib.ToString());
                            row.Cells[2].AddParagraph(result.ParticipantName);
                            row.Cells[2].Style = "ResultsRowName";
                            row.Cells[3].AddParagraph(result.Gender);
                            row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                            row.Cells[5].AddParagraph("");
                            row.Cells[6].AddParagraph("");
                            if (type != ValuesType.FINISHONLY)
                            {
                                string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                                    ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[7].AddParagraph(value);
                            }
                            if (distanceSegments)
                            {
                                foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                                {
                                    string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                        ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                        : "";
                                    value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                    row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                                }
                            }
                            row.Cells[FinishIndex].AddParagraph("");
                            row.Cells[FinishIndex + 1].AddParagraph("");
                        }
                    }
                    section.Add(table);
                }
            }
            return document;
        }

        private Document GetAgeGroupPrintableDocument(List<string> divisions, ValuesType type)
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all of the age groups for the race
            Dictionary<int, AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier).ToDictionary(x => x.GroupId, x => x);
            // Add an age group for our unknown age people/
            ageGroups[Constants.Timing.TIMERESULT_DUMMYAGEGROUP] = new AgeGroup(theEvent.Identifier, Constants.Timing.COMMON_AGEGROUPS_DIVISIONID, 0, 3000);
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId));
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            Dictionary<string, List<TimeResult>> divisionResult = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResult.ContainsKey(result.DivisionName))
                {
                    divisionResult[result.DivisionName] = new List<TimeResult>();
                }
                divisionResult[result.DivisionName].Add(result);
            }
            Dictionary<int, List<Segment>> segmentsDictionary = new Dictionary<int, List<Segment>>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (!segmentsDictionary.ContainsKey(seg.DivisionId))
                {
                    segmentsDictionary[seg.DivisionId] = new List<Segment>();
                }
                segmentsDictionary[seg.DivisionId].Add(seg);
            }
            Dictionary<string, int> divisionDictionary = database.GetDivisions(theEvent.Identifier).ToDictionary(x => x.Name, x => x.Identifier);

            // Create document to output;
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            foreach (string divName in divisionResult.Keys.OrderBy(i => i))
            {
                Dictionary<int, int> segmentIndexDictionary = new Dictionary<int, int>();
                // Set margins to really small
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                if (type == ValuesType.ALL)
                {
                    section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
                }
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Age Group Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Separate each age group into their own little world
                Dictionary<(int, string), List<TimeResult>> ageGroupResultsDictionary = new Dictionary<(int, string), List<TimeResult>>();
                foreach (TimeResult result in divisionResult[divName])
                {
                    if (!ageGroupResultsDictionary.ContainsKey((result.AgeGroupId, result.Gender)))
                    {
                        ageGroupResultsDictionary[(result.AgeGroupId, result.Gender)] = new List<TimeResult>();
                    }
                    ageGroupResultsDictionary[(result.AgeGroupId, result.Gender)].Add(result);
                }
                IOrderedEnumerable<(int, string)> ageGroupKeys;
                try
                {
                    ageGroupKeys = ageGroupResultsDictionary.Keys
                       .OrderBy(c => c.Item2).ThenBy(i => ageGroups[i.Item1].StartAge);
                }
                catch
                {
                    ageGroupKeys = ageGroupResultsDictionary.Keys.OrderBy(c => c.Item2);
                }
                foreach ((int AgeGroupID, string gender) in ageGroupKeys)
                {
                    if (AgeGroupID != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                    {
                        int FinishIndex = 6;
                        string subheading = string.Format("{0} {1} - {2}",
                            gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                            ageGroups[AgeGroupID].StartAge,
                            ageGroups[AgeGroupID].EndAge);
                        if (ageGroups[AgeGroupID].LastGroup)
                        {
                            subheading = string.Format("{0} {1}+",
                                    gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                                    ageGroups[AgeGroupID].StartAge);
                        }
                        section.AddParagraph(subheading, "SubHeading");
                        // Create a tabel to display the results.
                        Table table = new Table();
                        table.Borders.Width = 0.0;
                        table.Rows.Alignment = RowAlignment.Center;
                        // Create the rows we're displaying
                        table.AddColumn(Unit.FromCentimeter(1));   // place
                        table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                        table.AddColumn(Unit.FromCentimeter(5));   // name
                        table.AddColumn(Unit.FromCentimeter(0.6)); // gender
                        table.AddColumn(Unit.FromCentimeter(0.6)); // age
                        table.AddColumn(Unit.FromCentimeter(1.3)); // Overall
                        if (type != ValuesType.FINISHONLY)
                        {
                            table.AddColumn(Unit.FromCentimeter(2.3)); // start
                            FinishIndex++;
                        }
                        bool distanceSegments = type == ValuesType.ALL && segmentsDictionary.ContainsKey(divisionDictionary[divName]);
                        if (distanceSegments)
                        {
                            foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                            {
                                segmentIndexDictionary[s.Identifier] = FinishIndex++;
                                table.AddColumn(Unit.FromCentimeter(2.3));
                            }
                        }
                        table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
                        table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                                   // add the header row
                        Row row = table.AddRow();
                        row.Style = "ResultsHeader";
                        row.Cells[0].AddParagraph("Place");
                        row.Cells[1].AddParagraph("Bib");
                        row.Cells[2].AddParagraph("Name");
                        row.Cells[2].Style = "ResultsHeaderName";
                        row.Cells[3].AddParagraph("G");
                        row.Cells[4].AddParagraph("Age");
                        row.Cells[5].AddParagraph("Overall");
                        if (type != ValuesType.FINISHONLY)
                        {
                            row.Cells[6].AddParagraph("Start");
                        }
                        if (distanceSegments)
                        {
                            foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                            {
                                row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(s.Name);
                            }
                        }
                        row.Cells[FinishIndex].AddParagraph("Finish Gun");
                        int numColumns = table.Columns.Count;
                        row.Cells[FinishIndex + 1].AddParagraph("Finish Chip");
                        List<TimeResult> finishTimes = new List<TimeResult>();
                        finishTimes.AddRange(ageGroupResultsDictionary[(AgeGroupID, gender)]);
                        finishTimes.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                        finishTimes.Sort(TimeResult.CompareByDivisionAgeGroupPlace);
                        // The key is (EVENTSPECIFICID, SEGMENTID) and is used to identify segment results
                        // such as the start chip read and any other reads there may be other than finish.
                        Dictionary<(int, int), TimeResult> personSegmentResultDictionary = new Dictionary<(int, int), TimeResult>();
                        foreach (TimeResult result in ageGroupResultsDictionary[(AgeGroupID, gender)])
                        {
                            personSegmentResultDictionary[(result.EventSpecificId, result.SegmentId)] = result;
                        }
                        foreach (TimeResult result in finishTimes)
                        {
                            row = table.AddRow();
                            row.Style = "ResultsRow";
                            row.Cells[0].AddParagraph(result.AgePlace.ToString());
                            row.Cells[1].AddParagraph(result.Bib.ToString());
                            row.Cells[2].AddParagraph(result.ParticipantName);
                            row.Cells[2].Style = "ResultsRowName";
                            row.Cells[3].AddParagraph(result.Gender);
                            row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                            row.Cells[5].AddParagraph(result.Place.ToString());
                            if (type != ValuesType.FINISHONLY)
                            {
                                string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                                    ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                                    : "";
                                value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                row.Cells[6].AddParagraph(value);
                            }
                            if (distanceSegments)
                            {
                                foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                                {
                                    string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                        ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                        : "";
                                    value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                    row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                                }
                            }
                            row.Cells[FinishIndex].AddParagraph(result.Time.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                            row.Cells[FinishIndex + 1].AddParagraph(result.ChipTime.Substring(0, result.Time.Length > 3 ? result.Time.Length - 2 : result.Time.Length));
                        }
                        if (type != ValuesType.FINISHONLY)
                        {
                            HashSet<int> DidNotFinish = new HashSet<int>(); // Event Specific ID
                            foreach ((int val, int x) in personSegmentResultDictionary.Keys)
                            {
                                DidNotFinish.Add(val);
                            }
                            foreach (TimeResult result in finishTimes)
                            {
                                DidNotFinish.Remove(result.EventSpecificId);
                            }
                            foreach (int identifier in DidNotFinish)
                            {
                                TimeResult result = personSegmentResultDictionary.ContainsKey((identifier, Constants.Timing.SEGMENT_START))
                                    ? personSegmentResultDictionary[(identifier, Constants.Timing.SEGMENT_START)] : null;
                                if (result == null)
                                {
                                    foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                                    {
                                        result = personSegmentResultDictionary.ContainsKey((identifier, s.Identifier))
                                      ? personSegmentResultDictionary[(identifier, s.Identifier)] : null;
                                    }
                                }
                                row = table.AddRow();
                                row.Style = "ResultsRow";
                                row.Cells[0].AddParagraph("");
                                row.Cells[1].AddParagraph(result.Bib.ToString());
                                row.Cells[2].AddParagraph(result.ParticipantName);
                                row.Cells[2].Style = "ResultsRowName";
                                row.Cells[3].AddParagraph(result.Gender);
                                row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                                row.Cells[5].AddParagraph("");
                                row.Cells[6].AddParagraph("");
                                if (type != ValuesType.FINISHONLY)
                                {
                                    string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, Constants.Timing.SEGMENT_START))
                                        ? personSegmentResultDictionary[(result.EventSpecificId, Constants.Timing.SEGMENT_START)].Time
                                        : "";
                                    value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                    row.Cells[7].AddParagraph(value);
                                }
                                if (distanceSegments)
                                {
                                    foreach (Segment s in segmentsDictionary[divisionDictionary[divName]])
                                    {
                                        string value = personSegmentResultDictionary.ContainsKey((result.EventSpecificId, s.Identifier))
                                            ? personSegmentResultDictionary[(result.EventSpecificId, s.Identifier)].Time
                                            : "";
                                        value = value.Length > 0 ? value.Substring(0, value.Length - 2) : "";
                                        row.Cells[segmentIndexDictionary[s.Identifier]].AddParagraph(value);
                                    }
                                }
                                row.Cells[FinishIndex].AddParagraph("");
                                row.Cells[FinishIndex + 1].AddParagraph("");
                            }
                        }
                        row = table.AddRow();
                        if (type != ValuesType.FINISHONLY || distanceSegments || finishTimes.Count > 0)
                        {
                            section.Add(table);
                        }
                    }
                }
            }
            return document;
        }

        public void Search(string value) { }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() {}

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            parent.LoadMainDisplay();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.D("All times - print clicked.");
            System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog
            {
                AllowSomePages = true,
                UseEXDialog = true
            };
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DivisionsBox.SelectedItems)
            {
                if (divItem.Content.Equals("All"))
                {
                    divsToPrint = null;
                    break;
                }
                divsToPrint.Add(divItem.Content.ToString());
            }
            if (divsToPrint != null && divsToPrint.Count < 1)
            {
                divsToPrint = null;
            }
            ValuesType reportTypeValue = ValuesType.ALL;
            if (ReportType.SelectedIndex == 0)
            {
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    reportTypeValue = ValuesType.FINISHONLY;
                }
                else
                {
                    reportTypeValue = ValuesType.TIME_TOTAL;
                }
            }
            else if (ReportType.SelectedIndex == 1)
            {
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    reportTypeValue = ValuesType.STARTFINISH;
                }
                else
                {
                    reportTypeValue = ValuesType.TIME_ALL;
                }
            }
            if (printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                if (PlacementType.SelectedIndex == 0)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetOverallPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetOverallPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else if (PlacementType.SelectedIndex == 1)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetGenderPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetGenderPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else if (PlacementType.SelectedIndex == 2)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetAgeGroupPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetAgeGroupPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a type.");
                    return;
                }
                renderer.RenderDocument();
                MigraDocPrintDocument printDocument = new MigraDocPrintDocument
                {
                    Renderer = renderer.DocumentRenderer,
                    PrinterSettings = printDialog.PrinterSettings
                };
                try
                {
                    printDocument.Print();
                }
                catch
                {
                    MessageBox.Show("Something went wrong when attempting to print.");
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Log.D("All times - save clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PDF (*.pdf)|*.pdf",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DivisionsBox.SelectedItems)
            {
                if (divItem.Content.Equals("All"))
                {
                    divsToPrint = null;
                    break;
                }
                divsToPrint.Add(divItem.Content.ToString());
            }
            if (divsToPrint != null && divsToPrint.Count < 1)
            {
                divsToPrint = null;
            }
            ValuesType reportTypeValue = ValuesType.ALL;
            if (ReportType.SelectedIndex == 0)
            {
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    reportTypeValue = ValuesType.FINISHONLY;
                }
                else
                {
                    reportTypeValue = ValuesType.TIME_TOTAL;
                }
            }
            else if (ReportType.SelectedIndex == 1)
            {
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    reportTypeValue = ValuesType.STARTFINISH;
                }
                else
                {
                    reportTypeValue = ValuesType.TIME_ALL;
                }
            }
            if (saveFileDialog.ShowDialog() == true)
            {
                PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                if (PlacementType.SelectedIndex == 0)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetOverallPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetOverallPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else if (PlacementType.SelectedIndex == 1)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetGenderPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetGenderPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else if (PlacementType.SelectedIndex == 2)
                {
                    if (reportTypeValue == ValuesType.TIME_ALL || reportTypeValue == ValuesType.TIME_TOTAL)
                    {
                        renderer.Document = GetAgeGroupPrintableDocumentTime(divsToPrint, reportTypeValue);
                    }
                    else
                    {
                        renderer.Document = GetAgeGroupPrintableDocument(divsToPrint, reportTypeValue);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a type.");
                    return;
                }
                renderer.RenderDocument();
                try
                {
                    renderer.PdfDocument.Save(saveFileDialog.FileName);
                    MessageBox.Show("File saved.");
                }
                catch
                {
                    MessageBox.Show("Unable to save file.");
                }
            }
        }
    }
}
