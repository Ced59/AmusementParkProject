using System.Diagnostics.CodeAnalysis;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RankingSnapshotRepository : IRankingSnapshotRepository
{
    private const int MaximumFailureCodeLength = 200;
    private const int RetainedSnapshotVersionCountPerScope = 5;
    private const int RetentionPruneBatchSize = 20;
    private const int OrphanChunkPruneBatchSize = 100;
    private static readonly TimeSpan OrphanChunkMinimumAge = TimeSpan.FromMinutes(5);
    private readonly IMongoCollection<RankingSnapshotHeaderDocument> headers;
    private readonly IMongoCollection<RankingSnapshotChunkDocument> chunks;
    private readonly IMongoCollection<RankingPublicationPointerDocument> pointers;
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly RankingSnapshotChecksumCalculator checksumCalculator;
    private readonly RankingSnapshotIntegrityValidator integrityValidator;
    private readonly TimeProvider timeProvider;

    public RankingSnapshotRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IRankingScopeRegistry scopeRegistry,
        RankingSnapshotChecksumCalculator checksumCalculator,
        RankingSnapshotIntegrityValidator integrityValidator,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scopeRegistry);
        ArgumentNullException.ThrowIfNull(checksumCalculator);
        ArgumentNullException.ThrowIfNull(integrityValidator);

        this.headers = database.GetCollection<RankingSnapshotHeaderDocument>(
            settings.RatingRankingSnapshotHeadersCollectionName);
        this.chunks = database.GetCollection<RankingSnapshotChunkDocument>(
            settings.RatingRankingSnapshotChunksCollectionName);
        this.pointers = database.GetCollection<RankingPublicationPointerDocument>(
            settings.RatingRankingPublicationPointersCollectionName);
        this.scopeRegistry = scopeRegistry;
        this.checksumCalculator = checksumCalculator;
        this.integrityValidator = integrityValidator;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RankingSnapshotBuildStartResult> StartBuildAsync(
        StartRankingSnapshotBuildRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!this.TryResolveScope(request.ScopeKey, request.MethodologyVersion, out RankingScopeDefinition? scope))
        {
            return new RankingSnapshotBuildStartResult(RankingSnapshotBuildStartDisposition.Conflict, null);
        }

        await this.PruneOrphanedChunksAsync(request.ScopeKey, cancellationToken);
        DateTime nowUtc = this.GetUtcNow();
        int chunkCount = request.EligibleEntryCount == 0
            ? 0
            : ((request.EligibleEntryCount - 1) / scope.PageSize) + 1;
        RankingSnapshotHeader header = new RankingSnapshotHeader(
            RankingSnapshotId.Parse(Guid.NewGuid().ToString("N")),
            scope.Key,
            scope.MethodologyVersion,
            request.SourceRevision,
            RankingSnapshotStatus.Building,
            request.TotalEntryCount,
            request.EligibleEntryCount,
            scope.PageSize,
            chunkCount,
            request.Checksum,
            nowUtc);
        RankingSnapshotHeaderDocument document = header.ToDocument(nowUtc);

        try
        {
            await this.headers.InsertOneAsync(document, cancellationToken: cancellationToken);
            return new RankingSnapshotBuildStartResult(RankingSnapshotBuildStartDisposition.Created, header);
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            RankingSnapshotHeaderDocument? existingDocument = await this.headers
                .Find(RankingSnapshotMongoDefinitions.BuildHeaderNaturalKeyFilter(
                    scope.Key,
                    scope.MethodologyVersion,
                    request.SourceRevision))
                .FirstOrDefaultAsync(cancellationToken);
            if (!TryMapHeader(existingDocument, out RankingSnapshotHeader? existing) ||
                !HasSameBuildDefinition(existing, header))
            {
                return new RankingSnapshotBuildStartResult(RankingSnapshotBuildStartDisposition.Conflict, existing);
            }

            if (existing.Status == RankingSnapshotStatus.Failed)
            {
                return await this.RestartExistingBuildAsync(
                    existingDocument!,
                    existing,
                    header,
                    RankingSnapshotStatus.Failed,
                    nowUtc,
                    cancellationToken);
            }

            if (request.ForceRebuild && existing.Status == RankingSnapshotStatus.Current)
            {
                bool isValid = await this.HasValidStoredChunksAsync(
                    existing,
                    scope,
                    cancellationToken);
                if (isValid)
                {
                    return new RankingSnapshotBuildStartResult(
                        RankingSnapshotBuildStartDisposition.Existing,
                        existing);
                }

                return await this.RestartExistingBuildAsync(
                    existingDocument!,
                    existing,
                    header,
                    RankingSnapshotStatus.Current,
                    nowUtc,
                    cancellationToken);
            }

            if (request.ForceRebuild && existing.Status == RankingSnapshotStatus.Superseded)
            {
                DateTime replacementGeneratedAtUtc =
                    await this.ResolveReplacementGeneratedAtUtcAsync(
                        existing.ScopeKey,
                        nowUtc,
                        cancellationToken);
                return await this.RestartExistingBuildAsync(
                    existingDocument!,
                    existing,
                    header,
                    RankingSnapshotStatus.Superseded,
                    replacementGeneratedAtUtc,
                    cancellationToken);
            }

            return new RankingSnapshotBuildStartResult(RankingSnapshotBuildStartDisposition.Existing, existing);
        }
    }

    private async Task<RankingSnapshotBuildStartResult> RestartExistingBuildAsync(
        RankingSnapshotHeaderDocument existingDocument,
        RankingSnapshotHeader existing,
        RankingSnapshotHeader expected,
        RankingSnapshotStatus expectedStatus,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        int observedBuildAttempt = RankingSnapshotMongoDefinitions.NormalizeBuildAttempt(
            existingDocument.BuildAttempt);
        int nextBuildAttempt = observedBuildAttempt + 1;
        UpdateDefinition<RankingSnapshotHeaderDocument> restartUpdate =
            Builders<RankingSnapshotHeaderDocument>.Update
                .Set(item => item.Status, RankingSnapshotStatus.Building)
                .Set(item => item.GeneratedAtUtc, nowUtc)
                .Set(item => item.UpdatedAt, nowUtc)
                .Set(item => item.BuildAttempt, nextBuildAttempt)
                .Unset(item => item.ValidatedAtUtc)
                .Unset(item => item.PublishedAtUtc)
                .Unset(item => item.FailureCode)
                .Unset(item => item.ReconciledPointerVersion);
        RankingSnapshotHeaderDocument? restartedDocument = await this.headers.FindOneAndUpdateAsync(
            RankingSnapshotMongoDefinitions.BuildHeaderRestartFilter(
                existing.Id,
                observedBuildAttempt,
                expectedStatus),
            restartUpdate,
            new FindOneAndUpdateOptions<RankingSnapshotHeaderDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (TryMapHeader(restartedDocument, out RankingSnapshotHeader? restarted))
        {
            return new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Restarted,
                restarted);
        }

        RankingSnapshotHeader? raced = await this.LoadHeaderAsync(existing.Id, cancellationToken);
        return raced is not null && HasSameBuildDefinition(raced, expected)
            ? new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Existing,
                raced)
            : new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Conflict,
                raced);
    }

    private async Task<bool> HasValidStoredChunksAsync(
        RankingSnapshotHeader header,
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        try
        {
            List<RankingSnapshotChunkDocument> storedDocuments = await this.chunks
                .Find(RankingSnapshotMongoDefinitions.BuildChunkAttemptFilter(
                    header.Id,
                    header.BuildAttempt))
                .SortBy(document => document.ChunkIndex)
                .Limit(header.ChunkCount + 1)
                .ToListAsync(cancellationToken);
            IReadOnlyCollection<RankingSnapshotChunk> storedChunks = storedDocuments
                .Select(document => document.ToDomain(header.MethodologyVersion))
                .ToArray();
            return this.integrityValidator.Validate(header, storedChunks, scope).IsValid;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private async Task<DateTime> ResolveReplacementGeneratedAtUtcAsync(
        RankingScopeKey scopeKey,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        RankingPublicationPointer? pointer = await this.GetPointerAsync(scopeKey, cancellationToken);
        return pointer is not null && nowUtc <= pointer.UpdatedAtUtc
            ? pointer.UpdatedAtUtc.AddTicks(1)
            : nowUtc;
    }

    public async Task<RankingSnapshotChunkWriteResult> WriteChunkAsync(
        RankingSnapshotChunk chunk,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        RankingSnapshotHeaderDocument? headerDocument = await this.headers
            .Find(document => document.Id == chunk.SnapshotId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (!TryMapHeader(headerDocument, out RankingSnapshotHeader? header) ||
            !this.TryResolveScope(header.ScopeKey, header.MethodologyVersion, out RankingScopeDefinition? scope))
        {
            return new RankingSnapshotChunkWriteResult(RankingSnapshotChunkWriteDisposition.BuildNotWritable);
        }

        if (header.Status != RankingSnapshotStatus.Building)
        {
            return new RankingSnapshotChunkWriteResult(RankingSnapshotChunkWriteDisposition.BuildNotWritable);
        }

        if (!IsChunkDefinitionValid(header, chunk, scope, this.checksumCalculator))
        {
            return new RankingSnapshotChunkWriteResult(RankingSnapshotChunkWriteDisposition.Conflict);
        }

        RankingSnapshotChunkDocument document = chunk.ToDocument(header.ScopeKey, this.GetUtcNow());
        int currentBuildAttempt = RankingSnapshotMongoDefinitions.NormalizeBuildAttempt(
            headerDocument.BuildAttempt);
        if (chunk.BuildAttempt != currentBuildAttempt)
        {
            return new RankingSnapshotChunkWriteResult(
                RankingSnapshotChunkWriteDisposition.BuildNotWritable);
        }

        try
        {
            await this.chunks.InsertOneAsync(document, cancellationToken: cancellationToken);
            return await this.FinalizeChunkWriteAsync(
                chunk,
                RankingSnapshotChunkWriteDisposition.Written,
                cancellationToken);
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            RankingSnapshotChunkDocument? existing = await this.chunks
                .Find(item => item.SnapshotId == chunk.SnapshotId.Value && item.ChunkIndex == chunk.ChunkIndex)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null && existing.BuildAttempt < currentBuildAttempt)
            {
                ReplaceOneResult replaceResult = await this.chunks.ReplaceOneAsync(
                    RankingSnapshotMongoDefinitions.BuildStaleChunkAttemptFilter(
                        chunk.SnapshotId,
                        chunk.ChunkIndex,
                        currentBuildAttempt),
                    document,
                    new ReplaceOptions { IsUpsert = false },
                    cancellationToken);
                if (replaceResult.MatchedCount == 1)
                {
                    return await this.FinalizeChunkWriteAsync(
                        chunk,
                        RankingSnapshotChunkWriteDisposition.Written,
                        cancellationToken);
                }

                existing = await this.chunks
                    .Find(item => item.SnapshotId == chunk.SnapshotId.Value &&
                        item.ChunkIndex == chunk.ChunkIndex)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            bool isIdentical = existing is not null &&
                existing.BuildAttempt == currentBuildAttempt &&
                IsSameChunkDefinition(existing, chunk);
            return isIdentical
                ? await this.FinalizeChunkWriteAsync(
                    chunk,
                    RankingSnapshotChunkWriteDisposition.AlreadyWritten,
                    cancellationToken)
                : new RankingSnapshotChunkWriteResult(RankingSnapshotChunkWriteDisposition.Conflict);
        }
    }

    public async Task<RankingSnapshotValidationResult> ValidateBuildAsync(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        CancellationToken cancellationToken)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        RankingSnapshotHeaderDocument? headerDocument = await this.headers
            .Find(document => document.Id == snapshotId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (headerDocument is null)
        {
            return new RankingSnapshotValidationResult(RankingSnapshotValidationDisposition.Missing, null);
        }

        if (!TryMapHeader(headerDocument, out RankingSnapshotHeader? header))
        {
            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.Failed,
                null,
                RankingSnapshotErrorCodes.BuildFailed);
        }

        if (header.BuildAttempt != expectedBuildAttempt)
        {
            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.ConcurrencyConflict,
                header);
        }

        if (header.Status is RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded)
        {
            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.AlreadyValidated,
                header);
        }

        if (header.Status != RankingSnapshotStatus.Building ||
            !this.TryResolveScope(header.ScopeKey, header.MethodologyVersion, out RankingScopeDefinition? scope))
        {
            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.BuildNotValidatable,
                header,
                header.FailureCode);
        }

        IReadOnlyCollection<RankingSnapshotChunk> mappedChunks;
        try
        {
            List<RankingSnapshotChunkDocument> chunkDocuments = await this.chunks
                .Find(RankingSnapshotMongoDefinitions.BuildChunkAttemptFilter(
                    snapshotId,
                    expectedBuildAttempt))
                .SortBy(document => document.ChunkIndex)
                .Limit(header.ChunkCount + 1)
                .ToListAsync(cancellationToken);
            mappedChunks = chunkDocuments
                .Select(document => document.ToDomain(header.MethodologyVersion))
                .ToArray();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return await this.MarkValidationFailedAsync(
                header,
                RankingSnapshotErrorCodes.BuildFailed,
                cancellationToken);
        }

        RankingSnapshotIntegrityResult integrity = this.integrityValidator.Validate(header, mappedChunks, scope);
        if (!integrity.IsValid)
        {
            return await this.MarkValidationFailedAsync(
                header,
                integrity.ErrorCode ?? RankingSnapshotErrorCodes.BuildFailed,
                cancellationToken);
        }

        DateTime nowUtc = this.GetUtcNow();
        FilterDefinition<RankingSnapshotHeaderDocument> filter = Builders<RankingSnapshotHeaderDocument>.Filter.And(
            RankingSnapshotMongoDefinitions.BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Building));
        UpdateDefinition<RankingSnapshotHeaderDocument> update = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Validated)
            .Set(document => document.ValidatedAtUtc, nowUtc)
            .Set(document => document.UpdatedAt, nowUtc)
            .Unset(document => document.FailureCode);
        UpdateResult result = await this.headers.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            RankingSnapshotHeader? raced = await this.LoadHeaderAsync(snapshotId, cancellationToken);
            if (raced?.BuildAttempt == expectedBuildAttempt &&
                raced.Status is RankingSnapshotStatus.Validated
                or RankingSnapshotStatus.Current
                or RankingSnapshotStatus.Superseded)
            {
                return new RankingSnapshotValidationResult(
                    RankingSnapshotValidationDisposition.AlreadyValidated,
                    raced);
            }

            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.ConcurrencyConflict,
                raced);
        }

        RankingSnapshotHeader validated = new RankingSnapshotHeader(
            header.Id,
            header.ScopeKey,
            header.MethodologyVersion,
            header.SourceRevision,
            RankingSnapshotStatus.Validated,
            header.TotalEntryCount,
            header.EligibleEntryCount,
            header.ChunkSize,
            header.ChunkCount,
            header.Checksum,
            header.GeneratedAtUtc,
            nowUtc,
            buildAttempt: header.BuildAttempt);
        return new RankingSnapshotValidationResult(RankingSnapshotValidationDisposition.Validated, validated);
    }

    public async Task<bool> FailBuildAsync(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        string errorCode,
        CancellationToken cancellationToken)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        string normalizedErrorCode = NormalizeFailureCode(errorCode);
        DateTime nowUtc = this.GetUtcNow();
        FilterDefinition<RankingSnapshotHeaderDocument> filter = Builders<RankingSnapshotHeaderDocument>.Filter.And(
            RankingSnapshotMongoDefinitions.BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Building));
        UpdateDefinition<RankingSnapshotHeaderDocument> update = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Failed)
            .Set(document => document.FailureCode, normalizedErrorCode)
            .Set(document => document.UpdatedAt, nowUtc);
        RankingSnapshotHeaderDocument? failedDocument = await this.headers.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<RankingSnapshotHeaderDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (failedDocument is null)
        {
            return false;
        }

        await this.PruneTerminalSnapshotsAsync(
            RankingScopeKey.Parse(failedDocument.ScopeKey),
            cancellationToken);
        return true;
    }

    public async Task<RankingSnapshotPublicationResult> PublishAsync(
        RankingSnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
        RankingSnapshotHeader? candidate = await this.LoadHeaderAsync(snapshotId, cancellationToken);
        if (candidate is null)
        {
            return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.Missing, null);
        }

        if (!this.TryResolveScope(
                candidate.ScopeKey,
                candidate.MethodologyVersion,
                out RankingScopeDefinition? candidateScope) ||
            !RankingSnapshotMongoDefinitions.IsPublishableForScope(candidate, candidateScope))
        {
            return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.InvalidSnapshot, null);
        }

        RankingPublicationPointerDocument? currentDocument = await this.pointers
            .Find(document => document.ScopeKey == candidate.ScopeKey.Value)
            .FirstOrDefaultAsync(cancellationToken);
        RankingPublicationPointer? pointer = null;
        if (currentDocument is not null && !TryMapPointer(currentDocument, out pointer))
        {
            return new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.ConcurrencyConflict,
                null);
        }

        if (pointer?.CurrentSnapshotId == candidate.Id)
        {
            await this.ReconcileAndPruneAsync(
                candidate,
                pointer,
                cancellationToken);
            return new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.AlreadyPublished,
                pointer);
        }

        if (candidate.Status != RankingSnapshotStatus.Validated)
        {
            return new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.InvalidSnapshot,
                pointer);
        }

        if (pointer is not null && RankingSnapshotMongoDefinitions.IsStale(pointer, candidate))
        {
            await this.PruneTerminalSnapshotsAsync(candidate.ScopeKey, cancellationToken);
            return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.Stale, pointer);
        }

        DateTime nowUtc = this.GetUtcNow();
        if (currentDocument is null)
        {
            RankingPublicationPointer firstPointer = new RankingPublicationPointer(
                candidate.ScopeKey,
                candidate.Id,
                nowUtc,
                null,
                null,
                candidate.MethodologyVersion,
                candidate.SourceRevision,
                candidate.SourceRevision,
                1,
                nowUtc);
            RankingPublicationPointerDocument firstDocument = firstPointer.ToDocument(
                Guid.NewGuid().ToString("N"),
                nowUtc);
            try
            {
                await this.pointers.InsertOneAsync(firstDocument, cancellationToken: cancellationToken);
                await this.ReconcileAndPruneAsync(
                    candidate,
                    firstPointer,
                    cancellationToken);
                return new RankingSnapshotPublicationResult(
                    RankingSnapshotPublicationDisposition.Published,
                    firstPointer);
            }
            catch (MongoWriteException exception) when (IsDuplicateKey(exception))
            {
                RankingPublicationPointer? raced = await this.GetPointerAsync(candidate.ScopeKey, cancellationToken);
                if (raced?.CurrentSnapshotId == candidate.Id)
                {
                    await this.ReconcileAndPruneAsync(
                        candidate,
                        raced,
                        cancellationToken);
                    return new RankingSnapshotPublicationResult(
                        RankingSnapshotPublicationDisposition.AlreadyPublished,
                        raced);
                }

                return new RankingSnapshotPublicationResult(
                    RankingSnapshotPublicationDisposition.ConcurrencyConflict,
                    raced);
            }
        }

        DateTime previousSnapshotPublishedAtUtc = await this.ResolveSnapshotPublishedAtAsync(
            pointer!.CurrentSnapshotId,
            pointer.CurrentSnapshotPublishedAtUtc,
            cancellationToken);
        RankingPublicationPointer nextPointer = new RankingPublicationPointer(
            candidate.ScopeKey,
            candidate.Id,
            nowUtc,
            pointer.CurrentSnapshotId,
            previousSnapshotPublishedAtUtc,
            candidate.MethodologyVersion,
            candidate.SourceRevision,
            Math.Max(pointer.HighestPublishedSourceRevision, candidate.SourceRevision),
            pointer.Version + 1,
            nowUtc);
        RankingPublicationPointerDocument replacement = nextPointer.ToDocument(currentDocument.Id, currentDocument.CreatedAt);
        ReplaceOneResult replaceResult = await this.pointers.ReplaceOneAsync(
            RankingSnapshotMongoDefinitions.BuildPointerVersionFilter(candidate.ScopeKey, pointer.Version),
            replacement,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        if (replaceResult.MatchedCount != 1)
        {
            RankingPublicationPointer? raced = await this.GetPointerAsync(candidate.ScopeKey, cancellationToken);
            if (raced?.CurrentSnapshotId == candidate.Id)
            {
                await this.ReconcileAndPruneAsync(
                    candidate,
                    raced,
                    cancellationToken);
                return new RankingSnapshotPublicationResult(
                    RankingSnapshotPublicationDisposition.AlreadyPublished,
                    raced);
            }

            return new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.ConcurrencyConflict,
                raced);
        }

        await this.ReconcileAndPruneAsync(
            candidate,
            nextPointer,
            cancellationToken);
        return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.Published, nextPointer);
    }

    public async Task<RankingSnapshotRetirementResult> RetirePublicationAsync(
        RetireRankingPublicationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceRevision < 0 ||
            !this.TryResolveScope(
                request.ScopeKey,
                request.MethodologyVersion,
                out RankingScopeDefinition? _))
        {
            return new RankingSnapshotRetirementResult(
                RankingSnapshotRetirementDisposition.ConcurrencyConflict,
                null);
        }

        RankingPublicationPointer? pointer = await this.GetPointerAsync(
            request.ScopeKey,
            cancellationToken);
        if (pointer is null)
        {
            return new RankingSnapshotRetirementResult(
                RankingSnapshotRetirementDisposition.AlreadyUnavailable,
                null);
        }

        if (IsRetirementStale(pointer, request))
        {
            return new RankingSnapshotRetirementResult(
                RankingSnapshotRetirementDisposition.Stale,
                pointer);
        }

        DeleteResult deletion = await this.pointers.DeleteOneAsync(
            RankingSnapshotMongoDefinitions.BuildLivePointerFilter(pointer),
            cancellationToken);
        if (deletion.DeletedCount != 1)
        {
            RankingPublicationPointer? raced = await this.GetPointerAsync(
                request.ScopeKey,
                cancellationToken);
            if (raced is null)
            {
                return new RankingSnapshotRetirementResult(
                    RankingSnapshotRetirementDisposition.AlreadyUnavailable,
                    null);
            }

            return IsRetirementStale(raced, request)
                ? new RankingSnapshotRetirementResult(
                    RankingSnapshotRetirementDisposition.Stale,
                    raced)
                : new RankingSnapshotRetirementResult(
                    RankingSnapshotRetirementDisposition.ConcurrencyConflict,
                    raced);
        }

        DateTime nowUtc = this.GetUtcNow();
        UpdateDefinition<RankingSnapshotHeaderDocument> retireHeader =
            Builders<RankingSnapshotHeaderDocument>.Update
                .Set(document => document.Status, RankingSnapshotStatus.Superseded)
                .Set(document => document.UpdatedAt, nowUtc);
        await this.headers.UpdateOneAsync(
            Builders<RankingSnapshotHeaderDocument>.Filter.And(
                Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                    document => document.Id,
                    pointer.CurrentSnapshotId.Value),
                Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                    document => document.Status,
                    RankingSnapshotStatus.Current)),
            retireHeader,
            cancellationToken: cancellationToken);
        await this.PruneTerminalSnapshotsAsync(request.ScopeKey, cancellationToken);
        return new RankingSnapshotRetirementResult(
            RankingSnapshotRetirementDisposition.Retired,
            pointer);
    }

    public async Task<RankingSnapshotRollbackResult> RollbackAsync(
        RankingSnapshotRollbackRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RankingPublicationPointerDocument? currentDocument = await this.pointers
            .Find(document => document.ScopeKey == request.ScopeKey.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (!TryMapPointer(currentDocument, out RankingPublicationPointer? current))
        {
            return new RankingSnapshotRollbackResult(RankingSnapshotRollbackDisposition.Missing, null);
        }

        bool wasAlreadyRolledBack = current.CurrentSnapshotId == request.ExpectedPreviousSnapshotId &&
            current.PreviousSnapshotId == request.ExpectedCurrentSnapshotId &&
            current.Version == request.ExpectedPointerVersion + 1;
        if (wasAlreadyRolledBack)
        {
            RankingSnapshotHeader? restored = await this.LoadHeaderAsync(
                request.ExpectedPreviousSnapshotId,
                cancellationToken);
            if (restored is null || restored.ScopeKey != request.ScopeKey ||
                !this.TryResolveScope(
                    restored.ScopeKey,
                    restored.MethodologyVersion,
                    out RankingScopeDefinition? restoredScope) ||
                !RankingSnapshotMongoDefinitions.IsPublishableForScope(restored, restoredScope))
            {
                return new RankingSnapshotRollbackResult(
                    RankingSnapshotRollbackDisposition.InvalidPreviousSnapshot,
                    current);
            }

            await this.ReconcileAndPruneAsync(
                restored,
                current,
                cancellationToken);
            return new RankingSnapshotRollbackResult(
                RankingSnapshotRollbackDisposition.AlreadyRolledBack,
                current);
        }

        bool matchesExpectation = current.CurrentSnapshotId == request.ExpectedCurrentSnapshotId &&
            current.PreviousSnapshotId == request.ExpectedPreviousSnapshotId &&
            current.Version == request.ExpectedPointerVersion;
        if (!matchesExpectation)
        {
            return new RankingSnapshotRollbackResult(
                RankingSnapshotRollbackDisposition.ConcurrencyConflict,
                current);
        }

        RankingSnapshotHeader? previous = await this.LoadHeaderAsync(
            request.ExpectedPreviousSnapshotId,
            cancellationToken);
        if (previous is null || previous.ScopeKey != request.ScopeKey ||
            !this.TryResolveScope(
                previous.ScopeKey,
                previous.MethodologyVersion,
                out RankingScopeDefinition? previousScope) ||
            !RankingSnapshotMongoDefinitions.IsPublishableForScope(previous, previousScope))
        {
            return new RankingSnapshotRollbackResult(
                RankingSnapshotRollbackDisposition.InvalidPreviousSnapshot,
                current);
        }

        DateTime nowUtc = this.GetUtcNow();
        DateTime rolledBackSnapshotPublishedAtUtc = await this.ResolveSnapshotPublishedAtAsync(
            request.ExpectedCurrentSnapshotId,
            current.CurrentSnapshotPublishedAtUtc,
            cancellationToken);
        RankingPublicationPointer rolledBack = new RankingPublicationPointer(
            request.ScopeKey,
            request.ExpectedPreviousSnapshotId,
            current.PreviousSnapshotPublishedAtUtc!.Value,
            request.ExpectedCurrentSnapshotId,
            rolledBackSnapshotPublishedAtUtc,
            previous.MethodologyVersion,
            previous.SourceRevision,
            current.HighestPublishedSourceRevision,
            current.Version + 1,
            nowUtc);
        RankingPublicationPointerDocument replacement = rolledBack.ToDocument(
            currentDocument!.Id,
            currentDocument.CreatedAt);
        ReplaceOneResult result = await this.pointers.ReplaceOneAsync(
            RankingSnapshotMongoDefinitions.BuildPointerVersionFilter(request.ScopeKey, current.Version),
            replacement,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount != 1)
        {
            return new RankingSnapshotRollbackResult(
                RankingSnapshotRollbackDisposition.ConcurrencyConflict,
                await this.GetPointerAsync(request.ScopeKey, cancellationToken));
        }

        await this.ReconcileAndPruneAsync(
            previous,
            rolledBack,
            cancellationToken);
        return new RankingSnapshotRollbackResult(RankingSnapshotRollbackDisposition.RolledBack, rolledBack);
    }

    internal static bool IsRetirementStale(
        RankingPublicationPointer pointer,
        RetireRankingPublicationRequest request)
    {
        return pointer.HighestPublishedSourceRevision > request.SourceRevision
            || (pointer.HighestPublishedSourceRevision == request.SourceRevision
                && pointer.MethodologyVersion == request.MethodologyVersion);
    }

    public async Task<RankingPublicationPointer?> GetPointerAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        RankingPublicationPointerDocument? document = await this.pointers
            .Find(item => item.ScopeKey == scopeKey.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return TryMapPointer(document, out RankingPublicationPointer? pointer) ? pointer : null;
    }

    public async Task<RankingSnapshotHeader?> GetCurrentHeaderAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        CancellationToken cancellationToken)
    {
        (RankingSnapshotHeader? Header, RankingScopeDefinition? Scope) resolved =
            await this.ResolveCurrentAsync(scopeKey, methodologyVersion, cancellationToken);
        return resolved.Header;
    }

    public async Task<RankingSnapshotPage?> GetCurrentPageAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        if (offset < 0 || offset > RankingSnapshotHeader.MaximumCandidateEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (limit <= 0 || limit > RankingScopeDefinition.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        (RankingSnapshotHeader? Header, RankingScopeDefinition? Scope) resolved =
            await this.ResolveCurrentAsync(scopeKey, methodologyVersion, cancellationToken);
        RankingSnapshotHeader? header = resolved.Header;
        RankingScopeDefinition? scope = resolved.Scope;
        if (header is null || scope is null)
        {
            return null;
        }

        if (offset >= header.EligibleEntryCount)
        {
            return new RankingSnapshotPage(header, Array.Empty<RankingSnapshotEntry>(), offset, limit);
        }

        int firstPosition = offset + 1;
        int expectedEntryCount = Math.Min(limit, header.EligibleEntryCount - offset);
        int lastPosition = firstPosition + expectedEntryCount - 1;
        int firstChunkIndex = (firstPosition - 1) / header.ChunkSize;
        int lastChunkIndex = (lastPosition - 1) / header.ChunkSize;
        List<RankingSnapshotChunkDocument> documents = await this.chunks
            .Find(RankingSnapshotMongoDefinitions.BuildPageChunkFilter(
                header.Id,
                firstChunkIndex,
                lastChunkIndex))
            .SortBy(document => document.ChunkIndex)
            .Limit((lastChunkIndex - firstChunkIndex) + 1)
            .ToListAsync(cancellationToken);
        if (documents.Count != (lastChunkIndex - firstChunkIndex) + 1)
        {
            return null;
        }

        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>(expectedEntryCount);
        try
        {
            for (int index = 0; index < documents.Count; index++)
            {
                RankingSnapshotChunk chunk = documents[index].ToDomain(header.MethodologyVersion);
                int expectedChunkIndex = firstChunkIndex + index;
                int expectedChunkEntryCount = expectedChunkIndex == header.ChunkCount - 1
                    ? header.EligibleEntryCount - (expectedChunkIndex * header.ChunkSize)
                    : header.ChunkSize;
                int expectedFirstPosition = (expectedChunkIndex * header.ChunkSize) + 1;
                if (chunk.SnapshotId != header.Id ||
                    chunk.ChunkIndex != expectedChunkIndex ||
                    chunk.Entries.Count != expectedChunkEntryCount ||
                    chunk.FirstPosition != expectedFirstPosition ||
                    this.checksumCalculator.CalculateChunk(chunk.Entries) != chunk.Checksum)
                {
                    return null;
                }

                foreach (RankingSnapshotEntry entry in chunk.Entries)
                {
                    RatingTargetType expectedTargetType = scope.TargetFamily == RankingTargetFamily.Parks
                        ? RatingTargetType.Park
                        : RatingTargetType.ParkItem;
                    if (entry.Evidence.MethodologyVersion != header.MethodologyVersion ||
                        entry.TargetType != expectedTargetType)
                    {
                        return null;
                    }

                    if (entry.Position >= firstPosition && entry.Position <= lastPosition)
                    {
                        entries.Add(entry);
                    }
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return null;
        }

        if (entries.Count != expectedEntryCount ||
            entries[0].Position != firstPosition ||
            entries[^1].Position != lastPosition)
        {
            return null;
        }

        return new RankingSnapshotPage(header, entries.AsReadOnly(), offset, limit);
    }

    private async Task<RankingSnapshotValidationResult> MarkValidationFailedAsync(
        RankingSnapshotHeader header,
        string errorCode,
        CancellationToken cancellationToken)
    {
        bool changed = await this.FailBuildAsync(
            header.Id,
            header.BuildAttempt,
            errorCode,
            cancellationToken);
        RankingSnapshotHeader? current = await this.LoadHeaderAsync(header.Id, cancellationToken);
        if (!changed &&
            (current?.BuildAttempt != header.BuildAttempt ||
                current.Status != RankingSnapshotStatus.Failed))
        {
            return new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.ConcurrencyConflict,
                current,
                errorCode);
        }

        return new RankingSnapshotValidationResult(
            RankingSnapshotValidationDisposition.Failed,
            current,
            errorCode);
    }

    private async Task<(RankingSnapshotHeader? Header, RankingScopeDefinition? Scope)> ResolveCurrentAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        CancellationToken cancellationToken)
    {
        if (!this.TryResolveScope(scopeKey, methodologyVersion, out RankingScopeDefinition? scope))
        {
            return (null, null);
        }

        RankingPublicationPointer? pointer = await this.GetPointerAsync(scopeKey, cancellationToken);
        if (pointer is null || pointer.MethodologyVersion != methodologyVersion)
        {
            return (null, scope);
        }

        RankingSnapshotHeader? header = await this.LoadHeaderAsync(pointer.CurrentSnapshotId, cancellationToken);
        bool isValidatedLifecycle = header?.Status is RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded;
        if (!isValidatedLifecycle ||
            header!.ScopeKey != scopeKey ||
            header.MethodologyVersion != methodologyVersion ||
            header.SourceRevision != pointer.SourceRevision)
        {
            return (null, scope);
        }

        return (header, scope);
    }

    private async Task ReconcilePublicationStatusesAsync(
        RankingSnapshotHeader current,
        RankingPublicationPointer expectedPointer,
        CancellationToken cancellationToken)
    {
        if (current.Id != expectedPointer.CurrentSnapshotId)
        {
            return;
        }

        if (!await this.IsLivePointerAsync(expectedPointer, cancellationToken))
        {
            return;
        }

        DateTime nowUtc = this.GetUtcNow();
        UpdateDefinition<RankingSnapshotHeaderDocument> currentUpdate = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Current)
            .Set(
                document => document.PublishedAtUtc,
                RankingSnapshotMongoDefinitions.ResolvePublishedAt(
                    current,
                    expectedPointer.CurrentSnapshotPublishedAtUtc))
            .Set(document => document.ReconciledPointerVersion, expectedPointer.Version)
            .Set(document => document.UpdatedAt, nowUtc);
        await this.headers.UpdateOneAsync(
            RankingSnapshotMongoDefinitions.BuildHeaderReconciliationFilter(
                current.Id,
                expectedPointer.Version),
            currentUpdate,
            cancellationToken: cancellationToken);

        RankingSnapshotId? previousSnapshotId = expectedPointer.PreviousSnapshotId;
        if (!previousSnapshotId.HasValue || previousSnapshotId.Value == current.Id)
        {
            return;
        }

        if (!await this.IsLivePointerAsync(expectedPointer, cancellationToken))
        {
            return;
        }

        UpdateDefinition<RankingSnapshotHeaderDocument> previousUpdate = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Superseded)
            .Set(
                document => document.PublishedAtUtc,
                expectedPointer.PreviousSnapshotPublishedAtUtc!.Value)
            .Set(document => document.ReconciledPointerVersion, expectedPointer.Version)
            .Set(document => document.UpdatedAt, nowUtc);
        await this.headers.UpdateOneAsync(
            RankingSnapshotMongoDefinitions.BuildHeaderReconciliationFilter(
                previousSnapshotId.Value,
                expectedPointer.Version),
            previousUpdate,
            cancellationToken: cancellationToken);
    }

    private async Task ReconcileAndPruneAsync(
        RankingSnapshotHeader current,
        RankingPublicationPointer expectedPointer,
        CancellationToken cancellationToken)
    {
        await this.ReconcilePublicationStatusesAsync(current, expectedPointer, cancellationToken);
        await this.PruneTerminalSnapshotsAsync(expectedPointer.ScopeKey, cancellationToken);
    }

    private async Task PruneTerminalSnapshotsAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        await this.PruneOrphanedChunksAsync(scopeKey, cancellationToken);
        RankingPublicationPointer? livePointer = await this.GetPointerAsync(scopeKey, cancellationToken);
        RatingMethodologyVersion? activeMethodologyVersion = this.scopeRegistry.Definitions
            .FirstOrDefault(definition => string.Equals(
                definition.Key.Value,
                scopeKey.Value,
                StringComparison.Ordinal))
            ?.MethodologyVersion;
        List<RankingSnapshotId> protectedSnapshotIds = new List<RankingSnapshotId>();
        if (livePointer is not null)
        {
            protectedSnapshotIds.Add(livePointer.CurrentSnapshotId);
            if (livePointer.PreviousSnapshotId.HasValue &&
                livePointer.PreviousSnapshotId.Value != livePointer.CurrentSnapshotId)
            {
                protectedSnapshotIds.Add(livePointer.PreviousSnapshotId.Value);
            }

            await this.ReconcileOrphanedCurrentHeadersAsync(
                livePointer,
                cancellationToken);
        }

        int retainedTerminalCount = Math.Max(
            0,
            RetainedSnapshotVersionCountPerScope - protectedSnapshotIds.Count);
        List<RankingSnapshotHeaderDocument> candidates = await this.headers
            .Find(RankingSnapshotMongoDefinitions.BuildRetentionCandidateFilter(
                scopeKey,
                protectedSnapshotIds,
                livePointer?.HighestPublishedSourceRevision,
                activeMethodologyVersion))
            .SortByDescending(document => document.GeneratedAtUtc)
            .ThenByDescending(document => document.Id)
            .Skip(retainedTerminalCount)
            .Limit(RetentionPruneBatchSize)
            .ToListAsync(cancellationToken);

        foreach (RankingSnapshotHeaderDocument candidate in candidates)
        {
            RankingSnapshotId candidateId;
            try
            {
                candidateId = RankingSnapshotId.Parse(candidate.Id);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (candidate.Status == RankingSnapshotStatus.Failed)
            {
                int buildAttempt = RankingSnapshotMongoDefinitions.NormalizeBuildAttempt(
                    candidate.BuildAttempt);
                await this.chunks.DeleteManyAsync(
                    RankingSnapshotMongoDefinitions.BuildChunkAttemptAtMostFilter(
                        candidateId,
                        buildAttempt),
                    cancellationToken);
                await this.headers.DeleteOneAsync(
                    RankingSnapshotMongoDefinitions.BuildFailedHeaderRestartFilter(
                        candidateId,
                        buildAttempt),
                    cancellationToken);
                continue;
            }

            if (candidate.Status == RankingSnapshotStatus.Building && livePointer is not null)
            {
                int buildAttempt = RankingSnapshotMongoDefinitions.NormalizeBuildAttempt(
                    candidate.BuildAttempt);
                DeleteResult headerDeleteResult = await this.headers.DeleteOneAsync(
                    RankingSnapshotMongoDefinitions.BuildStaleBuildingHeaderPruneFilter(
                        candidateId,
                        buildAttempt,
                        livePointer.HighestPublishedSourceRevision,
                        activeMethodologyVersion),
                    cancellationToken);
                if (headerDeleteResult.DeletedCount == 1)
                {
                    await this.chunks.DeleteManyAsync(
                        RankingSnapshotMongoDefinitions.BuildChunkAttemptAtMostFilter(
                            candidateId,
                            buildAttempt),
                        cancellationToken);
                }

                continue;
            }

            if (candidate.Status == RankingSnapshotStatus.Superseded)
            {
                await this.chunks.DeleteManyAsync(
                    document => document.SnapshotId == candidate.Id,
                    cancellationToken);
                await this.headers.DeleteOneAsync(
                    RankingSnapshotMongoDefinitions.BuildSupersededHeaderPruneFilter(candidateId),
                    cancellationToken);
                continue;
            }

            if (candidate.Status == RankingSnapshotStatus.Validated && livePointer is not null)
            {
                await this.chunks.DeleteManyAsync(
                    document => document.SnapshotId == candidate.Id,
                    cancellationToken);
                await this.headers.DeleteOneAsync(
                    RankingSnapshotMongoDefinitions.BuildStaleValidatedHeaderPruneFilter(
                        candidateId,
                        livePointer.HighestPublishedSourceRevision,
                        activeMethodologyVersion),
                    cancellationToken);
            }
        }
    }

    private async Task PruneOrphanedChunksAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        DateTime staleBeforeUtc = this.GetUtcNow().Subtract(OrphanChunkMinimumAge);
        IReadOnlyCollection<BsonDocument> stages =
            RankingSnapshotMongoDefinitions.BuildOrphanChunkCleanupPipeline(
                scopeKey,
                this.headers.CollectionNamespace.CollectionName,
                staleBeforeUtc,
                OrphanChunkPruneBatchSize);
        PipelineDefinition<RankingSnapshotChunkDocument, BsonDocument> pipeline = stages.ToArray();
        List<BsonDocument> orphanDocuments = await this.chunks
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync(cancellationToken);
        string[] orphanDocumentIds = orphanDocuments
            .Where(static document => document.TryGetValue("_id", out BsonValue? id) && id.IsString)
            .Select(static document => document["_id"].AsString)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (orphanDocumentIds.Length == 0)
        {
            return;
        }

        await this.chunks.DeleteManyAsync(
            RankingSnapshotMongoDefinitions.BuildConfirmedOrphanChunkPruneFilter(
                scopeKey,
                orphanDocumentIds,
                staleBeforeUtc),
            cancellationToken);
    }

    private async Task ReconcileOrphanedCurrentHeadersAsync(
        RankingPublicationPointer livePointer,
        CancellationToken cancellationToken)
    {
        if (!await this.IsLivePointerAsync(livePointer, cancellationToken))
        {
            return;
        }

        UpdateDefinition<RankingSnapshotHeaderDocument> update =
            Builders<RankingSnapshotHeaderDocument>.Update
                .Set(document => document.Status, RankingSnapshotStatus.Superseded)
                .Set(document => document.ReconciledPointerVersion, livePointer.Version)
                .Set(document => document.UpdatedAt, this.GetUtcNow());
        await this.headers.UpdateManyAsync(
            RankingSnapshotMongoDefinitions.BuildOrphanedCurrentHeadersReconciliationFilter(
                livePointer),
            update,
            cancellationToken: cancellationToken);
    }

    private async Task<RankingSnapshotChunkWriteResult> FinalizeChunkWriteAsync(
        RankingSnapshotChunk chunk,
        RankingSnapshotChunkWriteDisposition successfulDisposition,
        CancellationToken cancellationToken)
    {
        FilterDefinition<RankingSnapshotHeaderDocument> liveAttemptFilter =
            Builders<RankingSnapshotHeaderDocument>.Filter.And(
                RankingSnapshotMongoDefinitions.BuildHeaderAttemptFilter(
                    chunk.SnapshotId,
                    chunk.BuildAttempt),
                Builders<RankingSnapshotHeaderDocument>.Filter.Ne(
                    document => document.Status,
                    RankingSnapshotStatus.Failed));
        bool attemptStillOwnsSnapshot = await this.headers
            .Find(liveAttemptFilter)
            .Limit(1)
            .AnyAsync(cancellationToken);
        if (attemptStillOwnsSnapshot)
        {
            return new RankingSnapshotChunkWriteResult(successfulDisposition);
        }

        await this.chunks.DeleteOneAsync(
            RankingSnapshotMongoDefinitions.BuildChunkIdentityAttemptFilter(
                chunk.SnapshotId,
                chunk.ChunkIndex,
                chunk.BuildAttempt),
            cancellationToken);
        return new RankingSnapshotChunkWriteResult(
            RankingSnapshotChunkWriteDisposition.BuildNotWritable);
    }

    private async Task<bool> IsLivePointerAsync(
        RankingPublicationPointer expectedPointer,
        CancellationToken cancellationToken)
    {
        RankingPublicationPointerDocument? livePointer = await this.pointers
            .Find(RankingSnapshotMongoDefinitions.BuildLivePointerFilter(expectedPointer))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return livePointer is not null;
    }

    private async Task<DateTime> ResolveSnapshotPublishedAtAsync(
        RankingSnapshotId snapshotId,
        DateTime fallbackPublishedAtUtc,
        CancellationToken cancellationToken)
    {
        RankingSnapshotHeaderDocument? document = await this.headers
            .Find(item => item.Id == snapshotId.Value)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        if (document?.PublishedAtUtc is not DateTime publishedAtUtc)
        {
            return fallbackPublishedAtUtc;
        }

        return publishedAtUtc.Kind == DateTimeKind.Utc
            ? publishedAtUtc
            : DateTime.SpecifyKind(publishedAtUtc, DateTimeKind.Utc);
    }

    private async Task<RankingSnapshotHeader?> LoadHeaderAsync(
        RankingSnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
        RankingSnapshotHeaderDocument? document = await this.headers
            .Find(item => item.Id == snapshotId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return TryMapHeader(document, out RankingSnapshotHeader? header) ? header : null;
    }

    private bool TryResolveScope(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        [NotNullWhen(true)] out RankingScopeDefinition? scope)
    {
        try
        {
            return this.scopeRegistry.TryResolve(scopeKey.Value, methodologyVersion, out scope);
        }
        catch (InvalidOperationException)
        {
            scope = null;
            return false;
        }
    }

    private DateTime GetUtcNow()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }

    private static bool IsChunkDefinitionValid(
        RankingSnapshotHeader header,
        RankingSnapshotChunk chunk,
        RankingScopeDefinition scope,
        RankingSnapshotChecksumCalculator checksumCalculator)
    {
        if (chunk.SnapshotId != header.Id ||
            chunk.ChunkIndex >= header.ChunkCount ||
            chunk.FirstPosition != (chunk.ChunkIndex * header.ChunkSize) + 1)
        {
            return false;
        }

        int expectedEntryCount = chunk.ChunkIndex == header.ChunkCount - 1
            ? header.EligibleEntryCount - (chunk.ChunkIndex * header.ChunkSize)
            : header.ChunkSize;
        if (chunk.Entries.Count != expectedEntryCount ||
            checksumCalculator.CalculateChunk(chunk.Entries) != chunk.Checksum)
        {
            return false;
        }

        return chunk.Entries.All(entry =>
            scope.AcceptsTarget(entry.TargetType, entry.ParkItemCategory) &&
            entry.Evidence.MethodologyVersion == header.MethodologyVersion);
    }

    private static bool HasSameBuildDefinition(
        RankingSnapshotHeader existing,
        RankingSnapshotHeader requested)
    {
        return existing.ScopeKey == requested.ScopeKey &&
            existing.MethodologyVersion == requested.MethodologyVersion &&
            existing.SourceRevision == requested.SourceRevision &&
            existing.TotalEntryCount == requested.TotalEntryCount &&
            existing.EligibleEntryCount == requested.EligibleEntryCount &&
            existing.ChunkSize == requested.ChunkSize &&
            existing.ChunkCount == requested.ChunkCount &&
            existing.Checksum == requested.Checksum;
    }

    private static bool IsSameChunkDefinition(
        RankingSnapshotChunkDocument existing,
        RankingSnapshotChunk requested)
    {
        return existing.EntryCount == requested.Entries.Count &&
            existing.FirstRank == requested.FirstRank &&
            existing.LastRank == requested.LastRank &&
            existing.FirstPosition == requested.FirstPosition &&
            existing.LastPosition == requested.LastPosition &&
            string.Equals(existing.Checksum, requested.Checksum.Value, StringComparison.Ordinal);
    }

    private static bool TryMapHeader(
        RankingSnapshotHeaderDocument? document,
        [NotNullWhen(true)] out RankingSnapshotHeader? header)
    {
        header = null;
        if (document is null)
        {
            return false;
        }

        try
        {
            header = document.ToDomain();
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryMapPointer(
        RankingPublicationPointerDocument? document,
        [NotNullWhen(true)] out RankingPublicationPointer? pointer)
    {
        pointer = null;
        if (document is null)
        {
            return false;
        }

        try
        {
            pointer = document.ToDomain();
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsDuplicateKey(MongoWriteException exception)
    {
        return exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;
    }

    private static string NormalizeFailureCode(string errorCode)
    {
        string normalized = errorCode?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > MaximumFailureCodeLength)
        {
            throw new ArgumentException("A bounded failure code is required.", nameof(errorCode));
        }

        return normalized;
    }
}

internal static class RankingSnapshotMongoDefinitions
{
    public static IReadOnlyCollection<BsonDocument> BuildOrphanChunkCleanupPipeline(
        RankingScopeKey scopeKey,
        string headerCollectionName,
        DateTime staleBeforeUtc,
        int maximumResultCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerCollectionName);
        if (staleBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The orphan cutoff must use UTC.", nameof(staleBeforeUtc));
        }

        if (maximumResultCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResultCount));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "scopeKey", scopeKey.Value },
                { "updatedAt", new BsonDocument("$lte", staleBeforeUtc) },
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", headerCollectionName },
                { "localField", "snapshotId" },
                { "foreignField", "_id" },
                { "as", "_snapshotHeader" },
            }),
            new BsonDocument("$match", new BsonDocument(
                "_snapshotHeader",
                new BsonDocument("$size", 0))),
            new BsonDocument("$sort", new BsonDocument
            {
                { "updatedAt", 1 },
                { "_id", 1 },
            }),
            new BsonDocument("$limit", maximumResultCount),
            new BsonDocument("$project", new BsonDocument("_id", 1)),
        };
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildConfirmedOrphanChunkPruneFilter(
        RankingScopeKey scopeKey,
        IReadOnlyCollection<string> orphanDocumentIds,
        DateTime staleBeforeUtc)
    {
        ArgumentNullException.ThrowIfNull(orphanDocumentIds);
        if (staleBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The orphan cutoff must use UTC.", nameof(staleBeforeUtc));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.ScopeKey,
                scopeKey.Value),
            Builders<RankingSnapshotChunkDocument>.Filter.In(
                document => document.Id,
                orphanDocumentIds),
            Builders<RankingSnapshotChunkDocument>.Filter.Lte(
                document => document.UpdatedAt,
                staleBeforeUtc));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderNaturalKeyFilter(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(document => document.ScopeKey, scopeKey.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.MethodologyVersion,
                methodologyVersion.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.SourceRevision,
                sourceRevision));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildFailedHeaderRestartFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        return BuildHeaderRestartFilter(
            snapshotId,
            expectedBuildAttempt,
            RankingSnapshotStatus.Failed);
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderRestartFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        RankingSnapshotStatus expectedStatus)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                expectedStatus));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderAttemptFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        FilterDefinition<RankingSnapshotHeaderDocument> attemptFilter =
            filters.Eq(document => document.BuildAttempt, expectedBuildAttempt);
        if (expectedBuildAttempt == 1)
        {
            attemptFilter = filters.Or(
                attemptFilter,
                filters.Exists(document => document.BuildAttempt, false),
                filters.Eq(document => document.BuildAttempt, 0));
        }

        return filters.And(
            filters.Eq(document => document.Id, snapshotId.Value),
            attemptFilter);
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildStaleChunkAttemptFilter(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        int currentBuildAttempt)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        if (currentBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            filters.Eq(document => document.ChunkIndex, chunkIndex),
            filters.Or(
                filters.Exists(document => document.BuildAttempt, false),
                filters.Lt(document => document.BuildAttempt, currentBuildAttempt)));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkAttemptFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        FilterDefinition<RankingSnapshotChunkDocument> attemptFilter =
            filters.Eq(document => document.BuildAttempt, expectedBuildAttempt);
        if (expectedBuildAttempt == 1)
        {
            attemptFilter = filters.Or(
                attemptFilter,
                filters.Exists(document => document.BuildAttempt, false),
                filters.Eq(document => document.BuildAttempt, 0));
        }

        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            attemptFilter);
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkIdentityAttemptFilter(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        int expectedBuildAttempt)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            BuildChunkAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.ChunkIndex,
                chunkIndex));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkAttemptAtMostFilter(
        RankingSnapshotId snapshotId,
        int maximumBuildAttempt)
    {
        if (maximumBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            filters.Or(
                filters.Lte(document => document.BuildAttempt, maximumBuildAttempt),
                filters.Exists(document => document.BuildAttempt, false)));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildRetentionCandidateFilter(
        RankingScopeKey scopeKey,
        IReadOnlyCollection<RankingSnapshotId> protectedSnapshotIds,
        long? highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        ArgumentNullException.ThrowIfNull(protectedSnapshotIds);
        if (highestPublishedSourceRevision.HasValue && highestPublishedSourceRevision.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        string[] protectedIds = protectedSnapshotIds
            .Select(static snapshotId => snapshotId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        List<FilterDefinition<RankingSnapshotHeaderDocument>> retentionConditions =
            new List<FilterDefinition<RankingSnapshotHeaderDocument>>
            {
                filters.In(
                    document => document.Status,
                    new[]
                    {
                        RankingSnapshotStatus.Superseded,
                        RankingSnapshotStatus.Failed,
                    }),
            };
        if (highestPublishedSourceRevision.HasValue)
        {
            FilterDefinition<RankingSnapshotHeaderDocument> staleSnapshotFilter =
                BuildStaleSnapshotRevisionFilter(
                    filters,
                    highestPublishedSourceRevision.Value,
                    activeMethodologyVersion);
            retentionConditions.Add(filters.And(
                filters.Eq(
                    document => document.Status,
                    RankingSnapshotStatus.Building),
                staleSnapshotFilter));
            retentionConditions.Add(filters.And(
                filters.Eq(
                    document => document.Status,
                    RankingSnapshotStatus.Validated),
                staleSnapshotFilter));
        }

        FilterDefinition<RankingSnapshotHeaderDocument> filter = filters.And(
            filters.Eq(document => document.ScopeKey, scopeKey.Value),
            filters.Or(retentionConditions));
        return protectedIds.Length == 0
            ? filter
            : filters.And(filter, filters.Nin(document => document.Id, protectedIds));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildSupersededHeaderPruneFilter(
        RankingSnapshotId snapshotId)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Id,
                snapshotId.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Superseded));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleBuildingHeaderPruneFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        if (highestPublishedSourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Building),
            BuildStaleSnapshotRevisionFilter(
                Builders<RankingSnapshotHeaderDocument>.Filter,
                highestPublishedSourceRevision,
                activeMethodologyVersion));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument>
        BuildOrphanedCurrentHeadersReconciliationFilter(RankingPublicationPointer livePointer)
    {
        ArgumentNullException.ThrowIfNull(livePointer);
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        List<string> protectedSnapshotIds = new List<string>
        {
            livePointer.CurrentSnapshotId.Value,
        };
        if (livePointer.PreviousSnapshotId.HasValue &&
            livePointer.PreviousSnapshotId.Value != livePointer.CurrentSnapshotId)
        {
            protectedSnapshotIds.Add(livePointer.PreviousSnapshotId.Value.Value);
        }

        return filters.And(
            filters.Eq(document => document.ScopeKey, livePointer.ScopeKey.Value),
            filters.Eq(document => document.Status, RankingSnapshotStatus.Current),
            filters.Nin(document => document.Id, protectedSnapshotIds),
            filters.Or(
                filters.Exists(document => document.ReconciledPointerVersion, false),
                filters.Eq(document => document.ReconciledPointerVersion, null),
                filters.Lt(document => document.ReconciledPointerVersion, livePointer.Version)));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleValidatedHeaderPruneFilter(
        RankingSnapshotId snapshotId,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        if (highestPublishedSourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Id,
                snapshotId.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Validated),
            BuildStaleSnapshotRevisionFilter(
                Builders<RankingSnapshotHeaderDocument>.Filter,
                highestPublishedSourceRevision,
                activeMethodologyVersion));
    }

    private static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleSnapshotRevisionFilter(
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        FilterDefinition<RankingSnapshotHeaderDocument> olderRevision = filters.Lt(
            document => document.SourceRevision,
            highestPublishedSourceRevision);
        if (!activeMethodologyVersion.HasValue)
        {
            return filters.Lte(
                document => document.SourceRevision,
                highestPublishedSourceRevision);
        }

        return filters.Or(
            olderRevision,
            filters.And(
                filters.Eq(
                    document => document.SourceRevision,
                    highestPublishedSourceRevision),
                filters.Ne(
                    document => document.MethodologyVersion,
                    activeMethodologyVersion.Value.Value)));
    }

    public static int NormalizeBuildAttempt(int buildAttempt)
    {
        return Math.Max(1, buildAttempt);
    }

    public static FilterDefinition<RankingPublicationPointerDocument> BuildPointerVersionFilter(
        RankingScopeKey scopeKey,
        long expectedVersion)
    {
        return Builders<RankingPublicationPointerDocument>.Filter.And(
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.ScopeKey, scopeKey.Value),
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.Version, expectedVersion));
    }

    public static FilterDefinition<RankingPublicationPointerDocument> BuildLivePointerFilter(
        RankingPublicationPointer pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        return Builders<RankingPublicationPointerDocument>.Filter.And(
            BuildPointerVersionFilter(pointer.ScopeKey, pointer.Version),
            Builders<RankingPublicationPointerDocument>.Filter.Eq(
                document => document.CurrentSnapshotId,
                pointer.CurrentSnapshotId.Value));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderReconciliationFilter(
        RankingSnapshotId snapshotId,
        long pointerVersion)
    {
        if (pointerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointerVersion));
        }

        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.Id, snapshotId.Value),
            filters.Nin(
                document => document.Status,
                new[] { RankingSnapshotStatus.Building, RankingSnapshotStatus.Failed }),
            filters.Or(
                filters.Exists(document => document.ReconciledPointerVersion, false),
                filters.Eq(document => document.ReconciledPointerVersion, null),
                filters.Lte(document => document.ReconciledPointerVersion, pointerVersion)));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildPageChunkFilter(
        RankingSnapshotId snapshotId,
        int firstChunkIndex,
        int lastChunkIndex)
    {
        if (firstChunkIndex < 0 || lastChunkIndex < firstChunkIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(firstChunkIndex));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.SnapshotId,
                snapshotId.Value),
            Builders<RankingSnapshotChunkDocument>.Filter.Gte(
                document => document.ChunkIndex,
                firstChunkIndex),
            Builders<RankingSnapshotChunkDocument>.Filter.Lte(
                document => document.ChunkIndex,
                lastChunkIndex));
    }

    public static bool IsStale(RankingPublicationPointer pointer, RankingSnapshotHeader candidate)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(candidate);
        return pointer.ScopeKey == candidate.ScopeKey &&
            (pointer.HighestPublishedSourceRevision > candidate.SourceRevision ||
                (pointer.HighestPublishedSourceRevision == candidate.SourceRevision &&
                    pointer.MethodologyVersion == candidate.MethodologyVersion &&
                    candidate.GeneratedAtUtc <= pointer.UpdatedAtUtc));
    }

    public static bool IsPublishableForScope(
        RankingSnapshotHeader snapshot,
        RankingScopeDefinition scope)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scope);
        bool hasValidatedLifecycle = snapshot.Status is RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded;
        return hasValidatedLifecycle &&
            snapshot.ScopeKey == scope.Key &&
            snapshot.MethodologyVersion == scope.MethodologyVersion &&
            scope.EvaluatePublication(snapshot.EligibleEntryCount).IsEligible;
    }

    public static DateTime ResolvePublishedAt(
        RankingSnapshotHeader snapshot,
        DateTime fallbackPublishedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (fallbackPublishedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The fallback publication timestamp must use UTC.",
                nameof(fallbackPublishedAtUtc));
        }

        return snapshot.PublishedAtUtc ?? fallbackPublishedAtUtc;
    }
}
