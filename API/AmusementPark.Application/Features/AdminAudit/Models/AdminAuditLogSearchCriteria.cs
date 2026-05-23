using AmusementPark.Application.Common.Requests;

namespace AmusementPark.Application.Features.AdminAudit.Models;

/// <summary>
/// Critères applicatifs de consultation des traces d'audit d'administration.
/// </summary>
public sealed record AdminAuditLogSearchCriteria(
    PagedQuery Paging,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? ActorUserId = null,
    string? ActorEmail = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    string? TraceId = null);
