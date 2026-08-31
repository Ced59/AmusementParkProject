using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;
using static AmusementPark.Infrastructure.Persistence.Mongo.Repositories.DurableBackgroundJobMongoDefinitions;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class DurableBackgroundJobRepository : IDurableBackgroundJobRepository
{
    private const int MaximumPayloadSizeBytes = 64 * 1024;
    private const int MaximumDiagnosticLimit = 500;
    private const int MaximumTextLength = 200;
    private const int MaximumCoalesceInsertAttempts = 3;
    private readonly IMongoCollection<DurableBackgroundJobDocument> collection;
    private readonly TimeProvider timeProvider;

    public DurableBackgroundJobRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<DurableBackgroundJobDocument>(
            settings.DurableBackgroundJobsCollectionName);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DurableBackgroundJob> EnqueueExactAsync(
        EnqueueExactBackgroundJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string kind = NormalizeRequired(request.Kind, nameof(request.Kind));
        string idempotencyKey = NormalizeRequired(request.IdempotencyKey, nameof(request.IdempotencyKey));
        ValidatePayload(request.PayloadVersion, request.Payload);
        ValidatePriority(request.Priority);
        TimeSpan delay = ValidateDelay(request.Delay);
        string? correlationId = NormalizeOptional(request.CorrelationId, nameof(request.CorrelationId));
        DateTime nowUtc = this.GetUtcNow();

        DurableBackgroundJobDocument document = new DurableBackgroundJobDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = kind,
            IdempotencyKey = idempotencyKey,
            PayloadVersion = request.PayloadVersion,
            Payload = request.Payload.ToBsonPayload(),
            Status = DurableBackgroundJobStatus.Pending,
            Priority = request.Priority,
            NotBeforeUtc = nowUtc.Add(delay),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CorrelationId = correlationId,
        };

        try
        {
            await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            return document.ToApplication();
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            DurableBackgroundJobDocument? existing = await this.collection
                .Find(item => item.Kind == kind && item.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is not null)
            {
                return existing.ToApplication();
            }

            throw;
        }
    }

    public async Task<DurableBackgroundJob> CoalesceAsync(
        CoalesceBackgroundJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string kind = NormalizeRequired(request.Kind, nameof(request.Kind));
        string naturalKey = NormalizeRequired(request.NaturalKey, nameof(request.NaturalKey));
        if (request.RequestedRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.RequestedRevision));
        }

        ValidatePayload(request.PayloadVersion, request.Payload);
        ValidatePriority(request.Priority);
        TimeSpan delay = ValidateDelay(request.Delay);
        string? correlationId = NormalizeOptional(request.CorrelationId, nameof(request.CorrelationId));
        DateTime nowUtc = this.GetUtcNow();
        DateTime notBeforeUtc = nowUtc.Add(delay);
        FilterDefinition<DurableBackgroundJobDocument> activeFilter = BuildActiveNaturalKeyFilter(kind, naturalKey);
        UpdateDefinition<DurableBackgroundJobDocument> coalesceUpdate = BuildCoalesceUpdate(
            request.RequestedRevision,
            request.Priority,
            notBeforeUtc,
            nowUtc,
            correlationId);
        FindOneAndUpdateOptions<DurableBackgroundJobDocument> options = new FindOneAndUpdateOptions<DurableBackgroundJobDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        DurableBackgroundJobDocument? coalesced = await this.collection.FindOneAndUpdateAsync(
            activeFilter,
            coalesceUpdate,
            options,
            cancellationToken);
        if (coalesced is not null)
        {
            return coalesced.ToApplication();
        }

        DurableBackgroundJobDocument pending = new DurableBackgroundJobDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = kind,
            NaturalKey = naturalKey,
            PayloadVersion = request.PayloadVersion,
            Payload = request.Payload.ToBsonPayload(),
            RequestedRevision = request.RequestedRevision,
            Status = DurableBackgroundJobStatus.Pending,
            Priority = request.Priority,
            NotBeforeUtc = notBeforeUtc,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CorrelationId = correlationId,
        };

        for (int insertAttempt = 0; insertAttempt < MaximumCoalesceInsertAttempts; insertAttempt++)
        {
            try
            {
                await this.collection.InsertOneAsync(pending, cancellationToken: cancellationToken);
                return pending.ToApplication();
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                DurableBackgroundJobDocument? raced = await this.collection.FindOneAndUpdateAsync(
                    activeFilter,
                    coalesceUpdate,
                    options,
                    cancellationToken);
                if (raced is not null)
                {
                    return raced.ToApplication();
                }

                if (!CanRetryCoalesceInsert(insertAttempt))
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("The bounded coalescing retry loop completed unexpectedly.");
    }

    public async Task<DurableBackgroundJob?> TryLeaseNextAsync(
        LeaseBackgroundJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyCollection<string> kinds = NormalizeKinds(request.Kinds);
        string leaseOwner = NormalizeRequired(request.LeaseOwner, nameof(request.LeaseOwner));
        TimeSpan leaseDuration = ValidateLeaseDuration(request.LeaseDuration);
        DateTime nowUtc = this.GetUtcNow();
        string leaseToken = Guid.NewGuid().ToString("N");
        DurableBackgroundJobDocument? leased = await this.TryLeaseAsync(
            BuildExpiredLeaseRunnableFilter(kinds, nowUtc),
            BuildExpiredLeaseRunnableSort(),
            leaseOwner,
            leaseToken,
            leaseDuration,
            nowUtc,
            cancellationToken);
        if (leased is not null)
        {
            return leased.ToApplication();
        }

        leased = await this.TryLeaseAsync(
            BuildScheduledRunnableFilter(kinds, nowUtc),
            BuildScheduledRunnableSort(),
            leaseOwner,
            leaseToken,
            leaseDuration,
            nowUtc,
            cancellationToken);
        return leased?.ToApplication();
    }

    private async Task<DurableBackgroundJobDocument?> TryLeaseAsync(
        FilterDefinition<DurableBackgroundJobDocument> filter,
        SortDefinition<DurableBackgroundJobDocument> sort,
        string leaseOwner,
        string leaseToken,
        TimeSpan leaseDuration,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        FindOneAndUpdateOptions<DurableBackgroundJobDocument> options = new FindOneAndUpdateOptions<DurableBackgroundJobDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = sort,
        };
        return await this.collection.FindOneAndUpdateAsync(
            filter,
            BuildLeaseUpdate(leaseOwner, leaseToken, nowUtc.Add(leaseDuration), nowUtc),
            options,
            cancellationToken);
    }

    public async Task<bool> RenewLeaseAsync(
        DurableBackgroundJobLease lease,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);

        DateTime nowUtc = this.GetUtcNow();
        TimeSpan validLeaseDuration = ValidateLeaseDuration(leaseDuration);
        UpdateDefinition<DurableBackgroundJobDocument> update = BuildRenewLeaseUpdate(
            nowUtc.Add(validLeaseDuration),
            nowUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildLeaseOwnershipFilter(lease, nowUtc),
            update,
            cancellationToken: cancellationToken);
        return WasSingleJobMatched(result);
    }

    public async Task<DurableBackgroundJobCompletionResult?> CompleteAsync(
        DurableBackgroundJobLease lease,
        long? processedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (processedRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processedRevision));
        }

        DateTime nowUtc = this.GetUtcNow();
        FindOneAndUpdateOptions<DurableBackgroundJobDocument> options = new FindOneAndUpdateOptions<DurableBackgroundJobDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };
        DurableBackgroundJobDocument? completed = await this.collection.FindOneAndUpdateAsync(
            BuildLeaseOwnershipFilter(lease, nowUtc),
            BuildCompletionUpdate(processedRevision, nowUtc),
            options,
            cancellationToken);

        return completed is null
            ? null
            : new DurableBackgroundJobCompletionResult(
                completed.Id,
                completed.Status,
                completed.RequestedRevision,
                completed.ProcessedRevision);
    }

    public async Task<bool> ScheduleRetryAsync(
        DurableBackgroundJobLease lease,
        TimeSpan delay,
        string errorCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);

        DateTime nowUtc = this.GetUtcNow();
        TimeSpan validDelay = ValidateDelay(delay);
        string normalizedErrorCode = NormalizeRequired(errorCode, nameof(errorCode));
        UpdateDefinition<DurableBackgroundJobDocument> update = BuildScheduleRetryUpdate(
            nowUtc.Add(validDelay),
            normalizedErrorCode,
            nowUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildLeaseOwnershipFilter(lease, nowUtc),
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeadLetterAsync(
        DurableBackgroundJobLease lease,
        string errorCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);

        DateTime nowUtc = this.GetUtcNow();
        string normalizedErrorCode = NormalizeRequired(errorCode, nameof(errorCode));
        UpdateDefinition<DurableBackgroundJobDocument> update = BuildDeadLetterUpdate(normalizedErrorCode, nowUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(
            BuildLeaseOwnershipFilter(lease, nowUtc),
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken)
    {
        string normalizedJobId = NormalizeRequired(jobId, nameof(jobId));
        DateTime nowUtc = this.GetUtcNow();
        FilterDefinition<DurableBackgroundJobDocument> filter = BuildCancelFilter(normalizedJobId);
        UpdateDefinition<DurableBackgroundJobDocument> update = BuildCancelUpdate(nowUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<int> ReleaseExpiredLeasesAsync(int maximumCount, CancellationToken cancellationToken)
    {
        if (maximumCount <= 0 || maximumCount > MaximumDiagnosticLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        DateTime nowUtc = this.GetUtcNow();
        FilterDefinition<DurableBackgroundJobDocument> expiredFilter = BuildExpiredLeaseFilter(nowUtc);
        List<string> ids = await this.collection
            .Find(expiredFilter)
            .SortBy(item => item.LeaseExpiresAtUtc)
            .Limit(maximumCount)
            .Project(item => item.Id)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return 0;
        }

        FilterDefinition<DurableBackgroundJobDocument> releaseFilter =
            Builders<DurableBackgroundJobDocument>.Filter.In(item => item.Id, ids)
            & expiredFilter;
        UpdateDefinition<DurableBackgroundJobDocument> update = BuildReleaseExpiredLeaseUpdate(nowUtc);
        UpdateResult result = await this.collection.UpdateManyAsync(releaseFilter, update, cancellationToken: cancellationToken);
        return checked((int)result.ModifiedCount);
    }

    public async Task<IReadOnlyCollection<DurableBackgroundJobDiagnosticItem>> ListDiagnosticsAsync(
        DurableBackgroundJobDiagnosticQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Limit <= 0 || query.Limit > MaximumDiagnosticLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit));
        }

        FilterDefinition<DurableBackgroundJobDocument> filter = Builders<DurableBackgroundJobDocument>.Filter.Empty;
        if (query.Statuses is { Count: > 0 })
        {
            filter &= Builders<DurableBackgroundJobDocument>.Filter.In(item => item.Status, query.Statuses.Distinct());
        }

        string? kind = NormalizeOptional(query.Kind, nameof(query.Kind));
        if (kind is not null)
        {
            filter &= Builders<DurableBackgroundJobDocument>.Filter.Eq(item => item.Kind, kind);
        }

        ProjectionDefinition<DurableBackgroundJobDocument> projection =
            Builders<DurableBackgroundJobDocument>.Projection.Exclude(item => item.Payload);
        List<DurableBackgroundJobDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(item => item.UpdatedAt)
            .Limit(query.Limit)
            .Project<DurableBackgroundJobDocument>(projection)
            .ToListAsync(cancellationToken);
        return documents.Select(static item => item.ToDiagnosticItem()).ToList();
    }

    private static IReadOnlyCollection<string> NormalizeKinds(IReadOnlyCollection<string> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        string[] normalizedKinds = kinds
            .Select(kind => NormalizeRequired(kind, nameof(kinds)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedKinds.Length == 0)
        {
            throw new ArgumentException("At least one job kind is required.", nameof(kinds));
        }

        return normalizedKinds;
    }

    internal static string NormalizeRequired(string value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > MaximumTextLength)
        {
            throw new ArgumentException(
                $"The value must contain between 1 and {MaximumTextLength} characters.",
                parameterName);
        }

        return normalized;
    }

    internal static bool WasSingleJobMatched(UpdateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }

    internal static bool CanRetryCoalesceInsert(int failedAttempt)
    {
        return failedAttempt >= 0 && failedAttempt < MaximumCoalesceInsertAttempts - 1;
    }

    private static string? NormalizeOptional(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(value, parameterName);
    }

    private static void ValidatePayload(int payloadVersion, JsonElement payload)
    {
        if (payloadVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadVersion));
        }

        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("A background job payload must be a JSON object.", nameof(payload));
        }

        if (Encoding.UTF8.GetByteCount(payload.GetRawText()) > MaximumPayloadSizeBytes)
        {
            throw new ArgumentException(
                $"A background job payload cannot exceed {MaximumPayloadSizeBytes} UTF-8 bytes.",
                nameof(payload));
        }
    }

    private static void ValidatePriority(int priority)
    {
        if (priority < -1000 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }
    }

    private static TimeSpan ValidateDelay(TimeSpan? delay)
    {
        TimeSpan value = delay ?? TimeSpan.Zero;
        if (value < TimeSpan.Zero || value > TimeSpan.FromDays(365))
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }

        return value;
    }

    private static TimeSpan ValidateLeaseDuration(TimeSpan leaseDuration)
    {
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        return leaseDuration;
    }

    private DateTime GetUtcNow()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }
}
