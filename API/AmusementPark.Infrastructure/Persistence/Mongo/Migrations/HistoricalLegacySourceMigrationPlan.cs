using AmusementPark.Core.Domain.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

internal sealed record HistoricalLegacySourceMigrationPlan(
    HistoricalSourceReference Source,
    HistoricalReviewEvent ReviewEvent,
    HistoricalSourceRevisionReference Reference);
