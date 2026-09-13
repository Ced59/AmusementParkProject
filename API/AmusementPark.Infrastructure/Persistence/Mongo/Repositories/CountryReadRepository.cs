using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des pays.
/// </summary>
public sealed class CountryReadRepository : ICountryReadRepository
{
    private readonly IMongoCollection<CountryDocument> collection;

    public CountryReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<CountryDocument>(settings.CountriesCollectionName);
    }

    public async Task<IReadOnlyCollection<Country>> GetAllAsync(string? languageCode, CancellationToken cancellationToken)
    {
        List<CountryDocument> documents = await this.collection.Find(Builders<CountryDocument>.Filter.Empty)
            .SortBy(document => document.IsoCode)
            .ToListAsync(cancellationToken);

        List<Country> countries = documents.Select(document => document.ToDomain()).ToList();

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return countries;
        }

        string normalizedLanguageCode = languageCode.Trim();

        foreach (Country country in countries)
        {
            country.Names = country.Names
                .OrderByDescending(value => string.Equals(value.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value.LanguageCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return countries;
    }
}
