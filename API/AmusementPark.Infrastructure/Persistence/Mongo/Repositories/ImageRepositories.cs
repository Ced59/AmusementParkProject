using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Geo;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des images.
/// </summary>
public sealed class ImageRepository : IImageRepository
{
    private static readonly TimeSpan OwnerImagesCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReservationMaximumLifetime =
        TimeSpan.FromHours(24);
    private static long cacheVersion;
    private readonly IMongoCollection<ImageDocument> collection;
    private readonly IMemoryCache cache;

    public ImageRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<ImageDocument>(settings.ImagesCollectionName);
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<Image>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ImageDocument> documents = await this.collection.Find(Builders<ImageDocument>.Filter.Empty)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<Image>> GetPageAsync(int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        FilterDefinition<ImageDocument> filter = BuildFilter(criteria);
        SortDefinition<ImageDocument> sort = BuildSort(criteria);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<ImageDocument> documents = await this.collection
            .Find(filter)
            .Sort(sort)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Image>(documents.Select(static document => document.ToDomain()).ToList(), safePage, safePageSize, totalItems);
    }

    public async Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken)
    {
        string cacheKey = BuildImageByIdCacheKey(imageId);
        if (this.cache.TryGetValue(cacheKey, out Image? cachedImage) && cachedImage is not null)
        {
            return cachedImage;
        }

        ImageDocument? document = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        Image? image = document?.ToDomain();
        if (image is not null)
        {
            this.cache.Set(cacheKey, image, OwnerImagesCacheDuration);
        }

        return image;
    }

    public async Task<IReadOnlyCollection<Image>> GetByIdsAsync(
        IReadOnlyCollection<string> imageIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedIds = imageIds
            .Where(static imageId => !string.IsNullOrWhiteSpace(imageId))
            .Select(static imageId => imageId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedIds.Count == 0)
        {
            return Array.Empty<Image>();
        }

        FilterDefinition<ImageDocument> filter =
            Builders<ImageDocument>.Filter.In(static document => document.Id, normalizedIds);
        List<ImageDocument> documents = await this.collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Image>> GetByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category, CancellationToken cancellationToken)
    {
        string cacheKey = BuildOwnerImagesCacheKey(ownerType, ownerId, category);
        if (this.cache.TryGetValue(cacheKey, out IReadOnlyCollection<Image>? cachedImages) && cachedImages is not null)
        {
            return cachedImages;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, ownerId);

        if (category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, category.Value);
        }

        List<ImageDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<Image> images = documents.Select(static document => document.ToDomain()).ToList();
        this.cache.Set(cacheKey, images, OwnerImagesCacheDuration);
        return images;
    }

    public async Task<long> CountActiveCommentDraftsByOwnerAsync(
        string ownerId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter =
            BuildActiveCommentDraftsByOwnerFilter(ownerId);
        return await this.collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
    }

    internal static FilterDefinition<ImageDocument> BuildActiveCommentDraftsByOwnerFilter(
        string ownerId)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        return
            builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.OwnerId, ownerId)
            & builder.Eq(static document => document.IsPublished, false)
            & builder.Or(
                builder.Eq(static document => document.CleanupRequestedAt, null),
                builder.Ne(static document => document.PendingCommentId, null));
    }

    public async Task<IReadOnlyCollection<Image>> GetByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory? category, CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedOwnerIds.Count == 0)
        {
            return Array.Empty<Image>();
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.In(static document => document.OwnerId, normalizedOwnerIds);

        if (category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, category.Value);
        }

        List<ImageDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<Image?> GetByOwnerAndSourceUrlAsync(ImageOwnerType ownerType, string ownerId, string sourceUrl, CancellationToken cancellationToken)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedSourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? string.Empty : sourceUrl.Trim();
        if (string.IsNullOrWhiteSpace(normalizedOwnerId) || string.IsNullOrWhiteSpace(normalizedSourceUrl))
        {
            return null;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, normalizedOwnerId) &
                                                 builder.Eq(static document => document.SourceUrl, normalizedSourceUrl);

        ImageDocument? document = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Image>> GetCommentImagesRequiringReconciliationAsync(
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        int safeLimit = Math.Clamp(limit, 1, 100);
        FilterDefinition<ImageDocument> reconciliationFilter =
            BuildCommentImageReconciliationFilter(
                dueBeforeUtc,
                draftCreatedBeforeUtc,
                DateTime.UtcNow);
        SortDefinition<ImageDocument> reconciliationSort =
            BuildCommentImageReconciliationSort();
        List<ImageDocument> documents = await this.collection
            .Find(reconciliationFilter)
            .Sort(reconciliationSort)
            .Limit(safeLimit)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    internal static FilterDefinition<ImageDocument>
        BuildCommentImageReconciliationFilter(
            DateTime dueBeforeUtc,
            DateTime draftCreatedBeforeUtc,
            DateTime variantLeaseExpiredBeforeUtc)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> commentFilter =
            builder.Eq(static document => document.Category, ImageCategory.Comment);
        FilterDefinition<ImageDocument> dueCleanupFilter =
            builder.Ne(static document => document.CleanupRequestedAt, null)
            & builder.Lte(static document => document.CleanupRequestedAt, dueBeforeUtc);
        FilterDefinition<ImageDocument> dueReservationReconciliationFilter =
            builder.Eq(
                static document => document.OwnerType,
                ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.IsPublished, false)
            & builder.Or(
                builder.Ne(
                    static document => document.ReservationReconcileAfter,
                    null)
                    & builder.Lte(
                        static document => document.ReservationReconcileAfter,
                        dueBeforeUtc),
                builder.Eq(
                    static document => document.ReservationReconcileAfter,
                    null)
                    & builder.Ne(
                        static document => document.PendingCommentId,
                        null)
                    & dueCleanupFilter);
        FilterDefinition<ImageDocument> publishedCleanupFilter =
            builder.Eq(static document => document.OwnerType, ImageOwnerType.Comment)
            & builder.Eq(static document => document.IsPublished, true)
            & builder.Eq(
                static document => document.CommentReuseReservationToken,
                null)
            & dueCleanupFilter;
        FilterDefinition<ImageDocument> publishedReuseFilter =
            builder.Eq(static document => document.OwnerType, ImageOwnerType.Comment)
            & builder.Eq(static document => document.IsPublished, true)
            & builder.Ne(
                static document => document.CommentReuseReservationToken,
                null)
            & builder.Ne(
                static document => document.CommentReuseReconcileAfter,
                null)
            & builder.Lte(
                static document => document.CommentReuseReconcileAfter,
                dueBeforeUtc);
        FilterDefinition<ImageDocument> draftCleanupOrExpiryFilter =
            builder.Eq(static document => document.OwnerType, ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.IsPublished, false)
            & builder.Or(
                dueCleanupFilter,
                builder.Lt(static document => document.CreatedAt, draftCreatedBeforeUtc));
        FilterDefinition<ImageDocument> availableClaimFilter =
            builder.Eq(static document => document.CleanupClaimToken, null)
            | builder.Eq(static document => document.CleanupClaimedUntil, null)
            | builder.Lte(static document => document.CleanupClaimedUntil, dueBeforeUtc);
        FilterDefinition<ImageDocument> availableVariantGenerationFilter =
            builder.Eq(
                static document => document.VariantGenerationClaimToken,
                null)
            | builder.Lte(
                static document => document.VariantGenerationClaimedUntil,
                variantLeaseExpiredBeforeUtc);
        return commentFilter
            & builder.Or(
                publishedCleanupFilter,
                publishedReuseFilter,
                dueReservationReconciliationFilter,
                draftCleanupOrExpiryFilter)
            & availableClaimFilter
            & availableVariantGenerationFilter;
    }

    internal static SortDefinition<ImageDocument>
        BuildCommentImageReconciliationSort()
    {
        return Builders<ImageDocument>.Sort
            .Ascending(static document => document.UpdatedAt)
            .Ascending(static document => document.CreatedAt);
    }

    public async Task<bool> TryClaimCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        string? observedCommentReuseReservationToken,
        string claimToken,
        DateTime claimUntilUtc,
        CancellationToken cancellationToken)
    {
        if (ownerType is not (ImageOwnerType.Comment or ImageOwnerType.CommentDraft)
            || string.IsNullOrWhiteSpace(claimToken))
        {
            return false;
        }

        FilterDefinition<ImageDocument> filter = BuildCommentImageCleanupClaimFilter(
            imageId,
            ownerType,
            ownerId,
            dueBeforeUtc,
            draftCreatedBeforeUtc,
            observedCommentReuseReservationToken,
            DateTime.UtcNow);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.CleanupClaimToken, claimToken)
            .Set(static document => document.CleanupClaimedUntil, claimUntilUtc)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.MatchedCount > 0;
    }

    public async Task<PublishedCommentImageReusePreparation> TryPreparePublishedCommentImageForReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        DateTime reconcileAfterUtc,
        long targetCommentRevision,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> baseFilter =
            BuildPublishedCommentImageReuseFilter(imageId, commentId);
        FilterDefinition<ImageDocument> prepareFilter =
            baseFilter
            & builder.Eq(
                static document => document.CommentReuseReservationToken,
                null)
            & builder.Ne(static document => document.CleanupRequestedAt, null)
            & builder.Or(
                builder.Eq(
                    static document => document.CleanupCommentRevision,
                    null),
                builder.Lte(
                    static document => document.CleanupCommentRevision,
                    targetCommentRevision));
        UpdateDefinitionBuilder<ImageDocument> updateBuilder =
            Builders<ImageDocument>.Update;
        UpdateDefinition<ImageDocument> update = updateBuilder
            .Set(
                static document => document.CommentReuseReservationToken,
                reservationToken)
            .Set(
                static document => document.CommentReuseReconcileAfter,
                reconcileAfterUtc)
            .Set(
                static document => document.CommentReuseTargetRevision,
                targetCommentRevision)
            .Unset(static document => document.CleanupRequestedAt)
            .Unset(static document => document.CleanupCommentRevision)
            .Unset(static document => document.ReservationReconcileAfter)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        FindOneAndUpdateOptions<ImageDocument> options =
            new FindOneAndUpdateOptions<ImageDocument>
            {
                ReturnDocument = ReturnDocument.Before,
            };
        ImageDocument? previous = await this.collection.FindOneAndUpdateAsync(
            prepareFilter,
            update,
            options,
            cancellationToken);
        if (previous is not null)
        {
            InvalidateReadCache();
            return PublishedCommentImageReusePreparation.PreparedAndCleanupCleared;
        }

        FilterDefinition<ImageDocument> readyFilter =
            baseFilter
            & builder.Or(
                builder.Eq(
                    static document => document.CommentReuseReservationToken,
                    reservationToken),
                builder.Eq(
                    static document => document.CommentReuseReservationToken,
                    null)
                    & builder.Eq(
                        static document => document.CleanupRequestedAt,
                        null));
        ImageDocument? ready = await this.collection
            .Find(readyFilter)
            .FirstOrDefaultAsync(cancellationToken);
        if (ready is null)
        {
            return PublishedCommentImageReusePreparation.Rejected;
        }

        return string.Equals(
            ready.CommentReuseReservationToken,
            reservationToken,
            StringComparison.Ordinal)
            ? PublishedCommentImageReusePreparation.PreparedAndCleanupCleared
            : PublishedCommentImageReusePreparation.Prepared;
    }

    public Task<bool> FinalizePublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        CancellationToken cancellationToken)
    {
        return this.CompletePublishedCommentImageReuseAsync(
            imageId,
            commentId,
            reservationToken,
            null,
            null,
            cancellationToken);
    }

    public Task<bool> ReleasePublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        DateTime cleanupRequestedAtUtc,
        long cleanupCommentRevision,
        CancellationToken cancellationToken)
    {
        return this.CompletePublishedCommentImageReuseAsync(
            imageId,
            commentId,
            reservationToken,
            cleanupRequestedAtUtc,
            cleanupCommentRevision,
            cancellationToken);
    }

    public async Task<bool> ResolveClaimedPublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        string claimToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.Comment,
                commentId,
                claimToken)
            & builder.Eq(
                static document => document.CommentReuseReservationToken,
                reservationToken);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Unset(
                    static document => document.CommentReuseReservationToken)
                .Unset(
                    static document => document.CommentReuseReconcileAfter)
                .Unset(
                    static document => document.CommentReuseTargetRevision)
                .Unset(static document => document.CleanupClaimToken)
                .Unset(static document => document.CleanupClaimedUntil)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeferClaimedPublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        string claimToken,
        DateTime reconcileAfterUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.Comment,
                commentId,
                claimToken)
            & builder.Eq(
                static document => document.CommentReuseReservationToken,
                reservationToken);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.CommentReuseReconcileAfter,
                    reconcileAfterUtc)
                .Unset(static document => document.CleanupClaimToken)
                .Unset(static document => document.CleanupClaimedUntil)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.MatchedCount > 0;
    }

    private async Task<bool> CompletePublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        DateTime? cleanupRequestedAtUtc,
        long? cleanupCommentRevision,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildPublishedCommentImageReuseFilter(imageId, commentId)
            & builder.Eq(
                static document => document.CommentReuseReservationToken,
                reservationToken);
        UpdateDefinitionBuilder<ImageDocument> updateBuilder =
            Builders<ImageDocument>.Update;
        UpdateDefinition<ImageDocument> update = updateBuilder
            .Unset(static document => document.CommentReuseReservationToken)
            .Unset(static document => document.CommentReuseReconcileAfter)
            .Unset(static document => document.CommentReuseTargetRevision)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        if (cleanupRequestedAtUtc.HasValue)
        {
            long revision = cleanupCommentRevision
                ?? throw new InvalidOperationException(
                    "A cleanup revision is required when restoring cleanup.");
            update = update.Max(
                static document => document.CleanupRequestedAt,
                cleanupRequestedAtUtc.Value)
                .Max(
                    static document => document.CleanupCommentRevision,
                    revision)
                .Max(
                    static document => document.ReservationReconcileAfter,
                    cleanupRequestedAtUtc.Value);
        }

        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.MatchedCount > 0;
    }

    public async Task<bool> CancelClaimedCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        string claimToken,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> unchangedCleanupFilter =
            BuildUnchangedClaimedCommentImageFilter(
                imageId,
                ownerType,
                ownerId,
                observedCleanupRequestedAtUtc,
                observedCleanupCommentRevision,
                claimToken);
        UpdateDefinition<ImageDocument> cancelCleanupUpdate =
            BuildCancelClaimedCommentImageCleanupUpdate();
        UpdateResult cancelResult = await this.collection.UpdateOneAsync(
            unchangedCleanupFilter,
            cancelCleanupUpdate,
            cancellationToken: cancellationToken);
        if (cancelResult.MatchedCount > 0)
        {
            InvalidateReadCache();
            return true;
        }

        FilterDefinition<ImageDocument> claimedFilter =
            BuildClaimedCommentImageFilter(
                imageId,
                ownerType,
                ownerId,
                claimToken);
        UpdateDefinition<ImageDocument> releaseClaimUpdate =
            BuildReleaseCommentImageCleanupClaimUpdate();
        UpdateResult releaseResult = await this.collection.UpdateOneAsync(
            claimedFilter,
            releaseClaimUpdate,
            cancellationToken: cancellationToken);
        if (releaseResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return releaseResult.MatchedCount > 0;
    }

    internal static FilterDefinition<ImageDocument>
        BuildUnchangedClaimedCommentImageFilter(
            string imageId,
            ImageOwnerType ownerType,
            string ownerId,
            DateTime observedCleanupRequestedAtUtc,
            long? observedCleanupCommentRevision,
            string claimToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        return BuildClaimedCommentImageFilter(
            imageId,
            ownerType,
            ownerId,
            claimToken)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedCleanupRequestedAtUtc)
            & builder.Eq(
                static document => document.CleanupCommentRevision,
                observedCleanupCommentRevision);
    }

    internal static UpdateDefinition<ImageDocument>
        BuildCancelClaimedCommentImageCleanupUpdate()
    {
        return Builders<ImageDocument>.Update
            .Unset(static document => document.CleanupRequestedAt)
            .Unset(static document => document.CleanupCommentRevision)
            .Unset(static document => document.ReservationReconcileAfter)
            .Unset(static document => document.CleanupClaimToken)
            .Unset(static document => document.CleanupClaimedUntil)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    internal static UpdateDefinition<ImageDocument>
        BuildReleaseCommentImageCleanupClaimUpdate()
    {
        return Builders<ImageDocument>.Update
            .Unset(static document => document.CleanupClaimToken)
            .Unset(static document => document.CleanupClaimedUntil)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    internal static FilterDefinition<ImageDocument> BuildCommentImageCleanupClaimFilter(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        string? observedCommentReuseReservationToken,
        DateTime variantLeaseExpiredBeforeUtc)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> availableClaimFilter =
            builder.Eq(static document => document.CleanupClaimToken, null)
            | builder.Eq(static document => document.CleanupClaimedUntil, null)
            | builder.Lte(static document => document.CleanupClaimedUntil, dueBeforeUtc);
        FilterDefinition<ImageDocument> cleanupDueFilter =
            builder.Ne(static document => document.CleanupRequestedAt, null)
            & builder.Lte(static document => document.CleanupRequestedAt, dueBeforeUtc);
        FilterDefinition<ImageDocument> reconciliationDueFilter =
            builder.Ne(
                static document => document.ReservationReconcileAfter,
                null)
            & builder.Lte(
                static document => document.ReservationReconcileAfter,
                dueBeforeUtc);
        FilterDefinition<ImageDocument> availableVariantGenerationFilter =
            builder.Eq(
                static document => document.VariantGenerationClaimToken,
                null)
            | builder.Lte(
                static document => document.VariantGenerationClaimedUntil,
                variantLeaseExpiredBeforeUtc);
        FilterDefinition<ImageDocument> publishedScopeFilter =
            builder.Eq(static document => document.IsPublished, true);
        publishedScopeFilter &= string.IsNullOrWhiteSpace(
            observedCommentReuseReservationToken)
            ? builder.Eq(
                static document => document.CommentReuseReservationToken,
                null)
                & cleanupDueFilter
            : builder.Eq(
                static document => document.CommentReuseReservationToken,
                observedCommentReuseReservationToken)
                & builder.Ne(
                    static document => document.CommentReuseReconcileAfter,
                    null)
                & builder.Lte(
                    static document => document.CommentReuseReconcileAfter,
                    dueBeforeUtc);
        FilterDefinition<ImageDocument> scopeFilter =
            ownerType == ImageOwnerType.Comment
            ? publishedScopeFilter
            : builder.Eq(static document => document.IsPublished, false)
                & builder.Eq(static document => document.PendingCommentId, null)
                & builder.Eq(static document => document.PendingReservationToken, null)
                & builder.Or(
                    cleanupDueFilter,
                    reconciliationDueFilter,
                    builder.Lt(static document => document.CreatedAt, draftCreatedBeforeUtc));

        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ownerType)
            & builder.Eq(static document => document.OwnerId, ownerId)
            & scopeFilter
            & availableClaimFilter
            & availableVariantGenerationFilter;
    }

    internal static FilterDefinition<ImageDocument> BuildPublishedCommentImageReuseFilter(
        string imageId,
        string commentId)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ImageOwnerType.Comment)
            & builder.Eq(static document => document.OwnerId, commentId)
            & builder.Eq(static document => document.IsPublished, true)
            & builder.Eq(static document => document.CleanupClaimToken, null);
    }

    internal static FilterDefinition<ImageDocument> BuildClaimedCommentImageFilter(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        string claimToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ownerType)
            & builder.Eq(static document => document.OwnerId, ownerId)
            & builder.Eq(static document => document.IsPublished, ownerType == ImageOwnerType.Comment)
            & builder.Eq(static document => document.CleanupClaimToken, claimToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetMainImageIdsByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory category, bool publishedOnly, CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedOwnerIds.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.In(static document => document.OwnerId, normalizedOwnerIds) &
                                                 BuildCategoryFilter(builder, category);

        if (publishedOnly)
        {
            filter &= builder.Eq(static document => document.IsPublished, true);
        }

        List<ImageOwnerMainImageProjection> projections = await this.collection.Find(filter)
            .SortByDescending(static document => document.IsCurrent)
            .ThenByDescending(static document => document.CreatedAt)
            .Project(static document => new ImageOwnerMainImageProjection
            {
                Id = document.Id,
                OwnerId = document.OwnerId,
            })
            .ToListAsync(cancellationToken);

        Dictionary<string, string> imageIdsByOwnerId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ImageOwnerMainImageProjection projection in projections)
        {
            if (string.IsNullOrWhiteSpace(projection.OwnerId) || string.IsNullOrWhiteSpace(projection.Id))
            {
                continue;
            }

            if (!imageIdsByOwnerId.ContainsKey(projection.OwnerId))
            {
                imageIdsByOwnerId[projection.OwnerId] = projection.Id;
            }
        }

        return imageIdsByOwnerId;
    }

    public async Task<Image?> GetCurrentByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, CancellationToken cancellationToken)
    {
        string cacheKey = BuildCurrentOwnerImageCacheKey(ownerType, ownerId, category);
        if (this.cache.TryGetValue(cacheKey, out Image? cachedImage) && cachedImage is not null)
        {
            return cachedImage;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, ownerId) &
                                                 BuildCategoryFilter(builder, category) &
                                                 builder.Eq(static document => document.IsCurrent, true);

        ImageDocument? document = await this.collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        Image? image = document?.ToDomain();
        if (image is not null)
        {
            this.cache.Set(cacheKey, image, OwnerImagesCacheDuration);
        }

        return image;
    }

    public async Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken)
    {
        DateTime nowUtc = DateTime.UtcNow;
        List<LocalizedTextDocument> captions = string.IsNullOrWhiteSpace(request.Description)
            ? new List<LocalizedTextDocument>()
            : new List<LocalizedTextDocument>
            {
                new LocalizedTextDocument
                {
                    LanguageCode = "fr",
                    Value = request.Description,
                },
            };

        ImageDocument document = new ImageDocument
        {
            Id = string.IsNullOrWhiteSpace(request.ImageId) ? Guid.NewGuid().ToString("N") : request.ImageId,
            Category = request.Category,
            Description = request.Description,
            Path = string.IsNullOrWhiteSpace(request.StoragePath) ? $"{request.Category}/{Guid.NewGuid():N}_{request.File.FileName}" : request.StoragePath,
            SizeInBytes = request.SizeInBytes > 0 ? request.SizeInBytes : request.File.Length,
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            DraftOwnerId = request.OwnerType == ImageOwnerType.CommentDraft
                ? request.OwnerId
                : null,
            CommentDraftUploadToken = request.CommentDraftUploadToken,
            CleanupRequestedAt = request.CleanupRequestedAtUtc,
            IsPublished = request.IsPublished,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            IsWatermarked = request.WithWatermark,
            Width = request.Width,
            Height = request.Height,
            GeoLocation = request.GeoLocation is null ? null : CommonMongoMappers.ToDocument(new GeoPoint(request.GeoLocation.Latitude, request.GeoLocation.Longitude)),
            ExifMetadata = request.ExifMetadata?.ToDocument(),
            Captions = captions,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        InvalidateReadCache();
        return document.ToDomain();
    }

    public async Task<Image?> CompleteCommentDraftUploadAsync(
        string imageId,
        string draftOwnerId,
        string uploadToken,
        DateTime observedCleanupRequestedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter =
            BuildCompleteCommentDraftUploadFilter(
                imageId,
                draftOwnerId,
                uploadToken,
                observedCleanupRequestedAtUtc);
        UpdateDefinition<ImageDocument> update =
            BuildCompleteCommentDraftUploadUpdate();
        FindOneAndUpdateOptions<ImageDocument> options =
            new FindOneAndUpdateOptions<ImageDocument>
            {
                ReturnDocument = ReturnDocument.After,
            };
        ImageDocument? document =
            await this.collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    internal static FilterDefinition<ImageDocument>
        BuildCompleteCommentDraftUploadFilter(
            string imageId,
            string draftOwnerId,
            string uploadToken,
            DateTime observedCleanupRequestedAtUtc)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(
                static document => document.Category,
                ImageCategory.Comment)
            & builder.Eq(
                static document => document.OwnerType,
                ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.OwnerId, draftOwnerId)
            & builder.Eq(static document => document.IsPublished, false)
            & builder.Eq(static document => document.PendingCommentId, null)
            & builder.Eq(
                static document => document.CommentDraftUploadToken,
                uploadToken)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedCleanupRequestedAtUtc)
            & builder.Eq(static document => document.CleanupClaimToken, null);
    }

    internal static UpdateDefinition<ImageDocument>
        BuildCompleteCommentDraftUploadUpdate()
    {
        return Builders<ImageDocument>.Update
            .Unset(static document => document.CommentDraftUploadToken)
            .Unset(static document => document.CleanupRequestedAt)
            .Unset(static document => document.CleanupCommentRevision)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    public async Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.OwnerType, ownerType)
            .Set(static document => document.OwnerId, ownerId)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> ReserveCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string reservationToken,
        long pendingCommentRevision,
        DateTime reconcileAfterUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> draftFilter =
            BuildCommentDraftReservationFilter(
                imageId,
                draftOwnerId,
                commentId,
                reservationToken,
                pendingCommentRevision);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.DraftOwnerId, draftOwnerId)
            .Set(static document => document.PendingCommentId, commentId)
            .Set(static document => document.PendingReservationToken, reservationToken)
            .Set(
                static document => document.PendingCommentRevision,
                pendingCommentRevision)
            .Set(
                static document => document.ReservationReconcileAfter,
                reconcileAfterUtc)
            .Set(
                static document => document.PendingReservationExpiresAt,
                reconcileAfterUtc.Add(ReservationMaximumLifetime))
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(
            draftFilter,
            update,
            options,
            cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    internal static FilterDefinition<ImageDocument> BuildCommentDraftReservationFilter(
        string imageId,
        string draftOwnerId,
        string commentId,
        string reservationToken,
        long pendingCommentRevision)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.OwnerId, draftOwnerId)
            & builder.Eq(static document => document.IsPublished, false)
            & builder.Eq(static document => document.CleanupClaimToken, null)
            & builder.Not(builder.AnyEq(
                static document => document.AbortedReservationTokens,
                reservationToken))
            & builder.Or(
                builder.Eq(static document => document.PendingCommentId, null)
                    & builder.Eq(static document => document.PendingReservationToken, null)
                    & builder.Eq(static document => document.CleanupRequestedAt, null),
                builder.Eq(static document => document.PendingCommentId, commentId)
                    & builder.Eq(
                        static document => document.PendingReservationToken,
                        reservationToken)
                    & builder.Eq(
                        static document => document.PendingCommentRevision,
                        pendingCommentRevision));
    }

    public async Task<Image?> FinalizeCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter =
            BuildPendingCommentDraftFilter(
                imageId,
                draftOwnerId,
                commentId,
                reservationToken);
        UpdateDefinition<ImageDocument> update =
            BuildFinalizeCommentDraftUpdate(draftOwnerId, commentId);
        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };
        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    public async Task<bool> ReleaseCommentDraftReservationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        CancellationToken cancellationToken)
    {
        bool abortRecorded = false;
        if (!string.IsNullOrWhiteSpace(reservationToken))
        {
            FilterDefinitionBuilder<ImageDocument> builder =
                Builders<ImageDocument>.Filter;
            FilterDefinition<ImageDocument> abortFilter =
                builder.Eq(static document => document.Id, imageId)
                & builder.Eq(
                    static document => document.Category,
                    ImageCategory.Comment)
                & builder.Eq(
                    static document => document.OwnerType,
                    ImageOwnerType.CommentDraft)
                & builder.Eq(static document => document.OwnerId, draftOwnerId)
                & builder.Eq(static document => document.IsPublished, false);
            UpdateResult abortResult = await this.collection.UpdateOneAsync(
                abortFilter,
                BuildAbortCommentDraftReservationUpdate(
                    reservationToken),
                cancellationToken: cancellationToken);
            if (abortResult.MatchedCount == 0)
            {
                return false;
            }

            abortRecorded = true;
        }

        FilterDefinition<ImageDocument> filter =
            BuildPendingCommentDraftFilter(
                imageId,
                draftOwnerId,
                commentId,
                reservationToken);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Unset(static document => document.PendingCommentId)
            .Unset(static document => document.PendingReservationToken)
            .Unset(static document => document.PendingCommentRevision)
            .Unset(
                static document => document.PendingReservationExpiresAt)
            .Unset(static document => document.ReservationReconcileAfter)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        if (abortRecorded)
        {
            InvalidateReadCache();
        }

        return abortRecorded || result.ModifiedCount > 0;
    }

    internal static UpdateDefinition<ImageDocument>
        BuildAbortCommentDraftReservationUpdate(string reservationToken)
    {
        return Builders<ImageDocument>.Update
            .AddToSet(
                static document => document.AbortedReservationTokens,
                reservationToken)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    public async Task<bool> ReleaseCommentDraftReservationForReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        DateTime observedReconcileAfterUtc,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> pendingFilter =
            BuildPendingCommentDraftFilter(
                imageId,
                draftOwnerId,
                commentId,
                reservationToken);
        FilterDefinition<ImageDocument> modernFilter =
            pendingFilter
            & builder.Eq(
                static document => document.ReservationReconcileAfter,
                observedReconcileAfterUtc);
        UpdateDefinition<ImageDocument> modernUpdate =
            BuildReleaseDraftForReconciliationUpdate(
                nextReconcileAfterUtc,
                false);
        UpdateResult modernResult = await this.collection.UpdateOneAsync(
            modernFilter,
            modernUpdate,
            cancellationToken: cancellationToken);
        if (modernResult.MatchedCount > 0)
        {
            InvalidateReadCache();
            return true;
        }

        FilterDefinition<ImageDocument> legacyFilter =
            pendingFilter
            & builder.Eq(
                static document => document.ReservationReconcileAfter,
                null)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedReconcileAfterUtc);
        UpdateDefinition<ImageDocument> legacyUpdate =
            BuildReleaseDraftForReconciliationUpdate(
                nextReconcileAfterUtc,
                true);
        UpdateResult legacyResult = await this.collection.UpdateOneAsync(
            legacyFilter,
            legacyUpdate,
            cancellationToken: cancellationToken);
        if (legacyResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return legacyResult.MatchedCount > 0;
    }

    public async Task<bool> ReschedulePendingCommentDraftReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        DateTime observedReconcileAfterUtc,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> pendingFilter =
            BuildPendingCommentDraftFilter(
                imageId,
                draftOwnerId,
                commentId,
                reservationToken);
        UpdateDefinition<ImageDocument> modernUpdate =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.ReservationReconcileAfter,
                    nextReconcileAfterUtc)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult modernResult = await this.collection.UpdateOneAsync(
            pendingFilter
            & builder.Eq(
                static document => document.ReservationReconcileAfter,
                observedReconcileAfterUtc),
            modernUpdate,
            cancellationToken: cancellationToken);
        if (modernResult.MatchedCount > 0)
        {
            InvalidateReadCache();
            return true;
        }

        UpdateDefinition<ImageDocument> legacyUpdate =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.ReservationReconcileAfter,
                    nextReconcileAfterUtc)
                .Unset(static document => document.CleanupRequestedAt)
                .Unset(static document => document.CleanupCommentRevision)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult legacyResult = await this.collection.UpdateOneAsync(
            pendingFilter
            & builder.Eq(
                static document => document.ReservationReconcileAfter,
                null)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedReconcileAfterUtc),
            legacyUpdate,
            cancellationToken: cancellationToken);
        if (legacyResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return legacyResult.MatchedCount > 0;
    }

    public async Task<bool> RescheduleClaimedCommentDraftReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string claimToken,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.CommentDraft,
                draftOwnerId,
                claimToken)
            & builder.Eq(static document => document.PendingCommentId, null)
            & builder.Eq(
                static document => document.PendingReservationToken,
                null);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.ReservationReconcileAfter,
                    nextReconcileAfterUtc)
                .Unset(static document => document.CleanupClaimToken)
                .Unset(static document => document.CleanupClaimedUntil)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
            return true;
        }

        UpdateResult releaseResult = await this.collection.UpdateOneAsync(
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.CommentDraft,
                draftOwnerId,
                claimToken),
            BuildReleaseCommentImageCleanupClaimUpdate(),
            cancellationToken: cancellationToken);
        if (releaseResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return releaseResult.MatchedCount > 0;
    }

    public async Task<bool> RescheduleClaimedCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        string claimToken,
        DateTime nextCleanupRequestedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildClaimedCommentImageFilter(
                imageId,
                ownerType,
                ownerId,
                claimToken)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedCleanupRequestedAtUtc)
            & builder.Eq(
                static document => document.CleanupCommentRevision,
                observedCleanupCommentRevision);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.CleanupRequestedAt,
                    nextCleanupRequestedAtUtc)
                .Unset(static document => document.CleanupClaimToken)
                .Unset(static document => document.CleanupClaimedUntil)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
            return true;
        }

        UpdateResult releaseResult = await this.collection.UpdateOneAsync(
            BuildClaimedCommentImageFilter(
                imageId,
                ownerType,
                ownerId,
                claimToken),
            BuildReleaseCommentImageCleanupClaimUpdate(),
            cancellationToken: cancellationToken);
        if (releaseResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return releaseResult.MatchedCount > 0;
    }

    public async Task<Image?> RecoverClaimedReferencedCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string claimToken,
        DateTime? observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        DateTime safetyCleanupRequestedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.CommentDraft,
                draftOwnerId,
                claimToken)
            & builder.Eq(static document => document.PendingCommentId, null)
            & builder.Eq(
                static document => document.PendingReservationToken,
                null)
            & builder.Eq(
                static document => document.CleanupRequestedAt,
                observedCleanupRequestedAtUtc)
            & builder.Eq(
                static document => document.CleanupCommentRevision,
                observedCleanupCommentRevision);
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Set(
                    static document => document.OwnerType,
                    ImageOwnerType.Comment)
                .Set(static document => document.OwnerId, commentId)
                .Set(static document => document.IsPublished, true)
                .Unset(static document => document.PendingCommentId)
                .Unset(
                    static document => document.PendingReservationToken)
                .Unset(static document => document.PendingCommentRevision)
                .Unset(
                    static document => document.PendingReservationExpiresAt)
                .Unset(
                    static document => document.ReservationReconcileAfter)
                .Unset(static document => document.CleanupClaimToken)
                .Unset(static document => document.CleanupClaimedUntil)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        if (!observedCleanupRequestedAtUtc.HasValue)
        {
            update = update
                .Set(
                    static document => document.CleanupRequestedAt,
                    safetyCleanupRequestedAtUtc)
                .Unset(static document => document.CleanupCommentRevision);
        }
        FindOneAndUpdateOptions<ImageDocument> options =
            new FindOneAndUpdateOptions<ImageDocument>
            {
                ReturnDocument = ReturnDocument.After,
            };
        ImageDocument? document =
            await this.collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
            return document.ToDomain();
        }

        UpdateResult releaseResult = await this.collection.UpdateOneAsync(
            BuildClaimedCommentImageFilter(
                imageId,
                ImageOwnerType.CommentDraft,
                draftOwnerId,
                claimToken),
            BuildReleaseCommentImageCleanupClaimUpdate(),
            cancellationToken: cancellationToken);
        if (releaseResult.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return null;
    }

    internal static UpdateDefinition<ImageDocument>
        BuildFinalizeCommentDraftUpdate(
            string draftOwnerId,
            string commentId)
    {
        return Builders<ImageDocument>.Update
            .Set(static document => document.OwnerType, ImageOwnerType.Comment)
            .Set(static document => document.OwnerId, commentId)
            .Set(static document => document.DraftOwnerId, draftOwnerId)
            .Set(static document => document.IsPublished, true)
            .Unset(static document => document.PendingCommentId)
            .Unset(static document => document.PendingReservationToken)
            .Unset(static document => document.PendingCommentRevision)
            .Unset(
                static document => document.PendingReservationExpiresAt)
            .Unset(static document => document.AbortedReservationTokens)
            .Unset(static document => document.ReservationReconcileAfter)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    internal static UpdateDefinition<ImageDocument>
        BuildReleaseDraftForReconciliationUpdate(
            DateTime nextReconcileAfterUtc,
            bool clearLegacyCleanupDeadline)
    {
        UpdateDefinition<ImageDocument> update =
            Builders<ImageDocument>.Update
                .Unset(static document => document.PendingCommentId)
                .Unset(
                    static document => document.PendingReservationToken)
                .Unset(static document => document.PendingCommentRevision)
                .Unset(
                    static document => document.PendingReservationExpiresAt)
                .Set(
                    static document => document.ReservationReconcileAfter,
                    nextReconcileAfterUtc)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        return clearLegacyCleanupDeadline
            ? update
                .Unset(static document => document.CleanupRequestedAt)
                .Unset(static document => document.CleanupCommentRevision)
            : update;
    }

    internal static FilterDefinition<ImageDocument> BuildPendingCommentDraftFilter(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        return builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.OwnerId, draftOwnerId)
            & builder.Eq(static document => document.PendingCommentId, commentId)
            & builder.Eq(
                static document => document.PendingReservationToken,
                reservationToken)
            & builder.Eq(static document => document.IsPublished, false);
    }

    public async Task<bool> RequestCommentDraftCleanupAsync(
        string imageId,
        string draftOwnerId,
        DateTime cleanupRequestedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter =
            builder.Eq(static document => document.Id, imageId)
            & builder.Eq(static document => document.Category, ImageCategory.Comment)
            & builder.Eq(static document => document.OwnerType, ImageOwnerType.CommentDraft)
            & builder.Eq(static document => document.OwnerId, draftOwnerId)
            & builder.Eq(static document => document.PendingCommentId, null)
            & builder.Eq(static document => document.IsPublished, false);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.CleanupRequestedAt, cleanupRequestedAtUtc)
            .Unset(static document => document.CleanupCommentRevision)
            .Unset(static document => document.CommentDraftUploadToken)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.MatchedCount > 0;
    }

    public async Task<int> RequestCommentImagesCleanupAsync(
        IReadOnlyCollection<string> imageIds,
        string commentId,
        long cleanupCommentRevision,
        DateTime cleanupRequestedAtUtc,
        CancellationToken cancellationToken)
    {
        List<string> normalizedIds = imageIds
            .Where(static imageId => !string.IsNullOrWhiteSpace(imageId))
            .Select(static imageId => imageId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<ImageDocument> filter =
            BuildRequestCommentImagesCleanupFilter(
                normalizedIds,
                commentId);
        UpdateDefinition<ImageDocument> update =
            BuildRequestCommentImagesCleanupUpdate(
                cleanupCommentRevision,
                cleanupRequestedAtUtc);
        UpdateResult result = await this.collection.UpdateManyAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return checked((int)result.MatchedCount);
    }

    internal static UpdateDefinition<ImageDocument>
        BuildRequestCommentImagesCleanupUpdate(
            long cleanupCommentRevision,
            DateTime cleanupRequestedAtUtc)
    {
        return Builders<ImageDocument>.Update
            .Max(
                static document => document.CleanupRequestedAt,
                cleanupRequestedAtUtc)
            .Max(
                static document => document.CleanupCommentRevision,
                cleanupCommentRevision)
            .Max(
                static document => document.ReservationReconcileAfter,
                cleanupRequestedAtUtc)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);
    }

    internal static FilterDefinition<ImageDocument>
        BuildRequestCommentImagesCleanupFilter(
            IReadOnlyCollection<string> imageIds,
            string commentId)
    {
        FilterDefinitionBuilder<ImageDocument> builder =
            Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> publishedFilter =
            builder.Eq(
                static document => document.OwnerType,
                ImageOwnerType.Comment)
            & builder.Eq(static document => document.OwnerId, commentId)
            & builder.Eq(static document => document.IsPublished, true);
        FilterDefinition<ImageDocument> reservedDraftFilter =
            builder.Eq(
                static document => document.OwnerType,
                ImageOwnerType.CommentDraft)
            & builder.Eq(
                static document => document.PendingCommentId,
                commentId)
            & builder.Eq(static document => document.IsPublished, false);
        return builder.In(static document => document.Id, imageIds)
            & builder.Eq(
                static document => document.Category,
                ImageCategory.Comment)
            & builder.Or(publishedFilter, reservedDraftFilter);
    }

    public async Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        ImageDocument? currentDocument = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDocument is null)
        {
            return null;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> ownerFilter = BuildOwnerTypeFilter(builder, ownerType) &
                                                     builder.Eq(static document => document.OwnerId, ownerId) &
                                                     BuildCategoryFilter(builder, currentDocument.Category);

        await this.collection.UpdateManyAsync(
            ownerFilter,
            Builders<ImageDocument>.Update
                .Set(static document => document.IsCurrent, false)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        FilterDefinition<ImageDocument> targetFilter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> targetUpdate = Builders<ImageDocument>.Update
            .Set(static document => document.OwnerType, ownerType)
            .Set(static document => document.OwnerId, ownerId)
            .Set(static document => document.IsCurrent, true)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(targetFilter, targetUpdate, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.Description, metadata.Description)
            .Set(static document => document.GeoLocation, metadata.GeoLocation is null ? null : CommonMongoMappers.ToDocument(new GeoPoint(metadata.GeoLocation.Latitude, metadata.GeoLocation.Longitude)))
            .Set(static document => document.AltTexts, CommonMongoMappers.ToDocuments(metadata.AltTexts))
            .Set(static document => document.Captions, CommonMongoMappers.ToDocuments(metadata.Captions))
            .Set(static document => document.Credits, CommonMongoMappers.ToDocuments(metadata.Credits))
            .Set(static document => document.TagIds, metadata.TagIds.Distinct(StringComparer.Ordinal).ToList())
            .Set(static document => document.Category, metadata.Category)
            .Set(static document => document.IsPublished, metadata.IsPublished)
            .Set(static document => document.SourceUrl, string.IsNullOrWhiteSpace(metadata.SourceUrl) ? null : metadata.SourceUrl.Trim())
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        if (metadata.OwnerType.HasValue)
        {
            update = update
                .Set(static document => document.OwnerType, metadata.OwnerType.Value)
                .Set(static document => document.OwnerId, string.IsNullOrWhiteSpace(metadata.OwnerId) ? null : metadata.OwnerId.Trim());
        }

        if (metadata.IsCurrent.HasValue)
        {
            update = update.Set(static document => document.IsCurrent, metadata.IsCurrent.Value);
        }

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> MarkWatermarkedAsync(string imageId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.IsWatermarked, true)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    public async Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == imageId, cancellationToken: cancellationToken);
        if (result.DeletedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.DeletedCount > 0;
    }

    public async Task<bool> DeleteClaimedCommentImageAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        string claimToken,
        CancellationToken cancellationToken)
    {
        if (ownerType is not (ImageOwnerType.Comment or ImageOwnerType.CommentDraft)
            || string.IsNullOrWhiteSpace(claimToken))
        {
            return false;
        }

        FilterDefinition<ImageDocument> filter = BuildClaimedCommentImageFilter(
            imageId,
            ownerType,
            ownerId,
            claimToken);
        DeleteResult result = await this.collection.DeleteOneAsync(
            filter,
            cancellationToken);
        if (result.DeletedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.DeletedCount > 0;
    }

    public async Task<int> UpdateBulkMetadataAsync(IReadOnlyCollection<string> imageIds, ImageBulkMetadataUpdate metadata, CancellationToken cancellationToken)
    {
        List<string> normalizedImageIds = imageIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedImageIds.Count == 0)
        {
            return 0;
        }

        List<UpdateDefinition<ImageDocument>> updates = new List<UpdateDefinition<ImageDocument>>
        {
            Builders<ImageDocument>.Update.Set(static document => document.UpdatedAt, DateTime.UtcNow),
        };

        if (metadata.IsPublished.HasValue)
        {
            updates.Add(Builders<ImageDocument>.Update.Set(static document => document.IsPublished, metadata.IsPublished.Value));
        }

        if (metadata.Category.HasValue)
        {
            updates.Add(Builders<ImageDocument>.Update.Set(static document => document.Category, metadata.Category.Value));
        }

        List<string> tagIdsToAdd = metadata.AddTagIds?
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? new List<string>();

        if (tagIdsToAdd.Count > 0)
        {
            updates.Add(Builders<ImageDocument>.Update.AddToSetEach(static document => document.TagIds, tagIdsToAdd));
        }

        List<string> tagIdsToRemove = metadata.RemoveTagIds?
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? new List<string>();

        if (tagIdsToRemove.Count > 0)
        {
            updates.Add(Builders<ImageDocument>.Update.PullAll(static document => document.TagIds, tagIdsToRemove));
        }

        if (updates.Count <= 1)
        {
            return 0;
        }

        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.In(static document => document.Id, normalizedImageIds);
        UpdateResult result = await this.collection.UpdateManyAsync(filter, Builders<ImageDocument>.Update.Combine(updates), cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return checked((int)result.ModifiedCount);
    }

    private static string BuildImageByIdCacheKey(string imageId)
    {
        string normalizedImageId = string.IsNullOrWhiteSpace(imageId) ? string.Empty : imageId.Trim();
        return $"images:by-id:{GetCacheVersion()}:{normalizedImageId}";
    }

    private static string BuildOwnerImagesCacheKey(ImageOwnerType ownerType, string ownerId, ImageCategory? category)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedCategory = category.HasValue ? category.Value.ToString() : "all";
        return $"images:owner:{GetCacheVersion()}:{ownerType}:{normalizedOwnerId}:{normalizedCategory}";
    }

    private static string BuildCurrentOwnerImageCacheKey(ImageOwnerType ownerType, string ownerId, ImageCategory category)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        return $"images:current:{GetCacheVersion()}:{ownerType}:{normalizedOwnerId}:{category}";
    }

    private static long GetCacheVersion()
    {
        return Volatile.Read(ref cacheVersion);
    }

    private static void InvalidateReadCache()
    {
        Interlocked.Increment(ref cacheVersion);
    }

    private static FilterDefinition<ImageDocument> BuildFilter(ImageSearchCriteria criteria)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = builder.Empty;

        if (criteria.Category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, criteria.Category.Value);
        }

        if (criteria.OwnerType.HasValue)
        {
            filter &= BuildOwnerTypeFilter(builder, criteria.OwnerType.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId))
        {
            filter &= builder.Eq(static document => document.OwnerId, criteria.OwnerId.Trim());
        }
        else if (criteria.OwnerIds is not null)
        {
            List<string> normalizedOwnerIds = criteria.OwnerIds
                .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
                .Select(static ownerId => ownerId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            filter &= builder.In(static document => document.OwnerId, normalizedOwnerIds);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TagId))
        {
            filter &= builder.AnyEq(static document => document.TagIds, criteria.TagId.Trim());
        }

        if (criteria.IsPublished.HasValue)
        {
            filter &= builder.Eq(static document => document.IsPublished, criteria.IsPublished.Value);
        }

        if (criteria.HasOwner.HasValue)
        {
            FilterDefinition<ImageDocument> hasOwnerFilter = builder.Ne(static document => document.OwnerType, ImageOwnerType.None) &
                                                             builder.Ne(static document => document.OwnerId, null) &
                                                             builder.Ne(static document => document.OwnerId, string.Empty);
            filter &= criteria.HasOwner.Value ? hasOwnerFilter : builder.Not(hasOwnerFilter);
        }

        if (criteria.HasGeoLocation.HasValue)
        {
            FilterDefinition<ImageDocument> hasGeoLocationFilter = builder.Ne(static document => document.GeoLocation, null);
            filter &= criteria.HasGeoLocation.Value ? hasGeoLocationFilter : builder.Not(hasGeoLocationFilter);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            string escapedSearch = Regex.Escape(criteria.Search.Trim());
            BsonRegularExpression regex = new BsonRegularExpression(escapedSearch, "i");
            filter &= builder.Or(
                builder.Regex(static document => document.Id, regex),
                builder.Regex(static document => document.OriginalFileName, regex),
                builder.Regex(static document => document.Path, regex),
                builder.Regex(static document => document.Description, regex),
                builder.Regex(static document => document.ContentType, regex),
                builder.Regex(static document => document.OwnerId, regex),
                builder.Regex("altTexts.value", regex),
                builder.Regex("captions.value", regex),
                builder.Regex("credits.value", regex));
        }

        return filter;
    }

    private static FilterDefinition<ImageDocument> BuildOwnerTypeFilter(FilterDefinitionBuilder<ImageDocument> builder, ImageOwnerType ownerType)
    {
        FilterDefinition<ImageDocument> currentFilter = builder.Eq(static document => document.OwnerType, ownerType);
        return ownerType == ImageOwnerType.ParkItem
            ? builder.Or(currentFilter, builder.Eq("ownerType", "Attraction"))
            : currentFilter;
    }

    private static FilterDefinition<ImageDocument> BuildCategoryFilter(FilterDefinitionBuilder<ImageDocument> builder, ImageCategory category)
    {
        FilterDefinition<ImageDocument> currentFilter = builder.Eq(static document => document.Category, category);
        return category == ImageCategory.ParkItem
            ? builder.Or(currentFilter, builder.Eq("category", "Attraction"))
            : currentFilter;
    }

    private static SortDefinition<ImageDocument> BuildSort(ImageSearchCriteria criteria)
    {
        bool descending = !string.Equals(criteria.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        SortDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Sort;

        return (criteria.SortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("filename", true) => builder.Descending(static document => document.OriginalFileName).Descending(static document => document.CreatedAt),
            ("filename", false) => builder.Ascending(static document => document.OriginalFileName).Ascending(static document => document.CreatedAt),
            ("size", true) => builder.Descending(static document => document.SizeInBytes).Descending(static document => document.CreatedAt),
            ("size", false) => builder.Ascending(static document => document.SizeInBytes).Ascending(static document => document.CreatedAt),
            ("dimensions", true) => builder.Descending(static document => document.Width).Descending(static document => document.Height),
            ("dimensions", false) => builder.Ascending(static document => document.Width).Ascending(static document => document.Height),
            ("updated", true) => builder.Descending(static document => document.UpdatedAt),
            ("updated", false) => builder.Ascending(static document => document.UpdatedAt),
            ("created", false) => builder.Ascending(static document => document.CreatedAt),
            _ => builder.Descending(static document => document.CreatedAt),
        };
    }

    private sealed class ImageOwnerMainImageProjection
    {
        public string? Id { get; init; }

        public string? OwnerId { get; init; }
    }
}

/// <summary>
/// Repository Mongo des tags d'images.
/// </summary>
public sealed class ImageTagRepository : IImageTagRepository
{
    private static readonly TimeSpan TagCacheDuration = TimeSpan.FromMinutes(30);
    private static long tagCacheVersion;
    private readonly IMongoCollection<ImageTagDocument> collection;
    private readonly IMemoryCache cache;

    public ImageTagRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<ImageTagDocument>(settings.ImageTagsCollectionName);
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<ImageTag>> GetAllAsync(CancellationToken cancellationToken)
    {
        string cacheKey = BuildAllTagsCacheKey();
        if (this.cache.TryGetValue(cacheKey, out IReadOnlyCollection<ImageTag>? cachedTags) && cachedTags is not null)
        {
            return cachedTags;
        }

        List<ImageTagDocument> documents = await this.collection.Find(Builders<ImageTagDocument>.Filter.Empty)
            .SortBy(static document => document.Slug)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<ImageTag> tags = documents.Select(static document => document.ToDomain()).ToList();
        this.cache.Set(cacheKey, tags, TagCacheDuration);
        return tags;
    }

    public async Task<ImageTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken)
    {
        ImageTagDocument? document = await this.collection.Find(document => document.Id == tagId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ImageTag?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        ImageTagDocument? document = await this.collection.Find(document => document.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ImageTag> CreateAsync(ImageTagWriteModel tag, CancellationToken cancellationToken)
    {
        ImageTagDocument document = new ImageTagDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Slug = tag.Slug,
            Labels = CommonMongoMappers.ToDocuments(tag.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(tag.Descriptions),
            IsActive = tag.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        InvalidateTagCache();
        return document.ToDomain();
    }

    public async Task<ImageTag?> UpdateAsync(string tagId, ImageTagWriteModel tag, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageTagDocument> filter = Builders<ImageTagDocument>.Filter.Eq(static document => document.Id, tagId);
        UpdateDefinition<ImageTagDocument> update = Builders<ImageTagDocument>.Update
            .Set(static document => document.Slug, tag.Slug)
            .Set(static document => document.Labels, CommonMongoMappers.ToDocuments(tag.Labels))
            .Set(static document => document.Descriptions, CommonMongoMappers.ToDocuments(tag.Descriptions))
            .Set(static document => document.IsActive, tag.IsActive)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageTagDocument> options = new FindOneAndUpdateOptions<ImageTagDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageTagDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateTagCache();
        }

        return document?.ToDomain();
    }

    private static string BuildAllTagsCacheKey()
    {
        return $"image-tags:all:{Volatile.Read(ref tagCacheVersion)}";
    }

    private static void InvalidateTagCache()
    {
        Interlocked.Increment(ref tagCacheVersion);
    }
}
