using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Persistance Mongo des traces d'audit d'administration.
/// </summary>
public sealed class AdminAuditLogWriter : IAdminAuditLogWriter
{
    private readonly IMongoCollection<AdminAuditLogDocument> collection;

    public AdminAuditLogWriter(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal AdminAuditLogWriter(IMongoCollection<AdminAuditLogDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task WriteAsync(AdminAuditLogEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        AdminAuditLogDocument document = new AdminAuditLogDocument
        {
            Id = entry.Id,
            CreatedAt = entry.OccurredAtUtc,
            UpdatedAt = entry.OccurredAtUtc,
            OccurredAtUtc = entry.OccurredAtUtc,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            ActorUserId = entry.ActorUserId,
            ActorEmail = entry.ActorEmail,
            ActorRoles = entry.ActorRoles.ToList(),
            HttpMethod = entry.HttpMethod,
            Path = entry.Path,
            StatusCode = entry.StatusCode,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            TraceId = entry.TraceId,
            Metadata = new Dictionary<string, string>(entry.Metadata, StringComparer.OrdinalIgnoreCase),
        };

        try
        {
            await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // A deterministic audit identifier makes a replay after a worker crash idempotent.
        }
    }

    private static IMongoCollection<AdminAuditLogDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<AdminAuditLogDocument>(
            settings.AdminAuditLogsCollectionName);
    }
}
