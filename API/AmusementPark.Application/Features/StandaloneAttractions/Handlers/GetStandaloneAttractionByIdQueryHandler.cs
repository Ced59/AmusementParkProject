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

public sealed class GetStandaloneAttractionByIdQueryHandler
    : IQueryHandler<GetStandaloneAttractionByIdQuery, ApplicationResult<StandaloneAttraction>>
{
    private readonly IStandaloneAttractionRepository repository;

    public GetStandaloneAttractionByIdQueryHandler(IStandaloneAttractionRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(GetStandaloneAttractionByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(query.Id)));
        }

        StandaloneAttraction? attraction = await this.repository.GetByIdAsync(query.Id.Trim(), query.IncludeHidden, cancellationToken);
        return attraction is null
            ? ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), query.Id))
            : ApplicationResult<StandaloneAttraction>.Success(attraction);
    }
}
