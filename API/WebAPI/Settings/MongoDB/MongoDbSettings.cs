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
        public string SearchItemCollectionName { get; set; } = string.Empty;
        public string ImagesCollectionName { get; set; } = string.Empty;
        public string CountriesCollectionName { get; set; } = string.Empty;
    }
}