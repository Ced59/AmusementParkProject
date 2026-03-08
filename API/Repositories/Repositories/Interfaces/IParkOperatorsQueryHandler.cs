using Entities.Model.Parks;

namespace Repositories.Interfaces;

public interface IParkOperatorsQueryHandler
{
    Task<IEnumerable<ParkOperator>> GetAllAsync();
    Task<ParkOperator?> GetByIdAsync(string id);
    Task<ParkOperator?> CreateAsync(ParkOperator parkOperator);
    Task<ParkOperator?> UpdateAsync(ParkOperator parkOperator);
}