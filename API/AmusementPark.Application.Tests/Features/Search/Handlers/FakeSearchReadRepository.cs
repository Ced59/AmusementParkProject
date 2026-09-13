using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Search.Handlers;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Search.Queries;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Validation;
using Xunit;
using Moq;

namespace AmusementPark.Application.Tests.Features.Search.Handlers;

internal sealed class FakeSearchReadRepository : ISearchReadRepository
{
    public string? LastText { get; private set; }

    public IReadOnlyCollection<string> LastCategories { get; private set; } = Array.Empty<string>();

    public int LastPage { get; private set; }

    public IReadOnlyCollection<string> LastMatchingCountryCodes { get; private set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> LastRegionCountryCodes { get; private set; } = Array.Empty<string>();

    public int LastPageSize { get; private set; }

    public string? LastLanguageCode { get; private set; }

    public Task<SearchResultPage<SearchHitResult>> SearchAsync(
        string text,
        IReadOnlyCollection<string> categories,
        IReadOnlyCollection<string> matchingCountryCodes,
        IReadOnlyCollection<string> regionCountryCodes,
        int page,
        int pageSize,
        string languageCode,
        CancellationToken cancellationToken)
    {
        this.LastText = text;
        this.LastCategories = categories;
        this.LastMatchingCountryCodes = matchingCountryCodes;
        this.LastRegionCountryCodes = regionCountryCodes;
        this.LastPage = page;
        this.LastPageSize = pageSize;
        this.LastLanguageCode = languageCode;

        SearchResultPage<SearchHitResult> result = new SearchResultPage<SearchHitResult>(Array.Empty<SearchHitResult>(), page, pageSize, 0);
        return Task.FromResult(result);
    }
}
