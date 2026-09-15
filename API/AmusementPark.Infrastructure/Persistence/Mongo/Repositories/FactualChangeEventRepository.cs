using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class FactualChangeEventRepository : IFactualChangeEventRepository
{
    private readonly IMongoCollection<FactualChangeEventDocument> collection;

    public FactualChangeEventRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal FactualChangeEventRepository(
        IMongoCollection<FactualChangeEventDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<FactualChangeEventWriteDisposition> CreateAsync(
        FactualChangeEvent factualEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        try
        {
            await this.collection.InsertOneAsync(
                factualEvent.ToDocument(),
                cancellationToken: cancellationToken);
            return FactualChangeEventWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            FactualChangeEvent? existing = await this.GetByLogicalRevisionAsync(
                factualEvent.DeduplicationKey,
                factualEvent.Revision,
                cancellationToken);
            return existing is not null && HasSameFact(existing, factualEvent)
                ? FactualChangeEventWriteDisposition.AlreadyExists
                : FactualChangeEventWriteDisposition.Conflict;
        }
    }

    public async Task<FactualChangeEvent?> GetByLogicalRevisionAsync(
        string deduplicationKey,
        long sourceRevision,
        CancellationToken cancellationToken)
    {
        string normalizedKey = deduplicationKey?.Trim() ?? string.Empty;
        if (normalizedKey.Length == 0)
        {
            throw new ArgumentException("A deduplication key is required.", nameof(deduplicationKey));
        }

        if (sourceRevision < 1)
        {
            return null;
        }

        FilterDefinition<FactualChangeEventDocument> filter =
            Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.DeduplicationKey,
                normalizedKey)
            & Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Revision,
                sourceRevision);
        FactualChangeEventDocument? document = await this.collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    private static bool HasSameFact(FactualChangeEvent left, FactualChangeEvent right)
    {
        return left.Id == right.Id
            && left.Type == right.Type
            && left.DefinitionVersion == right.DefinitionVersion
            && left.Target == right.Target
            && left.PreviousValue == right.PreviousValue
            && left.NewValue == right.NewValue
            && left.Source == right.Source
            && left.Confidence == right.Confidence
            && left.OccurredAtUtc == right.OccurredAtUtc
            && left.HasSameLogicalRevisionAs(right)
            && left.Status == right.Status;
    }

    private static IMongoCollection<FactualChangeEventDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<FactualChangeEventDocument>(
            settings.FactualChangeEventsCollectionName);
    }
}
