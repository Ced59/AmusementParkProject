using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed class HistoricalFactPublicProjectionMigration
{
    private const int WriteBatchSize = 500;
    private readonly IMongoCollection<HistoricalFactDocument> factCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemCollection;
    private readonly IMongoCollection<ParkZoneDocument> parkZoneCollection;
    private readonly IMongoCollection<HistoricalSubjectScopeDocument> subjectScopeCollection;
    private readonly IMongoCollection<BsonDocument> narrativeCollection;

    public HistoricalFactPublicProjectionMigration(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.factCollection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
        this.parkItemCollection = database.GetCollection<ParkItemDocument>(
            settings.ParkItemsCollectionName);
        this.parkZoneCollection = database.GetCollection<ParkZoneDocument>(
            settings.ParkZonesCollectionName);
        this.subjectScopeCollection = database.GetCollection<HistoricalSubjectScopeDocument>(
            settings.HistoricalSubjectScopesCollectionName);
        this.narrativeCollection = database.GetCollection<BsonDocument>(
            settings.HistoricalNarrativesCollectionName);
    }

    public async Task<long> ExecuteAsync(CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<HistoricalFactDocument> builder =
            Builders<HistoricalFactDocument>.Filter;
        FilterDefinition<HistoricalFactDocument> contextualSubject = builder.In(
            "subject.type",
            new[]
            {
                HistoricalSubjectType.Park.ToString(),
                HistoricalSubjectType.ParkItem.ToString(),
                HistoricalSubjectType.ParkZone.ToString(),
            });
        FilterDefinition<HistoricalFactDocument> requiresMigration = builder.Or(
            builder.Exists("timelineSortOrdinal", false),
            contextualSubject & builder.Exists("subject.contextParkId", false));
        List<HistoricalFactDocument> facts = await this.factCollection
            .Find(requiresMigration)
            .ToListAsync(cancellationToken);
        if (facts.Count == 0)
        {
            return 0;
        }

        Dictionary<string, string> parkItemScopes = await this.LoadParkItemScopesAsync(cancellationToken);
        Dictionary<string, string> parkZoneScopes = await this.LoadParkZoneScopesAsync(cancellationToken);
        Dictionary<(HistoricalSubjectType Type, string Id), string> retainedScopes =
            await this.LoadRetainedScopesAsync(cancellationToken);
        Dictionary<(HistoricalSubjectType Type, string Id), string> narrativeScopes =
            await this.LoadNarrativeScopesAsync(cancellationToken);
        List<WriteModel<HistoricalFactDocument>> writes = new List<WriteModel<HistoricalFactDocument>>(
            Math.Min(facts.Count, WriteBatchSize));
        long modifiedCount = 0;
        foreach (HistoricalFactDocument fact in facts)
        {
            List<UpdateDefinition<HistoricalFactDocument>> updates =
                new List<UpdateDefinition<HistoricalFactDocument>>
                {
                    Builders<HistoricalFactDocument>.Update.Set(
                        static document => document.TimelineSortOrdinal,
                        HistoricalTimelineOrdering.ResolveDayNumber(fact.ToDomain().Period)),
                };
            string? contextParkId = ResolveContextParkId(
                fact.Subject,
                parkItemScopes,
                parkZoneScopes,
                retainedScopes,
                narrativeScopes);
            if (!string.IsNullOrWhiteSpace(contextParkId))
            {
                updates.Add(Builders<HistoricalFactDocument>.Update.Set(
                    "subject.contextParkId",
                    contextParkId));
            }

            writes.Add(new UpdateOneModel<HistoricalFactDocument>(
                builder.Eq(static document => document.Id, fact.Id),
                Builders<HistoricalFactDocument>.Update.Combine(updates)));
            if (writes.Count == WriteBatchSize)
            {
                modifiedCount += await this.FlushAsync(writes, cancellationToken);
            }
        }

        modifiedCount += await this.FlushAsync(writes, cancellationToken);
        return modifiedCount;
    }

    internal static string? ResolveContextParkId(
        HistoricalSubjectDocument subject,
        IReadOnlyDictionary<string, string> parkItemScopes,
        IReadOnlyDictionary<string, string> parkZoneScopes,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id), string> retainedScopes,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id), string> narrativeScopes)
    {
        if (!string.IsNullOrWhiteSpace(subject.ContextParkId))
        {
            return subject.ContextParkId;
        }

        return subject.Type switch
        {
            HistoricalSubjectType.Park => subject.Id,
            HistoricalSubjectType.ParkItem => parkItemScopes.GetValueOrDefault(subject.Id)
                ?? retainedScopes.GetValueOrDefault((HistoricalSubjectType.ParkItem, subject.Id))
                ?? narrativeScopes.GetValueOrDefault((HistoricalSubjectType.ParkItem, subject.Id)),
            HistoricalSubjectType.ParkZone => parkZoneScopes.GetValueOrDefault(subject.Id)
                ?? retainedScopes.GetValueOrDefault((HistoricalSubjectType.ParkZone, subject.Id)),
            _ => null,
        };
    }

    private async Task<Dictionary<(HistoricalSubjectType Type, string Id), string>> LoadRetainedScopesAsync(
        CancellationToken cancellationToken)
    {
        List<HistoricalSubjectScopeDocument> scopes = await this.subjectScopeCollection
            .Find(Builders<HistoricalSubjectScopeDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);
        return scopes
            .Where(static scope => Enum.IsDefined(scope.SubjectType)
                && !string.IsNullOrWhiteSpace(scope.SubjectId)
                && !string.IsNullOrWhiteSpace(scope.ContextParkId))
            .GroupBy(static scope => (scope.SubjectType, scope.SubjectId))
            .Select(static group => new
            {
                Subject = group.Key,
                ParkIds = group.Select(static scope => scope.ContextParkId)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
            })
            .Where(static scope => scope.ParkIds.Length == 1)
            .ToDictionary(
                static scope => scope.Subject,
                static scope => scope.ParkIds[0],
                EqualityComparer<(HistoricalSubjectType Type, string Id)>.Default);
    }

    private async Task<Dictionary<string, string>> LoadParkItemScopesAsync(
        CancellationToken cancellationToken)
    {
        List<ParkItemDocument> items = await this.parkItemCollection
            .Find(Builders<ParkItemDocument>.Filter.Empty)
            .Project<ParkItemDocument>(Builders<ParkItemDocument>.Projection
                .Include(static item => item.Id)
                .Include(static item => item.ParkId))
            .ToListAsync(cancellationToken);
        return items
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.ParkId))
            .DistinctBy(static item => item.Id, StringComparer.Ordinal)
            .ToDictionary(static item => item.Id, static item => item.ParkId, StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, string>> LoadParkZoneScopesAsync(
        CancellationToken cancellationToken)
    {
        List<ParkZoneDocument> zones = await this.parkZoneCollection
            .Find(Builders<ParkZoneDocument>.Filter.Empty)
            .Project<ParkZoneDocument>(Builders<ParkZoneDocument>.Projection
                .Include(static zone => zone.Id)
                .Include(static zone => zone.ParkId))
            .ToListAsync(cancellationToken);
        return zones
            .Where(static zone => !string.IsNullOrWhiteSpace(zone.Id)
                && !string.IsNullOrWhiteSpace(zone.ParkId))
            .DistinctBy(static zone => zone.Id, StringComparer.Ordinal)
            .ToDictionary(static zone => zone.Id, static zone => zone.ParkId, StringComparer.Ordinal);
    }

    private async Task<Dictionary<(HistoricalSubjectType Type, string Id), string>> LoadNarrativeScopesAsync(
        CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> scopedNarratives =
            Builders<BsonDocument>.Filter.Eq(
                "entityType",
                HistoryEntityType.ParkItem.ToString());
        List<BsonDocument> narratives = await this.narrativeCollection
            .Find(scopedNarratives)
            .Project(Builders<BsonDocument>.Projection
                .Include("entityType")
                .Include("ownerId")
                .Include("contextParkId")
                .Include("parkId"))
            .ToListAsync(cancellationToken);
        return narratives
            .Select(static narrative => new
            {
                SubjectType = ResolveNarrativeSubjectType(ReadString(narrative, "entityType")),
                SubjectId = ReadString(narrative, "ownerId"),
                ParkId = ReadString(narrative, "contextParkId")
                    ?? ReadString(narrative, "parkId"),
            })
            .Where(static scope => scope.SubjectType.HasValue
                && scope.SubjectId is not null
                && scope.ParkId is not null)
            .GroupBy(static scope => (scope.SubjectType!.Value, scope.SubjectId!))
            .Select(static group => new
            {
                Subject = group.Key,
                ParkIds = group.Select(static scope => scope.ParkId!)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
            })
            .Where(static scope => scope.ParkIds.Length == 1)
            .ToDictionary(
                static scope => scope.Subject,
                static scope => scope.ParkIds[0],
                EqualityComparer<(HistoricalSubjectType Type, string Id)>.Default);
    }

    private async Task<long> FlushAsync(
        List<WriteModel<HistoricalFactDocument>> writes,
        CancellationToken cancellationToken)
    {
        if (writes.Count == 0)
        {
            return 0;
        }

        BulkWriteResult<HistoricalFactDocument> result = await this.factCollection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        writes.Clear();
        return result.ModifiedCount;
    }

    private static string? ReadString(BsonDocument document, string fieldName)
    {
        BsonValue value = document.GetValue(fieldName, BsonNull.Value);
        return value.IsString && !string.IsNullOrWhiteSpace(value.AsString)
            ? value.AsString.Trim()
            : null;
    }

    private static HistoricalSubjectType? ResolveNarrativeSubjectType(string? entityType)
    {
        return string.Equals(entityType, HistoryEntityType.ParkItem.ToString(), StringComparison.Ordinal)
            ? HistoricalSubjectType.ParkItem
            : null;
    }
}
