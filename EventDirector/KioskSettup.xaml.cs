﻿using System;
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
    public partial class KioskSettup : Window
    {
        MainWindow mainWindow;
        ExampleLiabilityWaiver exampleLiabilityWaiver = null;
        IDBInterface database;
        String liabilityWaiver;
        int eventId;

        public KioskSettup(MainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.database = database;
            Log.D("Showing first page.");
            KioskFrame.Content = new KioskSettupPage1(this);
        }

        public void GotoPage2()
        {
            Log.D("Showing second page.");
            KioskFrame.Content = new KioskSettupPage2(this, database);
        }

        public void GotoPage3(int eventId)
        {
            this.eventId = eventId;
            Log.D("Showing third page.");
            KioskFrame.Content = new KioskSettupPage3(this);
        }

        public void GotoPage4()
        {
            Log.D("Showing final page.");
            KioskFrame.Content = new KioskSettupPage4(mainWindow, this);
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