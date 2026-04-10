using Entities.Model.Countries;

namespace Repositories.Interfaces
{
    public interface ICountriesQueryHandler
    {
        Task<List<Country>> GetAllAsync();
        Task<Country?> GetByIsoAsync(string isoCode);
    }
}