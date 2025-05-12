using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.Participants;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for TimingResultsPage.xaml
    /// </summary>
    public partial class TimingResultsPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        List<TimeResult> results = [];

        public TimingResultsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
            {
                ChipTimeHeader.Text = "Lap Time";
            }
            if (database is SQLiteInterface)
            {
                Results.GetStaticVariables(database);
            }
            if (theEvent.DivisionsEnabled)
            {
                DivisionHeaderCol.Width = new System.Windows.GridLength(80);
                divisionText.Margin = new System.Windows.Thickness(4);
                DivisionPlaceHeaderCol.Width = new System.Windows.GridLength(40);
                divisionPlaceText.Margin = new System.Windows.Thickness(4);
            }
            else
            {
                DivisionHeaderCol.Width = new System.Windows.GridLength(0);
                divisionText.Margin = new System.Windows.Thickness(0);
                DivisionPlaceHeaderCol.Width = new System.Windows.GridLength(0);
                divisionPlaceText.Margin = new System.Windows.Thickness(0);
            }
        }

        public void Closing() { }

        public void EditSelected() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private void Customize(
            SortType sortType,
            PeopleType peopleType,
            List<TimeResult> newResults,
            string search,
            string location)
        {
            if (peopleType == PeopleType.DEFAULT)
            {
                newResults.RemoveAll(TimeResult.StartTimes);
            }
            else if (peopleType == PeopleType.KNOWN)
            {
                newResults.RemoveAll(TimeResult.IsNotKnown);
            }
            else if (peopleType == PeopleType.UNKNOWN)
            {
                newResults.RemoveAll(TimeResult.IsKnown);
            }
            else if (peopleType == PeopleType.UNKNOWN_FINISHES)
            {
                if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                {
                    Log.D("UI.Timing.TimingResultsPage", "Time based event.");
                    Dictionary<int, TimeResult> validResults = new Dictionary<int, TimeResult>();
                    foreach (TimeResult result in newResults)
                    {
                        if (Constants.Timing.TIMERESULT_DUMMYPERSON != result.EventSpecificId)
                        {
                            validResults[result.EventSpecificId] = result;
                        }
                    }
                    newResults.RemoveAll(x => !validResults.ContainsValue(x) && TimeResult.IsKnown(x));
                }
                else
                {
                    newResults.RemoveAll(TimeResult.IsNotFinishOrKnown);
                }
            }
            else if (peopleType == PeopleType.UNKNOWN_STARTS)
            {
                newResults.RemoveAll(TimeResult.IsNotStartOrKnown);
            }
            else if (peopleType == PeopleType.FINISHES)
            {
                if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                {
                    Log.D("UI.Timing.TimingResultsPage", "Time based event.");
                    Dictionary<int, TimeResult> validResults = new Dictionary<int, TimeResult>();
                    foreach (TimeResult result in newResults)
                    {
                        if (Constants.Timing.TIMERESULT_DUMMYPERSON != result.EventSpecificId)
                        {
                            validResults[result.EventSpecificId] = result;
                        }
                    }
                    newResults.RemoveAll(x => !validResults.ContainsValue(x));
                }
                else
                {
                    newResults.RemoveAll(TimeResult.IsNotFinish);
                }
            }
            else if (peopleType == PeopleType.STARTS)
            {
                newResults.RemoveAll(TimeResult.IsNotStart);
            }
            newResults.RemoveAll(result => result.IsNotMatch(search));
            Log.D("UI.Timing.TimingResultsPage", "Removing all location based items. " + location);
            if (location != null && location.Length > 0 && !location.Equals("All Locations", StringComparison.OrdinalIgnoreCase))
            {
                newResults.RemoveAll(read => !read.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase));
            }
            if (sortType == SortType.BIB)
            {
                newResults.Sort(TimeResult.CompareByBib);
            }
            else if (sortType == SortType.GUNTIME)
            {
                newResults.Sort(TimeResult.CompareByGunTime);
            }
            else if (sortType == SortType.DISTANCE)
            {
                newResults.Sort(TimeResult.CompareByDistance);
            }
            else if (sortType == SortType.AGEGROUP)
            {
                newResults.Sort(TimeResult.CompareByAgeGroup);
            }
            else if (sortType == SortType.GENDER)
            {
                newResults.Sort(TimeResult.CompareByGender);
            }
            else if (sortType == SortType.PLACE)
            {
                newResults.Sort(TimeResult.CompareByDistancePlace);
            }
            else
            {
                newResults.Sort(TimeResult.CompareBySystemTime);
            }
        }

        public async void SortBy(SortType sortType)
        {
            List<TimeResult> newResults = [.. results];
            PeopleType peopleType = parent.GetPeopleType();
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults, search, location);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        public async void Location(string location)
        {
            List<TimeResult> newResults = [.. results];
            PeopleType peopleType = parent.GetPeopleType();
            SortType sortType = parent.GetSortType();
            string search = parent.GetSearchValue();
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults, search, location);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        public void UpdateDatabase() { }

        public async void UpdateView()
        {
            List<TimeResult> newResults = null;
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            await Task.Run(() =>
            {
                newResults = database.GetTimingResults(theEvent.Identifier);
            });
            results.Clear();
            results.AddRange(newResults);
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults, search, location);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
            updateListView.SelectedItem = null;
            if (theEvent.DisplayPlacements)
            {
                DisplayPlacements();
            }
            else
            {
                HidePlacements();
            }
        }

        public void DisplayPlacements()
        {
            placeText.Visibility = System.Windows.Visibility.Visible;
            genderPlaceText.Visibility = System.Windows.Visibility.Visible;
            agePlaceText.Visibility = System.Windows.Visibility.Visible;
            divisionPlaceText.Visibility = System.Windows.Visibility.Visible;
        }

        public void HidePlacements()
        {
            placeText.Visibility = System.Windows.Visibility.Hidden;
            genderPlaceText.Visibility = System.Windows.Visibility.Hidden;
            agePlaceText.Visibility = System.Windows.Visibility.Hidden;
            divisionPlaceText.Visibility = System.Windows.Visibility.Hidden;
        }

        public void CancelableUpdateView(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            UpdateView();
        }

        public async void Show(PeopleType peopleType)
        {
            List<TimeResult> newResults = [.. results];
            SortType sortType = parent.GetSortType();
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults, search, location);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        private void UpdateListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateView();
        }

        private void UpdateListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (updateListView.SelectedItem == null) return;
            TimeResult selected = (TimeResult)updateListView.SelectedItem;
            ModifyParticipantWindow modifyParticipant = new ModifyParticipantWindow(parent, database, selected.EventSpecificId, selected.Bib);
            modifyParticipant.ShowDialog();
        }

        private void updateListView_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            labelsViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }
    }
}
