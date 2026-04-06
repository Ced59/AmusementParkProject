using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Countries;

/// <summary>
/// Pays disponible dans le référentiel métier.
/// </summary>
public sealed class Country : AuditableEntity
{
    /// <summary>
    /// Code ISO alpha-2.
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>
    /// Noms localisés du pays.
    /// </summary>
    public List<LocalizedText> Names { get; set; } = new();
}
