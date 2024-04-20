using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace WebAPI.Settings
{
    public static class MongoDbInitializer
    {
        public static void InitializeCollections(IMongoDatabase database, IMongoDbSettings settings)
        {
            EnsureCollectionExists(database, settings.UsersCollectionName);
            //EnsureCollectionExists(database, settings.AnotherCollectionName); // Répéter pour d'autres collections
        }

        private static void EnsureCollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });
            var exists = collections.Any();

            if (!exists)
            {
                database.CreateCollection(collectionName);
            }
        }
    }

}
