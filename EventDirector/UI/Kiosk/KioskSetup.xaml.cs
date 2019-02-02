using EventDirector.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for KioskSettup.xaml
    /// </summary>
    public partial class KioskSetup : Window
    {
        IWindowCallback window = null;
        IDBInterface database;

        ExampleLiabilityWaiver exampleLiabilityWaiver = null;
        String liabilityWaiver;
        int eventId = -1;
        int print = 0;

        private KioskSetup(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            eventId = Convert.ToInt32(database.GetAppSetting(Constants.Settings.CURRENT_EVENT).value);
            Log.D("Showing first page.");
            KioskFrame.Content = new KioskSetupPage1(this);
        }

        public static KioskSetup NewWindow(IWindowCallback window, IDBInterface database)
        {
            return new KioskSetup(window, database);
        }

        public void GotoPage2()
        {
            if (eventId != -1)
            {
                Log.D("Showing last page.");
                KioskFrame.Content = new KioskSetupPage4(this, database);
            }
            else
            {
                Log.D("Showing second page.");
                KioskFrame.Content = new KioskSetupPage2(this, database);
            }
        }

        public void GotoPage3(int eventId)
        {
            this.eventId = eventId;
            Log.D("Showing third page. EventId is " + eventId + " print is set to " + print);
            KioskFrame.Content = new KioskSetupPage3(this);
        }

        public void GotoPage4()
        {
            Log.D("Showing final page.");
            KioskFrame.Content = new KioskSetupPage4(this, database);
        }

        public void Finish(String liabilityWaiver, int print)
        {
            this.print = print;
            // TODO replace codes in pre-made liability waiver.
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
            Log.D("We've clicked finish. Event id is " + eventId + " print is "+print+" and the waiver is " + this.liabilityWaiver);
            database.SetLiabilityWaiver(eventId, this.liabilityWaiver);
            database.SetPrintOption(eventId, print);
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == Constants.JsonOptions.KIOSK)
                {
                    opt.Value = Constants.JsonOptions.TRUE;
                }
            }
            database.SetEventOptions(eventId, list);
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

        public bool ExampleWaiverWindowOpen()
        {
            return this.exampleLiabilityWaiver != null;
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
            if (window != null) window.WindowFinalize(this);
        }
    }
}
