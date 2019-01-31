using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
using System;

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for TimingResultsPage.xaml
    /// </summary>
    public partial class TimingResultsPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        public TimingResultsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
        }

        public void Closing() { }

        public void EditSelected() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Search(string value) { }

        public void UpdateDatabase() { }

        public void UpdateView() { }
    }
}
