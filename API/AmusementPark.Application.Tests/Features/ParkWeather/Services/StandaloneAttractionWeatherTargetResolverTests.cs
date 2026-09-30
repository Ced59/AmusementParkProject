using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Services;

public sealed class StandaloneAttractionWeatherTargetResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenSingleTargetIsEligibleStandaloneAttraction_ShouldReturnIt()
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Alpine Coaster",
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
        attraction.SetPosition(45.09, 6.07);
        ParkWeatherRun run = new ParkWeatherRun
        {
            Scope = ParkWeatherRefreshScope.SinglePark,
            TargetParkId = "standalone-1",
        };
        Mock<IStandaloneAttractionRepository> attractionRepository =
            new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        attractionRepository
            .Setup(repository => repository.GetByIdAsync(
                "standalone-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);

        IReadOnlyCollection<StandaloneAttraction> result =
            await StandaloneAttractionWeatherTargetResolver.ResolveAsync(
                run,
                attractionRepository.Object,
                Mock.Of<IStandaloneAttractionWeatherRepository>(MockBehavior.Strict),
                Mock.Of<IParkWeatherRunRepository>(MockBehavior.Strict),
                CancellationToken.None);

        Assert.Same(attraction, Assert.Single(result));
        attractionRepository.VerifyAll();
    }
}
