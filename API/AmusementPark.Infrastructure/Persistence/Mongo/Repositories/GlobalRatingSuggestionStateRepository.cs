using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class GlobalRatingSuggestionStateRepository
    : IGlobalRatingSuggestionStateRepository,
        IGlobalRatingSuggestionAnalyticsOutboxReconciler
{
    private readonly IMongoCollection<GlobalRatingSuggestionStateDocument> states;
    private readonly IMongoCollection<GlobalRatingSuggestionPreferenceDocument> preferences;
    private readonly IMongoCollection<GlobalRatingSuggestionInteractionDocument> interactions;

    public GlobalRatingSuggestionStateRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<GlobalRatingSuggestionStateDocument>(
                settings.GlobalRatingSuggestionStatesCollectionName),
            database.GetCollection<GlobalRatingSuggestionPreferenceDocument>(
                settings.GlobalRatingSuggestionPreferencesCollectionName),
            database.GetCollection<GlobalRatingSuggestionInteractionDocument>(
                settings.GlobalRatingSuggestionInteractionsCollectionName))
    {
    }

    internal GlobalRatingSuggestionStateRepository(
        IMongoCollection<GlobalRatingSuggestionStateDocument> states,
        IMongoCollection<GlobalRatingSuggestionPreferenceDocument> preferences,
        IMongoCollection<GlobalRatingSuggestionInteractionDocument> interactions)
    {
        this.states = states ?? throw new ArgumentNullException(nameof(states));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
    }

    public async Task<bool> IsEnabledAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        GlobalRatingSuggestionPreferenceDocument? preference = await this.preferences
            .Find(document => document.UserId == normalizedUserId)
            .FirstOrDefaultAsync(cancellationToken);
        return preference?.IsEnabled ?? true;
    }

    public async Task<IReadOnlyCollection<GlobalRatingSuggestionTargetState>> GetStatesAsync(
        string userId,
        IReadOnlyCollection<GlobalRatingSuggestionTargetKey> targets,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0)
        {
            return Array.Empty<GlobalRatingSuggestionTargetState>();
        }

        HashSet<string> targetIds = targets.Select(static target => target.TargetId)
            .ToHashSet(StringComparer.Ordinal);
        List<GlobalRatingSuggestionStateDocument> documents = await this.states.Find(
                Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                    static document => document.UserId,
                    normalizedUserId)
                & Builders<GlobalRatingSuggestionStateDocument>.Filter.In(
                    static document => document.TargetId,
                    targetIds))
            .ToListAsync(cancellationToken);
        HashSet<GlobalRatingSuggestionTargetKey> requested = targets.ToHashSet();
        return documents
            .Where(document => requested.Contains(new GlobalRatingSuggestionTargetKey(
                document.TargetType,
                document.TargetId)))
            .Select(static document => new GlobalRatingSuggestionTargetState(
                document.TargetType,
                document.TargetId,
                document.LastPresentedAtUtc,
                document.LastAcceptedAtUtc,
                document.LastDismissedAtUtc,
                document.IsAwaitingResolution))
            .ToArray();
    }

    public Task SetEnabledAsync(
        string userId,
        bool isEnabled,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        FilterDefinition<GlobalRatingSuggestionPreferenceDocument> filter =
            Builders<GlobalRatingSuggestionPreferenceDocument>.Filter.Eq(
                static document => document.UserId,
                normalizedUserId);
        UpdateDefinition<GlobalRatingSuggestionPreferenceDocument> update =
            Builders<GlobalRatingSuggestionPreferenceDocument>.Update
                .SetOnInsert(static document => document.Id, Guid.NewGuid().ToString("N"))
                .SetOnInsert(static document => document.CreatedAt, updatedAtUtc)
                .Set(static document => document.UserId, normalizedUserId)
                .Set(static document => document.IsEnabled, isEnabled)
                .Set(static document => document.UpdatedAt, updatedAtUtc);
        return this.preferences.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<bool> TryRecordInteractionAsync(
        string userId,
        RatingTargetType targetType,
        string targetId,
        DateTime? expectedLastPresentedAtUtc,
        GlobalRatingSuggestionInteractionType interactionType,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        ValidateUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (expectedLastPresentedAtUtc.HasValue)
        {
            ValidateUtc(expectedLastPresentedAtUtc.Value, nameof(expectedLastPresentedAtUtc));
        }

        if (!Enum.IsDefined(targetType) || !Enum.IsDefined(interactionType))
        {
            throw new ArgumentOutOfRangeException(nameof(interactionType));
        }

        FilterDefinition<GlobalRatingSuggestionStateDocument> filter =
            Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                static document => document.UserId,
                normalizedUserId)
            & Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                static document => document.TargetType,
                targetType)
            & Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                static document => document.TargetId,
                normalizedTargetId);
        filter &= Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
            static document => document.LastPresentedAtUtc,
            expectedLastPresentedAtUtc);
        UpdateDefinitionBuilder<GlobalRatingSuggestionStateDocument> updates =
            Builders<GlobalRatingSuggestionStateDocument>.Update;
        GlobalRatingSuggestionPendingAnalyticsEventDocument pendingAnalyticsEvent = new()
        {
            EventId = Guid.NewGuid().ToString("N"),
            InteractionType = interactionType,
            OccurredAtUtc = occurredAtUtc,
        };
        UpdateDefinition<GlobalRatingSuggestionStateDocument> update = updates
            .SetOnInsert(static document => document.Id, Guid.NewGuid().ToString("N"))
            .SetOnInsert(static document => document.CreatedAt, occurredAtUtc)
            .Set(static document => document.UserId, normalizedUserId)
            .Set(static document => document.TargetType, targetType)
            .Set(static document => document.TargetId, normalizedTargetId)
            .Set(static document => document.UpdatedAt, occurredAtUtc)
            .Push(static document => document.PendingAnalyticsEvents, pendingAnalyticsEvent);
        bool isPresentation = interactionType == GlobalRatingSuggestionInteractionType.Presented;
        if (isPresentation)
        {
            update = update
                .Set(static document => document.LastPresentedAtUtc, occurredAtUtc)
                .Set(static document => document.IsAwaitingResolution, true);
        }
        else
        {
            if (!expectedLastPresentedAtUtc.HasValue)
            {
                return false;
            }

            filter &= Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                static document => document.IsAwaitingResolution,
                true);
            update = update.Set(
                static document => document.IsAwaitingResolution,
                false);
        }

        if (interactionType == GlobalRatingSuggestionInteractionType.Accepted)
        {
            update = update.Set(static document => document.LastAcceptedAtUtc, occurredAtUtc);
        }
        else if (interactionType == GlobalRatingSuggestionInteractionType.Dismissed)
        {
            update = update.Set(static document => document.LastDismissedAtUtc, occurredAtUtc);
        }

        UpdateResult result;
        try
        {
            result = await this.states.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions
                {
                    IsUpsert = isPresentation && !expectedLastPresentedAtUtc.HasValue,
                },
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }

        if (result.MatchedCount == 0 && result.UpsertedId is null)
        {
            return false;
        }

        return true;
    }

    public async Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        if (maximumEventCount < 1 || maximumEventCount > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEventCount));
        }

        FilterDefinition<GlobalRatingSuggestionStateDocument> filter =
            Builders<GlobalRatingSuggestionStateDocument>.Filter.Exists(
                "pendingAnalyticsEvents.eventId",
                true);
        List<GlobalRatingSuggestionStateDocument> documents = await this.states
            .Find(filter)
            .Limit(maximumEventCount)
            .ToListAsync(cancellationToken);
        int reconciledCount = 0;
        foreach (GlobalRatingSuggestionStateDocument document in documents)
        {
            foreach (GlobalRatingSuggestionPendingAnalyticsEventDocument pendingEvent
                in document.PendingAnalyticsEvents)
            {
                if (reconciledCount == maximumEventCount)
                {
                    return reconciledCount;
                }

                await this.PublishPendingAnalyticsEventAsync(
                    document,
                    pendingEvent,
                    cancellationToken);
                reconciledCount++;
            }
        }

        return reconciledCount;
    }

    private async Task PublishPendingAnalyticsEventAsync(
        GlobalRatingSuggestionStateDocument state,
        GlobalRatingSuggestionPendingAnalyticsEventDocument pendingEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await this.interactions.InsertOneAsync(
                new GlobalRatingSuggestionInteractionDocument
                {
                    Id = pendingEvent.EventId,
                    UserCohortKey = HashUserId(state.UserId),
                    TargetType = state.TargetType,
                    InteractionType = pendingEvent.InteractionType,
                    OccurredAtUtc = pendingEvent.OccurredAtUtc,
                    CreatedAt = pendingEvent.OccurredAtUtc,
                    UpdatedAt = pendingEvent.OccurredAtUtc,
                },
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // The stable outbox event id makes a retry safe after a lost acknowledgement.
        }

        FilterDefinition<GlobalRatingSuggestionStateDocument> stateFilter =
            Builders<GlobalRatingSuggestionStateDocument>.Filter.Eq(
                static document => document.Id,
                state.Id);
        UpdateDefinition<GlobalRatingSuggestionStateDocument> removePendingEvent =
            Builders<GlobalRatingSuggestionStateDocument>.Update.PullFilter(
                static document => document.PendingAnalyticsEvents,
                candidate => candidate.EventId == pendingEvent.EventId);
        await this.states.UpdateOneAsync(
            stateFilter,
            removePendingEvent,
            cancellationToken: cancellationToken);
    }

    private static string HashUserId(string userId)
    {
        byte[] value = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexStringLower(value);
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be UTC.", parameterName);
        }
    }
}
