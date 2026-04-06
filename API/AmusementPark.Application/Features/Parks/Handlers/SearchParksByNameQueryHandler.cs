using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de recherche de parcs par nom.
/// </summary>
public sealed class SearchParksByNameQueryHandler : IQueryHandler<SearchParksByNameQuery, ApplicationResult<PagedResult<Park>>>
{
    private readonly IParkRepository parkRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public SearchParksByNameQueryHandler(IParkRepository parkRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.parkRepository = parkRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<Park>>> HandleAsync(SearchParksByNameQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            return ApplicationResult<PagedResult<Park>>.Failure(ApplicationErrors.Required(nameof(query.Name)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<Park>>.Failure(errors);
        }

        PagedResult<Park> page = await this.parkRepository.SearchByNameAsync(query.Name, query.Paging.Page, query.Paging.PageSize, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<Park>>.Success(page);
    }
}
