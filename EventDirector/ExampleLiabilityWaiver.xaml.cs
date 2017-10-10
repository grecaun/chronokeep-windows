﻿using System;
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
    /// Interaction logic for ExampleLiabilityWaiver.xaml
    /// </summary>
    public partial class ExampleLiabilityWaiver : Window
    {
        MainWindow mainWindow;
        KioskSettup kioskSettup;

        public ExampleLiabilityWaiver(MainWindow mainWindow, KioskSettup kioskSettup)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.kioskSettup = kioskSettup;
            kioskSettup.RegisterExampleWaiver(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                kioskSettup.DeRegisterExampleWaiver();
            }
            catch { }
            mainWindow.WindowClosed(this);
        }
    }
}
