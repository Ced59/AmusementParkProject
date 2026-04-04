using Repositories.Interfaces;

namespace WebAPI.Settings.MongoDB
{
    public class MongoDbSettings : IMongoDbSettings
    {
        public string Url { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UsersCollectionName { get; set; } = string.Empty;
        public string ParksCollectionName { get; set; } = string.Empty;
        public string ParkFoundersCollectionName { get; set; } = string.Empty;
        public string ParkOperatorsCollectionName { get; set; } = string.Empty;
        public string AttractionManufacturersCollectionName { get; set; } = string.Empty;
        public string ParkZonesCollectionName { get; set; } = string.Empty;
        public string ParkItemsCollectionName { get; set; } = string.Empty;
        public string SearchItemCollectionName { get; set; } = string.Empty;
        public string ImagesCollectionName { get; set; } = string.Empty;
        public string CountriesCollectionName { get; set; } = string.Empty;
        public string CaptainCoasterSettingsCollectionName { get; set; } = string.Empty;
        public string CaptainCoasterParksCollectionName { get; set; } = string.Empty;
        public string CaptainCoasterCoastersCollectionName { get; set; } = string.Empty;
        public string CaptainCoasterSyncSessionsCollectionName { get; set; } = string.Empty;
        public string CaptainCoasterComparisonResultsCollectionName { get; set; } = string.Empty;
    }
}