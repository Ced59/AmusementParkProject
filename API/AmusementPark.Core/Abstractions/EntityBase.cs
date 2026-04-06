namespace AmusementPark.Core.Abstractions;

/// <summary>
/// Représente l'entité de base du domaine, indépendante de toute technologie de persistance.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Identifiant fonctionnel de l'entité.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Date de création UTC de l'entité.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière mise à jour UTC de l'entité.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
