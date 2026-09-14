using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitOperationalStatusMongoDefinitions
{
    public const string StateUpdatedIndexName = "idx_park_fit_operational_state_updated";

    public static FilterDefinition<ParkFitOperationalStatusDocument> BuildParkIdFilter(string parkId)
    {
        return Builders<ParkFitOperationalStatusDocument>.Filter.Eq(
            static document => document.Id,
            parkId);
    }

    public static FilterDefinition<ParkFitOperationalStatusDocument> BuildRevisionFilter(
        string parkId,
        long revision)
    {
        return BuildParkIdFilter(parkId)
            & Builders<ParkFitOperationalStatusDocument>.Filter.Eq(
                static document => document.Revision,
                revision);
    }

    public static IReadOnlyCollection<CreateIndexModel<ParkFitOperationalStatusDocument>> BuildIndexes()
    {
        return new[]
        {
            new CreateIndexModel<ParkFitOperationalStatusDocument>(
                Builders<ParkFitOperationalStatusDocument>.IndexKeys
                    .Ascending(static document => document.State)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = StateUpdatedIndexName }),
        };
    }
}
