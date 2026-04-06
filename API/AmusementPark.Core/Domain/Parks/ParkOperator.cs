using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Exploitant de parc.
/// </summary>
public sealed class ParkOperator : AuditableEntity
{
    /// <summary>
    /// Nom de l'exploitant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description localisée.
    /// </summary>
    public List<LocalizedText> Description { get; set; } = new();
}
