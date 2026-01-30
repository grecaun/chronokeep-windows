using Chronokeep.Database;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;

namespace Chronokeep.UI.API.Parts;

public partial class APIExpanderPart : UserControl
{

    public APIExpanderPart(
        APIObject api,
        List<RemoteReader> readers,
        Dictionary<(int, string), RemoteReader> savedReaders,
        IDBInterface database,
        IMainWindow mainWindow)
    {
        foreach (RemoteReader reader in readers)
        {
            reader.APIIDentifier = api.Identifier;
            if (savedReaders.TryGetValue((reader.APIIDentifier, reader.Name), out RemoteReader rReader))
            {
                reader.LocationID = rReader.LocationID;
            }
            readerListView.Items.Add(new ReaderListItem(reader, api, savedReaders, database, mainWindow));
        }
    }

    public Dictionary<RemoteReader, bool> GetAutoDownloadDictionary()
    {
        Dictionary<RemoteReader, bool> output = [];
        foreach (ReaderListItem item in readerListView.Items)
        {
            output[item.GetUpdatedReader()] = item.AutoDownloadReads();
        }
        return output;
    }
}