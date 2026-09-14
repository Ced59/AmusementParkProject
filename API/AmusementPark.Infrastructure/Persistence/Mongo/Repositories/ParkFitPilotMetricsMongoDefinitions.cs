using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitPilotMetricsMongoDefinitions
{
    public const string DateIndexName = "idx_park_fit_pilot_date";
    public const string RetentionIndexName = "idx_park_fit_pilot_retention";

    public static IReadOnlyCollection<CreateIndexModel<ParkFitPilotDailyMetricsDocument>>
        BuildIndexes()
    {
        CreateIndexModel<ParkFitPilotDailyMetricsDocument> date = new(
            Builders<ParkFitPilotDailyMetricsDocument>.IndexKeys
                .Ascending(static document => document.DateUtc),
            new CreateIndexOptions { Name = DateIndexName });
        CreateIndexModel<ParkFitPilotDailyMetricsDocument> retention = new(
            Builders<ParkFitPilotDailyMetricsDocument>.IndexKeys
                .Ascending(static document => document.ExpiresAtUtc),
            new CreateIndexOptions
            {
                Name = RetentionIndexName,
                ExpireAfter = TimeSpan.Zero,
            });
        return [date, retention];
    }
}
