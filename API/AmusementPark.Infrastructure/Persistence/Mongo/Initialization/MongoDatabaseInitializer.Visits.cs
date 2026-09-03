using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeUserVisitIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserVisitDocument> collection =
            this.database.GetCollection<UserVisitDocument>(this.settings.UserVisitsCollectionName);
        await collection.Indexes.CreateManyAsync(
            UserVisitMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }
}
