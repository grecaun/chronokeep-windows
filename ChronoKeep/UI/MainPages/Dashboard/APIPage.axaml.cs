using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages.Dashboard;

public partial class APIPage : UserControl
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private List<APIObject> resultsAPI;

    public APIPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        this.database = database;
        UpdateView();
    }

    public void Keyboard_Ctrl_A()
    {
        Log.D("UI.MainPages.APIPage", "Ctrl + A Passed to this page.");
        Add_Click(null, null);
    }

    public void Keyboard_Ctrl_S()
    {
        Log.D("UI.MainPages.APIPage", "Ctrl + S Passed to this page.");
        UpdateResultsAPI();
        UpdateView();
    }

    public void Keyboard_Ctrl_Z()
    {
        UpdateView();
    }

    public void UpdateView()
    {
        APIBox.Items.Clear();
        resultsAPI = database.GetAllAPI();
        foreach (APIObject api in resultsAPI)
        {
            APIBox.Items.Add(new AnAPI(this, api));
        }
    }

    public void Closing()
    {
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateResultsAPI();
        }
    }

    public void UpdateResultsAPI()
    {
        foreach (AnAPI listDiv in APIBox.Items)
        {
            listDiv.UpdateResultsAPI();
            database.UpdateAPI(listDiv.theAPI);
        }
    }

    public void RemoveAPI(APIObject api)
    {
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateResultsAPI();
        }
        database.RemoveAPI(api.Identifier);
        UpdateView();
    }

    public void UpdateResultsAPI(APIObject api)
    {
        database.UpdateAPI(api);
        UpdateView();
    }

    private void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.APIPage", "Add api clicked.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateResultsAPI();
        }
        database.AddAPI(new APIObject());
        UpdateView();
    }

    private void Update_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.APIPage", "Update clicked.");
        UpdateResultsAPI();
        UpdateView();
    }

    private void Revert_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.APIPage", "Revert clicked.");
        UpdateView();
    }

    private void DoneBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        mWindow.SwitchPage(new DashboardPage(mWindow, database));
    }
}