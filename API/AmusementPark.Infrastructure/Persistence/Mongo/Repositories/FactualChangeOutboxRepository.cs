using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class FactualChangeOutboxRepository : IFactualChangeOutboxRepository
{
    private readonly IMongoCollection<FactualChangeOutboxDocument> collection;

    public FactualChangeOutboxRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal FactualChangeOutboxRepository(
        IMongoCollection<FactualChangeOutboxDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<FactualChangeOutboxWriteResult> RecordAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            await this.collection.InsertOneAsync(
                entry.ToDocument(),
                cancellationToken: cancellationToken);
            return new FactualChangeOutboxWriteResult(
                FactualChangeOutboxWriteDisposition.Created,
                entry);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            FactualChangeOutboxDocument? existingDocument = await this.collection.Find(
                BuildLogicalRevisionFilter(entry.DeduplicationKey, entry.SourceRevision))
                .FirstOrDefaultAsync(cancellationToken);
            if (existingDocument is null)
            {
                return new FactualChangeOutboxWriteResult(
                    FactualChangeOutboxWriteDisposition.Conflict,
                    null);
            }

            FactualChangeOutboxEntry existing = existingDocument.ToDomain();
            return HasSameFact(existing, entry)
                ? new FactualChangeOutboxWriteResult(
                    FactualChangeOutboxWriteDisposition.AlreadyRecorded,
                    existing)
                : new FactualChangeOutboxWriteResult(
                    FactualChangeOutboxWriteDisposition.Conflict,
                    existing);
        }
    }

    public async Task<FactualChangeOutboxEntry?> GetAsync(
        string entryId,
        CancellationToken cancellationToken)
    {
        string normalizedEntryId = NormalizeIdentifier(entryId, nameof(entryId));
        FactualChangeOutboxDocument? document = await this.collection.Find(
            Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.Id,
                normalizedEntryId)).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<FactualChangeOutboxEntry>> ListPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount < 1 || maximumCount > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        FilterDefinition<FactualChangeOutboxDocument> filter =
            Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.MaterializedAtUtc,
                null);
        List<FactualChangeOutboxDocument> documents = await this.collection.Find(filter)
            .SortBy(static value => value.CreatedAt)
            .ThenBy(static value => value.Id)
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        return documents.Select(static value => value.ToDomain()).ToArray();
    }

    public async Task<bool> MarkMaterializedAsync(
        string entryId,
        string eventId,
        long expectedVersion,
        DateTime materializedAtUtc,
        CancellationToken cancellationToken)
    {
        string normalizedEntryId = NormalizeIdentifier(entryId, nameof(entryId));
        string normalizedEventId = NormalizeIdentifier(eventId, nameof(eventId));
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        }

        if (materializedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The materialization timestamp must be UTC.",
                nameof(materializedAtUtc));
        }

        FilterDefinition<FactualChangeOutboxDocument> filter =
            Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.Id,
                normalizedEntryId)
            & Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.EventId,
                normalizedEventId)
            & Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.Version,
                expectedVersion)
            & Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.MaterializedAtUtc,
                null);
        UpdateDefinition<FactualChangeOutboxDocument> update =
            Builders<FactualChangeOutboxDocument>.Update
                .Set(static value => value.MaterializedAtUtc, materializedAtUtc)
                .Set(static value => value.UpdatedAt, materializedAtUtc)
                .Set(static value => value.Version, expectedVersion + 1);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private static FilterDefinition<FactualChangeOutboxDocument> BuildLogicalRevisionFilter(
        string deduplicationKey,
        long sourceRevision)
    {
        return Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.DeduplicationKey,
                deduplicationKey)
            & Builders<FactualChangeOutboxDocument>.Filter.Eq(
                static value => value.SourceRevision,
                sourceRevision);
    }

    private static bool HasSameFact(
        FactualChangeOutboxEntry left,
        FactualChangeOutboxEntry right)
    {
        return left.Type == right.Type
            && left.DefinitionVersion == right.DefinitionVersion
            && left.Target == right.Target
            && left.PreviousValue == right.PreviousValue
            && left.NewValue == right.NewValue
            && left.Source == right.Source
            && left.Confidence == right.Confidence
            && left.OccurredAtUtc == right.OccurredAtUtc
            && string.Equals(
                left.DeduplicationKey,
                right.DeduplicationKey,
                StringComparison.Ordinal)
            && left.SourceRevision == right.SourceRevision;
    }

    private static string NormalizeIdentifier(string value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("An identifier is required.", parameterName);
        }

        return normalized;
    }

    private static IMongoCollection<FactualChangeOutboxDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<FactualChangeOutboxDocument>(
            settings.FactualChangeOutboxCollectionName);
    }
}
