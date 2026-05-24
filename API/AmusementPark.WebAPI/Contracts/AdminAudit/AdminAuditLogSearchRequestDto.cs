using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.AdminAudit;

/// <summary>
/// Requête HTTP de consultation des traces d'audit d'administration.
/// </summary>
public sealed class AdminAuditLogSearchRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
    public int Size { get; init; } = 20;

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public string? Action { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? TraceId { get; init; }
}
