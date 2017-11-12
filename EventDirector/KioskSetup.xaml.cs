using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for KioskSettup.xaml
    /// </summary>
    public partial class KioskSetup : Window
    {
        MainWindow mainWindow;
        ExampleLiabilityWaiver exampleLiabilityWaiver = null;
        IDBInterface database;
        String liabilityWaiver;
        int eventId;
        int print = 0;

        public KioskSetup(MainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.database = database;
            Log.D("Showing first page.");
            KioskFrame.Content = new KioskSetupPage1(this);
        }

        public void GotoPage2()
        {
            Log.D("Showing second page.");
            KioskFrame.Content = new KioskSetupPage2(this, database);
        }

        public void GotoPage3(int eventId, int print)
        {
            this.eventId = eventId;
            this.print = print;
            Log.D("Showing third page. EventId is " + eventId + " print is set to " + print);
            KioskFrame.Content = new KioskSetupPage3(this);
        }

        public void GotoPage4()
        {
            Log.D("Showing final page.");
            KioskFrame.Content = new KioskSetupPage4(mainWindow, this);
        }

        public void Finish(String liabilityWaiver)
        {
            this.liabilityWaiver = WebUtility.HtmlEncode(liabilityWaiver);
            if (this.liabilityWaiver.Contains("\r"))
            {
                Log.D("Found a \\r");
                this.liabilityWaiver = this.liabilityWaiver.Replace("\r", "");
            }
            if (this.liabilityWaiver.Contains("\n"))
            {
                Log.D("Found a \\n");
                this.liabilityWaiver = this.liabilityWaiver.Replace("\n", "&#10;");
            }
            if (this.liabilityWaiver.Contains("\n") || this.liabilityWaiver.Contains("\r"))
            {
                Log.D("We didn't get them all! The horror.");
            }
            Log.D("We've clicked finish. Event id is " + eventId + " and the waiver is " + this.liabilityWaiver);
            database.SetLiabilityWaiver(eventId, this.liabilityWaiver);
            database.SetPrintOption(eventId, print);
            mainWindow.EnableKiosk(eventId);
            this.Close();
        }

        public void RegisterExampleWaiver(ExampleLiabilityWaiver example)
        {
            this.exampleLiabilityWaiver = example;
        }

        public void DeRegisterExampleWaiver()
        {
            this.exampleLiabilityWaiver = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (exampleLiabilityWaiver != null)
            {
                try
                {
                    this.exampleLiabilityWaiver.Close();
                }
                catch { }
            }
            mainWindow.WindowClosed(this);
        }
    }
}
