namespace AmusementPark.WebAPI.Contracts.AdminAudit;

/// <summary>
/// Trace d'audit d'administration retournée au panel admin.
/// </summary>
public sealed class AdminAuditLogDto
{
    public string Id { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string? EntityId { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorEmail { get; init; }

    public IReadOnlyCollection<string> ActorRoles { get; init; } = Array.Empty<string>();

    public string HttpMethod { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string TraceId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
