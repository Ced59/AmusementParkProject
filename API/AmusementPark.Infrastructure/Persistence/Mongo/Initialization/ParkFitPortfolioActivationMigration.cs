using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Fige une seule fois le portefeuille historique en créant un statut actif explicite.
/// Les parcs créés ensuite restent non activés tant qu'un administrateur ne les valide pas.
/// </summary>
internal sealed class ParkFitPortfolioActivationMigration
{
    internal const string MigrationId = "fit-15-portfolio-activation-v1";
    private const int BatchSize = 500;

    private readonly IMongoCollection<ParkDocument> parks;
    private readonly IMongoCollection<ParkFitOperationalStatusDocument> statuses;
    private readonly IMongoCollection<ParkFitPortfolioMigrationDocument> migrations;
    private readonly TimeProvider timeProvider;

    public ParkFitPortfolioActivationMigration(
        IMongoCollection<ParkDocument> parks,
        IMongoCollection<ParkFitOperationalStatusDocument> statuses,
        IMongoCollection<ParkFitPortfolioMigrationDocument> migrations,
        TimeProvider? timeProvider = null)
    {
        this.parks = parks ?? throw new ArgumentNullException(nameof(parks));
        this.statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        this.migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<long> MigrateAsync(CancellationToken cancellationToken)
    {
        DateTime requestedCutoffAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await TryCreateMigrationPlanAsync(
            this.migrations,
            requestedCutoffAtUtc,
            cancellationToken);
        ParkFitPortfolioMigrationDocument? migration = await this.migrations
            .Find(Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                static migration => migration.Id,
                MigrationId))
            .FirstOrDefaultAsync(cancellationToken);
        if (migration is null)
        {
            throw new InvalidOperationException(
                "The Park Fit portfolio migration plan could not be persisted.");
        }

        if (migration.CompletedAtUtc.HasValue)
        {
            return 0;
        }

        DateTime cutoffAtUtc = migration.CutoffAtUtc;
        long activatedParkCount = 0;
        IFindFluent<ParkDocument, ParkDocument> find = this.parks
            .Find(BuildLegacyParkFilter(cutoffAtUtc))
            .Project(static park => new ParkDocument { Id = park.Id });
        find.Options.BatchSize = BatchSize;
        using IAsyncCursor<ParkDocument> cursor = await find.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            List<WriteModel<ParkFitOperationalStatusDocument>> writes = cursor.Current
                .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
                .Select(park => new UpdateOneModel<ParkFitOperationalStatusDocument>(
                    Builders<ParkFitOperationalStatusDocument>.Filter.Eq(
                        static status => status.Id,
                        park.Id),
                    BuildLegacyStatusUpsert(cutoffAtUtc))
                {
                    IsUpsert = true,
                })
                .Cast<WriteModel<ParkFitOperationalStatusDocument>>()
                .ToList();
            if (writes.Count == 0)
            {
                continue;
            }

            try
            {
                BulkWriteResult<ParkFitOperationalStatusDocument> result =
                    await this.statuses.BulkWriteAsync(
                        writes,
                        new BulkWriteOptions { IsOrdered = false },
                        cancellationToken);
                activatedParkCount += result.Upserts.Count;
            }
            catch (MongoBulkWriteException<ParkFitOperationalStatusDocument> exception)
                when (exception.WriteErrors.Count > 0
                    && exception.WriteErrors.All(static error =>
                        error.Category == ServerErrorCategory.DuplicateKey))
            {
                // Plusieurs instances peuvent exécuter la migration au même instant.
                // Les collisions d'identifiant prouvent que l'autre instance a déjà créé le statut.
            }
        }

        DateTime completedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.migrations.UpdateOneAsync(
            Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                static value => value.Id,
                MigrationId),
            Builders<ParkFitPortfolioMigrationDocument>.Update.Set(
                static value => value.CompletedAtUtc,
                completedAtUtc),
            cancellationToken: cancellationToken);
        return activatedParkCount;
    }

    internal static async Task TryCreateMigrationPlanAsync(
        IMongoCollection<ParkFitPortfolioMigrationDocument> migrations,
        DateTime cutoffAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await migrations.UpdateOneAsync(
                Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                    static migration => migration.Id,
                    MigrationId),
                Builders<ParkFitPortfolioMigrationDocument>.Update
                    .SetOnInsert(static migration => migration.CutoffAtUtc, cutoffAtUtc),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Une autre instance a figé la cohorte entre-temps. La lecture qui
            // suit réutilisera obligatoirement sa borne persistée.
        }
    }

    internal static FilterDefinition<ParkDocument> BuildLegacyParkFilter(
        DateTime cutoffAtUtc)
    {
        return Builders<ParkDocument>.Filter.Eq(static park => park.IsVisible, true)
            & Builders<ParkDocument>.Filter.Eq(
                static park => park.Status,
                ParkStatus.Operating)
            & Builders<ParkDocument>.Filter.Ne(static park => park.Latitude, null)
            & Builders<ParkDocument>.Filter.Ne(static park => park.Longitude, null)
            & Builders<ParkDocument>.Filter.Or(
                Builders<ParkDocument>.Filter.Ne(static park => park.Latitude, 0d),
                Builders<ParkDocument>.Filter.Ne(static park => park.Longitude, 0d))
            & Builders<ParkDocument>.Filter.Lt(
                static park => park.UpdatedAt,
                cutoffAtUtc);
    }

    internal static UpdateDefinition<ParkFitOperationalStatusDocument> BuildLegacyStatusUpsert(
        DateTime migratedAtUtc)
    {
        return Builders<ParkFitOperationalStatusDocument>.Update
            .SetOnInsert(static status => status.State, ParkFitRecommendationState.Active)
            .SetOnInsert(static status => status.Revision, 0)
            .SetOnInsert(
                static status => status.Decisions,
                new List<ParkFitOperationalDecisionDocument>())
            .SetOnInsert(static status => status.CreatedAt, migratedAtUtc)
            .SetOnInsert(static status => status.UpdatedAt, migratedAtUtc);
    }
}
