using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.IO;
using Chronokeep.IO.HtmlTemplates.Printables;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for AwardPage.xaml
    /// </summary>
    public partial class AlarmsPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;

        public AlarmsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("UI.Timing.AwardPage", "Something went wrong and no proper event was returned.");
                return;
            }
            UpdateView();
        }
        
        public void CancelableUpdateView(CancellationToken token) { }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AwardPage", "Done clicked.");
            parent.LoadMainDisplay();
        }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() { }

        public void UpdateView()
        {
        }

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }
    }
}
 