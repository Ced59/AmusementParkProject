using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise une seule fois les états absents en non activés.
/// Une activation reste ainsi toujours une décision administrative explicite.
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
        ParkFitPortfolioMigrationDocument? migration = await this.migrations
            .Find(Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                static migration => migration.Id,
                MigrationId))
            .FirstOrDefaultAsync(cancellationToken);
        if (migration is null)
        {
            DateTime startedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            await TryCreateMigrationPlanAsync(
                this.migrations,
                startedAtUtc,
                cancellationToken);
            migration = await this.migrations
                .Find(Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                    static value => value.Id,
                    MigrationId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (migration is null)
        {
            throw new InvalidOperationException(
                "The Park Fit portfolio migration plan could not be persisted.");
        }

        if (migration.CompletedAtUtc.HasValue)
        {
            return 0;
        }

        long initializedParkCount = 0;
        DateTime migratedAtUtc = migration.StartedAtUtc;
        IFindFluent<ParkDocument, ParkDocument> find = this.parks
            .Find(BuildPortfolioParkFilter())
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
                    BuildDefaultStatusUpsert(migratedAtUtc))
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
                initializedParkCount += result.Upserts.Count;
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
        return initializedParkCount;
    }

    internal static async Task TryCreateMigrationPlanAsync(
        IMongoCollection<ParkFitPortfolioMigrationDocument> migrations,
        DateTime startedAtUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<ParkFitPortfolioMigrationDocument> update =
            BuildMigrationPlanUpsert(startedAtUtc);
        try
        {
            await migrations.UpdateOneAsync(
                Builders<ParkFitPortfolioMigrationDocument>.Filter.Eq(
                    static migration => migration.Id,
                    MigrationId),
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Une autre instance a démarré la même migration. La lecture qui
            // suit réutilisera son plan idempotent.
        }
    }

    internal static UpdateDefinition<ParkFitPortfolioMigrationDocument>
        BuildMigrationPlanUpsert(DateTime startedAtUtc)
    {
        return Builders<ParkFitPortfolioMigrationDocument>.Update
            .SetOnInsert(static migration => migration.StartedAtUtc, startedAtUtc);
    }

    internal static FilterDefinition<ParkDocument> BuildPortfolioParkFilter()
    {
        return Builders<ParkDocument>.Filter.Ne(static park => park.Id, string.Empty);
    }

    internal static UpdateDefinition<ParkFitOperationalStatusDocument> BuildDefaultStatusUpsert(
        DateTime migratedAtUtc)
    {
        return Builders<ParkFitOperationalStatusDocument>.Update
            .SetOnInsert(static status => status.State, ParkFitRecommendationState.NotActivated)
            .SetOnInsert(static status => status.Revision, 0)
            .SetOnInsert(
                static status => status.Decisions,
                new List<ParkFitOperationalDecisionDocument>())
            .SetOnInsert(static status => status.CreatedAt, migratedAtUtc)
            .SetOnInsert(static status => status.UpdatedAt, migratedAtUtc);
    }
}
