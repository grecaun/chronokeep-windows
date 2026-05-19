using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Timing.Interfaces;

namespace Chronokeep.UI.Timing.ReaderSettings.Parts;

public partial class APIPart : UserControl
{
    private PortalAPI? api = null;
    private readonly ChronokeepInterface? reader = null;

    public APIPart(PortalAPI api, ChronokeepInterface reader)
    {
        InitializeComponent();
        this.api = api;
        this.reader = reader;
        nameBox.Text = api.Nickname;
        kindBox.SelectedIndex = api.Kind switch
        {
            PortalAPI.API_TYPE_CHRONOKEEP_REMOTE => 0,
            PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF => 1,
            _ => 0,
        };
        tokenBox.Text = api.Token;
        uriBox.Text = api.Uri;
        PrivateUpdateURI();
    }

    public void UpdateAPI(PortalAPI api)
    {
        this.api = api;
        nameBox.Text = api.Nickname;
        switch (api.Kind)
        {
            case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE:
                kindBox.SelectedIndex = 0;
                break;
            case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF:
                kindBox.SelectedIndex = 1;
                break;
            default:
                kindBox.SelectedIndex = 0;
                break;
        }
        tokenBox.Text = api.Token;
        uriBox.Text = api.Uri;
        PrivateUpdateURI();
    }

    public void PrivateUpdateURI()
    {
        switch (((ComboBoxItem)kindBox.SelectedItem!).Tag)
        {
            case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE:
                uriBox.IsVisible = false;
                uriBox.Text = PortalAPI.API_URI_CHRONOKEEP_REMOTE;
                break;
            case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF:
            default:
                uriBox.IsVisible = true;
                uriBox.Text = api.Uri;
                break;
        }
    }

    private void KindBox_ValueChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Selected type changed.");
        PrivateUpdateURI();
    }
    private void DeleteAPI(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Deleting api " + api.Id);
        reader.SendDeleteApi(api);
    }

    private void SaveAPI(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Saving api " + api.Id);
        api.Nickname = nameBox.Text!.Trim();
        api.Token = tokenBox.Text!.Trim();
        api.Uri = uriBox.Text!.Trim();
        api.Kind = ((ComboBoxItem)kindBox.SelectedItem).Tag as string;
        reader.SendSaveApi(api);
    }
}