using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Countries.Queries;
using AmusementPark.Core.Domain.Countries;

namespace AmusementPark.Application.Features.Countries.Handlers;

/// <summary>
/// Handler applicatif de récupération des pays.
/// </summary>
public sealed class GetCountriesQueryHandler : IQueryHandler<GetCountriesQuery, ApplicationResult<IReadOnlyCollection<Country>>>
{
    private readonly ICountryReadRepository countryReadRepository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetCountriesQueryHandler"/>.
    /// </summary>
    public GetCountriesQueryHandler(ICountryReadRepository countryReadRepository)
    {
        this.countryReadRepository = countryReadRepository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<IReadOnlyCollection<Country>>> HandleAsync(GetCountriesQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Country> countries = await this.countryReadRepository.GetAllAsync(query.LanguageCode, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Country>>.Success(countries);
    }
}
