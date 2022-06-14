using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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
using Uninstall.Objects;

namespace Uninstall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string appName = "Chronokeep";
        private string appExecutable = "Chronokeep.exe";
        private string appShortcut = "Chronokeep.lnk";
        private string dbName = "Chronokeep.sqlite";
        private bool installing = false;

        private bool delete = false;
        private string currentDirectory = Directory.GetCurrentDirectory();

        private string InstallID = "A5CD5D3E-0351-478C-A292-B7924CDAD08A";

        Regex backupRegex = new Regex(@"Chronokeep[\d\-](backup)?\.sqlite");

        public MainWindow()
        {
            InitializeComponent();
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                // not admin so exit
                MessageBox.Show("This program must be run as admin.");
                Close();
            }
            currentDirectory = Directory.GetCurrentDirectory();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (installing)
            {
                e.Cancel = true;
            }
            // if we correctly installed/updated and set delete to update, then we can delete everything left
            if (delete)
            {
                StreamWriter file = new StreamWriter("delete.bat");
                file.WriteLine("@echo off");
                file.WriteLine(":Loop");
                file.WriteLine("TaskList /fi \"PID eq " + Process.GetCurrentProcess().Id.ToString() + "\" | find \":\"");
                file.WriteLine("if Errorlevel 1 (");
                file.WriteLine("  Timeout /T 1 /Nobreak");
                file.WriteLine("  Goto Loop");
                file.WriteLine(")");
                file.WriteLine("rmdir /s /q \"" + currentDirectory + "\"");
                file.Close();

                Process bat_call = new Process();
                bat_call.StartInfo.FileName = currentDirectory + @"\delete.bat";
                bat_call.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                bat_call.StartInfo.UseShellExecute = true;
                bat_call.Start();
            }
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            installing = true;
            cancelButton.Content = "Uninstalling...";
            cancelButton.IsEnabled = false;
            installButton.Visibility = Visibility.Collapsed;
            settingsPanel.Visibility = Visibility.Collapsed;
            updateViewer.Visibility = Visibility.Visible;
            // Delete desktop shortcut.
            Log.D("MainWindow", "Creating shortcut.");
            string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), appShortcut);
            if (File.Exists(shortcutLocation))
            {
                updateBlock.Text = $"{updateBlock.Text}\nDeleting desktop shortcut.";
                File.Delete(shortcutLocation);
            }
            // Delete databases if told to
            if (deleteBox.IsChecked == true)
            {
                updateBlock.Text = $"{updateBlock.Text}\nDeleting database.";
                string dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), appName);
                string path = System.IO.Path.Combine(dirPath, dbName);
                Log.D("MainWindow", "Looking for database file.");
                if (Directory.Exists(dirPath))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                    foreach (FileInfo file in directoryInfo.GetFiles())
                    {
                        Match fileMatch = backupRegex.Match(file.Name);
                        if (fileMatch.Success)
                        {
                            updateBlock.Text = $"{updateBlock.Text}\n-----{file.Name}";
                            file.Delete();
                        }
                    }
                }
            }
            // Delete start menu shortcuts.
            string startMenuDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), appName);
            if (Directory.Exists(startMenuDirectory))
            {
                string startMenuShortcut = System.IO.Path.Combine(startMenuDirectory, appShortcut);
                if (File.Exists(startMenuShortcut))
                {
                    updateBlock.Text = $"{updateBlock.Text}\nCreating start menu shortcut.";
                    File.Delete(startMenuShortcut);
                }
            }
            // delete install
            string installLocation = Directory.GetCurrentDirectory();
            if (Directory.Exists(installLocation))
            {
                updateBlock.Text = $"{updateBlock.Text}\nDeleting old install.";
                DirectoryInfo installDir = new DirectoryInfo(installLocation);
                DeleteDirectory(installDir);

            }
            // set delete to true and let the user exit
            delete = true;
            installing = false;
            cancelButton.Content = "Done";
            cancelButton.IsEnabled = true;
        }

        private void DeleteDirectory(DirectoryInfo currentDirectory)
        {
            foreach (DirectoryInfo d in currentDirectory.GetDirectories())
            {
                updateBlock.Text = $"{updateBlock.Text}\n-----{d.Name}";
                DeleteDirectory(d);
                d.Delete();
            }
            foreach (FileInfo f in currentDirectory.GetFiles())
            {
                updateBlock.Text = $"{updateBlock.Text}\n----------{f.Name}";
                try
                {
                    f.Delete();
                }
                catch { }
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
