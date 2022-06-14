using Install.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using IWshRuntimeLibrary;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection;

namespace Install
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> arguments = new Dictionary<string, string>();
        private string appName = "Chronokeep";
        private string appExecutable = "Chronokeep.exe";
        private string appShortcut = "Chronokeep.lnk";
        private string dbName = "Chronokeep.sqlite";
        private bool installing = false;

        private readonly string InstallID = "A5CD5D3E-0351-478C-A292-B7924CDAD08A";

        private bool delete = false;
        private string currentDirectory = Directory.GetCurrentDirectory();

        Regex backupRegex = new Regex(@"Chronokeep-\d{4}-\d{2}-\d{2}-backup\.sqlite");
        Regex updateRegex = new Regex(@"^Install");

        public MainWindow()
        {
            InitializeComponent();
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                // not admin so exit
                MessageBox.Show("This program must be run as admin.");
                Close();
            }
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i += 2)
            {
                arguments.Add(args[i], args[i + 1]);
            }
            if (arguments.ContainsKey("--path"))
            {
                installLocationBox.Text = arguments["--path"];
            }
            else
            {
                installLocationBox.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appName);
            }
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
                file.WriteLine("TaskList /fi \"PID eq " + Process.GetCurrentProcess().Id.ToString() +"\" | find \":\"");
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
            cancelButton.Content = "Installing...";
            cancelButton.IsEnabled = false;
            installButton.Visibility = Visibility.Collapsed;
            settingsPanel.Visibility = Visibility.Collapsed;
            updateViewer.Visibility = Visibility.Visible;
            string installLocation = installLocationBox.Text;
            // Create desktop shortcut.
            if (createDesktopShortcutBox.IsChecked == true)
            {
                Log.D("MainWindow", "Creating shortcut.");
                string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), appShortcut);
                if (!System.IO.File.Exists(shortcutLocation))
                {
                    updateBlock.Text = $"{updateBlock.Text}\nCreating desktop shortcut.";
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = shell.CreateShortcut(shortcutLocation);
                    shortcut.Description = appName;
                    shortcut.TargetPath = System.IO.Path.Combine(installLocation, appExecutable);
                    shortcut.Save();
                }
                else
                {
                    updateBlock.Text = $"{updateBlock.Text}\nDesktop shortcut already exists.";
                }
            }
            // Backup database
            if (backupBox.IsChecked == true)
            {
                updateBlock.Text = $"{updateBlock.Text}\nChecking for old database files.";
                string dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), appName);
                string path = System.IO.Path.Combine(dirPath, dbName);
                Log.D("MainWindow", "Looking for database file.");
                if (Directory.Exists(dirPath))
                {
                    // delete old backups if told to
                    if (deleteOldBackupsBox.IsChecked == true)
                    {
                        updateBlock.Text = $"{updateBlock.Text}\nDeleting old database backups.";
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
                    if (System.IO.File.Exists(path))
                    {
                        string backup = System.IO.Path.Combine(dirPath, $"Chronokeep-{DateTime.Now.ToString("yyyy-MM-dd")}-backup.sqlite");
                        try
                        {
                            updateBlock.Text = $"{updateBlock.Text}\nBacking up database.";
                            System.IO.File.Copy(path, backup, false);
                        }
                        catch
                        {
                            updateBlock.Text = $"{updateBlock.Text}\nError backing up database.";
                        }
                    }
                }
            }
            // Create start menu shortcuts.
            string startMenuDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), appName);
            if (!Directory.Exists(startMenuDirectory))
            {
                updateBlock.Text = $"{updateBlock.Text}\nStart menu directory does not exist. Creating it.";
                Directory.CreateDirectory(startMenuDirectory);
            }
            string startMenuShortcut = System.IO.Path.Combine(startMenuDirectory, appShortcut);
            if (!System.IO.File.Exists(startMenuShortcut))
            {
                updateBlock.Text = $"{updateBlock.Text}\nCreating start menu shortcut.";
                WshShell shell = new WshShell();
                IWshShortcut shortcut = shell.CreateShortcut(startMenuShortcut);
                shortcut.Description = appName;
                shortcut.TargetPath = System.IO.Path.Combine(installLocation, appExecutable);
                shortcut.Save();
            } else
            {
                updateBlock.Text = $"{updateBlock.Text}\nStart menu shortcut already exists.";
            }
            // delete old install
            if (Directory.Exists(installLocation))
            {
                updateBlock.Text = $"{updateBlock.Text}\nDeleting old install.";
                DirectoryInfo installDir = new DirectoryInfo(installLocation);
                DeleteDirectory(installDir);

            } else
            {
                updateBlock.Text = $"{updateBlock.Text}\nInstallation destination does not exist. Creating folder.";
                Directory.CreateDirectory(installLocation);
            }
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            updateBlock.Text = $"{updateBlock.Text}\nMoving files over.";
            MoveDirectory(currentDirectory, installLocation);
            // add uninstall directive to registry
            string version = "";
            try
            {
                updateBlock.Text = $"{updateBlock.Text}\nGetting version number for uninstall registry.";
                using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep." + "version.txt"))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            version = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                updateBlock.Text = $"{updateBlock.Text}\nUnable to get version number.";

            }
            string softwareRegLoc = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            RegistryKey? regKey = Registry.LocalMachine.OpenSubKey(softwareRegLoc, true);
            if (regKey != null)
            {
                updateBlock.Text = $"{updateBlock.Text}\nAdding uninstall information.";
                try
                {
                    RegistryKey? appKey = regKey.OpenSubKey(InstallID); //regKey.CreateSubKey(InstallID);
                    if (appKey == null)
                    {
                        appKey = regKey.CreateSubKey(InstallID);
                    }
                    appKey.SetValue("DisplayName", appName, RegistryValueKind.String);
                    appKey.SetValue("Publisher", appName, RegistryValueKind.String);
                    appKey.SetValue("Version", version, RegistryValueKind.String);
                    appKey.SetValue("InstallLocation", installLocation, RegistryValueKind.String);
                    appKey.SetValue("UninstallString", System.IO.Path.Combine(installLocation, "Uninstall.exe"), RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    updateBlock.Text = $"{updateBlock.Text}\nUnable to add registry key. {ex.Message}";
                }
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
                f.Delete();
            }
        }

        private void MoveDirectory(DirectoryInfo currentDirectory, string installLocation)
        {
            foreach (DirectoryInfo d in currentDirectory.GetDirectories())
            {
                updateBlock.Text = $"{updateBlock.Text}\n-----{d.Name}.";
                Directory.CreateDirectory(System.IO.Path.Combine(installLocation, d.Name));
                MoveDirectory(d, System.IO.Path.Combine(installLocation, d.Name));
            }
            foreach (FileInfo f in currentDirectory.GetFiles())
            {
                Match updateMatch = updateRegex.Match(f.Name);
                if (!updateMatch.Success)
                {
                    try
                    {
                        updateBlock.Text = $"{updateBlock.Text}\n----------{f.Name}";
                        f.MoveTo(System.IO.Path.Combine(installLocation, f.Name));
                    }
                    catch
                    {
                        //updateBlock.Text = $"{updateBlock.Text}\nError moving {f.Name}";
                    }
                }
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
