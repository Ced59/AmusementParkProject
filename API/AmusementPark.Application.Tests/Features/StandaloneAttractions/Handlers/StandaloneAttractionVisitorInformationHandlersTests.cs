using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Handlers;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.StandaloneAttractions.Handlers;

public sealed class StandaloneAttractionVisitorInformationHandlersTests
{
    [Fact]
    public async Task GetOpeningHoursSchedule_WhenVisibleAttractionHasSchedule_ShouldReturnIt()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = "standalone-1",
            TimeZoneId = "Europe/Paris",
        };
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction);
        Mock<IStandaloneAttractionOpeningHoursRepository> openingHoursRepository =
            new Mock<IStandaloneAttractionOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(repository => repository.GetByStandaloneAttractionIdAsync(
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);
        GetStandaloneAttractionOpeningHoursScheduleQueryHandler handler = new GetStandaloneAttractionOpeningHoursScheduleQueryHandler(
            attractionRepository.Object,
            openingHoursRepository.Object);

        ApplicationResult<AmusementPark.Application.Features.ParkOpeningHours.Results.ParkOpeningHoursScheduleResult> result =
            await handler.HandleAsync(
                new GetStandaloneAttractionOpeningHoursScheduleQuery("standalone-1", IncludeHidden: false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("standalone-1", result.Value!.ParkId);
        Assert.Equal("Europe/Paris", result.Value.TimeZoneId);
        attractionRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
    }

    [Fact]
    public async Task GetOpeningHoursSchedule_WhenPublicAttractionIsNotOperating_ShouldNotExposeStoredSchedule()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        attraction.AttractionDetails!.Status = ParkItemStatusNormalizer.TemporarilyClosed;
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction);
        Mock<IStandaloneAttractionOpeningHoursRepository> openingHoursRepository =
            new Mock<IStandaloneAttractionOpeningHoursRepository>(MockBehavior.Strict);
        GetStandaloneAttractionOpeningHoursScheduleQueryHandler handler = new GetStandaloneAttractionOpeningHoursScheduleQueryHandler(
            attractionRepository.Object,
            openingHoursRepository.Object);

        ApplicationResult<AmusementPark.Application.Features.ParkOpeningHours.Results.ParkOpeningHoursScheduleResult> result =
            await handler.HandleAsync(
                new GetStandaloneAttractionOpeningHoursScheduleQuery("standalone-1", IncludeHidden: false),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        attractionRepository.VerifyAll();
        openingHoursRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPricing_WhenAdminRequestsExistingPricing_ShouldReturnIt()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "standalone-1",
            CurrencyCode = "EUR",
        };
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction, includeHidden: true);
        Mock<IStandaloneAttractionPricingRepository> pricingRepository =
            new Mock<IStandaloneAttractionPricingRepository>(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByStandaloneAttractionIdAsync(
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);
        GetStandaloneAttractionPricingQueryHandler handler = new GetStandaloneAttractionPricingQueryHandler(
            attractionRepository.Object,
            pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetStandaloneAttractionPricingQuery("standalone-1", IncludeHidden: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value!.CurrencyCode);
        attractionRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task GetPricing_WhenPublicAttractionIsNotOperating_ShouldNotExposeStoredPricing()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        attraction.AttractionDetails!.Status = ParkItemStatusNormalizer.Planned;
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction);
        Mock<IStandaloneAttractionPricingRepository> pricingRepository =
            new Mock<IStandaloneAttractionPricingRepository>(MockBehavior.Strict);
        GetStandaloneAttractionPricingQueryHandler handler = new GetStandaloneAttractionPricingQueryHandler(
            attractionRepository.Object,
            pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetStandaloneAttractionPricingQuery("standalone-1", IncludeHidden: false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        attractionRepository.VerifyAll();
        pricingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWeather_WhenBatchHasNotStoredForecastYet_ShouldReturnEmptyForecast()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        attraction.SetPosition(45.09, 6.07);
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction);
        Mock<IStandaloneAttractionWeatherRepository> weatherRepository =
            new Mock<IStandaloneAttractionWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.GetLatestForecastSnapshotAsync(
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AmusementPark.Core.Domain.Weather.ParkWeatherDailySnapshot?)null);
        weatherRepository
            .Setup(repository => repository.GetForecastAsync(
                "standalone-1",
                It.IsAny<DateOnly>(),
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AmusementPark.Core.Domain.Weather.ParkWeatherDailySnapshot>());
        GetStandaloneAttractionWeatherForecastQueryHandler handler = new GetStandaloneAttractionWeatherForecastQueryHandler(
            attractionRepository.Object,
            weatherRepository.Object,
            new ParkWeatherLocalDateResolver());

        ApplicationResult<AmusementPark.Application.Features.ParkWeather.Results.ParkWeatherForecastResult> result =
            await handler.HandleAsync(
                new GetStandaloneAttractionWeatherForecastQuery("standalone-1", 7),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("standalone-1", result.Value!.ParkId);
        Assert.Empty(result.Value.Days);
        attractionRepository.VerifyAll();
        weatherRepository.VerifyAll();
    }

    [Fact]
    public async Task GetWeather_WhenAttractionIsNotOperating_ShouldNotExposeStoredForecast()
    {
        StandaloneAttraction attraction = CreateOperatingAttraction();
        attraction.AttractionDetails!.Status = ParkItemStatusNormalizer.UnderConstruction;
        Mock<IStandaloneAttractionRepository> attractionRepository = CreateAttractionRepository(attraction);
        Mock<IStandaloneAttractionWeatherRepository> weatherRepository =
            new Mock<IStandaloneAttractionWeatherRepository>(MockBehavior.Strict);
        GetStandaloneAttractionWeatherForecastQueryHandler handler = new GetStandaloneAttractionWeatherForecastQueryHandler(
            attractionRepository.Object,
            weatherRepository.Object,
            new ParkWeatherLocalDateResolver());

        ApplicationResult<AmusementPark.Application.Features.ParkWeather.Results.ParkWeatherForecastResult> result =
            await handler.HandleAsync(
                new GetStandaloneAttractionWeatherForecastQuery("standalone-1", 7),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        attractionRepository.VerifyAll();
        weatherRepository.VerifyNoOtherCalls();
    }

    private static Mock<IStandaloneAttractionRepository> CreateAttractionRepository(
        StandaloneAttraction attraction,
        bool includeHidden = false)
    {
        Mock<IStandaloneAttractionRepository> repository =
            new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync(
                "standalone-1",
                includeHidden,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        return repository;
    }

    private static StandaloneAttraction CreateOperatingAttraction()
    {
        return new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Alpine Coaster",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails { Status = ParkItemStatusNormalizer.Operating },
        };
    }
}
