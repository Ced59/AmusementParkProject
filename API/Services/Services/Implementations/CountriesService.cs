using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.General.Localization;
using Dtos.Countries;
using Entities.Model.Countries;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CountriesService : ICountriesService
    {
        private readonly ICountriesQueryHandler countriesQueryHandler;

        private const string DefaultLang = "en";

        public CountriesService(ICountriesQueryHandler countriesQueryHandler)
        {
            this.countriesQueryHandler = countriesQueryHandler;
        }

        public async Task<IEnumerable<CountryDto>> GetAllAsync(string? languageCode)
        {
            string lang = (languageCode ?? DefaultLang).ToLowerInvariant();

            List<Country> countries = await countriesQueryHandler.GetAllAsync();

            return countries.Select(c => new CountryDto
            {
                IsoCode = c.IsoCode,
                Name = c.Names.Resolve(lang, DefaultLang) ?? string.Empty
            });
        }
    }
}