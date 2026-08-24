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

public sealed class SearchQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPassLanguageCodeToRepository()
    {
        FakeSearchReadRepository repository = new FakeSearchReadRepository();
        Mock<ICountryReferenceService> countryReferenceService = new Mock<ICountryReferenceService>(MockBehavior.Strict);
        countryReferenceService.Setup(service => service.GetCountryCodesForRegion(WorldRegionFilter.Europe))
            .Returns(new[] { "FR", "BE" });
        SearchQueryHandler handler = new SearchQueryHandler(repository, new PagedQueryValidator(), countryReferenceService.Object);

        await handler.HandleAsync(new SearchQuery("bellewaerde", new[] { "parks" }, new PagedQuery(2, 12), "fr", WorldRegionFilter.Europe), CancellationToken.None);

        Assert.Equal("bellewaerde", repository.LastText);
        Assert.Equal(new[] { "parks" }, repository.LastCategories);
        Assert.Equal(2, repository.LastPage);
        Assert.Equal(12, repository.LastPageSize);
        Assert.Equal("fr", repository.LastLanguageCode);
        Assert.Equal(new[] { "FR", "BE" }, repository.LastRegionCountryCodes);
    }

    private sealed class FakeSearchReadRepository : ISearchReadRepository
    {
        public string? LastText { get; private set; }

        public IReadOnlyCollection<string> LastCategories { get; private set; } = Array.Empty<string>();

        public int LastPage { get; private set; }

        public IReadOnlyCollection<string> LastRegionCountryCodes { get; private set; } = Array.Empty<string>();

        public int LastPageSize { get; private set; }

        public string? LastLanguageCode { get; private set; }

        public Task<SearchResultPage<SearchHitResult>> SearchAsync(
            string text,
            IReadOnlyCollection<string> categories,
            IReadOnlyCollection<string> regionCountryCodes,
            int page,
            int pageSize,
            string languageCode,
            CancellationToken cancellationToken)
        {
            this.LastText = text;
            this.LastCategories = categories;
            this.LastRegionCountryCodes = regionCountryCodes;
            this.LastPage = page;
            this.LastPageSize = pageSize;
            this.LastLanguageCode = languageCode;

            SearchResultPage<SearchHitResult> result = new SearchResultPage<SearchHitResult>(Array.Empty<SearchHitResult>(), page, pageSize, 0);
            return Task.FromResult(result);
        }
    }
}
