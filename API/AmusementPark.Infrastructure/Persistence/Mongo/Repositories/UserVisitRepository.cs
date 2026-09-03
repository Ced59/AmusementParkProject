using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserVisitRepository : IUserVisitRepository
{
    public const int MaximumListSize = 100;

    private readonly IMongoCollection<UserVisitDocument> collection;

    public UserVisitRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<UserVisitDocument>(
            settings.UserVisitsCollectionName);
    }

    public async Task<IdempotentVisitCreationResult?> ResolveExistingCreationAsync(
        Visit requestedVisit,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedVisit);
        string normalizedOperationId = NormalizeRequired(
            clientOperationId,
            nameof(clientOperationId));
        string operationKeyHash =
            UserVisitCreationFingerprint.HashOperationKey(normalizedOperationId);
        UserVisitDocument? existing = await this.collection
            .Find(UserVisitMongoDefinitions.BuildCreationOperationFilter(
                requestedVisit.UserId,
                operationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
        return existing is null
            ? null
            : ResolveIdempotentCreation(
                existing,
                UserVisitCreationFingerprint.HashPayload(requestedVisit));
    }

    public Task<IdempotentVisitCreationResult> CreateIdempotentAsync(
        Visit visit,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.CreateIdempotentCoreAsync(
            visit,
            clientOperationId,
            null,
            cancellationToken);
    }

    public Task<IdempotentVisitCreationResult> CreateIdempotentAuditedAsync(
        Visit visit,
        string clientOperationId,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.CreateIdempotentCoreAsync(
            visit,
            clientOperationId,
            pendingAuditEvent,
            cancellationToken);
    }

    private async Task<IdempotentVisitCreationResult> CreateIdempotentCoreAsync(
        Visit visit,
        string clientOperationId,
        PassportAuditEvent? pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        string normalizedOperationId = NormalizeRequired(
            clientOperationId,
            nameof(clientOperationId));
        UserVisitDocument document = visit.ToDocument();
        document.CreationOperationKeyHash =
            UserVisitCreationFingerprint.HashOperationKey(normalizedOperationId);
        document.CreationPayloadHash = UserVisitCreationFingerprint.HashPayload(visit);
        document.CreationSnapshot = document.CreateCreationSnapshot();
        if (pendingAuditEvent is not null)
        {
            ValidatePendingAuditEvent(visit, pendingAuditEvent);
            document.PendingAuditEvents = new List<PassportAuditEventDocument>
            {
                pendingAuditEvent.ToDocument(),
            };
        }

        try
        {
            await this.collection.InsertOneAsync(
                document,
                cancellationToken: cancellationToken);
            return new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Created,
                document.CreationSnapshotToDomain());
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            UserVisitDocument? existing = await this.collection
                .Find(UserVisitMongoDefinitions.BuildCreationOperationFilter(
                    document.UserId,
                    document.CreationOperationKeyHash))
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return ResolveIdempotentCreation(existing, document.CreationPayloadHash);
        }
    }

    public async Task<Visit?> GetOwnedAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        UserVisitDocument? document = await this.collection
            .Find(UserVisitMongoDefinitions.BuildOwnedVisitFilter(visitId.Value, userId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserVisitPage> ListOwnedAsync(
        UserVisitListCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.Limit is < 1 or > MaximumListSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(criteria),
                $"The visit list size must be between 1 and {MaximumListSize}.");
        }

        List<UserVisitDocument> documents = await this.collection
            .Find(UserVisitMongoDefinitions.BuildListFilter(criteria))
            .Sort(UserVisitMongoDefinitions.BuildNewestVisitSort())
            .Project<UserVisitDocument>(UserVisitMongoDefinitions.BuildListProjection())
            .Limit(criteria.Limit + 1)
            .ToListAsync(cancellationToken);

        bool hasNextPage = documents.Count > criteria.Limit;
        List<Visit> visits = documents
            .Take(criteria.Limit)
            .Select(static document => document.ToDomain())
            .ToList();
        UserVisitListCursor? nextCursor = hasNextPage && visits.Count > 0
            ? new UserVisitListCursor(
                visits[^1].Date,
                visits[^1].UpdatedAtUtc,
                visits[^1].Id)
            : null;
        return new UserVisitPage(visits, nextCursor);
    }

    public Task<bool> TryUpdateOwnedAsync(
        Visit visit,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.TryUpdateOwnedCoreAsync(
            visit,
            expectedVersion,
            null,
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAuditedAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingAuditEvent);
        return this.TryUpdateOwnedCoreAsync(
            visit,
            expectedVersion,
            pendingAuditEvent,
            cancellationToken);
    }

    private async Task<bool> TryUpdateOwnedCoreAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent? pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (expectedVersion == long.MaxValue || visit.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The persisted visit must be exactly one version ahead of the expected version.",
                nameof(visit));
        }

        UserVisitDocument document = visit.ToDocument();
        if (pendingAuditEvent is not null)
        {
            ValidatePendingAuditEvent(visit, pendingAuditEvent);
        }

        UpdateResult result = await this.collection.UpdateOneAsync(
            UserVisitMongoDefinitions.BuildOwnedMutableVersionFilter(
                document.Id,
                document.UserId,
                expectedVersion,
                document.UpdatedAt),
            BuildDomainUpdate(document, pendingAuditEvent),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> TryConfirmOwnedVersionAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        UpdateResult result = await this.collection.UpdateOneAsync(
            UserVisitMongoDefinitions.BuildOwnedMutableVersionFilter(
                visitId.Value,
                userId,
                expectedVersion,
                DateTime.UtcNow),
            Builders<UserVisitDocument>.Update.Set(
                static document => document.Version,
                expectedVersion),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> TryDeleteOwnedAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(
            UserVisitMongoDefinitions.BuildOwnedMutableVersionFilter(
                visitId.Value,
                userId,
                expectedVersion,
                DateTime.UtcNow),
            cancellationToken);
        return result.DeletedCount == 1;
    }

    internal static IdempotentVisitCreationResult ResolveIdempotentCreation(
        UserVisitDocument existing,
        string payloadHash)
    {
        ArgumentNullException.ThrowIfNull(existing);
        bool payloadMatches = !string.IsNullOrWhiteSpace(existing.CreationPayloadHash)
            && string.Equals(
                existing.CreationPayloadHash,
                payloadHash,
                StringComparison.Ordinal);
        return payloadMatches
            ? new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Replayed,
                existing.CreationSnapshotToDomain())
            : new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Conflict,
                null);
    }

    internal static UpdateDefinition<UserVisitDocument> BuildDomainUpdate(
        UserVisitDocument document,
        PassportAuditEvent? pendingAuditEvent = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        UpdateDefinitionBuilder<UserVisitDocument> updates =
            Builders<UserVisitDocument>.Update;
        List<UpdateDefinition<UserVisitDocument>> definitions = new List<UpdateDefinition<UserVisitDocument>>
        {
            updates.Set(static item => item.ParkId, document.ParkId),
            updates.Set(static item => item.Date, document.Date),
            updates.Set(static item => item.DateSortKey, document.DateSortKey),
            updates.Set(static item => item.ServiceDayConvention, document.ServiceDayConvention),
            updates.Set(static item => item.Status, document.Status),
            updates.Set(static item => item.Privacy, document.Privacy),
            updates.Set(static item => item.Version, document.Version),
            updates.Set(static item => item.CreatedAt, document.CreatedAt),
            updates.Set(static item => item.UpdatedAt, document.UpdatedAt),
        };
        AddOptionalUpdate(definitions, updates, "timeZoneId", document.TimeZoneId);
        AddOptionalUpdate(definitions, updates, "title", document.Title);
        AddOptionalUpdate(definitions, updates, "privateNote", document.PrivateNote);
        AddOptionalUpdate(definitions, updates, "parkAssessment", document.ParkAssessment);
        AddOptionalUpdate(definitions, updates, "completedAtUtc", document.CompletedAtUtc);
        if (pendingAuditEvent is not null)
        {
            definitions.Add(updates.Push(
                static item => item.PendingAuditEvents,
                pendingAuditEvent.ToDocument()));
        }

        return updates.Combine(definitions);
    }

    private static void ValidatePendingAuditEvent(
        Visit visit,
        PassportAuditEvent auditEvent)
    {
        if (!string.Equals(auditEvent.UserId, visit.UserId, StringComparison.Ordinal)
            || !string.Equals(auditEvent.VisitId, visit.Id.Value, StringComparison.Ordinal)
            || !string.Equals(auditEvent.EntityId, visit.Id.Value, StringComparison.Ordinal)
            || auditEvent.EntityType is not (
                PassportAuditEntityType.Visit or PassportAuditEntityType.ParkAssessment)
            || auditEvent.EntityVersion != visit.Version)
        {
            throw new ArgumentException(
                "The pending audit event does not match the visit mutation.",
                nameof(auditEvent));
        }
    }

    private static void AddOptionalUpdate<TValue>(
        ICollection<UpdateDefinition<UserVisitDocument>> definitions,
        UpdateDefinitionBuilder<UserVisitDocument> updates,
        string fieldName,
        TValue? value)
    {
        definitions.Add(value is null
            ? updates.Unset(fieldName)
            : updates.Set(fieldName, value));
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return normalizedValue;
    }
}
