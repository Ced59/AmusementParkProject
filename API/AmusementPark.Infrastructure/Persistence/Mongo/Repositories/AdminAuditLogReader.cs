using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Application.Features.AdminAudit.Results;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Lecture Mongo des traces d'audit d'administration.
/// </summary>
public sealed class AdminAuditLogReader : IAdminAuditLogReader
{
    private readonly IMongoCollection<AdminAuditLogDocument> collection;

    public AdminAuditLogReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<AdminAuditLogDocument>(settings.AdminAuditLogsCollectionName);
    }

    public async Task<PagedResult<AdminAuditLogResult>> SearchAsync(AdminAuditLogSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        FilterDefinition<AdminAuditLogDocument> filter = BuildFilter(criteria);
        int skip = (criteria.Paging.Page - 1) * criteria.Paging.PageSize;

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<AdminAuditLogDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(document => document.OccurredAtUtc)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<AdminAuditLogResult> items = documents.Select(ToResult).ToList();
        return new PagedResult<AdminAuditLogResult>(items, criteria.Paging.Page, criteria.Paging.PageSize, totalItems);
    }

    private static FilterDefinition<AdminAuditLogDocument> BuildFilter(AdminAuditLogSearchCriteria criteria)
    {
        FilterDefinitionBuilder<AdminAuditLogDocument> builder = Builders<AdminAuditLogDocument>.Filter;
        List<FilterDefinition<AdminAuditLogDocument>> filters = new List<FilterDefinition<AdminAuditLogDocument>>();

        if (criteria.FromUtc.HasValue)
        {
            filters.Add(builder.Gte(document => document.OccurredAtUtc, criteria.FromUtc.Value));
        }

        if (criteria.ToUtc.HasValue)
        {
            filters.Add(builder.Lte(document => document.OccurredAtUtc, criteria.ToUtc.Value));
        }

        AddEqualsFilter(filters, builder, document => document.ActorUserId, criteria.ActorUserId);
        AddEqualsFilter(filters, builder, document => document.ActorEmail, criteria.ActorEmail);
        AddEqualsFilter(filters, builder, document => document.Action, criteria.Action);
        AddEqualsFilter(filters, builder, document => document.EntityType, criteria.EntityType);
        AddEqualsFilter(filters, builder, document => document.EntityId, criteria.EntityId);
        AddEqualsFilter(filters, builder, document => document.TraceId, criteria.TraceId);

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private static void AddEqualsFilter(
        ICollection<FilterDefinition<AdminAuditLogDocument>> filters,
        FilterDefinitionBuilder<AdminAuditLogDocument> builder,
        System.Linq.Expressions.Expression<Func<AdminAuditLogDocument, string?>> field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        filters.Add(builder.Eq(field, value.Trim()));
    }

    private static AdminAuditLogResult ToResult(AdminAuditLogDocument document)
    {
        return new AdminAuditLogResult
        {
            Id = document.Id,
            OccurredAtUtc = document.OccurredAtUtc,
            Action = document.Action,
            EntityType = document.EntityType,
            EntityId = document.EntityId,
            ActorUserId = document.ActorUserId,
            ActorEmail = document.ActorEmail,
            ActorRoles = document.ActorRoles.ToList(),
            HttpMethod = document.HttpMethod,
            Path = document.Path,
            StatusCode = document.StatusCode,
            IpAddress = document.IpAddress,
            UserAgent = document.UserAgent,
            TraceId = document.TraceId,
            Metadata = new Dictionary<string, string>(document.Metadata, StringComparer.OrdinalIgnoreCase),
        };
    }
}
