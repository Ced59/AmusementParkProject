using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FeatureFlags;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class FeatureFlagStateRepository : IFeatureFlagStateRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private readonly IMongoCollection<FeatureFlagStateDocument> collection;
    private readonly IMemoryCache cache;

    public FeatureFlagStateRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHostEnvironment hostEnvironment,
        IMemoryCache cache)
    {
        this.collection = database.GetCollection<FeatureFlagStateDocument>(
            settings.FeatureFlagStatesCollectionName);
        this.Environment = hostEnvironment.EnvironmentName;
        this.cache = cache;
    }

    public string Environment { get; }

    public async Task<FeatureFlagState?> GetLatestAsync(
        string key,
        CancellationToken cancellationToken)
    {
        string cacheKey = BuildCacheKey(this.Environment, key);
        if (this.cache.TryGetValue(cacheKey, out FeatureFlagState? cached))
        {
            return cached;
        }

        FeatureFlagState? state = await this.LoadLatestAsync(key, cancellationToken);
        this.cache.Set(cacheKey, state, CacheDuration);
        return state;
    }

    public async Task<FeatureFlagWriteOutcome> AppendRevisionAsync(
        FeatureFlagState state,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (expectedRevision < 0
            || state.Revision != expectedRevision + 1
            || !string.Equals(state.Environment, this.Environment, StringComparison.Ordinal))
        {
            return FeatureFlagWriteOutcome.Conflict;
        }

        FeatureFlagState? current = await this.LoadLatestAsync(state.Key, cancellationToken);
        if ((current is null && expectedRevision != 0)
            || (current is not null && (current.Id != state.Id
                || current.Revision != expectedRevision)))
        {
            this.cache.Remove(BuildCacheKey(this.Environment, state.Key));
            return FeatureFlagWriteOutcome.Conflict;
        }

        FeatureFlagStateDocument document = ToDocument(state);
        try
        {
            await this.collection.InsertOneAsync(
                document,
                cancellationToken: cancellationToken);
            this.cache.Remove(BuildCacheKey(this.Environment, state.Key));
            return FeatureFlagWriteOutcome.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            this.cache.Remove(BuildCacheKey(this.Environment, state.Key));
            return FeatureFlagWriteOutcome.Conflict;
        }
    }

    private async Task<FeatureFlagState?> LoadLatestAsync(
        string key,
        CancellationToken cancellationToken)
    {
        FeatureFlagStateDocument? document = await this.collection
            .Find(item => item.Environment == this.Environment && item.Key == key)
            .SortByDescending(static item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToModel(document);
    }

    private static FeatureFlagState ToModel(FeatureFlagStateDocument document)
    {
        return new FeatureFlagState(
            Guid.Parse(document.FeatureFlagId),
            document.Key,
            document.Environment,
            document.EnabledOverride,
            document.Revision,
            document.SupersedesRevision,
            document.ChangedByUserId,
            document.Reason,
            document.RecordedAtUtc);
    }

    private static FeatureFlagStateDocument ToDocument(FeatureFlagState state)
    {
        return new FeatureFlagStateDocument
        {
            Id = Guid.NewGuid().ToString(),
            FeatureFlagId = state.Id.ToString(),
            Key = state.Key,
            Environment = state.Environment,
            EnabledOverride = state.EnabledOverride,
            Revision = state.Revision,
            SupersedesRevision = state.SupersedesRevision,
            ChangedByUserId = state.ChangedByUserId,
            Reason = state.Reason,
            RecordedAtUtc = state.RecordedAtUtc,
            CreatedAt = state.RecordedAtUtc,
            UpdatedAt = state.RecordedAtUtc,
        };
    }

    private static string BuildCacheKey(string environment, string key)
    {
        return $"feature-flag:{environment}:{key}";
    }
}
