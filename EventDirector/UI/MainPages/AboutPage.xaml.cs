using EventDirector.Interfaces;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : IMainPage
    {
        IMainWindow mWindow;

        public AboutPage(IMainWindow mWindow)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            string gitVersion = "";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EventDirector." + "version.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    gitVersion = reader.ReadToEnd();
                }
            }
            Log.D("Version: " + gitVersion);
            VersionLabel.Content = gitVersion;
        }

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateView() { }
    }
}
