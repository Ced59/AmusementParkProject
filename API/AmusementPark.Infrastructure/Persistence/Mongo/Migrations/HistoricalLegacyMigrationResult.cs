namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed record HistoricalLegacyMigrationResult(
    string? CanonicalFactId,
    bool IsBlocked,
    int SourceReferenceCount,
    IReadOnlyCollection<string> Warnings);
