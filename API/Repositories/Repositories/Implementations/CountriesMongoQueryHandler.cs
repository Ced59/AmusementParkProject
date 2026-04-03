using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Countries;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class CountriesMongoQueryHandler : ICountriesQueryHandler
    {
        private readonly IMongoCollection<Country> countriesCollection;

        public CountriesMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            countriesCollection = database.GetCollection<Country>(settings.CountriesCollectionName);
        }

        public async Task<List<Country>> GetAllAsync()
        {
            return await countriesCollection
                .Find(_ => true)
                .SortBy(c => c.IsoCode)
                .ToListAsync();
        }

        public async Task<Country?> GetByIsoAsync(string isoCode)
        {
            return await countriesCollection
                .Find(c => c.IsoCode == isoCode)
                .FirstOrDefaultAsync();
        }
    }
}