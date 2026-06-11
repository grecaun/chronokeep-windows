using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.UI.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Chronokeep.UI.MainPages;

public partial class AboutPage : UserControl, IMainPage
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;

    public AboutPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        string gitVersion = "";

        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep." + "version.txt")!)
        {
            using StreamReader reader = new(stream);
            gitVersion = reader.ReadToEnd();
        }
        Log.D("UI.MainPages.AboutPage", "Version: " + gitVersion);
        string dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.Settings.HELP_DIR);
        if (Directory.Exists(dirPath))
        {
            dirPath = Path.Combine(dirPath, "index.html");
            HelpDocsButton.Tag = dirPath;
        }
        VersionLabel.Text = gitVersion.Trim();
        HelpDocsButton.NavigateUri = new(Path.Combine(AppContext.BaseDirectory, "help", "index.html"));
        this.database = database;

    }

    public void Closing() { }

    public void Keyboard_Ctrl_A() { }

    public void Keyboard_Ctrl_S() { }

    public void Keyboard_Ctrl_Z() { }

    public void UpdateView() { }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.AboutPage", "Version clicked, checking for new version.");
        Updates.Check.Do(mWindow, true);
    }

    private void Changelog_Click(object? sender, RoutedEventArgs e)
    {
        ChangeLogWindow clw = ChangeLogWindow.NewWindow(mWindow, database);
        clw.Show();
    }

    private void OpenDataFolder_Click(object? sender, RoutedEventArgs e)
    {
        string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR);
        if (!Directory.Exists(dirPath))
        {
            return;
        }
        Process.Start("explorer", dirPath);
    }
}