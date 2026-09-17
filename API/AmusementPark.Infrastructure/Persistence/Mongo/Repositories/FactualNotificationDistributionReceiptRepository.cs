using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class FactualNotificationDistributionReceiptRepository
    : IFactualNotificationDistributionReceiptRepository,
        IFactualChangeEventDistributionStateReader
{
    private readonly IMongoCollection<FactualNotificationDistributionReceiptDocument> collection;

    public FactualNotificationDistributionReceiptRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<FactualNotificationDistributionReceiptDocument>(
            settings.FactualNotificationDistributionsCollectionName);
    }

    public Task<bool> IsCompletedAsync(string eventId, CancellationToken cancellationToken)
    {
        string normalizedEventId = eventId?.Trim() ?? string.Empty;
        if (normalizedEventId.Length == 0)
        {
            throw new ArgumentException("An event identifier is required.", nameof(eventId));
        }

        return this.collection.Find(
            Builders<FactualNotificationDistributionReceiptDocument>.Filter.Eq(
                static document => document.Id,
                normalizedEventId))
            .Limit(1)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> IsInitialDistributionCompletedAsync(
        string eventId,
        CancellationToken cancellationToken)
    {
        return this.IsCompletedAsync(eventId, cancellationToken);
    }

    public async Task CompleteAsync(
        FactualNotificationDistributionReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        FactualNotificationDistributionReceiptDocument document = new FactualNotificationDistributionReceiptDocument
        {
            Id = receipt.EventId.Trim(),
            CompletedAt = receipt.CompletedAtUtc,
            CreatedAt = receipt.CompletedAtUtc,
            UpdatedAt = receipt.CompletedAtUtc,
        };
        await this.collection.ReplaceOneAsync(
            Builders<FactualNotificationDistributionReceiptDocument>.Filter.Eq(
                static item => item.Id,
                document.Id),
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
