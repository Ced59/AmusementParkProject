using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Search.Queries;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Search.Handlers;

/// <summary>
/// Handler de recherche transversale.
/// </summary>
public sealed class SearchQueryHandler : IQueryHandler<SearchQuery, ApplicationResult<SearchResultPage<SearchHitResult>>>
{
    private readonly ISearchReadRepository searchReadRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public SearchQueryHandler(ISearchReadRepository searchReadRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.searchReadRepository = searchReadRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<SearchResultPage<SearchHitResult>>> HandleAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Text))
        {
            return ApplicationResult<SearchResultPage<SearchHitResult>>.Failure(ApplicationErrors.Required(nameof(query.Text)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<SearchResultPage<SearchHitResult>>.Failure(errors);
        }

        SearchResultPage<SearchHitResult> page = await this.searchReadRepository.SearchAsync(query.Text, query.Categories, query.Paging.Page, query.Paging.PageSize, cancellationToken);
        return ApplicationResult<SearchResultPage<SearchHitResult>>.Success(page);
    }
}
