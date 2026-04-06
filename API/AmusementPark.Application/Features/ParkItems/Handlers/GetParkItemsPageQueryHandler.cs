using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsPageQueryHandler : IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItem>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetParkItemsPageQueryHandler(IParkItemRepository parkItemRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkItem>>> HandleAsync(GetParkItemsPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkItem>>.Failure(errors);
        }

        PagedResult<ParkItem> page = await this.parkItemRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, query.ParkId, query.Search, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<ParkItem>>.Success(page);
    }
}
