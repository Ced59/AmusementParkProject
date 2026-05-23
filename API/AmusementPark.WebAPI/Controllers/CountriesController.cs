using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Queries;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Countries;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Countries migrée en phase 6.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class CountriesController : ControllerBase
{
    private readonly IQueryHandler<GetCountriesQuery, ApplicationResult<IReadOnlyCollection<Country>>> getCountriesQueryHandler;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CountriesController"/>.
    /// </summary>
    public CountriesController(IQueryHandler<GetCountriesQuery, ApplicationResult<IReadOnlyCollection<Country>>> getCountriesQueryHandler)
    {
        this.getCountriesQueryHandler = getCountriesQueryHandler;
    }

    /// <summary>
    /// Retourne la liste des pays avec le nom localisé.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<CountryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries([FromQuery] string? lang = null, [FromQuery] PaginationRequestDto? pagination = null, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<Country>> result = await this.getCountriesQueryHandler.HandleAsync(new GetCountriesQuery(lang), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PaginationRequestDto effectivePagination = pagination ?? new PaginationRequestDto();
        PagedResponseDto<CountryDto> response = effectivePagination.ToPagedResponse(result.Value, country => country.ToHttp(lang));
        return this.Ok(response);
    }
}
