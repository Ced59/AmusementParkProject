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
    {
        this.collection = database.GetCollection<AdminAuditLogDocument>(settings.AdminAuditLogsCollectionName);
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

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
