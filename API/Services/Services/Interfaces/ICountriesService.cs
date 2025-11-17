using Dtos.Countries;

namespace Services.Interfaces
{
    public interface ICountriesService
    {
        Task<IEnumerable<CountryDto>> GetAllAsync(string? languageCode);
    }
}