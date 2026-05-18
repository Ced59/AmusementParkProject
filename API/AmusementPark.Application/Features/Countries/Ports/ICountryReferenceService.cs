using AmusementPark.Application.Features.Countries;

namespace AmusementPark.Application.Features.Countries.Ports;

/// <summary>
/// Service applicatif de référence pour résoudre les pays et régions géographiques.
/// </summary>
public interface ICountryReferenceService
{
    /// <summary>
    /// Recherche les codes ISO des pays dont le code ou un nom localisé correspond au texte fourni.
    /// </summary>
    Task<IReadOnlyCollection<string>> FindCountryCodesByLocalizedSearchAsync(string? searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les codes ISO couverts par une région publique.
    /// </summary>
    IReadOnlyCollection<string> GetCountryCodesForRegion(WorldRegionFilter? region);
}
