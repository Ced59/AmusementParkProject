namespace AmusementPark.Core.Abstractions;

/// <summary>
/// Représente l'entité métier minimale indépendante de toute technologie de persistance.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Identifiant fonctionnel stable de l'entité.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
}
