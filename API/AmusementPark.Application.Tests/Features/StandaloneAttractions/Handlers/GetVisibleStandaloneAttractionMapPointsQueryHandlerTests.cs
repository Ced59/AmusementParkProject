using AmusementPark.Application.Features.Countries;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Handlers;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Errors;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.StandaloneAttractions.Handlers;

public sealed class GetVisibleStandaloneAttractionMapPointsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldResolveCountrySearchAndRegionBeforeLoadingMapPoints()
    {
        Mock<IStandaloneAttractionRepository> repository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetVisibleMapPointsAsync(
                It.Is<StandaloneAttractionSearchCriteria>(criteria =>
                    criteria.SearchTerm == "Autriche"
                    && criteria.MatchingCountryCodes.SequenceEqual(new[] { "AT" })
                    && criteria.RegionCountryCodes.Contains("AT")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new StandaloneAttraction { Id = "standalone-1", Name = "Pendolino" } });
        Mock<ICountryReferenceService> countries = new Mock<ICountryReferenceService>(MockBehavior.Strict);
        countries.Setup(service => service.FindCountryCodesByLocalizedSearchAsync("Autriche", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "AT" });
        countries.Setup(service => service.GetCountryCodesForRegion(WorldRegionFilter.Europe))
            .Returns(new[] { "AT", "FR" });
        GetVisibleStandaloneAttractionMapPointsQueryHandler handler = new GetVisibleStandaloneAttractionMapPointsQueryHandler(
            repository.Object,
            countries.Object);

        ApplicationResult<IReadOnlyCollection<StandaloneAttraction>> result = await handler.HandleAsync(
            new GetVisibleStandaloneAttractionMapPointsQuery("Autriche", WorldRegionFilter.Europe),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("standalone-1", Assert.Single(result.Value!).Id);
        repository.VerifyAll();
        countries.VerifyAll();
    }
}
