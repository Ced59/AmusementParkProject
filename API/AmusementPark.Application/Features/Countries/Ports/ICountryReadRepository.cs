using AmusementPark.Core.Domain.Countries;

namespace AmusementPark.Application.Features.Countries.Ports;

/// <summary>
/// Port applicatif de lecture des pays.
/// </summary>
public interface ICountryReadRepository
{
    /// <summary>
    /// Retourne tous les pays connus.
    /// </summary>
    Task<IReadOnlyCollection<Country>> GetAllAsync(string? languageCode, CancellationToken cancellationToken);
}
