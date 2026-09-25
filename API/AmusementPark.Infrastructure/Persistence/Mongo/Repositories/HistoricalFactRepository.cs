using System.Globalization;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalFactRepository : IHistoricalFactRepository
{
    private readonly IMongoCollection<HistoricalFactDocument> collection;

    public HistoricalFactRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
    }

    public async Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalFact fact,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fact);
        HistoricalFactDocument candidate = fact.ToDocument();
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            return HistoricalRevisionWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            HistoricalFactDocument? existing = await this.collection
                .Find(document => document.Id == candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return DocumentsMatch(existing, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }
    }

    public async Task<HistoricalFact?> GetRevisionAsync(
        Guid factId,
        int revision,
        CancellationToken cancellationToken)
    {
        if (factId == Guid.Empty || revision <= 0)
        {
            return null;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalFactDocument? document = await this.collection
            .Find(item => item.FactId == normalizedFactId && item.Revision == revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<HistoricalFact?> GetLatestRevisionAsync(
        Guid factId,
        CancellationToken cancellationToken)
    {
        if (factId == Guid.Empty)
        {
            return null;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalFactDocument? document = await this.collection
            .Find(item => item.FactId == normalizedFactId)
            .SortByDescending(item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    private static bool DocumentsMatch(
        HistoricalFactDocument? existing,
        HistoricalFactDocument candidate)
    {
        return existing is not null
            && existing.ToBsonDocument().Equals(candidate.ToBsonDocument());
    }
}
