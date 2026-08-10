using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeParkPricingIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkPricingDocument> collection =
            this.database.GetCollection<ParkPricingDocument>(this.settings.ParkPricingCollectionName);

        IReadOnlyCollection<CreateIndexModel<ParkPricingDocument>> indexes = BuildParkPricingIndexes();

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<ParkPricingDocument>> BuildParkPricingIndexes()
    {
        return new List<CreateIndexModel<ParkPricingDocument>>
        {
            new CreateIndexModel<ParkPricingDocument>(
                Builders<ParkPricingDocument>.IndexKeys.Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_pricing_park_id_unique", Unique = true }),
            new CreateIndexModel<ParkPricingDocument>(
                Builders<ParkPricingDocument>.IndexKeys.Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_pricing_updated" }),
        };
    }
}
