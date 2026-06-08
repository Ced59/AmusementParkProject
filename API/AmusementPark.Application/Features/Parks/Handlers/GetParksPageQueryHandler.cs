using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération paginée des parcs.
/// </summary>
public sealed class GetParksPageQueryHandler : IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>>
{
    private readonly IParkRepository parkRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetParksPageQueryHandler(IParkRepository parkRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.parkRepository = parkRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<Park>>> HandleAsync(GetParksPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<Park>>.Failure(errors);
        }

        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.HasValidCoordinates,
            cancellationToken);
        return ApplicationResult<PagedResult<Park>>.Success(page);
    }
}
