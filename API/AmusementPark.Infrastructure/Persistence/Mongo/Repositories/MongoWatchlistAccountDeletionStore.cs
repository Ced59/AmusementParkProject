using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoWatchlistAccountDeletionStore : IWatchlistAccountDeletionStore
{
    private readonly IMongoCollection<BsonDocument> collectionEntries;
    private readonly IMongoCollection<BsonDocument> subscriptions;
    private readonly IMongoCollection<BsonDocument> notifications;
    private readonly IMongoCollection<BsonDocument> digests;
    private readonly IMongoCollection<BsonDocument> emailPreferences;
    private readonly IMongoCollection<BsonDocument> deliveryAttempts;
    private readonly IMongoCollection<DurableBackgroundJobDocument> backgroundJobs;

    public MongoWatchlistAccountDeletionStore(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collectionEntries = database.GetCollection<BsonDocument>(
            settings.UserCollectionEntriesCollectionName);
        this.subscriptions = database.GetCollection<BsonDocument>(
            settings.WatchSubscriptionsCollectionName);
        this.notifications = database.GetCollection<BsonDocument>(
            settings.UserNotificationsCollectionName);
        this.digests = database.GetCollection<BsonDocument>(
            settings.NotificationDigestsCollectionName);
        this.emailPreferences = database.GetCollection<BsonDocument>(
            settings.NotificationPreferencesCollectionName);
        this.deliveryAttempts = database.GetCollection<BsonDocument>(
            settings.NotificationDeliveryAttemptsCollectionName);
        this.backgroundJobs = database.GetCollection<DurableBackgroundJobDocument>(
            settings.DurableBackgroundJobsCollectionName);
    }

    public async Task<WatchlistAccountDeletionResult> PurgeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string[] digestIds = await this.LoadDigestIdsAsync(normalizedUserId, cancellationToken);
        string[] ownedJobIds = await this.LoadOwnedJobIdsAsync(
            normalizedUserId,
            digestIds,
            cancellationToken);

        long emailPreferenceCount = await DeleteByUserAsync(
            this.emailPreferences,
            normalizedUserId,
            cancellationToken);
        long subscriptionCount = await DeleteByUserAsync(
            this.subscriptions,
            normalizedUserId,
            cancellationToken);
        long backgroundJobCount = await this.DeleteJobsAsync(ownedJobIds, cancellationToken);
        long deliveryAttemptCount = await DeleteByUserAsync(
            this.deliveryAttempts,
            normalizedUserId,
            cancellationToken);
        long digestCount = await DeleteByUserAsync(
            this.digests,
            normalizedUserId,
            cancellationToken);
        long notificationCount = await DeleteByUserAsync(
            this.notifications,
            normalizedUserId,
            cancellationToken);
        long collectionEntryCount = await DeleteByUserAsync(
            this.collectionEntries,
            normalizedUserId,
            cancellationToken);

        return new WatchlistAccountDeletionResult(
            collectionEntryCount,
            subscriptionCount,
            notificationCount,
            digestCount,
            emailPreferenceCount,
            deliveryAttemptCount,
            backgroundJobCount);
    }

    internal static bool IsOwnedDigestJob(
        DurableBackgroundJobDocument document,
        string userId)
    {
        if (!string.Equals(document.Kind, NotificationDigestJob.Kind, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            NotificationDigestJobPayload? payload =
                JsonSerializer.Deserialize<NotificationDigestJobPayload>(document.PayloadJson);
            return payload is not null
                && string.Equals(payload.UserId, userId, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<string[]> LoadDigestIdsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> documents = await this.digests
            .Find(Builders<BsonDocument>.Filter.Eq("userId", userId))
            .Project(Builders<BsonDocument>.Projection.Include("_id"))
            .ToListAsync(cancellationToken);
        return documents
            .Where(static document =>
                document.TryGetValue("_id", out BsonValue? value) && value.IsString)
            .Select(static document => document["_id"].AsString)
            .ToArray();
    }

    private async Task<string[]> LoadOwnedJobIdsAsync(
        string userId,
        IReadOnlyCollection<string> digestIds,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters =
            Builders<DurableBackgroundJobDocument>.Filter;
        List<FilterDefinition<DurableBackgroundJobDocument>> candidates =
            new List<FilterDefinition<DurableBackgroundJobDocument>>()
        {
            BuildOwnedDigestJobCandidateFilter(userId),
        };
        if (digestIds.Count > 0)
        {
            string[] emailNaturalKeys = digestIds
                .Select(static digestId => $"watch-email:{digestId}")
                .ToArray();
            candidates.Add(
                filters.Eq(static document => document.Kind, NotificationEmailDeliveryJob.Kind)
                & filters.In(static document => document.NaturalKey, emailNaturalKeys));
        }

        List<DurableBackgroundJobDocument> jobs = await this.backgroundJobs
            .Find(filters.Or(candidates))
            .ToListAsync(cancellationToken);
        return jobs
            .Where(job =>
                IsOwnedDigestJob(job, userId)
                || digestIds.Contains(
                    RemoveEmailNaturalKeyPrefix(job.NaturalKey),
                    StringComparer.Ordinal))
            .Select(static job => job.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    internal static FilterDefinition<DurableBackgroundJobDocument>
        BuildOwnedDigestJobCandidateFilter(string userId)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters =
            Builders<DurableBackgroundJobDocument>.Filter;
        string serializedUserId = JsonSerializer.Serialize(userId);
        string literalPayloadFragment = $"\"UserId\":{serializedUserId}";
        return filters.Eq(static document => document.Kind, NotificationDigestJob.Kind)
            & filters.Regex(
                static document => document.PayloadJson,
                new BsonRegularExpression(Regex.Escape(literalPayloadFragment)));
    }

    private async Task<long> DeleteJobsAsync(
        IReadOnlyCollection<string> jobIds,
        CancellationToken cancellationToken)
    {
        if (jobIds.Count == 0)
        {
            return 0;
        }

        DeleteResult result = await this.backgroundJobs.DeleteManyAsync(
            Builders<DurableBackgroundJobDocument>.Filter.In(
                static document => document.Id,
                jobIds),
            cancellationToken);
        return result.DeletedCount;
    }

    private static async Task<long> DeleteByUserAsync(
        IMongoCollection<BsonDocument> collection,
        string userId,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await collection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Eq("userId", userId),
            cancellationToken);
        return result.DeletedCount;
    }

    private static string RemoveEmailNaturalKeyPrefix(string? naturalKey)
    {
        const string prefix = "watch-email:";
        return naturalKey?.StartsWith(prefix, StringComparison.Ordinal) == true
            ? naturalKey[prefix.Length..]
            : string.Empty;
    }
}
