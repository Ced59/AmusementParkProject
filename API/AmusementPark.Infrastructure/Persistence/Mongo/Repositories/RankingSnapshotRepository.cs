using System.Diagnostics.CodeAnalysis;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RankingSnapshotRepository : IRankingSnapshotRepository
{
    private const int MaximumFailureCodeLength = 200;
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

            return new RankingSnapshotBuildStartResult(RankingSnapshotBuildStartDisposition.Existing, existing);
        }
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

        RankingSnapshotChunkDocument document = chunk.ToDocument(this.GetUtcNow());
        try
        {
            await this.chunks.InsertOneAsync(document, cancellationToken: cancellationToken);
            return new RankingSnapshotChunkWriteResult(RankingSnapshotChunkWriteDisposition.Written);
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            RankingSnapshotChunkDocument? existing = await this.chunks
                .Find(item => item.SnapshotId == chunk.SnapshotId.Value && item.ChunkIndex == chunk.ChunkIndex)
                .FirstOrDefaultAsync(cancellationToken);
            bool isIdentical = existing is not null &&
                existing.EntryCount == chunk.Entries.Count &&
                existing.FirstRank == chunk.FirstRank &&
                existing.LastRank == chunk.LastRank &&
                string.Equals(existing.Checksum, chunk.Checksum.Value, StringComparison.Ordinal);
            return new RankingSnapshotChunkWriteResult(
                isIdentical
                    ? RankingSnapshotChunkWriteDisposition.AlreadyWritten
                    : RankingSnapshotChunkWriteDisposition.Conflict);
        }
    }

    public async Task<RankingSnapshotValidationResult> ValidateBuildAsync(
        RankingSnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
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
                .Find(document => document.SnapshotId == snapshotId.Value)
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
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(document => document.Id, snapshotId.Value),
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
            if (raced?.Status is RankingSnapshotStatus.Validated
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
            nowUtc);
        return new RankingSnapshotValidationResult(RankingSnapshotValidationDisposition.Validated, validated);
    }

    public async Task<bool> FailBuildAsync(
        RankingSnapshotId snapshotId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        string normalizedErrorCode = NormalizeFailureCode(errorCode);
        DateTime nowUtc = this.GetUtcNow();
        FilterDefinition<RankingSnapshotHeaderDocument> filter = Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(document => document.Id, snapshotId.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Building));
        UpdateDefinition<RankingSnapshotHeaderDocument> update = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Failed)
            .Set(document => document.FailureCode, normalizedErrorCode)
            .Set(document => document.UpdatedAt, nowUtc);
        UpdateResult result = await this.headers.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
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
            await this.ReconcilePublicationStatusesAsync(
                candidate,
                pointer.PreviousSnapshotId,
                pointer.UpdatedAtUtc,
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
            return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.Stale, pointer);
        }

        DateTime nowUtc = this.GetUtcNow();
        if (currentDocument is null)
        {
            RankingPublicationPointer firstPointer = new RankingPublicationPointer(
                candidate.ScopeKey,
                candidate.Id,
                null,
                candidate.MethodologyVersion,
                candidate.SourceRevision,
                1,
                nowUtc);
            RankingPublicationPointerDocument firstDocument = firstPointer.ToDocument(
                Guid.NewGuid().ToString("N"),
                nowUtc);
            try
            {
                await this.pointers.InsertOneAsync(firstDocument, cancellationToken: cancellationToken);
                await this.ReconcilePublicationStatusesAsync(
                    candidate,
                    null,
                    firstPointer.UpdatedAtUtc,
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
                    await this.ReconcilePublicationStatusesAsync(
                        candidate,
                        raced.PreviousSnapshotId,
                        raced.UpdatedAtUtc,
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

        RankingPublicationPointer nextPointer = new RankingPublicationPointer(
            candidate.ScopeKey,
            candidate.Id,
            pointer!.CurrentSnapshotId,
            candidate.MethodologyVersion,
            candidate.SourceRevision,
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
                await this.ReconcilePublicationStatusesAsync(
                    candidate,
                    raced.PreviousSnapshotId,
                    raced.UpdatedAtUtc,
                    cancellationToken);
                return new RankingSnapshotPublicationResult(
                    RankingSnapshotPublicationDisposition.AlreadyPublished,
                    raced);
            }

            return new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.ConcurrencyConflict,
                raced);
        }

        await this.ReconcilePublicationStatusesAsync(
            candidate,
            pointer.CurrentSnapshotId,
            nextPointer.UpdatedAtUtc,
            cancellationToken);
        return new RankingSnapshotPublicationResult(RankingSnapshotPublicationDisposition.Published, nextPointer);
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

            await this.ReconcilePublicationStatusesAsync(
                restored,
                request.ExpectedCurrentSnapshotId,
                current.UpdatedAtUtc,
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
        RankingPublicationPointer rolledBack = new RankingPublicationPointer(
            request.ScopeKey,
            request.ExpectedPreviousSnapshotId,
            request.ExpectedCurrentSnapshotId,
            previous.MethodologyVersion,
            previous.SourceRevision,
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

        await this.ReconcilePublicationStatusesAsync(
            previous,
            request.ExpectedCurrentSnapshotId,
            rolledBack.UpdatedAtUtc,
            cancellationToken);
        return new RankingSnapshotRollbackResult(RankingSnapshotRollbackDisposition.RolledBack, rolledBack);
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

        int firstRank = offset + 1;
        int expectedEntryCount = Math.Min(limit, header.EligibleEntryCount - offset);
        int lastRank = firstRank + expectedEntryCount - 1;
        int firstChunkIndex = (firstRank - 1) / header.ChunkSize;
        int lastChunkIndex = (lastRank - 1) / header.ChunkSize;
        List<RankingSnapshotChunkDocument> documents = await this.chunks
            .Find(RankingSnapshotMongoDefinitions.BuildPageChunkFilter(header.Id, firstRank, lastRank))
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
                int expectedFirstRank = (expectedChunkIndex * header.ChunkSize) + 1;
                if (chunk.SnapshotId != header.Id ||
                    chunk.ChunkIndex != expectedChunkIndex ||
                    chunk.Entries.Count != expectedChunkEntryCount ||
                    chunk.FirstRank != expectedFirstRank ||
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

                    if (entry.Rank >= firstRank && entry.Rank <= lastRank)
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
            entries[0].Rank != firstRank ||
            entries[^1].Rank != lastRank)
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
        bool changed = await this.FailBuildAsync(header.Id, errorCode, cancellationToken);
        RankingSnapshotHeader? current = await this.LoadHeaderAsync(header.Id, cancellationToken);
        if (!changed && current?.Status != RankingSnapshotStatus.Failed)
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
        RankingSnapshotId? previousSnapshotId,
        DateTime pointerUpdatedAtUtc,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.GetUtcNow();
        UpdateDefinition<RankingSnapshotHeaderDocument> currentUpdate = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Current)
            .Set(
                document => document.PublishedAtUtc,
                RankingSnapshotMongoDefinitions.ResolvePublishedAt(current, pointerUpdatedAtUtc))
            .Set(document => document.UpdatedAt, nowUtc);
        await this.headers.UpdateOneAsync(
            document => document.Id == current.Id.Value &&
                document.Status != RankingSnapshotStatus.Building &&
                document.Status != RankingSnapshotStatus.Failed,
            currentUpdate,
            cancellationToken: cancellationToken);

        if (!previousSnapshotId.HasValue || previousSnapshotId.Value == current.Id)
        {
            return;
        }

        UpdateDefinition<RankingSnapshotHeaderDocument> previousUpdate = Builders<RankingSnapshotHeaderDocument>.Update
            .Set(document => document.Status, RankingSnapshotStatus.Superseded)
            .Set(document => document.UpdatedAt, nowUtc);
        await this.headers.UpdateOneAsync(
            document => document.Id == previousSnapshotId.Value.Value &&
                document.Status != RankingSnapshotStatus.Building &&
                document.Status != RankingSnapshotStatus.Failed,
            previousUpdate,
            cancellationToken: cancellationToken);
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
            chunk.FirstRank != (chunk.ChunkIndex * header.ChunkSize) + 1)
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

        RatingTargetType expectedTargetType = scope.TargetFamily == RankingTargetFamily.Parks
            ? RatingTargetType.Park
            : RatingTargetType.ParkItem;
        return chunk.Entries.All(entry =>
            entry.TargetType == expectedTargetType &&
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

    public static FilterDefinition<RankingPublicationPointerDocument> BuildPointerVersionFilter(
        RankingScopeKey scopeKey,
        long expectedVersion)
    {
        return Builders<RankingPublicationPointerDocument>.Filter.And(
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.ScopeKey, scopeKey.Value),
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.Version, expectedVersion));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildPageChunkFilter(
        RankingSnapshotId snapshotId,
        int firstRank,
        int lastRank)
    {
        if (firstRank <= 0 || lastRank < firstRank)
        {
            throw new ArgumentOutOfRangeException(nameof(firstRank));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.SnapshotId,
                snapshotId.Value),
            Builders<RankingSnapshotChunkDocument>.Filter.Lte(document => document.FirstRank, lastRank),
            Builders<RankingSnapshotChunkDocument>.Filter.Gte(document => document.LastRank, firstRank));
    }

    public static bool IsStale(RankingPublicationPointer pointer, RankingSnapshotHeader candidate)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(candidate);
        return pointer.ScopeKey == candidate.ScopeKey &&
            pointer.MethodologyVersion == candidate.MethodologyVersion &&
            pointer.SourceRevision >= candidate.SourceRevision;
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
        DateTime pointerUpdatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (pointerUpdatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The pointer timestamp must use UTC.", nameof(pointerUpdatedAtUtc));
        }

        return snapshot.PublishedAtUtc ?? pointerUpdatedAtUtc;
    }
}
