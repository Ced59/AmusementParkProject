namespace Repositories.Interfaces
{
    public interface IMongoDbSettings
    {
        string Url { get; }
        string DatabaseName { get; }
        string Username { get; }
        string Password { get; }
        string UsersCollectionName { get; }
        string ParksCollectionName { get; }
        string ParkFoundersCollectionName { get; }
        string ParkOperatorsCollectionName { get; }
        string AttractionManufacturersCollectionName { get; }
        string ParkZonesCollectionName { get; }
        string ParkItemsCollectionName { get; }
        string SearchItemCollectionName { get; }
        string ImagesCollectionName { get; }
        string CountriesCollectionName { get; }
        string CaptainCoasterSettingsCollectionName { get; }
        string CaptainCoasterParksCollectionName { get; }
        string CaptainCoasterCoastersCollectionName { get; }
        string CaptainCoasterSyncSessionsCollectionName { get; }
        string CaptainCoasterComparisonResultsCollectionName { get; }
    }
}