using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionsPageQueryHandler
    : IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly IApplicationValidator<PagedQuery> pagedQueryValidator;

    public GetStandaloneAttractionsPageQueryHandler(
        IStandaloneAttractionRepository repository,
        IApplicationValidator<PagedQuery> pagedQueryValidator)
    {
        this.repository = repository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<StandaloneAttraction>>> HandleAsync(GetStandaloneAttractionsPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> validationErrors = this.pagedQueryValidator.Validate(query.Paging);
        if (validationErrors.Count > 0)
        {
            return ApplicationResult<PagedResult<StandaloneAttraction>>.Failure(validationErrors);
        }

        PagedResult<StandaloneAttraction> page = await this.repository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.Search,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.ManufacturerId,
            cancellationToken,
            query.SortField,
            query.SortDescending);

        return ApplicationResult<PagedResult<StandaloneAttraction>>.Success(page);
    }
}
