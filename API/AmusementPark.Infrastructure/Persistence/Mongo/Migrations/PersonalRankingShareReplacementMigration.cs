using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

/// <summary>
/// Remplace une seule fois l'ancien partage de classement par SharePublication.
/// Une fois le cutover validé, l'ancienne collection n'est plus relue.
/// </summary>
public sealed class PersonalRankingShareReplacementMigration
{
    internal const string MigrationId = "share-04a-personal-ranking-v1";
    internal const int VerificationSampleSize = 10;
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private readonly IMongoCollection<LegacyUserRankingShareDocument> legacyCollection;
    private readonly IMongoCollection<SharePublicationDocument> publicationCollection;
    private readonly IMongoCollection<SharePublicationMigrationStateDocument> stateCollection;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly ISharePublicationSourceDescriptor sourceDescriptor;
    private readonly ISharePublicationPreviewBuilder previewBuilder;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PersonalRankingShareReplacementMigration> logger;

    public PersonalRankingShareReplacementMigration(
        IMongoDatabase database,
        MongoDbSettings settings,
        ISharePublicationRepository publicationRepository,
        IEnumerable<ISharePublicationSourceDescriptor> sourceDescriptors,
        IEnumerable<ISharePublicationPreviewBuilder> previewBuilders,
        ILogger<PersonalRankingShareReplacementMigration> logger)
        : this(
            database,
            settings,
            publicationRepository,
            ResolvePersonalRankingSource(sourceDescriptors),
            ResolvePersonalRankingPreviewBuilder(previewBuilders),
            TimeProvider.System,
            logger)
    {
    }

    internal PersonalRankingShareReplacementMigration(
        IMongoDatabase database,
        MongoDbSettings settings,
        ISharePublicationRepository publicationRepository,
        ISharePublicationSourceDescriptor sourceDescriptor,
        ISharePublicationPreviewBuilder previewBuilder,
        TimeProvider timeProvider,
        ILogger<PersonalRankingShareReplacementMigration> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.sourceDescriptor = sourceDescriptor
            ?? throw new ArgumentNullException(nameof(sourceDescriptor));
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (sourceDescriptor.PublicationType != SharePublicationType.PersonalRanking)
        {
            throw new ArgumentException(
                "The replacement migration requires the personal ranking source descriptor.",
                nameof(sourceDescriptor));
        }

        if (previewBuilder.PublicationType != SharePublicationType.PersonalRanking)
        {
            throw new ArgumentException(
                "The replacement migration requires the personal ranking preview builder.",
                nameof(previewBuilder));
        }

        this.legacyCollection = database.GetCollection<LegacyUserRankingShareDocument>(
            settings.UserRankingSharesCollectionName);
        this.publicationCollection = database.GetCollection<SharePublicationDocument>(
            settings.SharePublicationsCollectionName);
        this.stateCollection = database.GetCollection<SharePublicationMigrationStateDocument>(
            settings.SharePublicationMigrationsCollectionName);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await this.EnsureStateExistsAsync(cancellationToken);
        string leaseOwner = Guid.NewGuid().ToString("N");
        if (!await this.AcquireLeaseAsync(leaseOwner, cancellationToken))
        {
            return;
        }

        try
        {
            IReadOnlyList<LegacyUserRankingShareDocument> legacyShares =
                await this.LoadAndValidateLegacySharesAsync(cancellationToken);
            ShareContentPolicy policy = this.sourceDescriptor.CreateDefaultPolicy();
            foreach (LegacyUserRankingShareDocument legacyShare in legacyShares)
            {
                await this.RefreshLeaseAsync(leaseOwner, cancellationToken);
                await this.MigrateAsync(legacyShare, policy, cancellationToken);
            }

            long migratedCount = await this.publicationCollection.CountDocumentsAsync(
                static publication => publication.Type == SharePublicationType.PersonalRanking,
                cancellationToken: cancellationToken);
            if (migratedCount != legacyShares.Count)
            {
                throw new InvalidOperationException(
                    "The personal ranking share migration total does not match the legacy total.");
            }

            int verifiedSampleCount = await this.VerifyDeterministicSampleAsync(
                legacyShares,
                policy,
                cancellationToken);
            await this.CompleteAsync(
                leaseOwner,
                legacyShares.Count,
                migratedCount,
                verifiedSampleCount,
                cancellationToken);
            this.logger.LogInformation(
                "Completed personal ranking share replacement: {MigratedCount} migrated and {SampleCount} sampled.",
                migratedCount,
                verifiedSampleCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await this.RecordFailureAsync(leaseOwner, exception, cancellationToken);
            throw;
        }
    }

    internal static SharePublication BuildPublication(
        LegacyUserRankingShareDocument legacyShare,
        ShareContentPolicy policy,
        long sourceVersion)
    {
        ArgumentNullException.ThrowIfNull(legacyShare);
        ArgumentNullException.ThrowIfNull(policy);
        string legacyId = NormalizeRequired(legacyShare.Id, nameof(legacyShare.Id));
        string ownerUserId = NormalizeRequired(legacyShare.UserId, nameof(legacyShare.UserId));
        DateTime createdAtUtc = NormalizeUtc(legacyShare.CreatedAt);
        DateTime updatedAtUtc = NormalizeUtc(legacyShare.UpdatedAt);
        if (updatedAtUtc < createdAtUtc)
        {
            throw new InvalidOperationException("A legacy share update predates its creation.");
        }

        SharePublicationId publicationId = CreateDeterministicPublicationId(legacyId);
        string sourceScopeKey = PersonalRankingShareSourceScope.Create(ownerUserId);
        if (!legacyShare.IsPublic)
        {
            return SharePublication.Restore(
                publicationId,
                ownerUserId,
                SharePublicationType.PersonalRanking,
                sourceScopeKey,
                null,
                SharePublicationStatus.Draft,
                ShareVisibility.Private,
                policy,
                sourceVersion,
                0,
                0,
                null,
                null,
                createdAtUtc,
                updatedAtUtc);
        }

        if (!ShareToken.TryParse(legacyShare.ShareId, out ShareToken shareToken)
            || !legacyShare.PublishedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "A public legacy ranking share has no valid token or publication date.");
        }

        DateTime publishedAtUtc = NormalizeUtc(legacyShare.PublishedAtUtc.Value);
        if (publishedAtUtc < createdAtUtc || publishedAtUtc > updatedAtUtc)
        {
            throw new InvalidOperationException(
                "A legacy ranking share has inconsistent publication timestamps.");
        }

        return SharePublication.Restore(
            publicationId,
            ownerUserId,
            SharePublicationType.PersonalRanking,
            sourceScopeKey,
            shareToken,
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            policy,
            sourceVersion,
            1,
            1,
            publishedAtUtc,
            null,
            createdAtUtc,
            updatedAtUtc);
    }

    internal static SharePublicationId CreateDeterministicPublicationId(string legacyId)
    {
        string normalizedLegacyId = NormalizeRequired(legacyId, nameof(legacyId));
        byte[] digest = SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Concat(MigrationId, ":", normalizedLegacyId)));
        return SharePublicationId.Parse(Convert.ToHexString(digest).ToLowerInvariant());
    }

    private async Task EnsureStateExistsAsync(CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UpdateDefinition<SharePublicationMigrationStateDocument> update =
            Builders<SharePublicationMigrationStateDocument>.Update
                .SetOnInsert(static state => state.Id, MigrationId)
                .SetOnInsert(static state => state.UpdatedAtUtc, nowUtc);
        await this.stateCollection.UpdateOneAsync(
            static state => state.Id == MigrationId,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    private async Task<bool> AcquireLeaseAsync(
        string leaseOwner,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            FilterDefinition<SharePublicationMigrationStateDocument> claimable =
                Builders<SharePublicationMigrationStateDocument>.Filter.Eq(
                    static state => state.Id,
                    MigrationId)
                & Builders<SharePublicationMigrationStateDocument>.Filter.Eq(
                    static state => state.CompletedAtUtc,
                    null)
                & (Builders<SharePublicationMigrationStateDocument>.Filter.Eq(
                        static state => state.LeaseOwner,
                        null)
                    | Builders<SharePublicationMigrationStateDocument>.Filter.Lte(
                        static state => state.LeaseExpiresAtUtc,
                        nowUtc));
            UpdateDefinition<SharePublicationMigrationStateDocument> claim =
                Builders<SharePublicationMigrationStateDocument>.Update
                    .Set(static state => state.LeaseOwner, leaseOwner)
                    .Set(static state => state.LeaseExpiresAtUtc, nowUtc.Add(LeaseDuration))
                    .Set(static state => state.StartedAtUtc, nowUtc)
                    .Set(static state => state.LastFailureCode, null)
                    .Set(static state => state.UpdatedAtUtc, nowUtc);
            SharePublicationMigrationStateDocument? acquired =
                await this.stateCollection.FindOneAndUpdateAsync(
                    claimable,
                    claim,
                    new FindOneAndUpdateOptions<SharePublicationMigrationStateDocument>
                    {
                        ReturnDocument = ReturnDocument.After,
                    },
                    cancellationToken);
            if (acquired is not null)
            {
                return true;
            }

            SharePublicationMigrationStateDocument? state = await this.stateCollection
                .Find(static item => item.Id == MigrationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (state?.CompletedAtUtc is not null)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), this.timeProvider, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<LegacyUserRankingShareDocument>> LoadAndValidateLegacySharesAsync(
        CancellationToken cancellationToken)
    {
        List<LegacyUserRankingShareDocument> legacyShares = await this.legacyCollection
            .Find(Builders<LegacyUserRankingShareDocument>.Filter.Empty)
            .SortBy(static share => share.Id)
            .ToListAsync(cancellationToken);
        if (legacyShares.GroupBy(static share => share.UserId, StringComparer.Ordinal).Any(static group => group.Count() > 1))
        {
            throw new InvalidOperationException("Legacy personal ranking shares contain duplicate owners.");
        }

        if (legacyShares
            .Where(static share => share.IsPublic)
            .GroupBy(static share => share.ShareId, StringComparer.Ordinal)
            .Any(static group => group.Count() > 1))
        {
            throw new InvalidOperationException("Legacy personal ranking shares contain duplicate tokens.");
        }

        return legacyShares;
    }

    private async Task MigrateAsync(
        LegacyUserRankingShareDocument legacyShare,
        ShareContentPolicy policy,
        CancellationToken cancellationToken)
    {
        ApplicationResult<string> scopeResult = this.sourceDescriptor.ResolveSourceScopeKey(
            legacyShare.UserId,
            null);
        if (!scopeResult.IsSuccess || scopeResult.Value is null)
        {
            throw new InvalidOperationException("A legacy personal ranking owner is invalid.");
        }

        ApplicationResult<long> versionResult = await this.sourceDescriptor.GetCurrentSourceVersionAsync(
            scopeResult.Value,
            policy,
            cancellationToken);
        if (!versionResult.IsSuccess)
        {
            throw new InvalidOperationException("A personal ranking source is not stable during migration.");
        }

        SharePublication expected = BuildPublication(
            legacyShare,
            policy,
            versionResult.Value);
        SharePublication? existing = await this.publicationRepository.GetOwnedAsync(
            expected.Id,
            expected.OwnerUserId,
            cancellationToken);
        if (existing is null)
        {
            SharePublicationWriteOutcome outcome = await this.publicationRepository.CreateAsync(
                expected,
                cancellationToken);
            if (outcome == SharePublicationWriteOutcome.Success)
            {
                return;
            }

            existing = await this.publicationRepository.GetOwnedAsync(
                expected.Id,
                expected.OwnerUserId,
                cancellationToken);
        }

        if (existing is null)
        {
            throw new InvalidOperationException(
                "A migrated personal ranking publication collides with another publication.");
        }

        await this.SynchronizeAndVerifyAsync(existing, expected, cancellationToken);
    }

    private async Task SynchronizeAndVerifyAsync(
        SharePublication existing,
        SharePublication expected,
        CancellationToken cancellationToken)
    {
        EnsureLegacyIdentityMatches(existing, expected);
        if (existing.SourceVersion != expected.SourceVersion)
        {
            if (expected.SourceVersion < existing.SourceVersion)
            {
                throw new InvalidOperationException(
                    "A migrated personal ranking source version moved backwards.");
            }

            long expectedVersion = existing.Version;
            existing.MarkSourceChanged(expected.SourceVersion, expected.UpdatedAtUtc);
            SharePublicationWriteOutcome sourceOutcome = await this.publicationRepository.ReplaceAsync(
                existing,
                expectedVersion,
                cancellationToken);
            if (sourceOutcome != SharePublicationWriteOutcome.Success)
            {
                throw new InvalidOperationException(
                    "A migrated personal ranking publication changed concurrently.");
            }
        }

        if (expected.Status == SharePublicationStatus.Published
            && existing.Status == SharePublicationStatus.NeedsReview)
        {
            long expectedVersion = existing.Version;
            existing.Publish(
                expected.ShareToken!.Value,
                ShareVisibility.Unlisted,
                existing.SourceVersion,
                existing.ContentPolicy,
                existing.PublicationVersion,
                expected.UpdatedAtUtc);
            SharePublicationWriteOutcome publishOutcome = await this.publicationRepository.ReplaceAsync(
                existing,
                expectedVersion,
                cancellationToken);
            if (publishOutcome != SharePublicationWriteOutcome.Success)
            {
                throw new InvalidOperationException(
                    "A migrated personal ranking publication could not be republished.");
            }
        }

        EnsureLegacyIdentityMatches(existing, expected);
        if (existing.Status != expected.Status)
        {
            throw new InvalidOperationException(
                "A migrated personal ranking publication has an unexpected lifecycle state.");
        }
    }

    private async Task<int> VerifyDeterministicSampleAsync(
        IReadOnlyList<LegacyUserRankingShareDocument> legacyShares,
        ShareContentPolicy policy,
        CancellationToken cancellationToken)
    {
        IEnumerable<LegacyUserRankingShareDocument> candidates = legacyShares
            .Where(static share => share.IsPublic)
            .OrderBy(
                static share => CreateDeterministicPublicationId(share.Id).Value,
                StringComparer.Ordinal);
        int verifiedSampleCount = 0;
        foreach (LegacyUserRankingShareDocument legacyShare in candidates)
        {
            if (verifiedSampleCount == VerificationSampleSize)
            {
                break;
            }

            ApplicationResult<string> scopeResult = this.sourceDescriptor.ResolveSourceScopeKey(
                legacyShare.UserId,
                null);
            if (!scopeResult.IsSuccess || scopeResult.Value is null)
            {
                throw new InvalidOperationException("A sampled personal ranking owner is invalid.");
            }

            ApplicationResult<long> versionResult = await this.sourceDescriptor.GetCurrentSourceVersionAsync(
                scopeResult.Value,
                policy,
                cancellationToken);
            if (!versionResult.IsSuccess)
            {
                throw new InvalidOperationException("A sampled personal ranking source is unstable.");
            }

            SharePublication expected = BuildPublication(
                legacyShare,
                policy,
                versionResult.Value);
            SharePublication? migrated = await this.publicationRepository.GetOwnedAsync(
                expected.Id,
                expected.OwnerUserId,
                cancellationToken);
            if (migrated is null)
            {
                throw new InvalidOperationException("A sampled personal ranking publication is missing.");
            }

            EnsureLegacyIdentityMatches(migrated, expected);
            if (migrated.Status != expected.Status
                || migrated.SourceVersion != expected.SourceVersion)
            {
                throw new InvalidOperationException(
                    "A sampled personal ranking publication does not match its source snapshot.");
            }


            ApplicationResult<SharePublicationPreviewResult> previewResult =
                await this.previewBuilder.BuildAsync(
                    migrated.OwnerUserId,
                    null,
                    migrated.ContentPolicy,
                    cancellationToken);
            if (!previewResult.IsSuccess
                && previewResult.Errors.All(static error =>
                    error.Code == "share-publication.source-unavailable"))
            {
                continue;
            }

            if (!previewResult.IsSuccess
                || previewResult.Value is null
                || previewResult.Value.PersonalRanking is null
                || previewResult.Value.SourceVersion != migrated.SourceVersion
                || previewResult.Value.PolicySchemaVersion != migrated.ContentPolicy.SchemaVersion
                || previewResult.Value.DatePrecision != migrated.ContentPolicy.DatePrecision
                || !previewResult.Value.IncludedFields.SequenceEqual(
                    migrated.ContentPolicy.IncludedFields))
            {
                throw new InvalidOperationException(
                    "A sampled personal ranking public snapshot could not be reproduced.");
            }

            verifiedSampleCount++;
        }

        return verifiedSampleCount;
    }

    private async Task RefreshLeaseAsync(string leaseOwner, CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UpdateResult result = await this.stateCollection.UpdateOneAsync(
            state => state.Id == MigrationId
                && state.CompletedAtUtc == null
                && state.LeaseOwner == leaseOwner,
            Builders<SharePublicationMigrationStateDocument>.Update
                .Set(static state => state.LeaseExpiresAtUtc, nowUtc.Add(LeaseDuration))
                .Set(static state => state.UpdatedAtUtc, nowUtc),
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new InvalidOperationException("The personal ranking migration lease was lost.");
        }
    }

    private async Task CompleteAsync(
        string leaseOwner,
        long legacyCount,
        long migratedCount,
        int verifiedSampleCount,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UpdateResult result = await this.stateCollection.UpdateOneAsync(
            state => state.Id == MigrationId
                && state.CompletedAtUtc == null
                && state.LeaseOwner == leaseOwner,
            Builders<SharePublicationMigrationStateDocument>.Update
                .Set(static state => state.LegacyCount, legacyCount)
                .Set(static state => state.MigratedCount, migratedCount)
                .Set(static state => state.VerifiedSampleCount, verifiedSampleCount)
                .Set(static state => state.CompletedAtUtc, nowUtc)
                .Set(static state => state.LeaseOwner, null)
                .Set(static state => state.LeaseExpiresAtUtc, null)
                .Set(static state => state.LastFailureCode, null)
                .Set(static state => state.UpdatedAtUtc, nowUtc),
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new InvalidOperationException(
                "The personal ranking migration could not commit its cutover marker.");
        }
    }

    private async Task RecordFailureAsync(
        string leaseOwner,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.logger.LogError(exception, "Personal ranking share replacement failed before cutover.");
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await this.stateCollection.UpdateOneAsync(
                state => state.Id == MigrationId
                    && state.CompletedAtUtc == null
                    && state.LeaseOwner == leaseOwner,
                Builders<SharePublicationMigrationStateDocument>.Update
                    .Set(static state => state.LeaseOwner, null)
                    .Set(static state => state.LeaseExpiresAtUtc, null)
                    .Set(static state => state.LastFailureCode, exception.GetType().Name)
                    .Set(static state => state.UpdatedAtUtc, nowUtc),
                cancellationToken: cancellationToken);
        }
        catch (Exception recordingException) when (recordingException is not OperationCanceledException)
        {
            this.logger.LogError(
                recordingException,
                "Could not persist the personal ranking share migration failure marker.");
        }
    }

    private static void EnsureLegacyIdentityMatches(
        SharePublication actual,
        SharePublication expected)
    {
        if (actual.Id != expected.Id
            || !string.Equals(actual.OwnerUserId, expected.OwnerUserId, StringComparison.Ordinal)
            || actual.Type != SharePublicationType.PersonalRanking
            || !string.Equals(actual.SourceScopeKey, expected.SourceScopeKey, StringComparison.Ordinal)
            || actual.ShareToken != expected.ShareToken
            || !actual.ContentPolicy.HasSameSelectionAs(expected.ContentPolicy)
            || actual.PublishedAtUtc != expected.PublishedAtUtc)
        {
            throw new InvalidOperationException(
                "A migrated personal ranking publication differs from its legacy source.");
        }
    }

    private static ISharePublicationSourceDescriptor ResolvePersonalRankingSource(
        IEnumerable<ISharePublicationSourceDescriptor> sourceDescriptors)
    {
        ArgumentNullException.ThrowIfNull(sourceDescriptors);
        return sourceDescriptors.Single(
            static source => source.PublicationType == SharePublicationType.PersonalRanking);
    }

    private static ISharePublicationPreviewBuilder ResolvePersonalRankingPreviewBuilder(
        IEnumerable<ISharePublicationPreviewBuilder> previewBuilders)
    {
        ArgumentNullException.ThrowIfNull(previewBuilders);
        return previewBuilders.Single(
            static builder => builder.PublicationType == SharePublicationType.PersonalRanking);
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(
                string.Concat("A legacy share has no ", parameterName, "."));
        }

        return normalized;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
