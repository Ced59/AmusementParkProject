using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeParkOpeningHoursIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkOpeningHoursScheduleDocument> collection =
            this.database.GetCollection<ParkOpeningHoursScheduleDocument>(this.settings.ParkOpeningHoursCollectionName);

        List<CreateIndexModel<ParkOpeningHoursScheduleDocument>> indexes = new List<CreateIndexModel<ParkOpeningHoursScheduleDocument>>
        {
            new CreateIndexModel<ParkOpeningHoursScheduleDocument>(
                Builders<ParkOpeningHoursScheduleDocument>.IndexKeys.Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_opening_hours_park_id_unique", Unique = true }),
            new CreateIndexModel<ParkOpeningHoursScheduleDocument>(
                Builders<ParkOpeningHoursScheduleDocument>.IndexKeys.Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_opening_hours_updated" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
