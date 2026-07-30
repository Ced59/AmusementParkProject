using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Bail Mongo distribué empêchant une génération de variante de chevaucher un cleanup.
/// </summary>
public sealed class MongoImageVariantGenerationLease : IImageVariantGenerationLease
{
    private readonly IMongoCollection<ImageDocument> collection;

    public MongoImageVariantGenerationLease(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ImageDocument>(
            settings.ImagesCollectionName);
    }

    public async Task<bool> TryAcquireAsync(
        string pathWithoutExtension,
        string leaseToken,
        DateTime acquiredAtUtc,
        DateTime leaseUntilUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension)
            || string.IsNullOrWhiteSpace(leaseToken)
            || leaseUntilUtc <= acquiredAtUtc)
        {
            return false;
        }

        FilterDefinition<ImageDocument> filter = BuildAcquireFilter(
            pathWithoutExtension.Trim(),
            acquiredAtUtc);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.VariantGenerationClaimToken,
                    leaseToken)
                .Set(
                    static document => document.VariantGenerationClaimedUntil,
                    leaseUntilUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task ReleaseAsync(
        string pathWithoutExtension,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension)
            || string.IsNullOrWhiteSpace(leaseToken))
        {
            return;
        }

        FilterDefinition<ImageDocument> filter = BuildReleaseFilter(
            pathWithoutExtension.Trim(),
            leaseToken);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Unset(
                    static document => document.VariantGenerationClaimToken)
                .Unset(
                    static document => document.VariantGenerationClaimedUntil);
        await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    internal static FilterDefinition<ImageDocument> BuildAcquireFilter(
        string pathWithoutExtension,
        DateTime acquiredAtUtc)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> cleanupAvailableFilter =
            builder.Eq(static document => document.CleanupClaimToken, null)
            | builder.Lte(
                static document => document.CleanupClaimedUntil,
                acquiredAtUtc);
        FilterDefinition<ImageDocument> generationAvailableFilter =
            builder.Eq(
                static document => document.VariantGenerationClaimToken,
                null)
            | builder.Lte(
                static document => document.VariantGenerationClaimedUntil,
                acquiredAtUtc);

        return builder.Eq(
                static document => document.Path,
                pathWithoutExtension)
            & cleanupAvailableFilter
            & generationAvailableFilter;
    }

    internal static FilterDefinition<ImageDocument> BuildReleaseFilter(
        string pathWithoutExtension,
        string leaseToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        return builder.Eq(
                static document => document.Path,
                pathWithoutExtension)
            & builder.Eq(
                static document => document.VariantGenerationClaimToken,
                leaseToken);
    }
}
