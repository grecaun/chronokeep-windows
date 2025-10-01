using Chronokeep.IO;

namespace Chronokeep.Interfaces.IO
{
    public interface IDataImporter
    {
        ImportData Data { get; }
        void FetchHeaders();
        void FetchData();
        void Finish();
    }
}
