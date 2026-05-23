namespace AmusementPark.Application.Features.AdminAudit.Models;

/// <summary>
/// Trace applicative minimale d'une action d'administration sensible.
/// </summary>
public sealed class AdminAuditLogEntry
{
    /// <summary>
    /// Identifiant technique de la trace.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Date UTC de l'action auditée.
    /// </summary>
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Action métier normalisée, par exemple <c>park.visibility.update</c>.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Type d'entité métier concernée.
    /// </summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Identifiant métier concerné lorsque disponible.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Identifiant de l'utilisateur à l'origine de l'action.
    /// </summary>
    public string? ActorUserId { get; init; }

    /// <summary>
    /// Email de l'utilisateur à l'origine de l'action, lorsque présent dans le token.
    /// </summary>
    public string? ActorEmail { get; init; }

    /// <summary>
    /// Rôles portés par l'utilisateur au moment de l'action.
    /// </summary>
    public IReadOnlyCollection<string> ActorRoles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Méthode HTTP utilisée.
    /// </summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>
    /// Chemin HTTP appelé.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Code HTTP retourné par l'action.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Adresse IP cliente calculée par le pipeline HTTP, après ForwardedHeaders.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User-Agent du client.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Identifiant de corrélation permettant de relier l'audit aux logs serveur.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// Métadonnées volontairement limitées et non sensibles.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
