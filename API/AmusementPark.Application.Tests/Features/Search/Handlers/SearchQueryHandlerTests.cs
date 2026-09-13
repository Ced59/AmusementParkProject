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
        countryReferenceService.Setup(service => service.FindCountryCodesByLocalizedSearchAsync("bellewaerde", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "AT" });
        countryReferenceService.Setup(service => service.GetCountryCodesForRegion(WorldRegionFilter.Europe))
            .Returns(new[] { "FR", "BE" });
        SearchQueryHandler handler = new SearchQueryHandler(repository, new PagedQueryValidator(), countryReferenceService.Object);

        await handler.HandleAsync(new SearchQuery("bellewaerde", new[] { "parks" }, new PagedQuery(2, 12), "fr", WorldRegionFilter.Europe), CancellationToken.None);

        Assert.Equal("bellewaerde", repository.LastText);
        Assert.Equal(new[] { "parks" }, repository.LastCategories);
        Assert.Equal(2, repository.LastPage);
        Assert.Equal(12, repository.LastPageSize);
        Assert.Equal("fr", repository.LastLanguageCode);
        Assert.Equal(new[] { "AT" }, repository.LastMatchingCountryCodes);
        Assert.Equal(new[] { "FR", "BE" }, repository.LastRegionCountryCodes);
    }


}
