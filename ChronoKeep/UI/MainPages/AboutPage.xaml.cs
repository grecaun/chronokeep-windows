using Chronokeep.Interfaces;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Chronokeep.UI.MainPages
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

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep." + "version.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    gitVersion = reader.ReadToEnd();
                }
            }
            Log.D("UI.MainPages.AboutPage", "Version: " + gitVersion);
            VersionLabel.Content = gitVersion;
        }

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateView() { }

        private void VersionLabel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.AboutPage", "Version clicked, checking for new version.");
            Updates.Check.Do(mWindow, true);
        }
    }
}
