using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Database;
using Chronokeep.Objects;
using Chronokeep.UI.API.Windows;

namespace Chronokeep.UI.API;

public partial class APIPage1 : UserControl
{
    private readonly APIWindow window;
    private readonly Dictionary<string, APIObject> apiDict;

    public APIPage1(APIWindow window, IDBInterface database)
    {
        InitializeComponent();
        this.window = window;

        AppSetting last_api = database.GetAppSetting(Constants.Settings.LAST_USED_API_ID);
        List<APIObject> apis = database.GetAllAPI();
        apis.RemoveAll(x => !Constants.APIConstants.API_RESULTS[x.Type]);
        apiDict = [];
        int api_id = -1;
        if (last_api != null)
        {
            try
            {
                api_id = Convert.ToInt32(last_api.Value);
            }
            catch
            {
                api_id = -1;
            }
        }
        int ix = 0;
        int count = 0;
        foreach (APIObject api in apis)
        {
            apiDict[api.Identifier.ToString()] = api;
            APIBox.Items.Add(new ComboBoxItem
            {
                Content = api.Nickname,
                Uid = api.Identifier.ToString()
            });
            if (api_id > 0 && api_id == api.Identifier)
            {
                ix = count;
            }
            count++;
        }
        APIBox.SelectedIndex = ix;
        Event theEvent = database.GetCurrentEvent();
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.Close();
    }

    private void Next_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.GotoPage2(apiDict[((ComboBoxItem)APIBox.SelectedItem).Uid]);
    }
}