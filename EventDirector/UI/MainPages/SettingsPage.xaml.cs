using EventDirector.Interfaces;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, IMainPage
    {
        INewMainWindow mWindow;
        IDBInterface database;

        public SettingsPage(INewMainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = database;
            Update();
        }

        public void Update()
        {
            AppSetting setting = database.GetAppSetting(Constants.Settings.COMPANY_NAME);
            CompanyNameBox.Text = setting.value;
            DefaultTimingBox.Items.Clear();
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "Last Used",
                Uid = Constants.Settings.TIMING_LAST_USED
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "Manual",
                Uid = Constants.Settings.TIMING_MANUAL
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "RFID",
                Uid = Constants.Settings.TIMING_RFID
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "Ipico",
                Uid = Constants.Settings.TIMING_IPICO
            });
            setting = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM);
            if (setting.value == Constants.Settings.TIMING_MANUAL)
            {
                DefaultTimingBox.SelectedIndex = 1;
            }
            else if (setting.value == Constants.Settings.TIMING_RFID)
            {
                DefaultTimingBox.SelectedIndex = 2;
            }
            else if (setting.value == Constants.Settings.TIMING_IPICO)
            {
                DefaultTimingBox.SelectedIndex = 3;
            }
            else
            {
                DefaultTimingBox.SelectedIndex = 0;
            }
            setting = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR);
            DefaultExportDirBox.Text = setting.value;
            setting = database.GetAppSetting(Constants.Settings.DEFAULT_WAIVER);
            DefaultWaiverBox.Text = setting.value;
        }

        private void ResetDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Reset button clicked.");
        }

        private void RebuildDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Rebuild button clicked.");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Save button clicked.");
            database.SetAppSetting(Constants.Settings.COMPANY_NAME, CompanyNameBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, ((ComboBoxItem)DefaultTimingBox.SelectedItem).Uid);
            //database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, DefaultExportDirBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_WAIVER, DefaultWaiverBox.Text);
            Update();
        }
    }
}
