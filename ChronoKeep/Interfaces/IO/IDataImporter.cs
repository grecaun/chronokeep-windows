namespace Chronokeep
{
    public interface IDataImporter
    {
        ImportData Data { get; }
        void FetchHeaders();
        void FetchData();
        void Finish();
    }
}
