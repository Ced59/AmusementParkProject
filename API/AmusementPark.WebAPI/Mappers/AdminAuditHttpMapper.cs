using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Results;
using AmusementPark.WebAPI.Contracts.AdminAudit;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Conversions HTTP pour les traces d'audit d'administration.
/// </summary>
public static class AdminAuditHttpMapper
{
    public static AdminAuditLogSearchCriteria ToApplication(this AdminAuditLogSearchRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AdminAuditLogSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            NormalizeDateTime(request.FromUtc),
            NormalizeDateTime(request.ToUtc),
            NormalizeText(request.ActorUserId),
            NormalizeText(request.ActorEmail),
            NormalizeText(request.Action),
            NormalizeText(request.EntityType),
            NormalizeText(request.EntityId),
            NormalizeText(request.TraceId));
    }

    public static AdminAuditLogDto ToHttp(this AdminAuditLogResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AdminAuditLogDto
        {
            Id = result.Id,
            OccurredAtUtc = result.OccurredAtUtc,
            Action = result.Action,
            EntityType = result.EntityType,
            EntityId = result.EntityId,
            ActorUserId = result.ActorUserId,
            ActorEmail = result.ActorEmail,
            ActorRoles = result.ActorRoles.ToList(),
            HttpMethod = result.HttpMethod,
            Path = result.Path,
            StatusCode = result.StatusCode,
            IpAddress = result.IpAddress,
            UserAgent = result.UserAgent,
            TraceId = result.TraceId,
            Metadata = new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase),
        };
    }

    private static DateTime? NormalizeDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
