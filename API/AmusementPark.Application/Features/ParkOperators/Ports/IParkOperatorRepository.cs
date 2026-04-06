using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Ports;

/// <summary>
/// Port applicatif de persistance des park operators.
/// </summary>
public interface IParkOperatorRepository
{
    /// <summary>
    /// Retourne tous les park operators.
    /// </summary>
    Task<IReadOnlyCollection<ParkOperator>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retourne un park operator par identifiant.
    /// </summary>
    Task<ParkOperator?> GetByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Crée un park operator.
    /// </summary>
    Task<ParkOperator> CreateAsync(ParkOperator entity, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour un park operator existant.
    /// </summary>
    Task<ParkOperator?> UpdateAsync(string id, ParkOperator entity, CancellationToken cancellationToken);
}
