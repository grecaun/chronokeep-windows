using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        MainWindow mWindow;

        public About(MainWindow mWindow)
        {
            InitializeComponent();
            string gitVersion = String.Empty;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EventDirector." + "version.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    gitVersion = reader.ReadToEnd();
                }
            }

            this.mWindow = mWindow;

            Log.D("Version: " + gitVersion);
            VersionLabel.Content = gitVersion;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mWindow.WindowClosed(this);
        }
    }
}
