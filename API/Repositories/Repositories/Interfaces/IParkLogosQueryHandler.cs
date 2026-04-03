using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;

namespace Repositories.Interfaces;

public interface IParkLogosQueryHandler
{
    Task<ParkLogo?> GetByIdAsync(string id);
    Task<IReadOnlyList<ParkLogo>> GetByParkIdAsync(string parkId);
    Task<ParkLogo?> GetCurrentByParkIdAsync(string parkId);

    Task<ParkLogo> InsertAsync(ParkLogo logo);
    Task UpdateAsync(ParkLogo logo);

    /// <summary>
    /// Met IsCurrent = false pour tous les logos d'un parc,
    /// sauf éventuellement l'id exclu.
    /// </summary>
    Task UnsetCurrentForParkAsync(string parkId, string? excludeLogoId = null);

    Task DeleteAsync(string id);
}