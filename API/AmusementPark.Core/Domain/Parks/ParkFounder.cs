using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Fondateur d'un parc.
/// </summary>
public sealed class ParkFounder : AuditableEntity
{
    /// <summary>
    /// Nom du fondateur.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedText> Biography { get; set; } = new();
}
