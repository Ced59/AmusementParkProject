using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class NotificationDeliveryAttemptRepository
    : INotificationDeliveryAttemptRepository
{
    private readonly IMongoCollection<NotificationDeliveryAttemptDocument> collection;

    public NotificationDeliveryAttemptRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal NotificationDeliveryAttemptRepository(
        IMongoCollection<NotificationDeliveryAttemptDocument> collection)
    {
        this.collection = collection;
    }

    public async Task<NotificationDeliveryAttempt?> GetAsync(
        string attemptId,
        CancellationToken cancellationToken)
    {
        string normalizedAttemptId = IdentifierRules.NormalizeRequired(attemptId, nameof(attemptId));
        NotificationDeliveryAttemptDocument? document = await this.collection.Find(
            Builders<NotificationDeliveryAttemptDocument>.Filter.Eq(
                static item => item.Id,
                normalizedAttemptId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<NotificationDeliveryAttemptWriteOutcome> CreateAsync(
        NotificationDeliveryAttempt attempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        try
        {
            await this.collection.InsertOneAsync(
                attempt.ToDocument(),
                cancellationToken: cancellationToken);
            return NotificationDeliveryAttemptWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return NotificationDeliveryAttemptWriteOutcome.Conflict;
        }
    }

    public async Task<NotificationDeliveryAttemptWriteOutcome> ReplaceAsync(
        NotificationDeliveryAttempt attempt,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            Builders<NotificationDeliveryAttemptDocument>.Filter.Eq(
                static item => item.Id,
                attempt.Id)
            & Builders<NotificationDeliveryAttemptDocument>.Filter.Eq(
                static item => item.Version,
                expectedVersion),
            attempt.ToDocument(),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1
            ? NotificationDeliveryAttemptWriteOutcome.Success
            : NotificationDeliveryAttemptWriteOutcome.Conflict;
    }

    public async Task<NotificationDeliveryMetricsResult> GetMetricsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<NotificationDeliveryAttemptDocument> filters =
            Builders<NotificationDeliveryAttemptDocument>.Filter;
        FilterDefinition<NotificationDeliveryAttemptDocument> periodFilter =
            filters.Gte(static item => item.CreatedAt, fromUtc)
            & filters.Lt(static item => item.CreatedAt, toUtc);
        Task<long> pendingTask = this.collection.CountDocumentsAsync(
            periodFilter & filters.Eq(static item => item.Status, NotificationDeliveryAttemptStatus.Pending),
            cancellationToken: cancellationToken);
        Task<long> failedTask = this.collection.CountDocumentsAsync(
            periodFilter & filters.Eq(static item => item.Status, NotificationDeliveryAttemptStatus.Failed),
            cancellationToken: cancellationToken);
        Task<long> succeededTask = this.collection.CountDocumentsAsync(
            periodFilter & filters.Eq(static item => item.Status, NotificationDeliveryAttemptStatus.Succeeded),
            cancellationToken: cancellationToken);
        Task<long> cancelledTask = this.collection.CountDocumentsAsync(
            periodFilter & filters.Eq(static item => item.Status, NotificationDeliveryAttemptStatus.Cancelled),
            cancellationToken: cancellationToken);
        Task<DateTime?> latestFailureTask = this.collection.Find(
                periodFilter & filters.Eq(static item => item.Status, NotificationDeliveryAttemptStatus.Failed))
            .SortByDescending(static item => item.UpdatedAt)
            .Project(static item => (DateTime?)item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        await Task.WhenAll(
            pendingTask,
            failedTask,
            succeededTask,
            cancelledTask,
            latestFailureTask);
        return new NotificationDeliveryMetricsResult(
            fromUtc,
            toUtc,
            pendingTask.Result,
            failedTask.Result,
            succeededTask.Result,
            cancelledTask.Result,
            latestFailureTask.Result);
    }

    public async Task DeleteAsync(string attemptId, CancellationToken cancellationToken)
    {
        string normalizedAttemptId = IdentifierRules.NormalizeRequired(attemptId, nameof(attemptId));
        await this.collection.DeleteOneAsync(
            Builders<NotificationDeliveryAttemptDocument>.Filter.Eq(
                static item => item.Id,
                normalizedAttemptId),
            cancellationToken);
    }

    private static IMongoCollection<NotificationDeliveryAttemptDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<NotificationDeliveryAttemptDocument>(
            settings.NotificationDeliveryAttemptsCollectionName);
    }
}
