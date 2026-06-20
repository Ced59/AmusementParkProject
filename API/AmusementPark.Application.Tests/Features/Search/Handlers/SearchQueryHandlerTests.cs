using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Search.Handlers;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Search.Queries;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Application.Validation;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Search.Handlers;

public sealed class SearchQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPassLanguageCodeToRepository()
    {
        FakeSearchReadRepository repository = new FakeSearchReadRepository();
        SearchQueryHandler handler = new SearchQueryHandler(repository, new PagedQueryValidator());

        await handler.HandleAsync(new SearchQuery("bellewaerde", new[] { "parks" }, new PagedQuery(2, 12), "fr"), CancellationToken.None);

        Assert.Equal("bellewaerde", repository.LastText);
        Assert.Equal(new[] { "parks" }, repository.LastCategories);
        Assert.Equal(2, repository.LastPage);
        Assert.Equal(12, repository.LastPageSize);
        Assert.Equal("fr", repository.LastLanguageCode);
    }

    private sealed class FakeSearchReadRepository : ISearchReadRepository
    {
        public string? LastText { get; private set; }

        public IReadOnlyCollection<string> LastCategories { get; private set; } = Array.Empty<string>();

        public int LastPage { get; private set; }

        public int LastPageSize { get; private set; }

        public string? LastLanguageCode { get; private set; }

        public Task<SearchResultPage<SearchHitResult>> SearchAsync(
            string text,
            IReadOnlyCollection<string> categories,
            int page,
            int pageSize,
            string languageCode,
            CancellationToken cancellationToken)
        {
            this.LastText = text;
            this.LastCategories = categories;
            this.LastPage = page;
            this.LastPageSize = pageSize;
            this.LastLanguageCode = languageCode;

            SearchResultPage<SearchHitResult> result = new SearchResultPage<SearchHitResult>(Array.Empty<SearchHitResult>(), page, pageSize, 0);
            return Task.FromResult(result);
        }
    }
}
