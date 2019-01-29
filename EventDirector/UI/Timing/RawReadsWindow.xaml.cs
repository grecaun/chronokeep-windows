using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for RawReadsWindow.xaml
    /// </summary>
    public partial class RawReadsWindow : Window
    {
        IDBInterface database;
        IWindowCallback window;
        Event theEvent;

        private RawReadsWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            Update();
        }

        public static RawReadsWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            if (StaticEvent.rawReadsWindow != null)
            {
                return null;
            }
            RawReadsWindow output = new RawReadsWindow(window, database);
            StaticEvent.rawReadsWindow = output;
            return output;
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Ignore Button clicked.");
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done Button clicked.");
            this.Close();
        }

        public void Update()
        {
            List<ChipRead> reads = database.GetChipReads(theEvent.Identifier);
            updateListView.ItemsSource = reads;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
            StaticEvent.rawReadsWindow = null;
        }
    }
}
