using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Search.Queries;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Search.Handlers;

/// <summary>
/// Handler de recherche transversale.
/// </summary>
public sealed class SearchQueryHandler : IQueryHandler<SearchQuery, ApplicationResult<SearchResultPage<SearchHitResult>>>
{
    private readonly ISearchReadRepository searchReadRepository;
    private readonly PagedQueryValidator pagedQueryValidator;
    private readonly ICountryReferenceService countryReferenceService;

    public SearchQueryHandler(
        ISearchReadRepository searchReadRepository,
        PagedQueryValidator pagedQueryValidator,
        ICountryReferenceService countryReferenceService)
    {
        this.searchReadRepository = searchReadRepository;
        this.pagedQueryValidator = pagedQueryValidator;
        this.countryReferenceService = countryReferenceService;
    }

    public async Task<ApplicationResult<SearchResultPage<SearchHitResult>>> HandleAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        bool hasText = !string.IsNullOrWhiteSpace(query.Text);
        bool hasCategories = query.Categories.Count > 0 && query.Categories.Any(static value => !string.IsNullOrWhiteSpace(value));

        if (!hasText && !hasCategories)
        {
            return ApplicationResult<SearchResultPage<SearchHitResult>>.Failure(ApplicationErrors.Required(nameof(query.Text)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<SearchResultPage<SearchHitResult>>.Failure(errors);
        }

        IReadOnlyCollection<string> matchingCountryCodes = await this.countryReferenceService.FindCountryCodesByLocalizedSearchAsync(
            query.Text,
            cancellationToken);
        IReadOnlyCollection<string> regionCountryCodes = this.countryReferenceService.GetCountryCodesForRegion(query.Region);
        SearchResultPage<SearchHitResult> page = await this.searchReadRepository.SearchAsync(
            query.Text ?? string.Empty,
            query.Categories,
            matchingCountryCodes,
            regionCountryCodes,
            query.Paging.Page,
            query.Paging.PageSize,
            query.LanguageCode,
            cancellationToken);
        return ApplicationResult<SearchResultPage<SearchHitResult>>.Success(page);
    }
}
