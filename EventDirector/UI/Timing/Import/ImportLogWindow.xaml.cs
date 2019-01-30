using EventDirector.Interfaces;
using EventDirector.IO;
using EventDirector.UI.EventWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector.UI.Timing.Import
{
    /// <summary>
    /// Interaction logic for ImportLogWindow.xaml
    /// </summary>
    public partial class ImportLogWindow : Window
    {
        INewMainWindow window;
        IDBInterface database;
        LogImporter importer;

        Event theEvent;
        int locationId = Constants.Timing.LOCATION_DUMMY;

        Regex DateRegex = new Regex("\\d{4}-\\d{2}-\\d{2}");

        private ImportLogWindow(INewMainWindow window, LogImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.importer = importer;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
            if (theEvent.CommonStartFinish != 1)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }
            Frame.Content = new ImportLogPage1(this, importer, locations);
        }

        public void Update()
        {
            if (Frame.Content.GetType() == typeof(ImportLogPage1))
            {
                Log.D("Updating locations on page.");
                List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
                if (theEvent.CommonStartFinish != 1)
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                }
                ((ImportLogPage1)Frame.Content).UpdateLocations(locations);
            }
        }

        public static ImportLogWindow NewWindow(INewMainWindow window, LogImporter importer, IDBInterface database)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.importLogWindow != null)
            {
                return null;
            }
            ImportLogWindow output = new ImportLogWindow(window, importer, database);
            StaticEvent.importLogWindow = output;
            return output;
        }

        public void Cancel()
        {
            this.Close();
        }

        public void Next(int iLocationId)
        {
            locationId = iLocationId;
            importer.type = LogImporter.Type.CUSTOM;
            Frame.Content = new ImportLogPage2(this, importer);
        }

        public async void Import(LogImporter.Type type, int iLocationId, int ChipColumn, int TimeColumn)
        {
            Log.D("Type is " + type.ToString() + " ChipIx " + ChipColumn + " TimeIx " + TimeColumn);
            await Task.Run(() =>
            {
                importer.FetchData();
                ImportData data = importer.Data;
                int chip = ChipColumn, time = TimeColumn;
                locationId = iLocationId != Constants.Timing.LOCATION_DUMMY ? iLocationId : locationId;
                if (type == LogImporter.Type.RFID)
                {
                    if (importer.Data.Headers.Length < 4)
                    {
                        chip = 1;
                        time = 2;
                    }
                    else
                    {
                        chip = 2;
                        time = 4;
                    }
                }
                List<ChipRead> chipreads = new List<ChipRead>();
                bool dateIncluded = DateRegex.IsMatch(data.Headers[time]);
                DateTime date;
                if (!dateIncluded)
                {
                    date = DateTime.Parse(String.Format("{0} {1}", theEvent.Date, data.Headers[time]));
                }
                else
                {
                    date = DateTime.Parse(data.Headers[time]);
                }
                chipreads.Add(new ChipRead(theEvent.Identifier, locationId, data.Headers[chip], date));
                int numEntries = data.Data.Count;
                for (int counter = 0; counter < numEntries; counter++)
                {
                    if (!dateIncluded)
                    {
                        date = DateTime.Parse(String.Format("{0} {1}", theEvent.Date, data.Data[counter][time]));
                    }
                    else
                    {
                        date = DateTime.Parse(data.Data[counter][time]);
                    }
                    chipreads.Add(new ChipRead(theEvent.Identifier, locationId, data.Data[counter][chip], date));
                }
                database.AddChipReads(chipreads);
            });
            window.UpdateTimingWindow();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
            StaticEvent.importLogWindow = null;
        }
    }
}
