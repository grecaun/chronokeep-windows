using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages.Dashboard;

namespace Chronokeep.UI.Parts;

public partial class APIPart : UserControl
{
    readonly APIPage page;
    public APIObject theAPI;

    public APIPart(APIPage page, APIObject api)
    {
        InitializeComponent();
        theAPI = api;
        this.page = page;
        APINickname.Text = api.Nickname;
        foreach (string uid in Constants.APIConstants.API_TYPE_NAMES.Keys)
        {
            APIType.Items.Add(new ComboBoxItem()
            {
                Content = Constants.APIConstants.API_TYPE_NAMES[uid],
                Tag = uid,
                IsSelected = theAPI.Type.Equals(uid),
            });
        }
        APIURL.Text = api.URL;
        APIURL.IsEnabled = Constants.APIConstants.API_SELF_HOSTED[theAPI.Type];
        APIToken.Text = api.AuthToken;
        APIWebURL.Text = api.WebURL;
    }

    public void UpdateResultsAPI()
    {
        Log.D("UI.MainPages.APIPage", "Updating api.");
        theAPI.Nickname = APINickname.Text!;
        theAPI.URL = APIURL.Text!;
        if (!theAPI.URL!.EndsWith("/"))
        {
            theAPI.URL = theAPI.URL + "/";
        }
        theAPI.AuthToken = APIToken.Text!;
        theAPI.Type = (string)((ComboBoxItem)APIType.SelectedItem!).Tag!;
        theAPI.WebURL = APIWebURL.Text!;
        if (theAPI.WebURL!.Length > 0 && !theAPI.WebURL.EndsWith("/"))
        {
            theAPI.WebURL = theAPI.WebURL + "/";
        }
    }

    private void APIType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.APIPage", "Changing API Type!");
        // Ensure we've got something selected, and then change the URL if they've selected Chronokeep.
        if (APIType.SelectedItem != null)
        {
            string type = (string)((ComboBoxItem)APIType.SelectedItem).Tag!;
            if (!Constants.APIConstants.API_SELF_HOSTED[type])
            {
                theAPI.URL = Constants.APIConstants.API_URL[type];
                APIURL.Text = theAPI.URL;
                APIURL.IsEnabled = false;
            }
            else
            {
                APIURL.IsEnabled = true;
            }
            if (Constants.APIConstants.API_RESULTS[type])
            {
                APIWebURL.Text = theAPI.WebURL;
                APIWebURL.IsEnabled = true;
            }
            else
            {
                APIWebURL.Text = "";
                APIWebURL.IsEnabled = false;
            }
        }
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.APIPage", "Removing api.");
        this.page.RemoveAPI(theAPI);
    }
}